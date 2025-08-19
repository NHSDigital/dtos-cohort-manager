import { APIRequestContext } from '@playwright/test';
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

export const receiveParticipantViaServiceNow = (
  request: APIRequestContext,
  payload: ParticipantRecord
): Promise<ApiResponse> => {
  const endpoint = `${config.endpointSerNowReceiveParticipant}${config.routeSerNowReceiveParticipant}`;
  return apiClient.post(request, endpoint, payload);
};

export const invalidServiceNowEndpoint = (
  request: APIRequestContext,
  payload: ParticipantRecord
): Promise<ApiResponse> => {
  const endpoint = `${config.invalidEndpointSerNow}${config.invalidRouteSerNowEndpoint}`;
  return apiClient.post(request, endpoint, payload);
};

export const sendHttpPOSTCall = 
  async (
  url: string,
  body: string
): Promise<Response> => 
{
  const response = await fetch(url, {
    method: "POST",
    headers: {
      "Content-Type": "application/json",
    },
    body: body 
  });

  return response;     
}

export const sendHttpGet = 
  async (
    url: string
): Promise<Response> => 
{
  const response = await fetch(url);
  return response;
}

