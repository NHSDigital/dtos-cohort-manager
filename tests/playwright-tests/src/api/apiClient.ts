import { APIRequestContext, expect } from '@playwright/test';

class ApiClient {
  private request: APIRequestContext;

  constructor(request: APIRequestContext) {
    this.request = request;
  }

  async get(endpoint: string) {
    return this.request.get(endpoint);
  }

  async getResponseDetails(endpoint: string) {

    var responseJson = "NA";
    var length = 0;
    const response = await this.request.get(endpoint);

    if (response.status() != 204) {
      responseJson = await response.json();
      length = responseJson.length - 1;
    }
    const status = response.status();

    return {
      responseJson,
      length,
      status
    };
  }

  async post(endpoint: string, data: any) {
    return this.request.post(endpoint, { data });
  }

  async postWithRetry(endpoint: string, validRequestBody: any, retries: number = 8, waitTime: number = 5000): Promise<any> {

    for (let attempt = 1; attempt <= retries; attempt++) {
      try {
        const response = await this.request.post(`${endpoint}`, {
          data: validRequestBody
        });

        if (response.ok()) {
          return response;
        } else {
          console.warn(`API request failed for ${endpoint}, attempt ${attempt}, status: ${response.status()}`);
        }
      } catch (error) {
        console.warn(`API request failed for ${endpoint}, attempt ${attempt}`, error);
      }

      if (attempt < retries) {
        await new Promise(resolve => setTimeout(resolve, waitTime));
        waitTime += 5000;
      } else {
        throw new Error(`API request failed for ${endpoint} after ${retries} attempts`);
      }
    }
  }

  //TODO Merge with postWithRetry and control using assertion check flag
  async postWithRetryForAssertion(endpoint: string, validRequestBody: any, assertionKey: string, assertionValue: string, retries: number = 8, waitTime: number = 2000): Promise<any> {

    for (let attempt = 1; attempt <= retries; attempt++) {
      try {
        const response = await this.request.post(`${endpoint}`, {
          data: validRequestBody
        });

        if (response.ok()) {
          const responseBody = await response.json();
          try {
            expect(responseBody[assertionKey]).toBe(assertionValue);
            console.info(`Assertion passed for ${assertionKey} on attempt ${attempt}`);
            return response; // Return the response body if the assertion passes
          } catch (assertionError) {
            console.warn(`Assertion failed for ${assertionKey} on attempt ${attempt}`, assertionError);
          }
        } else {
          console.warn(`API request failed for ${endpoint}, attempt ${attempt}, status: ${response.status()}`);
        }
      } catch (error) {
        console.warn(`API request failed for ${endpoint}, attempt ${attempt}`, error);
      }

      if (attempt < retries) {
        await new Promise(resolve => setTimeout(resolve, waitTime));
        waitTime += 5000;
      } else {
        throw new Error(`API request failed for ${endpoint} after ${retries} attempts`);
      }
    }
  }

}

export default ApiClient;
