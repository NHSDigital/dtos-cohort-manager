#!/usr/bin/env node
/*
 Orchestrates local E2E runs cross‑platform using Podman + Playwright.
 Usage:
   npm run e2e:run -- --epic epic4c

 Supported values for --epic:
   epic1, epic2, epic3, epic4c, epic4d, epic1med, epic2med, epic3med
*/
const { execSync, spawnSync } = require('node:child_process');
const path = require('node:path');
const { URL } = require('node:url');
const net = require('node:net');

function log(msg) { console.log(`[e2e] ${msg}`); }
function run(cmd, opts={}) { log(`$ ${cmd}`); execSync(cmd, { stdio: 'inherit', ...opts }); }

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

async function waitHttp(targets, maxAttempts=90, intervalMs=3000) {
  function httpStatus(u) { return new Promise(resolve => {
    const h = new URL(u);
    const mod = h.protocol === 'https:' ? require('https') : require('http');
    const req = mod.request({ hostname:h.hostname, port:h.port, path:h.pathname || '/', method:'GET', timeout:2000 }, res => {
      const code = res.statusCode || 0; res.resume(); resolve(code);
    });
    req.on('timeout', () => { req.destroy(new Error('timeout')); });
    req.on('error', () => resolve(0));
    req.end();
  }); }
  for (const t of targets) {
    const h = new URL(t);
    const host = h.hostname; const port = Number(h.port || (h.protocol==='https:'?443:80));
    let attempt=0;
    while (true) {
      attempt++;
      const code = await httpStatus(t);
      // Consider 2xx-3xx ready; also accept 400/405 for POST-only endpoints
      if ((code >= 200 && code < 400) || code === 400 || code === 405) {
        log(`Ready (HTTP ${code}): ${t}`);
        break;
      }
      if (attempt>=maxAttempts) { throw new Error(`Timeout waiting for ${t}`); }
      log(`Waiting on ${t} (${attempt}/${maxAttempts})...`);
      await new Promise(r=>setTimeout(r, intervalMs));
    }
  }
}

(async () => {
  const { epic } = parseArgs();
  const testScript = epicToNpmScript(epic);

  // __dirname = tests/playwright-tests/scripts
  // repo root is three levels up
  const repoRoot = path.resolve(__dirname, '..', '..', '..');
  const cohortDir = path.join(repoRoot, 'application', 'CohortManager');
  const testsDir = path.join(repoRoot, 'tests', 'playwright-tests');

  // 1) Build services
  process.chdir(cohortDir);
  run('podman compose down');
  run('podman compose -f compose.core.yaml build');
  run('podman compose -f compose.cohort-distribution.yaml build');
  run('podman compose -f compose.data-services.yaml build');

  // 2) Ensure deps up and migrations complete
  run('podman compose -f compose.deps.yaml up -d');
  // Run db-migration to completion to ensure schema is ready
  try {
    run('podman compose -f compose.deps.yaml up --build db-migration');
  } catch (e) {
    // db-migration is expected to exit after completion; non-zero here indicates failure
    throw e;
  } finally {
    // cleanup any exited migration container
    try { run('podman compose -f compose.deps.yaml rm -f db-migration'); } catch {}
  }

  // 3) Start app profiles in detached mode
  run('podman compose down');
  // Include non-essential profile to ensure ManageServiceNowParticipant is started
  run('podman compose --profile service-now --profile bs-select --profile non-essential up -d');

  // 4) Wait for readiness
  const targets = [
    'http://localhost:7994/api/ParticipantManagementDataService',
    'http://localhost:7993/api/ParticipantDemographicDataService',
    'http://localhost:7992/api/CohortDistributionDataService',
    'http://localhost:7078/api/RetrieveCohortDistributionData',
    'http://localhost:8082/api/health',
    'http://localhost:7086/api/RetrieveCohortRequestAudit',
    // Prefer health endpoint (GET) for ServiceNow handler
    'http://localhost:9092/api/health'
  ];
  await waitHttp(targets, 120, 3000);

  // small stabilization delay
  await new Promise(r=>setTimeout(r, 3000));

  // 5) Install and run tests
  process.chdir(testsDir);
  run('npm install');
  run(`npm run ${testScript}`);
})();
