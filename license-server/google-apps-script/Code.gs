const LICENSE_SHEET_NAME = 'Licenses';
const ACTIVATION_SHEET_NAME = 'Activations';
const LICENSE_PRODUCT = 'LSTools';
const LEASE_HOURS = 72;
const DEFAULT_MAX_DEVICES = 1;
const MAX_DEVICE_LIMIT = 100;
const STORAGE_SCHEMA_VERSION = '2';

const LICENSE_HEADERS = [
  'LicenseId',
  'LicenseKeyHash',
  'Customer',
  'Product',
  'DeviceHash',
  'ExpiresUtc',
  'Status',
  'Features',
  'ActivatedUtc',
  'LastCheckUtc',
  'CreatedUtc',
  'Notes',
  'MaxDevices'
];

const ACTIVATION_HEADERS = [
  'LicenseId',
  'DeviceHash',
  'Status',
  'ActivatedUtc',
  'LastCheckUtc',
  'RevokedUtc',
  'Notes'
];

function doGet() {
  try {
    ensureLicenseStorageReady_();
    const privateKey = PropertiesService.getScriptProperties()
      .getProperty('LSTOOLS_PRIVATE_KEY');
    if (!privateKey) {
      throw new Error('Missing Script Property: LSTOOLS_PRIVATE_KEY');
    }

    return jsonResponse_(
      true,
      'READY',
      'LSTools license server is ready.'
    );
  } catch (error) {
    console.error(error && error.stack ? error.stack : error);
    return jsonResponse_(
      false,
      'CONFIG_ERROR',
      'License server chưa được cấu hình đầy đủ.'
    );
  }
}

function doPost(e) {
  try {
    if (!e || !e.postData || !e.postData.contents) {
      return jsonResponse_(false, 'BAD_REQUEST', 'Yêu cầu không có nội dung.');
    }

    const request = JSON.parse(e.postData.contents);
    const action = String(request.action || '').trim().toLowerCase();
    const credential = String(
      request.credential || request.licenseKey || ''
    ).trim();
    const deviceHash = normalizeDeviceHash_(request.deviceHash);
    const product = String(request.product || '').trim();

    if (action !== 'activate' && action !== 'validate') {
      return jsonResponse_(false, 'BAD_ACTION', 'Thao tác không hợp lệ.');
    }

    if (credential.length < 20) {
      return jsonResponse_(
        false,
        'BAD_CREDENTIAL',
        'Thông tin xác thực không đúng định dạng.'
      );
    }

    if (!/^[A-F0-9]{16,64}$/.test(deviceHash)) {
      return jsonResponse_(false, 'BAD_DEVICE', 'Mã thiết bị không hợp lệ.');
    }

    if (product !== LICENSE_PRODUCT) {
      return jsonResponse_(false, 'BAD_PRODUCT', 'License không dành cho sản phẩm này.');
    }

    const lock = LockService.getScriptLock();
    lock.waitLock(10000);
    try {
      return processLicenseRequest_(action, credential, deviceHash);
    } finally {
      lock.releaseLock();
    }
  } catch (error) {
    console.error(error && error.stack ? error.stack : error);
    return jsonResponse_(
      false,
      'SERVER_ERROR',
      'License server đang lỗi. Vui lòng liên hệ nhà cung cấp.'
    );
  }
}

function processLicenseRequest_(action, credential, deviceHash) {
  ensureLicenseStorageReady_();
  const sheet = getLicenseSheet_();
  const headers = getHeaderMap_(sheet, LICENSE_HEADERS);
  const activationSheet = getActivationSheet_();
  const activationHeaders = getHeaderMap_(
    activationSheet,
    ACTIVATION_HEADERS
  );
  let row = 0;

  if (action === 'activate') {
    const activationToken = normalizeLicenseKey_(credential);
    const keyHash = sha256Hex_(activationToken);
    row = findLicenseRow_(sheet, headers.LicenseKeyHash, keyHash);
  } else {
    const clientCredential = parseClientCredential_(credential);
    if (!clientCredential ||
        clientCredential.deviceHash !== deviceHash) {
      return jsonResponse_(
        false,
        'BAD_CREDENTIAL',
        'Thông tin xác thực không hợp lệ.'
      );
    }

    row = findLicenseRowByValue_(
      sheet,
      headers.LicenseId,
      clientCredential.licenseId
    );
  }

  if (row === 0) {
    return jsonResponse_(false, 'NOT_FOUND', 'Không tìm thấy quyền sử dụng.');
  }

  const record = readLicenseRecord_(sheet, headers, row);
  const now = new Date();

  if (record.product !== LICENSE_PRODUCT) {
    return jsonResponse_(false, 'BAD_PRODUCT', 'License không dành cho LSTools.');
  }

  if (record.status !== 'ACTIVE') {
    const code = record.status === 'REVOKED' ? 'REVOKED' : 'INACTIVE';
    return jsonResponse_(false, code, 'License đã bị khóa.');
  }

  if (!record.expiresUtc || record.expiresUtc.getTime() <= now.getTime()) {
    return jsonResponse_(false, 'EXPIRED', 'License đã hết hạn.');
  }

  let activationRow = findActivationRow_(
    activationSheet,
    activationHeaders,
    record.licenseId,
    deviceHash
  );
  const activation = activationRow > 0
    ? readActivationRecord_(
        activationSheet,
        activationHeaders,
        activationRow
      )
    : null;
  const decision = getActivationDecision_(
    action,
    activation ? activation.status : '',
    countActiveActivations_(
      activationSheet,
      activationHeaders,
      record.licenseId
    ),
    record.maxDevices
  );

  if (!decision.allowed) {
    return jsonResponse_(
      false,
      decision.code,
      decision.message
    );
  }

  if (decision.createActivation) {
    activationRow = appendRowByHeaders_(
      activationSheet,
      activationHeaders,
      {
        LicenseId: record.licenseId,
        DeviceHash: deviceHash,
        Status: 'Active',
        ActivatedUtc: now,
        LastCheckUtc: now,
        RevokedUtc: '',
        Notes: ''
      }
    );

    if (!record.deviceHash) {
      sheet.getRange(row, headers.DeviceHash).setValue(deviceHash);
      sheet.getRange(row, headers.ActivatedUtc).setValue(now);
      record.deviceHash = deviceHash;
    }
  }

  activationSheet
    .getRange(activationRow, activationHeaders.LastCheckUtc)
    .setValue(now);
  sheet.getRange(row, headers.LastCheckUtc).setValue(now);
  SpreadsheetApp.flush();

  const lease = createSignedLease_(record, deviceHash, now);
  const clientCredential = action === 'activate'
    ? createClientCredential_(record.licenseId, deviceHash)
    : '';
  return jsonResponse_(
    true,
    'OK',
    action === 'activate'
      ? 'Kích hoạt license thành công.'
      : 'Kiểm tra license thành công.',
    lease,
    clientCredential
  );
}

function getActivationDecision_(
  action,
  activationStatus,
  activeCount,
  maxDevices
) {
  const status = String(activationStatus || '').trim().toUpperCase();
  if (status === 'ACTIVE') {
    return {
      allowed: true,
      createActivation: false,
      code: 'OK',
      message: ''
    };
  }

  if (status) {
    return {
      allowed: false,
      createActivation: false,
      code: 'DEVICE_REVOKED',
      message: 'Thiết bị này đã bị thu hồi quyền sử dụng.'
    };
  }

  if (action !== 'activate') {
    return {
      allowed: false,
      createActivation: false,
      code: 'NOT_ACTIVATED',
      message: 'Thiết bị chưa được kích hoạt.'
    };
  }

  if (Number(activeCount) >= normalizeMaxDevices_(maxDevices)) {
    return {
      allowed: false,
      createActivation: false,
      code: 'DEVICE_LIMIT_REACHED',
      message: 'License đã đạt giới hạn số thiết bị.'
    };
  }

  return {
    allowed: true,
    createActivation: true,
    code: 'OK',
    message: ''
  };
}

function createClientCredential_(licenseId, deviceHash) {
  const payloadJson = JSON.stringify({
    version: 1,
    licenseId: String(licenseId || '').trim(),
    deviceHash: normalizeDeviceHash_(deviceHash)
  });
  const payload = base64UrlString_(payloadJson);
  const signature = base64UrlBytes_(
    Utilities.computeHmacSha256Signature(
      payload,
      getCredentialSecret_(),
      Utilities.Charset.UTF_8
    )
  );

  return 'LSTC.' + payload + '.' + signature;
}

function parseClientCredential_(value) {
  try {
    const parts = String(value || '').trim().split('.');
    if (parts.length !== 3 || parts[0] !== 'LSTC') {
      return null;
    }

    const expectedSignature = base64UrlBytes_(
      Utilities.computeHmacSha256Signature(
        parts[1],
        getCredentialSecret_(),
        Utilities.Charset.UTF_8
      )
    );
    if (!constantTimeEquals_(parts[2], expectedSignature)) {
      return null;
    }

    const payload = JSON.parse(base64UrlDecodeString_(parts[1]));
    const licenseId = String(payload.licenseId || '').trim();
    const deviceHash = normalizeDeviceHash_(payload.deviceHash);
    if (Number(payload.version) !== 1 ||
        !licenseId ||
        !/^[A-F0-9]{16,64}$/.test(deviceHash)) {
      return null;
    }

    return {
      licenseId: licenseId,
      deviceHash: deviceHash
    };
  } catch (error) {
    return null;
  }
}

function getCredentialSecret_() {
  const secret = normalizePrivateKey_(
    PropertiesService.getScriptProperties()
      .getProperty('LSTOOLS_PRIVATE_KEY')
  );
  if (!secret) {
    throw new Error('Missing Script Property: LSTOOLS_PRIVATE_KEY');
  }

  return secret;
}

function constantTimeEquals_(left, right) {
  const a = String(left || '');
  const b = String(right || '');
  let difference = a.length ^ b.length;
  const length = Math.max(a.length, b.length);
  for (let index = 0; index < length; index += 1) {
    difference |=
      (a.charCodeAt(index % Math.max(a.length, 1)) || 0) ^
      (b.charCodeAt(index % Math.max(b.length, 1)) || 0);
  }

  return difference === 0;
}

function createSignedLease_(record, deviceHash, issuedUtc) {
  const privateKey = normalizePrivateKey_(
    PropertiesService.getScriptProperties()
      .getProperty('LSTOOLS_PRIVATE_KEY')
  );

  if (!privateKey) {
    throw new Error('Missing Script Property: LSTOOLS_PRIVATE_KEY');
  }

  const leaseExpiresUtc = new Date(
    Math.min(
      record.expiresUtc.getTime(),
      issuedUtc.getTime() + LEASE_HOURS * 60 * 60 * 1000
    )
  );

  const payload = {
    schemaVersion: 1,
    licenseId: record.licenseId,
    customer: record.customer,
    product: LICENSE_PRODUCT,
    deviceHash: deviceHash,
    issuedUtc: issuedUtc.toISOString(),
    expiresUtc: record.expiresUtc.toISOString(),
    leaseExpiresUtc: leaseExpiresUtc.toISOString(),
    features: record.features,
    status: 'Active',
    nonce: Utilities.getUuid().replace(/-/g, '')
  };

  const payloadJson = JSON.stringify(payload);
  const signatureBytes = Utilities.computeRsaSha256Signature(
    payloadJson,
    privateKey,
    Utilities.Charset.UTF_8
  );

  return {
    payload: base64UrlString_(payloadJson),
    signature: base64UrlBytes_(signatureBytes)
  };
}

function normalizePrivateKey_(value) {
  const privateKey = String(value || '')
    .trim()
    .replace(/\\r\\n|\\n|\\r/g, '\n');

  return privateKey
    .replace(
      /-----BEGIN PRIVATE KEY-----\s*/,
      '-----BEGIN PRIVATE KEY-----\n'
    )
    .replace(
      /\s*-----END PRIVATE KEY-----/,
      '\n-----END PRIVATE KEY-----'
    );
}

function setupLicenseSheet() {
  ensureLicenseStorage_();
  const licenseSheet = getLicenseSheet_();
  const activationSheet = getActivationSheet_();
  formatLicenseSheets_(
    licenseSheet,
    getHeaderMap_(licenseSheet, LICENSE_HEADERS),
    activationSheet,
    getHeaderMap_(activationSheet, ACTIVATION_HEADERS)
  );
  return 'READY';
}

function ensureLicenseStorageReady_() {
  const properties = PropertiesService.getScriptProperties();
  if (properties.getProperty('LSTOOLS_STORAGE_VERSION') !==
      STORAGE_SCHEMA_VERSION) {
    ensureLicenseStorage_();
  }
}

function ensureLicenseStorage_() {
  const spreadsheet = getSpreadsheet_();
  spreadsheet.setSpreadsheetTimeZone('UTC');
  const licenseSheet = ensureSheet_(
    spreadsheet,
    LICENSE_SHEET_NAME,
    LICENSE_HEADERS
  );
  const activationSheet = ensureSheet_(
    spreadsheet,
    ACTIVATION_SHEET_NAME,
    ACTIVATION_HEADERS
  );
  const licenseHeaders = getHeaderMap_(licenseSheet, LICENSE_HEADERS);
  const activationHeaders = getHeaderMap_(
    activationSheet,
    ACTIVATION_HEADERS
  );

  initializeMaxDevices_(licenseSheet, licenseHeaders);
  migrateLegacyActivations_(
    licenseSheet,
    licenseHeaders,
    activationSheet,
    activationHeaders
  );
  PropertiesService.getScriptProperties().setProperty(
    'LSTOOLS_STORAGE_VERSION',
    STORAGE_SCHEMA_VERSION
  );
}

function ensureSheet_(spreadsheet, name, requiredHeaders) {
  let sheet = spreadsheet.getSheetByName(name);
  if (!sheet) {
    sheet = spreadsheet.insertSheet(name);
  }

  if (sheet.getLastRow() === 0) {
    sheet
      .getRange(1, 1, 1, requiredHeaders.length)
      .setValues([requiredHeaders]);
    return sheet;
  }

  const current = sheet
    .getRange(1, 1, 1, Math.max(sheet.getLastColumn(), 1))
    .getDisplayValues()[0]
    .map(function (value) {
      return String(value || '').trim();
    });
  requiredHeaders.forEach(function (header) {
    if (current.indexOf(header) < 0) {
      current.push(header);
      sheet.getRange(1, current.length).setValue(header);
    }
  });
  return sheet;
}

function initializeMaxDevices_(sheet, headers) {
  const lastRow = sheet.getLastRow();
  if (lastRow < 2) {
    return;
  }

  const range = sheet.getRange(
    2,
    headers.MaxDevices,
    lastRow - 1,
    1
  );
  const values = range.getValues();
  let changed = false;
  values.forEach(function (row) {
    const normalized = normalizeMaxDevices_(row[0]);
    if (Number(row[0]) !== normalized) {
      row[0] = normalized;
      changed = true;
    }
  });
  if (changed) {
    range.setValues(values);
  }
}

function migrateLegacyActivations_(
  licenseSheet,
  licenseHeaders,
  activationSheet,
  activationHeaders
) {
  const licenseLastRow = licenseSheet.getLastRow();
  if (licenseLastRow < 2) {
    return;
  }

  const existingKeys = {};
  const activationLastRow = activationSheet.getLastRow();
  if (activationLastRow >= 2) {
    const activationValues = activationSheet
      .getRange(
        2,
        1,
        activationLastRow - 1,
        activationSheet.getLastColumn()
      )
      .getValues();
    activationValues.forEach(function (row) {
      const licenseId = String(
        row[activationHeaders.LicenseId - 1] || ''
      ).trim();
      const deviceHash = normalizeDeviceHash_(
        row[activationHeaders.DeviceHash - 1]
      );
      if (licenseId && deviceHash) {
        existingKeys[activationKey_(licenseId, deviceHash)] = true;
      }
    });
  }

  const licenseValues = licenseSheet
    .getRange(
      2,
      1,
      licenseLastRow - 1,
      licenseSheet.getLastColumn()
    )
    .getValues();
  const rowsToAppend = [];
  licenseValues.forEach(function (row) {
    const licenseId = String(
      row[licenseHeaders.LicenseId - 1] || ''
    ).trim();
    const deviceHash = normalizeDeviceHash_(
      row[licenseHeaders.DeviceHash - 1]
    );
    const key = activationKey_(licenseId, deviceHash);
    if (!licenseId || !deviceHash || existingKeys[key]) {
      return;
    }

    const activatedUtc =
      parseDate_(row[licenseHeaders.ActivatedUtc - 1]) ||
      parseDate_(row[licenseHeaders.CreatedUtc - 1]) ||
      new Date();
    const lastCheckUtc =
      parseDate_(row[licenseHeaders.LastCheckUtc - 1]) ||
      activatedUtc;
    rowsToAppend.push([
      licenseId,
      deviceHash,
      'Active',
      activatedUtc,
      lastCheckUtc,
      '',
      'Migrated from Licenses.DeviceHash'
    ]);
    existingKeys[key] = true;
  });

  if (rowsToAppend.length > 0) {
    activationSheet
      .getRange(
        activationSheet.getLastRow() + 1,
        1,
        rowsToAppend.length,
        ACTIVATION_HEADERS.length
      )
      .setValues(rowsToAppend);
  }
}

function formatLicenseSheets_(
  licenseSheet,
  licenseHeaders,
  activationSheet,
  activationHeaders
) {
  licenseSheet.setFrozenRows(1);
  activationSheet.setFrozenRows(1);
  [
    licenseHeaders.ExpiresUtc,
    licenseHeaders.ActivatedUtc,
    licenseHeaders.LastCheckUtc,
    licenseHeaders.CreatedUtc
  ].forEach(function (column) {
    licenseSheet.getRange(2, column, licenseSheet.getMaxRows() - 1, 1)
      .setNumberFormat('yyyy-mm-dd hh:mm:ss');
  });
  [
    activationHeaders.ActivatedUtc,
    activationHeaders.LastCheckUtc,
    activationHeaders.RevokedUtc
  ].forEach(function (column) {
    activationSheet
      .getRange(2, column, activationSheet.getMaxRows() - 1, 1)
      .setNumberFormat('yyyy-mm-dd hh:mm:ss');
  });
  licenseSheet.autoResizeColumns(1, licenseSheet.getLastColumn());
  activationSheet.autoResizeColumns(1, activationSheet.getLastColumn());
}

function createTrialLicense() {
  const ui = SpreadsheetApp.getUi();
  const customerPrompt = ui.prompt(
    'Tạo license',
    'Tên khách hàng:',
    ui.ButtonSet.OK_CANCEL
  );
  if (customerPrompt.getSelectedButton() !== ui.Button.OK) {
    return;
  }

  const customer = customerPrompt.getResponseText().trim();
  if (!customer) {
    ui.alert('Tên khách hàng không được để trống.');
    return;
  }

  const daysPrompt = ui.prompt(
    'Thời hạn',
    'Số ngày sử dụng (1-3650):',
    ui.ButtonSet.OK_CANCEL
  );
  if (daysPrompt.getSelectedButton() !== ui.Button.OK) {
    return;
  }

  const days = Number(daysPrompt.getResponseText());
  if (!Number.isInteger(days) || days < 1 || days > 3650) {
    ui.alert('Số ngày phải là số nguyên từ 1 đến 3650.');
    return;
  }

  const maxDevicesPrompt = ui.prompt(
    'Số máy',
    'Số máy tối đa cho license này (1-' + MAX_DEVICE_LIMIT + '):',
    ui.ButtonSet.OK_CANCEL
  );
  if (maxDevicesPrompt.getSelectedButton() !== ui.Button.OK) {
    return;
  }

  const maxDevices = Number(maxDevicesPrompt.getResponseText());
  if (!Number.isInteger(maxDevices) ||
      maxDevices < 1 ||
      maxDevices > MAX_DEVICE_LIMIT) {
    ui.alert(
      'Số máy phải là số nguyên từ 1 đến ' + MAX_DEVICE_LIMIT + '.'
    );
    return;
  }

  const featuresPrompt = ui.prompt(
    'Tính năng',
    'Danh sách tính năng, cách nhau bằng dấu phẩy. Nhập * để mở toàn bộ:',
    ui.ButtonSet.OK_CANCEL
  );
  if (featuresPrompt.getSelectedButton() !== ui.Button.OK) {
    return;
  }

  const features = featuresPrompt.getResponseText().trim() || '*';
  const notesPrompt = ui.prompt(
    'Ghi chú',
    'Ghi chú nội bộ (có thể để trống):',
    ui.ButtonSet.OK_CANCEL
  );
  if (notesPrompt.getSelectedButton() !== ui.Button.OK) {
    return;
  }

  ensureLicenseStorage_();
  const sheet = getLicenseSheet_();
  const headers = getHeaderMap_(sheet, LICENSE_HEADERS);
  let licenseKey;
  let keyHash;
  do {
    licenseKey = generateLicenseKey_();
    keyHash = sha256Hex_(licenseKey);
  } while (findLicenseRow_(sheet, headers.LicenseKeyHash, keyHash) !== 0);

  const now = new Date();
  const expiresUtc = new Date(now.getTime() + days * 24 * 60 * 60 * 1000);
  const licenseId =
    'LST-' +
    Utilities.formatDate(now, 'UTC', 'yyyyMMdd') +
    '-' +
    Utilities.getUuid().replace(/-/g, '').slice(0, 8).toUpperCase();

  appendRowByHeaders_(
    sheet,
    headers,
    {
      LicenseId: licenseId,
      LicenseKeyHash: keyHash,
      Customer: customer,
      Product: LICENSE_PRODUCT,
      DeviceHash: '',
      ExpiresUtc: expiresUtc,
      Status: 'Active',
      Features: features,
      ActivatedUtc: '',
      LastCheckUtc: '',
      CreatedUtc: now,
      Notes: notesPrompt.getResponseText().trim(),
      MaxDevices: maxDevices
    }
  );

  ui.alert(
    'Mã đóng gói đã tạo',
    'Không gửi mã này cho khách. Hãy ghi nguyên chuỗi dưới đây vào ' +
      'LSTool\\Resources\\Settings\\ReleaseProfile.dat rồi build bộ cài riêng:\n\n' +
      Utilities.base64Encode(
        licenseKey,
        Utilities.Charset.UTF_8
      ) +
      '\n\nHết hạn UTC: ' +
      expiresUtc.toISOString() +
      '\nSố máy tối đa: ' +
      maxDevices,
    ui.ButtonSet.OK
  );
}

function resetSelectedDevice() {
  const context = getSelectedLicenseRow_();
  const ui = SpreadsheetApp.getUi();
  const confirmation = ui.alert(
    'Thu hồi toàn bộ máy',
    'Tất cả máy đang hoạt động của license này sẽ bị thu hồi. Tiếp tục?',
    ui.ButtonSet.YES_NO
  );
  if (confirmation !== ui.Button.YES) {
    return;
  }

  const record = readLicenseRecord_(
    context.sheet,
    context.headers,
    context.row
  );
  const activationSheet = getActivationSheet_();
  const activationHeaders = getHeaderMap_(
    activationSheet,
    ACTIVATION_HEADERS
  );
  const now = new Date();
  const lastRow = activationSheet.getLastRow();
  let revokedCount = 0;
  if (lastRow >= 2) {
    const values = activationSheet
      .getRange(
        2,
        1,
        lastRow - 1,
        activationSheet.getLastColumn()
      )
      .getValues();
    values.forEach(function (row, index) {
      const licenseId = String(
        row[activationHeaders.LicenseId - 1] || ''
      ).trim();
      const status = String(
        row[activationHeaders.Status - 1] || ''
      ).trim().toUpperCase();
      if (licenseId === record.licenseId && status === 'ACTIVE') {
        const sheetRow = index + 2;
        activationSheet
          .getRange(sheetRow, activationHeaders.Status)
          .setValue('Revoked');
        activationSheet
          .getRange(sheetRow, activationHeaders.RevokedUtc)
          .setValue(now);
        revokedCount += 1;
      }
    });
  }

  context.sheet.getRange(context.row, context.headers.DeviceHash).clearContent();
  context.sheet.getRange(context.row, context.headers.ActivatedUtc).clearContent();
  context.sheet.getRange(context.row, context.headers.LastCheckUtc).clearContent();
  ui.alert(
    'Đã thu hồi ' + revokedCount +
      ' máy. License có thể cấp chỗ cho máy mới.'
  );
}

function setSelectedMaxDevices() {
  const context = getSelectedLicenseRow_();
  const current = normalizeMaxDevices_(
    context.sheet
      .getRange(context.row, context.headers.MaxDevices)
      .getValue()
  );
  const ui = SpreadsheetApp.getUi();
  const prompt = ui.prompt(
    'Đặt số máy tối đa',
    'Nhập số máy từ 1 đến ' + MAX_DEVICE_LIMIT +
      '. Hiện tại: ' + current,
    ui.ButtonSet.OK_CANCEL
  );
  if (prompt.getSelectedButton() !== ui.Button.OK) {
    return;
  }

  const maxDevices = Number(prompt.getResponseText());
  if (!Number.isInteger(maxDevices) ||
      maxDevices < 1 ||
      maxDevices > MAX_DEVICE_LIMIT) {
    ui.alert(
      'Số máy phải là số nguyên từ 1 đến ' + MAX_DEVICE_LIMIT + '.'
    );
    return;
  }

  context.sheet
    .getRange(context.row, context.headers.MaxDevices)
    .setValue(maxDevices);
  ui.alert(
    'Đã đặt giới hạn ' + maxDevices +
      ' máy. Các máy đang hoạt động không bị tự động thu hồi.'
  );
}

function showSelectedLicenseActivations() {
  const context = getSelectedLicenseRow_();
  const record = readLicenseRecord_(
    context.sheet,
    context.headers,
    context.row
  );
  const activationSheet = getActivationSheet_();
  const activationHeaders = getHeaderMap_(
    activationSheet,
    ACTIVATION_HEADERS
  );
  const lastRow = activationSheet.getLastRow();
  let firstRow = 0;
  let count = 0;
  if (lastRow >= 2) {
    const values = activationSheet
      .getRange(2, activationHeaders.LicenseId, lastRow - 1, 1)
      .getDisplayValues();
    values.forEach(function (row, index) {
      if (String(row[0] || '').trim() === record.licenseId) {
        count += 1;
        if (firstRow === 0) {
          firstRow = index + 2;
        }
      }
    });
  }

  if (firstRow > 0) {
    getSpreadsheet_().setActiveSheet(activationSheet);
    activationSheet
      .getRange(firstRow, activationHeaders.LicenseId)
      .activate();
  }
  SpreadsheetApp.getUi().alert(
    count > 0
      ? 'Có ' + count +
        ' máy đã ghi nhận. Sheet Activations đã được mở tại dòng đầu tiên.'
      : 'License này chưa có máy nào được kích hoạt.'
  );
}

function revokeSelectedActivation() {
  const context = getSelectedActivationRow_();
  context.sheet
    .getRange(context.row, context.headers.Status)
    .setValue('Revoked');
  context.sheet
    .getRange(context.row, context.headers.RevokedUtc)
    .setValue(new Date());
  SpreadsheetApp.getUi().alert(
    'Đã thu hồi máy. Chỗ trống có thể được cấp cho một máy mới.'
  );
}

function reactivateSelectedActivation() {
  const context = getSelectedActivationRow_();
  const activation = readActivationRecord_(
    context.sheet,
    context.headers,
    context.row
  );
  const licenseSheet = getLicenseSheet_();
  const licenseHeaders = getHeaderMap_(licenseSheet, LICENSE_HEADERS);
  const licenseRow = findLicenseRowByValue_(
    licenseSheet,
    licenseHeaders.LicenseId,
    activation.licenseId
  );
  if (licenseRow === 0) {
    throw new Error('Không tìm thấy license của máy đang chọn.');
  }

  const record = readLicenseRecord_(
    licenseSheet,
    licenseHeaders,
    licenseRow
  );
  const activeCount = countActiveActivations_(
    context.sheet,
    context.headers,
    activation.licenseId
  );
  if (activation.status !== 'ACTIVE' &&
      activeCount >= record.maxDevices) {
    SpreadsheetApp.getUi().alert(
      'Không thể mở lại vì license đã đủ ' +
        record.maxDevices + ' máy.'
    );
    return;
  }

  context.sheet
    .getRange(context.row, context.headers.Status)
    .setValue('Active');
  context.sheet
    .getRange(context.row, context.headers.RevokedUtc)
    .clearContent();
  SpreadsheetApp.getUi().alert('Đã mở lại máy đang chọn.');
}

function revokeSelectedLicense() {
  const context = getSelectedLicenseRow_();
  context.sheet.getRange(context.row, context.headers.Status).setValue('Revoked');
  SpreadsheetApp.getUi().alert('Đã khóa license.');
}

function reactivateSelectedLicense() {
  const context = getSelectedLicenseRow_();
  context.sheet.getRange(context.row, context.headers.Status).setValue('Active');
  SpreadsheetApp.getUi().alert('Đã mở lại license.');
}

function onOpen() {
  SpreadsheetApp.getUi()
    .createMenu('LSTools License')
    .addItem('Chuẩn bị sheet', 'setupLicenseSheet')
    .addSeparator()
    .addItem('Tạo mã đóng gói mới', 'createTrialLicense')
    .addItem('Đặt số máy tối đa', 'setSelectedMaxDevices')
    .addItem('Xem máy của license đang chọn', 'showSelectedLicenseActivations')
    .addItem('Thu hồi toàn bộ máy của license', 'resetSelectedDevice')
    .addItem('Khóa dòng đang chọn', 'revokeSelectedLicense')
    .addItem('Mở lại dòng đang chọn', 'reactivateSelectedLicense')
    .addSeparator()
    .addItem('Thu hồi máy đang chọn', 'revokeSelectedActivation')
    .addItem('Mở lại máy đang chọn', 'reactivateSelectedActivation')
    .addToUi();
}

function getSpreadsheet_() {
  const sheetId = PropertiesService.getScriptProperties()
    .getProperty('LSTOOLS_SHEET_ID');
  if (!sheetId) {
    throw new Error('Missing Script Property: LSTOOLS_SHEET_ID');
  }

  return SpreadsheetApp.openById(sheetId);
}

function getLicenseSheet_() {
  const sheet = getSpreadsheet_().getSheetByName(LICENSE_SHEET_NAME);
  if (!sheet) {
    throw new Error('Chưa có sheet Licenses. Hãy chạy setupLicenseSheet().');
  }

  return sheet;
}

function getActivationSheet_() {
  const sheet = getSpreadsheet_().getSheetByName(ACTIVATION_SHEET_NAME);
  if (!sheet) {
    throw new Error(
      'Chưa có sheet Activations. Hãy chạy setupLicenseSheet().'
    );
  }

  return sheet;
}

function getHeaderMap_(sheet, requiredHeaders) {
  const row = sheet
    .getRange(1, 1, 1, sheet.getLastColumn())
    .getDisplayValues()[0];
  const result = {};
  row.forEach(function (header, index) {
    if (header) {
      result[header.trim()] = index + 1;
    }
  });

  requiredHeaders.forEach(function (header) {
    if (!result[header]) {
      throw new Error(
        'Sheet ' + sheet.getName() + ' thiếu cột ' + header + '.'
      );
    }
  });

  return result;
}

function appendRowByHeaders_(sheet, headers, valuesByHeader) {
  const values = new Array(sheet.getLastColumn()).fill('');
  Object.keys(valuesByHeader).forEach(function (header) {
    if (!headers[header]) {
      throw new Error(
        'Sheet ' + sheet.getName() + ' thiếu cột ' + header + '.'
      );
    }
    values[headers[header] - 1] = valuesByHeader[header];
  });
  sheet.appendRow(values);
  return sheet.getLastRow();
}

function findLicenseRow_(sheet, hashColumn, keyHash) {
  return findLicenseRowByValue_(sheet, hashColumn, keyHash);
}

function findLicenseRowByValue_(sheet, column, expectedValue) {
  const lastRow = sheet.getLastRow();
  if (lastRow < 2) {
    return 0;
  }

  const values = sheet
    .getRange(2, column, lastRow - 1, 1)
    .getDisplayValues();
  for (let index = 0; index < values.length; index += 1) {
    if (
      String(values[index][0]).trim().toUpperCase() ===
      String(expectedValue || '').trim().toUpperCase()
    ) {
      return index + 2;
    }
  }

  return 0;
}

function findActivationRow_(
  sheet,
  headers,
  licenseId,
  deviceHash
) {
  const lastRow = sheet.getLastRow();
  if (lastRow < 2) {
    return 0;
  }

  const values = sheet
    .getRange(2, 1, lastRow - 1, sheet.getLastColumn())
    .getDisplayValues();
  const expectedKey = activationKey_(licenseId, deviceHash);
  for (let index = 0; index < values.length; index += 1) {
    const rowKey = activationKey_(
      values[index][headers.LicenseId - 1],
      values[index][headers.DeviceHash - 1]
    );
    if (rowKey === expectedKey) {
      return index + 2;
    }
  }

  return 0;
}

function countActiveActivations_(sheet, headers, licenseId) {
  const lastRow = sheet.getLastRow();
  if (lastRow < 2) {
    return 0;
  }

  const values = sheet
    .getRange(2, 1, lastRow - 1, sheet.getLastColumn())
    .getDisplayValues();
  const expectedLicenseId = String(licenseId || '').trim();
  return values.filter(function (row) {
    return String(row[headers.LicenseId - 1] || '').trim() ===
        expectedLicenseId &&
      String(row[headers.Status - 1] || '').trim().toUpperCase() ===
        'ACTIVE';
  }).length;
}

function readLicenseRecord_(sheet, headers, row) {
  const values = sheet
    .getRange(row, 1, 1, sheet.getLastColumn())
    .getValues()[0];
  const at = function (header) {
    return values[headers[header] - 1];
  };

  return {
    licenseId: String(at('LicenseId') || '').trim(),
    customer: String(at('Customer') || '').trim(),
    product: String(at('Product') || '').trim(),
    deviceHash: normalizeDeviceHash_(at('DeviceHash')),
    expiresUtc: parseDate_(at('ExpiresUtc')),
    status: String(at('Status') || '').trim().toUpperCase(),
    features: parseFeatures_(at('Features')),
    maxDevices: normalizeMaxDevices_(at('MaxDevices'))
  };
}

function readActivationRecord_(sheet, headers, row) {
  const values = sheet
    .getRange(row, 1, 1, sheet.getLastColumn())
    .getValues()[0];
  const at = function (header) {
    return values[headers[header] - 1];
  };

  return {
    licenseId: String(at('LicenseId') || '').trim(),
    deviceHash: normalizeDeviceHash_(at('DeviceHash')),
    status: String(at('Status') || '').trim().toUpperCase(),
    activatedUtc: parseDate_(at('ActivatedUtc')),
    lastCheckUtc: parseDate_(at('LastCheckUtc')),
    revokedUtc: parseDate_(at('RevokedUtc'))
  };
}

function getSelectedLicenseRow_() {
  const sheet = SpreadsheetApp.getActiveSpreadsheet().getActiveSheet();
  if (sheet.getName() !== LICENSE_SHEET_NAME) {
    throw new Error('Hãy chọn một dòng trong sheet Licenses.');
  }

  const row = sheet.getActiveRange().getRow();
  if (row < 2) {
    throw new Error('Hãy chọn một dòng license, không chọn hàng tiêu đề.');
  }

  return {
    sheet: sheet,
    row: row,
    headers: getHeaderMap_(sheet, LICENSE_HEADERS)
  };
}

function getSelectedActivationRow_() {
  const sheet = SpreadsheetApp.getActiveSpreadsheet().getActiveSheet();
  if (sheet.getName() !== ACTIVATION_SHEET_NAME) {
    throw new Error('Hãy chọn một dòng trong sheet Activations.');
  }

  const row = sheet.getActiveRange().getRow();
  if (row < 2) {
    throw new Error('Hãy chọn một dòng máy, không chọn hàng tiêu đề.');
  }

  return {
    sheet: sheet,
    row: row,
    headers: getHeaderMap_(sheet, ACTIVATION_HEADERS)
  };
}

function parseDate_(value) {
  const parsed = value instanceof Date ? value : new Date(String(value || ''));
  return Number.isNaN(parsed.getTime()) ? null : parsed;
}

function parseFeatures_(value) {
  const features = String(value || '*')
    .split(',')
    .map(function (item) {
      return item.trim();
    })
    .filter(function (item) {
      return item.length > 0;
    });
  return features.length > 0 ? features : ['*'];
}

function normalizeLicenseKey_(value) {
  return String(value || '').trim().toUpperCase();
}

function normalizeDeviceHash_(value) {
  return String(value || '')
    .replace(/-/g, '')
    .replace(/\s/g, '')
    .trim()
    .toUpperCase();
}

function normalizeMaxDevices_(value) {
  const parsed = Number(value);
  if (!Number.isInteger(parsed) ||
      parsed < 1 ||
      parsed > MAX_DEVICE_LIMIT) {
    return DEFAULT_MAX_DEVICES;
  }

  return parsed;
}

function activationKey_(licenseId, deviceHash) {
  return String(licenseId || '').trim().toUpperCase() +
    '|' +
    normalizeDeviceHash_(deviceHash);
}

function sha256Hex_(value) {
  return Utilities.computeDigest(
    Utilities.DigestAlgorithm.SHA_256,
    value,
    Utilities.Charset.UTF_8
  )
    .map(function (byte) {
      return ('0' + ((byte + 256) % 256).toString(16)).slice(-2);
    })
    .join('')
    .toUpperCase();
}

function generateLicenseKey_() {
  const raw = (
    Utilities.getUuid().replace(/-/g, '') +
    Utilities.getUuid().replace(/-/g, '')
  ).toUpperCase();
  const groups = [];
  for (let index = 0; index < 24; index += 4) {
    groups.push(raw.slice(index, index + 4));
  }

  return 'LST-' + groups.join('-');
}

function base64UrlString_(value) {
  return Utilities.base64EncodeWebSafe(value, Utilities.Charset.UTF_8)
    .replace(/=+$/g, '');
}

function base64UrlBytes_(value) {
  return Utilities.base64EncodeWebSafe(value).replace(/=+$/g, '');
}

function base64UrlDecodeString_(value) {
  const encoded = String(value || '');
  const padding = (4 - (encoded.length % 4)) % 4;
  const bytes = Utilities.base64DecodeWebSafe(
    encoded + '='.repeat(padding)
  );
  return Utilities.newBlob(bytes).getDataAsString('UTF-8');
}

function jsonResponse_(success, code, message, lease, clientCredential) {
  const body = {
    success: success,
    code: code,
    message: message
  };
  if (lease) {
    body.lease = lease;
  }
  if (clientCredential) {
    body.clientCredential = clientCredential;
  }

  return ContentService.createTextOutput(JSON.stringify(body))
    .setMimeType(ContentService.MimeType.JSON);
}
