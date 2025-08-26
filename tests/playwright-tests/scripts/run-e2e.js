#!/usr/bin/env node
/*
 Orchestrates local E2E runs by verifying services are healthy, then running Playwright.
 Usage:
   npm run e2e:run -- --epic epic4c

 Supported values for --epic:
   epic1, epic2, epic3, epic4c, epic4d, epic1med, epic2med, epic3med
*/
const { spawnSync } = require('node:child_process');
const fs = require('node:fs');
const path = require('node:path');
const { URL } = require('node:url');
const net = require('node:net');

// Only unwriteable system directories in PATH to satisfy Sonar guidance
const SAFE_PATH = '/usr/sbin:/usr/bin:/sbin:/bin';
const SAFE_ENV = { PATH: SAFE_PATH };

function resolveBin(name) {
  const candidates = [
    `/usr/bin/${name}`,
    `/usr/sbin/${name}`,
    `/bin/${name}`,
    `/sbin/${name}`,
    // Allow common installation locations, even if not in SAFE_PATH. We will execute via absolute path.
    `/usr/local/bin/${name}`,
    `/opt/homebrew/bin/${name}`,
  ];
  for (const p of candidates) {
    try {
      if (fs.existsSync(p)) {
        fs.accessSync(p, fs.constants.X_OK);
        return p;
      }
    } catch (_) { /* continue */ }
  }
  throw new Error(`Unable to locate required binary '${name}'. Checked: ${candidates.join(', ')}`);
}
const NPM_BIN = resolveBin('npm');

function log(msg) { console.log(`[e2e] ${msg}`); }
function run(bin, args = [], opts = {}) {
  const printable = [bin, ...args].join(' ');
  log(`$ ${printable}`);
  const res = spawnSync(bin, args, { stdio: 'inherit', env: SAFE_ENV, ...opts });
  if (res.error) throw res.error;
  if (typeof res.status === 'number' && res.status !== 0) {
    const err = new Error(`Command failed (${res.status}): ${printable}`);
    err.status = res.status;
    throw err;
  }
}

function parseArgs() {
  const args = process.argv.slice(2);
  const out = {};
  for (let i=0; i<args.length; i++) {
    const a = args[i];
    if (a === '--epic' && args[i+1]) { out.epic = args[++i].toLowerCase(); }
  }
  if (!out.epic) { console.error('Error: --epic is required'); process.exit(1); }
  return out;
}

function epicToNpmScript(epic) {
  const map = {
    epic1: 'test:regression_e2e_epic1',
    epic2: 'test:regression_e2e_epic2',
    epic3: 'test:regression_e2e_epic3',
    epic4c: 'test:regression_e2e_epic4c',
    epic4d: 'test:regression_e2e_epic4d',
    epic1med: 'test:regression_e2e_epic1Med',
    epic2med: 'test:regression_e2e_epic2Med',
    epic3med: 'test:regression_e2e_epic3Med',
  };
  if (!map[epic]) { console.error(`Unknown epic '${epic}'.`); process.exit(1); }
  return map[epic];
}

// Use health-check script for better error reporting and consistency
async function waitForServices() {
  const res = spawnSync(process.execPath, ['scripts/health-check.js', '--max-attempts', '5', '--interval', '3000'], {
    stdio: 'inherit',
    cwd: __dirname,
    env: SAFE_ENV,
  });
  if (res.error) throw new Error('Health check failed: ' + res.error.message);
  if (typeof res.status === 'number' && res.status !== 0) {
    throw new Error('Health check failed with exit code: ' + res.status);
  }
}

(async () => {
  const { epic } = parseArgs();
  const testScript = epicToNpmScript(epic);

  // Find repo root by looking for .git directory
  let repoRoot = __dirname;
  while (repoRoot !== path.dirname(repoRoot)) {
    if (require('fs').existsSync(path.join(repoRoot, '.git'))) {
      break;
    }
    repoRoot = path.dirname(repoRoot);
  }

  if (!require('fs').existsSync(path.join(repoRoot, '.git'))) {
    throw new Error('Could not find git repository root');
  }

  const testsDir = path.join(repoRoot, 'tests', 'playwright-tests');
  // Only perform health check and run tests; orchestration is handled outside

  // Wait for readiness
  await waitForServices();

  // small stabilization delay
  await new Promise(r=>setTimeout(r, 3000));

  // Install and run tests
  process.chdir(testsDir);
  run(NPM_BIN, ['install']);
  run(NPM_BIN, ['run', testScript]);
})();
