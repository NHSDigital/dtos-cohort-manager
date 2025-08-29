interface WireMockRequestDetails {
  url: string;
  body: string;
}

interface WireMockRequest {
  request: WireMockRequestDetails;
}

export interface WireMockResponse {
  requests: WireMockRequest[];
}
