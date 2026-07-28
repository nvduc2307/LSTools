const LICENSE_SHEET_NAME = 'Licenses';
const LICENSE_PRODUCT = 'LSTools';
const LEASE_HOURS = 72;

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
  'Notes'
];

function doGet() {
  try {
    const sheet = getLicenseSheet_();
    getHeaderMap_(sheet);
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
  const sheet = getLicenseSheet_();
  const headers = getHeaderMap_(sheet);
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

  if (!record.deviceHash) {
    if (action !== 'activate') {
      return jsonResponse_(
        false,
        'NOT_ACTIVATED',
        'License chưa được kích hoạt trên thiết bị này.'
      );
    }

    sheet.getRange(row, headers.DeviceHash).setValue(deviceHash);
    sheet.getRange(row, headers.ActivatedUtc).setValue(now);
    record.deviceHash = deviceHash;
  } else if (record.deviceHash !== deviceHash) {
    return jsonResponse_(
      false,
      'DEVICE_MISMATCH',
      'License đã được kích hoạt trên một máy tính khác.'
    );
  }

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
  const spreadsheet = getSpreadsheet_();
  spreadsheet.setSpreadsheetTimeZone('UTC');
  let sheet = spreadsheet.getSheetByName(LICENSE_SHEET_NAME);
  if (!sheet) {
    sheet = spreadsheet.insertSheet(LICENSE_SHEET_NAME);
  }

  if (sheet.getLastRow() === 0) {
    sheet.getRange(1, 1, 1, LICENSE_HEADERS.length).setValues([LICENSE_HEADERS]);
  } else {
    const current = sheet
      .getRange(1, 1, 1, LICENSE_HEADERS.length)
      .getDisplayValues()[0];
    if (current.join('|') !== LICENSE_HEADERS.join('|')) {
      throw new Error(
        'Hàng tiêu đề không đúng. Hãy dùng đúng thứ tự trong LICENSE_HEADERS.'
      );
    }
  }

  sheet.setFrozenRows(1);
  sheet.getRange('F:F').setNumberFormat('yyyy-mm-dd hh:mm:ss');
  sheet.getRange('I:K').setNumberFormat('yyyy-mm-dd hh:mm:ss');
  sheet.autoResizeColumns(1, LICENSE_HEADERS.length);
  return 'READY';
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

  const sheet = getLicenseSheet_();
  const headers = getHeaderMap_(sheet);
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

  sheet.appendRow([
    licenseId,
    keyHash,
    customer,
    LICENSE_PRODUCT,
    '',
    expiresUtc,
    'Active',
    features,
    '',
    '',
    now,
    notesPrompt.getResponseText().trim()
  ]);

  ui.alert(
    'Mã đóng gói đã tạo',
    'Không gửi mã này cho khách. Hãy ghi nguyên chuỗi dưới đây vào ' +
      'LSTool\\Resources\\Settings\\ReleaseProfile.dat rồi build bộ cài riêng:\n\n' +
      Utilities.base64Encode(
        licenseKey,
        Utilities.Charset.UTF_8
      ) +
      '\n\nHết hạn UTC: ' +
      expiresUtc.toISOString(),
    ui.ButtonSet.OK
  );
}

function resetSelectedDevice() {
  const context = getSelectedLicenseRow_();
  context.sheet.getRange(context.row, context.headers.DeviceHash).clearContent();
  context.sheet.getRange(context.row, context.headers.ActivatedUtc).clearContent();
  context.sheet.getRange(context.row, context.headers.LastCheckUtc).clearContent();
  SpreadsheetApp.getUi().alert(
    'Đã reset thiết bị. Key có thể kích hoạt trên một máy khác.'
  );
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
    .addItem('Reset máy của dòng đang chọn', 'resetSelectedDevice')
    .addItem('Khóa dòng đang chọn', 'revokeSelectedLicense')
    .addItem('Mở lại dòng đang chọn', 'reactivateSelectedLicense')
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

function getHeaderMap_(sheet) {
  if (sheet.getLastColumn() < LICENSE_HEADERS.length) {
    throw new Error('Sheet Licenses thiếu cột.');
  }

  const row = sheet
    .getRange(1, 1, 1, sheet.getLastColumn())
    .getDisplayValues()[0];
  const result = {};
  row.forEach(function (header, index) {
    if (header) {
      result[header.trim()] = index + 1;
    }
  });

  LICENSE_HEADERS.forEach(function (header) {
    if (!result[header]) {
      throw new Error('Sheet Licenses thiếu cột ' + header + '.');
    }
  });

  return result;
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
    features: parseFeatures_(at('Features'))
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
    headers: getHeaderMap_(sheet)
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
