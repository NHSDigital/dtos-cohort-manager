import { test, APIRequestContext, expect } from "@playwright/test";
import { checkBlobExists, uploadToLocalStorage } from "../../storage/azureStorage";
import { InputData, ParticipantRecord, ServiceNowRequestValidations } from "../../interface/InputData";
import { config } from "../../config/env";
import * as fs from 'fs';
import path from "path";
import { validateApiResponse } from "../../api/apiHelper";
import { cleanDataBaseUsingServices } from "../../api/dataService/dataServiceCleaner";
import { ensureNhsNumbersStartWith999 } from "../fixtures/testDataHelper";
import { receiveParticipantViaServiceNow } from "../../api/distributionService/bsSelectService";
import { WireMockResponse } from "../../interface/wiremock";


export async function cleanupDatabaseFromAPI(request: APIRequestContext, numbers: string[]) {
  return test.step(`Cleanup database using data services`, async () => {
    await cleanDataBaseUsingServices(numbers, request);
  });
}

export async function cleanupNemsSubscriptions(request: APIRequestContext, numbers: string[]) {
  return test.step(`Cleanup NEMS subscriptions via data service`, async () => {
    // Note: current data service cleaner deletes all records in the selected service.
    // This keeps the environment clean and ensures first-time subscribe paths.
    await cleanDataBaseUsingServices(numbers, request, ['nemsSubscription' as any]);
  });
}

// Attempt to resolve a stable scenario folder name from a test title or tag string.
// Prefer the first @DTOSS-xxxxx-yy tag if present; otherwise return the original string.
function resolveScenarioFolder(raw: string): string {
  if (!raw) return raw;
  const match = raw.match(/@DTOSS-\d+-\d+/);
  if (match && match[0]) return match[0];
  // Fallback: if multiple tokens separated by spaces, use the first token
  const firstToken = raw.split(/[\s|]+/).filter(Boolean)[0];
  return firstToken || raw;
}

function getWireMockUrl(): string {
  const wireMockUrl = config.wireMockUrl;

  if (wireMockUrl.length === 0)
  {
    throw new Error(`❌ WIREMOCK_URL environment variable is empty`);
  }

  return wireMockUrl;
}

function getWireMockAdmin(base?: string): { admin: string; requests: string; mappings: string } {
  // Use shared WIREMOCK_URL for both ServiceNow and Mesh
  const raw = (base ?? config.wireMockUrl).trim();
  if (!raw) throw new Error('❌ WIREMOCK_URL is not set.');
  const noTrail = raw.replace(/\/$/, '');
  const admin = noTrail.endsWith('/__admin')
    ? noTrail
    : noTrail.endsWith('/__admin/requests') || noTrail.endsWith('/__admin/mappings')
      ? noTrail.replace(/\/(requests|mappings)$/,'')
      : `${noTrail}/__admin`;
  return {
    admin,
    requests: `${admin}/requests`,
    mappings: `${admin}/mappings`
  };
}

export async function cleanupWireMock(request: APIRequestContext) {
  const { requests: requestsUrl } = getWireMockAdmin();

  return test.step(`Cleaning up WireMock`, async () => {
    try {
      // Prefer the explicit reset endpoint if available
      const resetUrl = `${requestsUrl}/reset`;
      const res = await request.post(resetUrl);
      if (!res.ok()) {
        // Fallback to DELETE all requests
        await request.delete(requestsUrl);
      }
    }
    catch (e) {
      console.warn(`Failed to clean up WireMock requests. Attempted: POST ${requestsUrl}/reset then DELETE ${requestsUrl}. Error: ${e}`);
    }
  });
}

export async function validateSqlDatabaseFromAPI(
  request: APIRequestContext,
  validations: any,
  options?: { retries?: number; initialWaitMs?: number; stepMs?: number }
) {
  return test.step(`Validate database for assertions`, async () => {
    const { status, errorTrace } = await validateApiResponse(validations, request, options);
    if (!status) {
      throw new Error(`❌ Validation failed after ${config.apiRetry} attempts, please checks logs for more details: ${errorTrace}`);
    }
  });
}

/**
 * Validate that Mesh outbox HTTP calls were made to WireMock.
 * Looks for requests whose URL contains typical Mesh paths, e.g. `messageexchange` and `outbox`.
 * Optional criteria allow narrowing by mailbox id or custom substrings.
 */
export async function validateMeshRequestWithMockServer(
  request: APIRequestContext,
  options?: { toMailboxContains?: string; minCount?: number; pathIncludes?: string[]; attempts?: number; delayMs?: number }
)
{
  const { requests: requestsUrl } = getWireMockAdmin();

  const minCount = options?.minCount ?? 1;
  const includes = options?.pathIncludes ?? ["messageexchange", "outbox"];
  const mailboxHint = options?.toMailboxContains;
  const attempts = Math.max(1, options?.attempts ?? 5);
  const delayMs = Math.max(250, options?.delayMs ?? 2000);

  return test.step(`Validate Mesh requests with WireMock`, async () => {
    let lastBody: WireMockResponse | undefined;
    for (let i = 1; i <= attempts; i++) {
      try {
        const response = await request.get(requestsUrl);
        lastBody = await response.json() as WireMockResponse;

        let meshRequests = (lastBody.requests || []).filter(r =>
          includes.every(inc => r.request.url.includes(inc))
        );

        if (mailboxHint) {
          meshRequests = meshRequests.filter(r => r.request.url.includes(mailboxHint));
        }

        if (meshRequests.length >= minCount) {
          console.info(`✅ Validation Complete - Found ${meshRequests.length} Mesh request(s) to outbox via WireMock`);
          return;
        }

        if (i < attempts) {
          console.info(`⏳ No Mesh requests yet (found ${meshRequests.length}/${minCount}); retrying in ${Math.round(delayMs/1000)}s...`);
          await new Promise(res => setTimeout(res, delayMs));
          continue;
        }

        const sample = (lastBody.requests || []).slice(0, 5).map(r => r.request.url).join("\n - ");
        throw new Error(
          `❌ Expected at least ${minCount} Mesh request(s), found 0 after ${attempts} attempt(s).` +
          `\nWireMock requests endpoint: ${requestsUrl}` +
          `\nSample captured URLs:\n - ${sample || '(none)'}\n`
        );
      } catch (e) {
        if (i >= attempts) throw e;
        await new Promise(res => setTimeout(res, delayMs));
      }
    }
  });
}

/**
 * Configure WireMock to return a failure (e.g. 500) for Mesh outbox HTTP calls.
 */
export async function enableMeshOutboxFailureInWireMock(
  request: APIRequestContext,
  status: number = 500
) {
  const { mappings: mappingsUrl } = getWireMockAdmin();

  const body = {
    priority: 1,
    request: {
      method: 'POST',
      urlPattern: '.*messageexchange/.*/outbox.*'
    },
    response: {
      status,
      jsonBody: { error: 'Injected Mesh failure from tests' },
      headers: { 'Content-Type': 'application/json' }
    }
  };

  return test.step(`Enable Mesh failure stub in WireMock (status ${status})`, async () => {
    const res = await request.post(mappingsUrl, { data: body });
    if (!res.ok()) {
      throw new Error(`Failed to create WireMock mapping. ${res.status()} - ${await res.text()}`);
    }
  });
}

/** Configure WireMock to return success for Mesh outbox HTTP calls with a dynamic messageId. */
export async function enableMeshOutboxSuccessInWireMock(
  request: APIRequestContext
) {
  const { mappings: mappingsUrl } = getWireMockAdmin();

  const body = {
    priority: 5,
    request: {
      method: 'POST',
      urlPattern: '.*messageexchange/.*/outbox.*'
    },
    response: {
      status: 200,
      jsonBody: { messageId: "{{randomValue length=24 type='ALPHANUMERIC'}}" },
      headers: { 'Content-Type': 'application/json' },
      transformers: ["response-template"]
    }
  };

  return test.step(`Enable Mesh success stub in WireMock`, async () => {
    const res = await request.post(mappingsUrl, { data: body });
    if (!res.ok()) {
      throw new Error(`Failed to create WireMock success mapping. ${res.status()} - ${await res.text()}`);
    }
  });
}

/** Remove all WireMock mappings (stubs). Useful to clean after failure injection. */
export async function resetWireMockMappings(request: APIRequestContext) {
  const { mappings: mappingsUrl } = getWireMockAdmin();
  return test.step(`Reset WireMock mappings`, async () => {
    try {
      await request.delete(mappingsUrl);
    } catch (e) {
      console.warn(`Failed to reset WireMock mappings at ${mappingsUrl}. ${e}`);
    }
  });
}

export async function validateServiceNowRequestWithMockServer(request: APIRequestContext, validations: ServiceNowRequestValidations[]) {
  const wireMockUrl = getWireMockUrl();

  // Wait for 5 seconds
  await new Promise((resolve) => setTimeout(resolve, 5000));

  var response = await request.get(wireMockUrl);
  var body = await response.json() as WireMockResponse;

  return test.step(`Validate ServiceNow Requests with WireMock`, async () => {
    validations.forEach(validation => {
      const { caseNumber, messageType } = validation.validation;

      var request = body.requests.find(request => request.request.url.endsWith(caseNumber));

      if (!request)
      {
        throw new Error(`❌ Validation failed, request not found for: ${caseNumber}`);
      }

      console.info(`🚧 Validating ServiceNow Message Type ${messageType} was sent for ${caseNumber}`);

      switch (messageType) {
        case 1:
          expect(request.request.url == `/api/x_nhsd_intstation/nhs_integration/9c78f87c97912e10dd80f2df9153aff5/CohortCaseUpdate/${caseNumber}`).toBeTruthy();
          expect(JSON.parse(request.request.body)['needs_attention'] == true).toBeTruthy();
          break;
        case 2:
          expect(request.request.url == `/api/x_nhsd_intstation/nhs_integration/9c78f87c97912e10dd80f2df9153aff5/CohortCaseUpdate/${caseNumber}`).toBeTruthy();
          expect(JSON.parse(request.request.body)['needs_attention'] == false).toBeTruthy();
          break;
        case 3:
          expect(request.request.url == `/api/x_nhsd_intstation/nhs_integration/9c78f87c97912e10dd80f2df9153aff5/CohortCaseResolution/${caseNumber}`).toBeTruthy();
          break;
        default:
          throw new Error(`❌ Validation failed, unexpected message type: ${messageType}`);
      }

      console.info(`✅ Validation Complete - ServiceNow Message Type ${messageType} request was sent for ${caseNumber}`);
    });
  });
}

export async function processFileViaStorage(parquetFilePath: string) {
  return test.step(`Process file via Storage`, async () => {
    await uploadToLocalStorage(parquetFilePath);
  });
}


export async function sendParticipantViaSnowAPI(request: APIRequestContext,
  payload: ParticipantRecord ) {
  return test.step(`Process file via SnowAPI`, async () => {
    await receiveParticipantViaServiceNow(request,payload);
  });
}

export async function getTestData(scenarioFolderName: string
  , recordType: string = "ADD"
  , createParquetFile = false): Promise<[any, string[], string?, Record<string, any>?, string?]> { //TODO fix return type
  return test.step(`Creating Input Data from JSON file`, async () => {
    const folder = resolveScenarioFolder(scenarioFolderName);
    const testFilesPath = path.join(__dirname, `../`, `${config.e2eTestFilesPath}/${folder}/`);
    if (!fs.existsSync(testFilesPath)) {
      throw new Error(`Test files folder not found: ${testFilesPath} (from: ${scenarioFolderName})`);
    }
    const jsonFile = fs.readdirSync(testFilesPath).find(fileName => fileName.endsWith('.json') && fileName.startsWith(recordType));
    let parquetFile: string = "";
    if (createParquetFile) {
      console.info("Parquet file will be created from input JSON");
    } else {
      parquetFile = testFilesPath + jsonFile?.replace('.json', '.parquet');
    }

    const parsedData: InputData = JSON.parse(fs.readFileSync(testFilesPath + jsonFile, 'utf-8'));
    const inputParticipantRecord: Record<string, any> = parsedData.inputParticipantRecord;

    let nhsNumbers: string[] = [];
    if (parsedData.inputParticipantRecord.length !== parsedData.nhsNumbers.length) {
      console.info(`Input participant record length (${parsedData.inputParticipantRecord.length}) does not match NHS numbers length (${parsedData.nhsNumbers.length}). Using NHS numbers from parsed data nhsNumbers property to attempt multiply records when parquet file is created.`);
      nhsNumbers = parsedData.nhsNumbers;
    } else {
      nhsNumbers = parsedData.validations.map(item =>
        String(item.validations.NHSNumber || item.validations.NhsNumber)
      );
    }

    if (nhsNumbers.length === 0 || nhsNumbers[0] === '') {
      nhsNumbers = parsedData.nhsNumbers;
    }

    ensureNhsNumbersStartWith999(nhsNumbers);


    const uniqueNhsNumbers: string[] = Array.from(new Set(nhsNumbers));
    return [parsedData.validations, uniqueNhsNumbers, parquetFile, inputParticipantRecord, testFilesPath];
  });
}

export function getCheckInDataBaseValidations(scenarioFolderName: string
  , recordType: string = "ADD") {
  const folder = resolveScenarioFolder(scenarioFolderName);
  const testFilesPath = path.join(__dirname, `../`, `${config.e2eTestFilesPath}/${folder}/`);
  if (!fs.existsSync(testFilesPath)) {
    throw new Error(`Test files folder not found: ${testFilesPath} (from: ${scenarioFolderName})`);
  }
  const jsonFile = fs.readdirSync(testFilesPath).find(fileName => fileName.endsWith('.json') && fileName.startsWith(recordType));
  const parsedData: InputData = JSON.parse(fs.readFileSync(testFilesPath + jsonFile, 'utf-8'));
  return parsedData.validations;

}

export function getConsolidatedAllTestData(
  scenarioFolderName: string,
  recordType: string = "ADD"
) {
  const scenarioFolders = scenarioFolderName.split("|").map(name => name.trim());
  let testFilesPath: string = "";
  let allValidations: any[] = [];
  let allInputParticipantRecords: any[] = [];
  let allNhsNumbers: any[] = [];
  let allServiceNowRequestValidations: any[] = [];

  scenarioFolders.forEach(folder => {
    try {
      testFilesPath = path.join(__dirname, `../`, `${config.e2eTestFilesPath}/${folder}/`);
      const jsonFiles = fs.readdirSync(testFilesPath).filter(fileName => fileName.endsWith('.json') && fileName.startsWith(recordType));
      jsonFiles.forEach(jsonFile => {
        const srcPath = path.join(testFilesPath, jsonFile);
        try {
          const parsedData: InputData = JSON.parse(fs.readFileSync(srcPath, 'utf-8'));
          allValidations = allValidations.concat(parsedData.validations).filter(element => element !== undefined);
          allServiceNowRequestValidations = allServiceNowRequestValidations.concat(parsedData.serviceNowRequestValidations).filter(element => element !== undefined);
          allNhsNumbers = allNhsNumbers.concat(parsedData.nhsNumbers);
          if (Array.isArray(parsedData.inputParticipantRecord)) {
            allInputParticipantRecords = allInputParticipantRecords.concat(parsedData.inputParticipantRecord);
          } else if (parsedData.inputParticipantRecord) {
            allInputParticipantRecords.push(parsedData.inputParticipantRecord);
          }
        } catch (jsonErr) {
          console.error(`Failed to parse JSON file: ${srcPath}`, jsonErr);
        }
      });
    } catch (fsErr) {
      console.error(`Failed to read directory: ${testFilesPath}`, fsErr);
    }
  });

  return {
    validations: allValidations,
    inputParticipantRecords: allInputParticipantRecords,
    nhsNumbers: allNhsNumbers,
    serviceNowRequestValidations: allServiceNowRequestValidations,
    testFilesPath
  };
}

export async function getApiTestData(scenarioFolderName: string, recordType: string = "ADD"): Promise<any> { //TODO fix return type
  return test.step(`Creating Input Data from JSON file`, async () => {
    console.info('🏃‍♂️‍➡️\tRunning test For: ', scenarioFolderName);
    const folder = resolveScenarioFolder(scenarioFolderName);
    const testFilesPath = path.join(__dirname, `../`, `${config.apiTestFilesPath}/${folder}/`);
    if (!fs.existsSync(testFilesPath)) {
      throw new Error(`API test files folder not found: ${testFilesPath} (from: ${scenarioFolderName})`);
    }
    const jsonFile = fs.readdirSync(testFilesPath).find(fileName => fileName.endsWith('.json') && fileName.startsWith(recordType));
    const parsedData: InputData = JSON.parse(fs.readFileSync(testFilesPath + jsonFile, 'utf-8'));
    const inputParticipantRecord: Record<string, any> = parsedData.inputParticipantRecord;
    const nhsNumbers: string[] = parsedData.nhsNumbers;
    return [parsedData.validations, inputParticipantRecord, nhsNumbers, testFilesPath];
  });
}

export async function getApiQueryParams(scenarioFolderName: string, recordType: string): Promise<any> {
  return test.step(`Creating Input Data from JSON file`, async () => {
    console.info('🏃‍♂️‍➡️\tRunning test For: ', scenarioFolderName);
    const folder = resolveScenarioFolder(scenarioFolderName);
    const testFilesPath = path.join(__dirname, `../`, `${config.apiTestFilesPath}/${folder}/`);
    if (!fs.existsSync(testFilesPath)) {
      throw new Error(`API test files folder not found: ${testFilesPath} (from: ${scenarioFolderName})`);
    }
    const jsonFile = fs.readdirSync(testFilesPath).find(fileName => fileName.endsWith('.json') && fileName.startsWith(recordType));
    const parsedData: InputData = JSON.parse(fs.readFileSync(testFilesPath + jsonFile, 'utf-8'));
    return parsedData.queryParams;
  });
}

export async function verifyBlobExists(stepName: string, filePath: string) {
  await test.step(stepName, async () => {
    const expectedBlobName = path.basename(filePath);
    const outputFileExists = await checkBlobExists(expectedBlobName);
    expect(outputFileExists).toBe(true);
  });
}

export async function getLatestValidDatefromDatabase(
  request: APIRequestContext,
  getDataFromDatabse: (request: APIRequestContext) => Promise<any[]>,
  dateField: string,
  description = `Get latest valid database record date`
): Promise<string> {
  return await test.step(description, async () => {
    const result = await getDataFromDatabse(request);

    const validRecords = result.filter((r: Record<string, any>) =>
      r[dateField] && r[dateField].trim() !== '' && r[dateField] !== '0001-01-01T00:00:00'
    );

    if (validRecords.length === 0) {
      throw new Error(`No valid records found for expected date: "${dateField}"`);
    }

    validRecords.sort((a, b) =>
      new Date(b[dateField]).getTime() - new Date(a[dateField]).getTime()
    );

    const currentDate = validRecords[0][dateField].split('T')[0] + "T00:00:00";
    console.log(`Latest ${dateField}: ${currentDate}`);

    return currentDate;
  });
}
