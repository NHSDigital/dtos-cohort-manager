import { APIRequestContext } from '@playwright/test';
import * as apiClient from '../apiClient';
import { config } from '../../config/env';
import { ApiResponse, QueryParams } from '../core/types';


export const getRecords = (
  request: APIRequestContext,
  params: QueryParams
): Promise<ApiResponse> => {
  return apiClient.get(request, `${config.endpointBsSelectRetrieveCohortDistributionData}${config.routeBsSelectRetrieveCohortDistributionData}`,params);
};
