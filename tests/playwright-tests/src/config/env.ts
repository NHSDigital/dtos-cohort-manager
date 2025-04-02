import * as dotenv from 'dotenv';
import * as path from 'path';

const env = process.env.NODE_ENV || 'dev';
dotenv.config({ path: path.resolve(__dirname, `../../.env.${env}`) });

export const config = {
  baseURL: process.env.BASE_URL || '',
  azureConnectionString: process.env.AZURE_CONNECTION_STRING || '',
  sqlConfig: {
    host: process.env.SQL_HOST || '',
    user: process.env.SQL_USER || '',
    password: process.env.SQL_PASSWORD || '',
    database: process.env.SQL_DATABASE || '',
  },
  sqlRetry: process.env.SQL_RETRIES || 1,
  sqlWaitTime: process.env.SQL_WAIT_TIME || 2000,
  containerName: process.env.CONTAINER_NAME || '',
  endpointRetrieveCohortRequestAudit: process.env.endpointRetrieveCohortRequestAudit || '',
  endpointRetrieveCohortDistributionData: process.env.endpointRetrieveCohortDistributionData || ''
};
