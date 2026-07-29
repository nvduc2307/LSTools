const fs = require('node:fs');
const path = require('node:path');
const vm = require('node:vm');

const scriptPath = path.resolve(
  __dirname,
  '..',
  '..',
  'license-server',
  'google-apps-script',
  'Code.gs'
);
const context = vm.createContext({ console });
vm.runInContext(fs.readFileSync(scriptPath, 'utf8'), context, {
  filename: scriptPath
});

const tests = [
  {
    name: 'existing active device remains valid at the limit',
    run: () => {
      const result = context.getActivationDecision_(
        'validate',
        'Active',
        3,
        3
      );
      return result.allowed && !result.createActivation;
    }
  },
  {
    name: 'new device is allowed below the limit',
    run: () => {
      const result = context.getActivationDecision_(
        'activate',
        '',
        2,
        3
      );
      return result.allowed && result.createActivation;
    }
  },
  {
    name: 'new device is rejected at the limit',
    run: () => {
      const result = context.getActivationDecision_(
        'activate',
        '',
        3,
        3
      );
      return !result.allowed &&
        result.code === 'DEVICE_LIMIT_REACHED';
    }
  },
  {
    name: 'unknown device cannot validate without activation',
    run: () => {
      const result = context.getActivationDecision_(
        'validate',
        '',
        0,
        3
      );
      return !result.allowed && result.code === 'NOT_ACTIVATED';
    }
  },
  {
    name: 'revoked device cannot silently reactivate',
    run: () => {
      const result = context.getActivationDecision_(
        'activate',
        'Revoked',
        0,
        3
      );
      return !result.allowed && result.code === 'DEVICE_REVOKED';
    }
  },
  {
    name: 'legacy blank device limit defaults to one',
    run: () => context.normalizeMaxDevices_('') === 1
  },
  {
    name: 'configured device limit is preserved',
    run: () => context.normalizeMaxDevices_(5) === 5
  },
  {
    name: 'activation key normalizes device formatting',
    run: () => context.activationKey_(
      'lst-001',
      'aaaa-bbbb cccc'
    ) === 'LST-001|AAAABBBBCCCC'
  }
];

let failures = 0;
for (const test of tests) {
  try {
    const passed = Boolean(test.run());
    console.log(`${passed ? 'PASS' : 'FAIL'} ${test.name}`);
    if (!passed) {
      failures += 1;
    }
  } catch (error) {
    failures += 1;
    console.log(
      `FAIL ${test.name}: ${error.name} - ${error.message}`
    );
  }
}

console.log();
console.log(
  failures === 0
    ? `All ${tests.length} Apps Script license tests passed.`
    : `${failures} Apps Script license test(s) failed.`
);
process.exitCode = failures === 0 ? 0 : 1;
