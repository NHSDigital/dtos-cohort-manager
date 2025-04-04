import * as dotenv from 'dotenv';
import * as path from 'path';

const env = process.env.NODE_ENV || 'dev';
dotenv.config({ path: path.resolve(__dirname, `../../../../application/CohortManager/.env`) });

export const config = {
  baseURL: process.env.BASE_URL || '',
  azureConnectionString: process.env.AZURITE_CONNECTION_STRING || '',
  sqlConfig: {
    host: process.env.SQL_HOST || '',
    user: process.env.SQL_USER || '',
    password: process.env.PASSWORD || '',
    database: process.env.DB_NAME || '',
  },
  sqlRetry: process.env.SQL_RETRIES || 1,
  sqlWaitTime: process.env.SQL_WAIT_TIME || 2000,
  containerName: process.env.CONTAINER_NAME || ''
};
