#!/usr/bin/env node
/* Simple health check for core endpoints used in E2E runs */
const { URL } = require('node:url');

function log(msg) { console.log(`[health] ${msg}`); }

async function waitHttp(targets, maxAttempts=5, intervalMs=3000) {
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
  // Map ports to likely container names to help the user
  const portToContainer = {
    '7994': 'participant-management-data-service',
    '7993': 'participant-demographic-data-service',
    '7992': 'cohort-distribution-data-service',
    '7078': 'retrieve-cohort-distribution-data',
    '7086': 'retrieve-cohort-request-audit',
    '8082': 'retrieve-pds-demographic',
    '9092': 'servicenow-message-handler'
  };

  for (const t of targets) {
    let attempt = 0;
    while (true) {
      attempt++;
      const code = await httpStatus(t);
      if ((code >= 200 && code < 400) || code === 400 || code === 405) {
        log(`Ready (HTTP ${code}): ${t}`);
        break;
      }
      if (attempt >= maxAttempts) {
        try {
          const u = new URL(t);
          const port = String(u.port || (u.protocol === 'https:' ? 443 : 80));
          const name = portToContainer[port] || `service on port ${port}`;
          console.error(`[health] Timeout waiting for ${t}.`);
          console.error(`[health] This likely means container '${name}' is not running.`);
          console.error(`[health] Start containers via VS Code task "Spin up Docker for E2E regression (build + start)" or run compose profiles for service-now, bs-select, and non-essential.`);
        } catch {}
        throw new Error(`Timeout waiting for ${t}`);
      }
      log(`Waiting on ${t} (${attempt}/${maxAttempts})...`);
      await new Promise(r=>setTimeout(r, intervalMs));
    }
  }
}

(async () => {
  const targets = [
    'http://localhost:7994/api/ParticipantManagementDataService',
    'http://localhost:7993/api/ParticipantDemographicDataService',
    'http://localhost:7992/api/CohortDistributionDataService',
    'http://localhost:7078/api/RetrieveCohortDistributionData',
    'http://localhost:7086/api/RetrieveCohortRequestAudit',
    'http://localhost:8082/api/health',
    'http://localhost:9092/api/health'
  ];
  await waitHttp(targets);
  log('All targets healthy');
})().catch(err => { console.error(err.message || err); process.exit(1); });
