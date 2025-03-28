import { APIRequestContext } from '@playwright/test';

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
}

export default ApiClient;
