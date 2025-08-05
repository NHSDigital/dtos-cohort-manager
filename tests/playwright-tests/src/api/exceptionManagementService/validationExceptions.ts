import { APIRequestContext } from '@playwright/test';
import * as apiClient from '../apiClient';
import { config } from '../../config/env';
import { ApiResponse } from '../core/types';

export const getValidationExceptions = (
  request: APIRequestContext,
  exceptionCategory?: number,
  nhsNumber?: string
): Promise<ApiResponse> => {
  const params: any = {};
  if (exceptionCategory !== undefined) params.exceptionCategory = exceptionCategory;
  if (nhsNumber !== undefined) params.nhsNumber = nhsNumber;

  return apiClient.get(
    request,
    `${config.endpointExceptionManagementDataService}${config.routeGetValidationExceptions}`,
    params
  );
};
