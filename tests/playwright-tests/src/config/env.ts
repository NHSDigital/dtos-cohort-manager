import * as dotenv from 'dotenv';
import * as path from 'path';

const env = process.env.NODE_ENV ?? 'dev';
if(process.env.Is_CloudEnvironment){}else{
  dotenv.config({ path: path.resolve(__dirname, `../../../../application/CohortManager/.env`) });
}

export const config = {
  baseURL: process.env.BASE_URL ?? '',
  azureConnectionString: process.env.CAASFOLDER_STORAGE_CONNECTION_STRING || '',
  sqlConfig: {
    host: process.env.SQL_HOST ?? '',
    user: process.env.SQL_USER ?? '',
    password: process.env.PASSWORD ?? '',
    database: process.env.DB_NAME ?? '',
  },
  sqlRetry: process.env.SQL_RETRIES ?? 1,
  sqlWaitTime: process.env.SQL_WAIT_TIME ?? 2000,
  containerName: process.env.CONTAINER_NAME ?? '',
  e2eTestFilesPath: process.env.E2E_TEST_FILES_PATH ?? 'e2e/testFiles',
  endpointCohortDistributionDataService: process.env.ENDPOINT_COHORT_DISTRIBUTION_DATA_SERVICE ?? '',
};
