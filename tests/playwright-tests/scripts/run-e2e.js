#!/usr/bin/env node
/*
 Orchestrates local E2E runs by verifying services are healthy, then running Playwright.
 Usage:
  npm run e2e:run -- --epic epic4c [--force-install]

 Supported values for --epic:
  epic1, epic2, epic3, epic4c, epic4d, epic1med, epic2med, epic3med
*/
const { spawnSync } = require('node:child_process');
const fs = require('node:fs');
const path = require('node:path');

function resolveBin(name) {
  const candidates = [
    `/usr/bin/${name}`,
    `/usr/sbin/${name}`,
    `/bin/${name}`,
    `/sbin/${name}`,
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
  // Fall back to relying on PATH
  return name;
}
const NPM_BIN = resolveBin('npm');

function log(msg) { console.log(`[e2e] ${msg}`); }
function run(bin, args = [], opts = {}) {
  const printable = [bin, ...args].join(' ');
  log(`$ ${printable}`);
  const res = spawnSync(bin, args, { stdio: 'inherit', ...opts });
  if (res.error) throw res.error;
  if (typeof res.status === 'number' && res.status !== 0) {
    const err = new Error(`Command failed (${res.status}): ${printable}`);
    err.status = res.status;
    throw err;
  }
}

function parseArgs() {
  const args = process.argv.slice(2);
  const out = { forceInstall: false };
  for (let i=0; i<args.length; i++) {
    const a = args[i];
    if (a === '--epic' && args[i+1]) { out.epic = args[++i].toLowerCase(); }
    else if (a === '--force-install' || a === '--install') { out.forceInstall = true; }
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
  });
  if (res.error) throw new Error('Health check failed: ' + res.error.message);
  if (typeof res.status === 'number' && res.status !== 0) {
    throw new Error('Health check failed with exit code: ' + res.status);
  }
}

(async () => {
  const { epic, forceInstall } = parseArgs();
  const testScript = epicToNpmScript(epic);

  // tests dir relative to this script
  const testsDir = path.resolve(__dirname, '..');
  // Only perform health check and run tests; orchestration is handled outside

  // Wait for readiness
  await waitForServices();

  // small stabilization delay
  await new Promise(r=>setTimeout(r, 3000));

  // Install (skip if node_modules exists unless forced) and run tests
  process.chdir(testsDir);
  if (forceInstall || !fs.existsSync(path.join(testsDir, 'node_modules'))) {
    run(NPM_BIN, ['install']);
  } else {
    log('Skipping npm install (node_modules present). Use --force-install to override.');
  }
  run(NPM_BIN, ['run', testScript]);
})();
