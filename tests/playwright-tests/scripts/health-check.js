#!/usr/bin/env node
/* Simple health check for core endpoints used in E2E runs */
const { URL } = require('node:url');

function log(msg) { console.log(`[health] ${msg}`); }

// Parse command line arguments
function parseArgs() {
  const args = process.argv.slice(2);
  let maxAttempts = 5; // Default to 5 for fast failure
  let intervalMs = 3000;
  
  for (let i = 0; i < args.length; i++) {
    if (args[i] === '--max-attempts' && args[i + 1]) {
      maxAttempts = parseInt(args[++i], 10);
      if (isNaN(maxAttempts) || maxAttempts < 1) {
        console.error('Error: --max-attempts must be a positive number');
        process.exit(1);
      }
    } else if (args[i] === '--interval' && args[i + 1]) {
      intervalMs = parseInt(args[++i], 10);
      if (isNaN(intervalMs) || intervalMs < 100) {
        console.error('Error: --interval must be at least 100ms');
        process.exit(1);
      }
    } else if (args[i] === '--help') {
      console.log('Usage: node health-check.js [--max-attempts N] [--interval MS]');
      console.log('  --max-attempts N   Maximum number of attempts per endpoint (default: 5)');
      console.log('  --interval MS      Delay between attempts in milliseconds (default: 3000)');
      process.exit(0);
    }
  }
  
  return { maxAttempts, intervalMs };
}

async function waitHttp(targets, maxAttempts, intervalMs) {
  function httpStatus(url) { 
    return new Promise(resolve => {
      const u = new URL(url);
      const mod = u.protocol === 'https:' ? require('https') : require('http');
      const req = mod.request({ 
        hostname: u.hostname, 
        port: u.port, 
        path: u.pathname || '/', 
        method: 'GET', 
        timeout: 2000 
      }, res => {
        const code = res.statusCode || 0; 
        res.resume(); 
        resolve({ code, error: null });
      });
      req.on('timeout', () => { 
        req.destroy(); 
        resolve({ code: 0, error: 'Connection timeout' }); 
      });
      req.on('error', (err) => resolve({ 
        code: 0, 
        error: `${err.code || 'UNKNOWN'}: ${err.message}` 
      }));
      req.end();
    }); 
  }
  
  for (const target of targets) {
    let attempt = 0;
    while (true) {
      attempt++;
      const { code, error } = await httpStatus(target);
      
      if ((code >= 200 && code < 400) || code === 400 || code === 405) {
        log(`Ready (HTTP ${code}): ${target}`);
        break;
      }
      
      if (attempt >= maxAttempts) {
        const errorMsg = error ? ` - ${error}` : '';
        throw new Error(`Timeout waiting for ${target} after ${maxAttempts} attempts (last status: ${code}${errorMsg})`);
      }
      
      const errorContext = error ? ` (${error})` : '';
      log(`Waiting on ${target} (${attempt}/${maxAttempts}, HTTP ${code}${errorContext})...`);
      await new Promise(r => setTimeout(r, intervalMs));
    }
  }
}

(async () => {
  const { maxAttempts, intervalMs } = parseArgs();
  
  const targets = [
    'http://localhost:7994/api/ParticipantManagementDataService',
    'http://localhost:7993/api/ParticipantDemographicDataService',
    'http://localhost:7992/api/CohortDistributionDataService',
    'http://localhost:7078/api/RetrieveCohortDistributionData',
    'http://localhost:7086/api/RetrieveCohortRequestAudit',
    'http://localhost:8082/api/health',
    'http://localhost:9092/api/health'
  ];
  
  await waitHttp(targets, maxAttempts, intervalMs);
  log('All targets healthy');
})().catch(err => { console.error(err.message || err); process.exit(1); });
