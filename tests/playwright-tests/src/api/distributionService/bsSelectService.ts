import { APIRequestContext, APIResponse } from '@playwright/test';
import * as apiClient from '../apiClient';
import { config } from '../../config/env';
import { ApiResponse, QueryParams } from '../core/types';
import { ParticipantRecord } from '../../interface/InputData';

export const getRecordsFromBsSelectRetrieveCohort = (
  request: APIRequestContext,
  params: QueryParams
): Promise<ApiResponse> => {
  const promise = apiClient.get(request, `${config.endpointBsSelectRetrieveCohortDistributionData}${config.routeBsSelectRetrieveCohortDistributionData}`, params);
  return new Promise<ApiResponse>((resolve, reject) => {
    promise.then(
      result => setTimeout(() => resolve(result), config.apiWaitTime),
      error => setTimeout(() => reject(new Error(typeof error === 'string' ? error : JSON.stringify(error))), config.apiWaitTime)
    );
  });
};


export const getRecordsFromBsSelectRetrieveAudit = (
  request: APIRequestContext,
  params?: QueryParams
): Promise<ApiResponse> => {
  return apiClient.get(request, `${config.endpointBsSelectRetrieveCohortRequestAudit}${config.routeBsSelectRetrieveCohortRequestAudit}`, params);
};

export const getRecordsFromParticipantManagementService = (
  request: APIRequestContext
): Promise<ApiResponse> => {
  return apiClient.get(request, `${config.endpointParticipantManagementDataService}api/${config.participantManagementService}`);
};

export const getRecordsFromParticipantDemographicService = async (
  request: APIRequestContext
): Promise<ApiResponse> => {
  return await apiClient.get(request, `${config.endpointParticipantDemographicDataService}api/${config.participantDemographicDataService}`);
};

export const getRecordsFromExceptionManagementService = (
  request: APIRequestContext
): Promise<ApiResponse> => {
  return apiClient.get(request, `${config.endpointExceptionManagementDataService}api/${config.exceptionManagementService}`);
};

export const getRecordsFromNemsSubscription = (
  request: APIRequestContext,
  nhsNumbers: string
): Promise<ApiResponse> => {
  return apiClient.get(request, `${config.SubToNems}${config.CheckNemsSubPath}?nhsNumber=${nhsNumbers}`);
};

export function extractSubscriptionID(response: ApiResponse): string | null {
  // Prefer textual body if provided; fall back to serialised data
  const raw = typeof (response as any).text === 'string' && (response as any).text.length > 0
    ? (response as any).text
    : JSON.stringify((response as any).data ?? '');

  const cleaned = raw.replace(/[\r\n\t]+/g, ' ').replace(/\s+/g, ' ').trim();

  // 1) Try JSON shape: { subscriptionId: "..." } or similar
  try {
    const asJson = JSON.parse(cleaned);
    const cand = (asJson?.subscriptionId || asJson?.SubscriptionId || asJson?.subscriptionID || asJson?.id || null);
    if (typeof cand === 'string' && cand.length > 0) return cand;
  } catch { /* not JSON */ }

  // 2) Try explicit label formats e.g. "Subscription ID: <id>"
  let m = cleaned.match(/Subscription\s*ID\s*:\s*([A-Za-z0-9_\-]{8,})/i);
  if (m) return m[1];

  // 2b) Fallback: split on label and take first token
  if (/Subscription\s*ID\s*:/i.test(cleaned)) {
    const after = cleaned.split(/Subscription\s*ID\s*:/i)[1] ?? '';
    const token = after.trim().split(/\s+/)[0];
    if (/^(STUB_[a-f0-9]{32}|[a-f0-9]{32})$/i.test(token)) return token;
  }

  // 3) Accept plain IDs: STUB_<32-hex> or bare 32-hex anywhere in the body
  m = cleaned.match(/STUB_[a-f0-9]{32}/i);
  if (m) return m[0];
  m = cleaned.match(/\b[a-f0-9]{32}\b/i);
  if (m) return m[0];

  return null;
}

export const deleteParticipant = (
  request: APIRequestContext,
  payload: {
    NhsNumber: string;
    FamilyName: string;
    DateOfBirth: string;
  }
): Promise<ApiResponse> => {
  const endpoint = `${config.endpointBsSelectDeleteParticipant}${config.routeBsSelectDeleteParticipant}`;
  return apiClient.post(request, endpoint, payload);
};

export const BlockParticipant = (
  request: APIRequestContext,
  payload: {
    NhsNumber: string;
    FamilyName: string;
    DateOfBirth: string;
  }
): Promise<ApiResponse> => {
  const endpoint = `${config.endpointBsSelectUpdateBlockFlag}${config.routeBsSelectBlockParticipant}`;
  return apiClient.postWithQuery(request, endpoint, payload);
};


export const UnblockParticipant = (
  request: APIRequestContext,
  payload: {
    NhsNumber: string;
    FamilyName: string;
    DateOfBirth: string;
  }
): Promise<ApiResponse> => {
  const endpoint = `${config.endpointBsSelectUpdateBlockFlag}${config.routeBsSelectUnblockParticipant}`;
  return apiClient.postWithQuery(request, endpoint, payload);
};

export const receiveParticipantViaServiceNow = async(
  request: APIRequestContext,
  payload: ParticipantRecord
): Promise<ApiResponse> => {
  const endpoint = `${config.endpointSerNowReceiveParticipant}${config.routeSerNowReceiveParticipant}`;
  return await apiClient.post(request, endpoint, payload);
};

export const invalidServiceNowEndpoint = (
  request: APIRequestContext,
  payload: ParticipantRecord
): Promise<ApiResponse> => {
  const endpoint = `${config.invalidEndpointSerNow}${config.invalidRouteSerNowEndpoint}`;
  return apiClient.post(request, endpoint, payload);
};

export async function retry<T>(
  fn: () => Promise<T>,
  validate: (result: T) => boolean,
  options?: {
    retries?: number;
    delayMs?: number;
    throwLastError?: boolean;
  }
): Promise<T> {
  const { retries = 5, delayMs = 2000, throwLastError = true } = options || {};
  let lastError: any;
  for (let attempt = 1; attempt <= retries; attempt++) {
    try {
      const result = await fn();
      if (validate(result)) {
        return result;
      }
    } catch (err) {
      lastError = err;
    }
    if (attempt < retries) {
      await new Promise(res => setTimeout(res, delayMs));
    }
  }
  if (throwLastError && lastError) {
    throw lastError;
  }
  throw new Error(`Retry validation failed after ${retries} attempts`);
}

export const getRecordsFromParticipantDemographicDataService = async (
  request: APIRequestContext
): Promise<ApiResponse> => {
  const response = await apiClient.get(
    request,
    `${config.endpointParticipantDemographicDataService}api/${config.participantDemographicDataService}`
  );
  return response;
};

export const getRecordsFromParticipantManagementDataService = async (
  request: APIRequestContext
): Promise<ApiResponse> => {
  const response = await apiClient.get(
    request,
    `${config.endpointParticipantManagementDataService}api/${config.participantManagementService}`
  );
  return response;
};
