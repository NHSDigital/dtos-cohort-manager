#!/usr/bin/env node
/* Simple health check for core endpoints used in E2E runs */
const { URL } = require('node:url');

function log(msg) { console.log(`[health] ${msg}`); }

async function waitHttp(targets, maxAttempts=120, intervalMs=3000) {
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
    let attempt = 0;
    while (true) {
      attempt++;
      const code = await httpStatus(t);
      if ((code >= 200 && code < 400) || code === 400 || code === 405) {
        log(`Ready (HTTP ${code}): ${t}`);
        break;
      }
      if (attempt >= maxAttempts) throw new Error(`Timeout waiting for ${t}`);
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
