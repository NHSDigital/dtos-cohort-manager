import { APIResponse, expect } from "@playwright/test";
import { config } from "../../config/env";
import { fetchApiResponse, findMatchingObject, validateFields } from "../apiHelper";
import { ApiResponse } from "../core/types";

const NHS_NUMBER_KEY = config.nhsNumberKey;
const NHS_NUMBER_KEY_EXCEPTION_DEMOGRAPHIC = config.nhsNumberKeyExceptionDemographic;

export async function validateApiResponse(validationJson: any, request: any): Promise<{ status: boolean; errorTrace?: any }> {
    let endpoint = "";
    let errorTrace: any = undefined;
    let status = false;
    try {
        for (const apiValidation of validationJson) {
            endpoint = apiValidation.validations.apiEndpoint;
            var res = await pollAPI(endpoint, apiValidation, request);
            return res;
            
        }
    } 
    catch (error) {
        const errorMsg = `Endpoint: ${endpoint}, Error: ${error instanceof Error ? error.stack || error.message : error}`;
        errorTrace = errorMsg;
        console.error(errorMsg);
    }
    return {status, errorTrace };
}

async function handleOKResponse(apiValidation: any, endpoint: string, response: any ) : Promise<boolean>{
    expect(response.ok()).toBeTruthy();
    const responseBody = await response.json();
    expect(Array.isArray(responseBody)).toBeTruthy();
    const { matchingObject, nhsNumber, matchingObjects } = await findMatchingObject(endpoint, responseBody, apiValidation);
    
    console.info(`Validating fields using 🅰️\t🅿️\tℹ️\t ${endpoint}`);
    console.info(`From Response ${JSON.stringify(matchingObject, null, 2)}`);
    return await validateFields(apiValidation, matchingObject, nhsNumber, matchingObjects);
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
        return await validateFields(apiValidation, null, nhsNumber, []);
    } else {
        // 204 is unexpected, log error and return false to trigger retry
        console.warn(`Status 204: No data found in the table using endpoint ${endpoint}`);
        return false;
    }
}


async function pollAPI(endpoint: string, apiValidation: any, request: any): Promise<{  apiResponse: APIResponse, status: boolean}> {
    let apiResponse: APIResponse | null = null;
    let i = 0;

    let maxNumberOfRetries = config.maxNumberOfRetries;
    let maxTimeBetweenRequests = config.maxTimeBetweenRequests;
    let status = false;
    console.info(`now trying request for ${maxNumberOfRetries} retries`);
       while (i < Number(maxNumberOfRetries)) {
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
            if(status) {
                break;
            }
            i++;

            console.info(`http response completed ${i}/${maxNumberOfRetries} of number of retries`);
            await new Promise(res => setTimeout(res, maxTimeBetweenRequests));
       }
       

    if (!apiResponse) {
        throw new Error("apiResponse was never assigned");
    }

    return {apiResponse, status};
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
