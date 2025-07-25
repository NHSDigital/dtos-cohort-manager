import { APIRequestContext } from '@playwright/test';
import * as apiClient from '../apiClient';
import { config } from '../../config/env';
import { ApiResponse, QueryParams } from '../core/types';

export const getRecordsFromBsSelectRetrieveCohort = (
  request: APIRequestContext,
  params: QueryParams
): Promise<ApiResponse> => {
  return apiClient.get(request, `${config.endpointBsSelectRetrieveCohortDistributionData}${config.routeBsSelectRetrieveCohortDistributionData}`, params);
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
  payload: {
    number: string;
    u_case_variable_data: {
      nhs_number: string;
      forename_: string;
      surname_family_name: string;
      date_of_birth: string;
      enter_dummy_gp_code: string;
      BSO_code: string;
      reason_for_adding: string;
    }
  }
): Promise<ApiResponse> => {
  const endpoint = `${config.endpointSerNowReceiveParticipant}${config.routeSerNowReceiveParticipant}`;
  return apiClient.post(request, endpoint, payload);
};
