#!/usr/bin/env node
/*
 Orchestrates local E2E runs cross‑platform using Podman + Playwright.
 Usage:
   npm run e2e:run -- --epic epic4c

 Supported values for --epic:
   epic1, epic2, epic3, epic4c, epic4d, epic1med, epic2med, epic3med
*/
const { spawnSync } = require('node:child_process');
const os = require('node:os');
const path = require('node:path');
const { URL } = require('node:url');
const net = require('node:net');

const SAFE_PATH = process.platform === 'darwin'
  ? '/usr/local/bin:/usr/bin:/bin:/opt/homebrew/bin'
  : '/usr/local/sbin:/usr/local/bin:/usr/sbin:/usr/bin:/sbin:/bin';
const SAFE_ENV = { ...process.env, PATH: SAFE_PATH };

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
  
  const cohortDir = path.join(repoRoot, 'application', 'CohortManager');
  const testsDir = path.join(repoRoot, 'tests', 'playwright-tests');

  // 1) Build services
  process.chdir(cohortDir);
  run('podman', ['compose', 'down']);
  run('podman', ['compose', '-f', 'compose.core.yaml', 'build']);
  run('podman', ['compose', '-f', 'compose.cohort-distribution.yaml', 'build']);
  run('podman', ['compose', '-f', 'compose.data-services.yaml', 'build']);

  // 2) Ensure deps up and migrations complete
  run('podman', ['compose', '-f', 'compose.deps.yaml', 'up', '-d']);
  
  // Run db-migration to completion to ensure schema is ready
  log('Running database migrations...');
  try {
    run('podman', ['compose', '-f', 'compose.deps.yaml', 'up', '--build', 'db-migration']);
  } catch (e) {
    log('Database migration failed or completed with non-zero exit code');
    // Check if migration actually failed or just completed
    const result = spawnSync('podman', ['compose', '-f', 'compose.deps.yaml', 'ps', 'db-migration'], { encoding: 'utf8', env: SAFE_ENV });
    if (result.stdout && result.stdout.includes('Exited (0)')) {
      log('Migration completed successfully (exit code 0)');
    } else {
      throw new Error('Database migration failed: ' + e.message);
    }
  }
  
  // Cleanup migration container
  try { 
    run('podman', ['compose', '-f', 'compose.deps.yaml', 'rm', '-f', 'db-migration']); 
  } catch (cleanupError) {
    log('Warning: Failed to cleanup migration container: ' + cleanupError.message);
  }

  // 3) Start app profiles in detached mode
  run('podman', ['compose', 'down']);
  // Include non-essential profile to ensure ManageServiceNowParticipant is started, and nems profile for manage-nems-subscription
  run('podman', ['compose', '--profile', 'service-now', '--profile', 'bs-select', '--profile', 'non-essential', '--profile', 'nems', 'up', '-d']);

  // 4) Wait for readiness
  await waitForServices();

  // small stabilization delay
  await new Promise(r=>setTimeout(r, 3000));

  // 5) Install and run tests
  process.chdir(testsDir);
  run('npm', ['install']);
  run('npm', ['run', testScript]);
})();
