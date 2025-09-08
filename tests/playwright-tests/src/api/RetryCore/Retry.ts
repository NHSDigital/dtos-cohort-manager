import { APIResponse, expect } from "@playwright/test";
import { config } from "../../config/env";
import { ApiResponse } from "../core/types";
import { fetchApiResponse, findMatchingObject, validateFields } from "../apiHelper";





const PARTICIPANT_DEMOGRAPHIC_SERVICE = config.participantDemographicDataService;
const NHS_NUMBER_KEY = config.nhsNumberKey;
const NHS_NUMBER_KEY_EXCEPTION_DEMOGRAPHIC = config.nhsNumberKeyExceptionDemographic;
const IGNORE_VALIDATION_KEY = config.ignoreValidationKey;

export async function validateApiResponse(validationJson: any, request: any): Promise<{ status: boolean; errorTrace?: any }> {
    let endpoint = "";
    let errorTrace: any = undefined;
    let status = false;

    let resultFromPolling: {  apiResponse: APIResponse, status: boolean, errorTrace: any} | null = null;

    for (const apiValidation of validationJson) {
        endpoint = apiValidation.validations.apiEndpoint;
        resultFromPolling = await pollAPI(endpoint, apiValidation, request);    
    }

    if(resultFromPolling) {
        status = resultFromPolling.status
        errorTrace = resultFromPolling.errorTrace
    }
    return {status, errorTrace };
}


async function pollAPI(endpoint: string, apiValidation: any, request: any): Promise<{  apiResponse: APIResponse, status: boolean, errorTrace: any}> {
    let apiResponse: APIResponse | null = null;
    let i = 0;
    let errorTrace: any = undefined;

    let maxNumberOfRetries = config.maxNumberOfRetries;
    let maxTimeBetweenRequests = config.maxTimeBetweenRequests;
    let status = false;
    console.info(`now trying request for ${maxNumberOfRetries} retries`);
       while (i < Number(maxNumberOfRetries)) {
        try{
                apiResponse =  await fetchApiResponse(endpoint, request);
                switch(apiResponse.status()) {
                    case 204:
                        console.info("now handling no content response");
                        const expectedCount = apiValidation.validations.expectedCount;
                        status = await HandleNoContentResponse(expectedCount, apiValidation, endpoint);
                        break;
                    case 200: 
                        console.info("now handling OK response");
                        status = await handleOKResponse(apiValidation, endpoint, apiResponse);
                        break;
                    default: 
                        console.error("there was an error when handling response from ");
                        break;
                }
                console.log("api status code is: ", apiResponse.status());
                if(status) {
                    break;
                }
                i++;

                console.info(`http response completed ${i}/${maxNumberOfRetries} of number of retries`);
                await new Promise(res => setTimeout(res, maxTimeBetweenRequests));            
        
            } catch (error) {
                const errorMsg = `Endpoint: ${endpoint}, Status: ${apiResponse?.status?.()}, Error: ${error instanceof Error ? error.stack || error.message : error}`;
                errorTrace = errorMsg;
                if (apiResponse?.status?.() === 204) {
                    console.info(`ℹ️\t Status 204: No data found in the table using endpoint ${endpoint}`);
                }
            }
       }
       

        if (!apiResponse) {
            throw new Error("apiResponse was never assigned");
        }

        return {apiResponse, status, errorTrace};
}

async function HandleNoContentResponse(expectedCount: number, apiValidation: any, endpoint: string): Promise<boolean> {
    if (expectedCount !== undefined && Number(expectedCount) === 0) {
        console.info(`✅ Status 204: Expected 0 records for endpoint ${endpoint}`);

        // Get NHS number for validation
        const nhsNumber = apiValidation.validations.NHSNumber ||
                        apiValidation.validations.NhsNumber ||
                        apiValidation.validations[NHS_NUMBER_KEY] ||
                        apiValidation.validations[NHS_NUMBER_KEY_EXCEPTION_DEMOGRAPHIC];

        console.info(`Validating fields using 🅰️\t🅿️\tℹ️\t ${endpoint}`);
        console.info(`From Response: null (204 No Content - 0 records as expected)`);
        let status = await validateFields(apiValidation, null, nhsNumber, []);
        return status;
    } else {
        // 204 is unexpected, log error and return false to trigger retry
        console.warn(`Status 204: No data found in the table using endpoint ${endpoint}`);
        return false;
    }
}

async function handleOKResponse(apiValidation: any, endpoint: string, response: any ) : Promise<boolean>{
   // Normal response handling (200, etc.)
    expect(response.ok()).toBeTruthy();
    const responseBody = await response.json();
    expect(Array.isArray(responseBody)).toBeTruthy();
    const { matchingObject, nhsNumber, matchingObjects } = await findMatchingObject(endpoint, responseBody, apiValidation);
    console.info(`Validating fields using 🅰️\t🅿️\tℹ️\t ${endpoint}`);
    console.info(`From Response ${JSON.stringify(matchingObject, null, 2)}`);
    let status = await validateFields(apiValidation, matchingObject, nhsNumber, matchingObjects);

    return status;
}





export async function pollApiForOKResponse(httpRequest: () => Promise<ApiResponse>): Promise<ApiResponse>{
    let apiResponse: ApiResponse | null = null;
    let i = 0;
    let maxNumberOfRetries = config.maxNumberOfRetries;
    let maxTimeBetweenRequests = config.maxTimeBetweenRequests;

    console.info(`now trying request for ${maxNumberOfRetries} retries`);
    while (i < Number(maxNumberOfRetries)) {
        try {
            apiResponse =  await httpRequest();
            if (apiResponse.status == 200) {
                console.info("200 response found") 
                break;
            }
        }
        catch(exception) {
            console.error("Error reading request body:", exception);
        }
        i++;

        console.info(`http response completed ${i}/${maxNumberOfRetries} of number of retries`);
        await new Promise(res => setTimeout(res, maxTimeBetweenRequests));
    }

    if (!apiResponse) {
        throw new Error("apiResponse was never assigned");
    }
    return apiResponse;
};
