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
