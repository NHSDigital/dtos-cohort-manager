import { APIRequestContext } from '@playwright/test';
import * as apiClient from '../apiClient';
import { config } from '../../config/env';
import { ApiResponse } from '../core/types';

export const getRecordsFromExceptionService = (
  request: APIRequestContext
): Promise<ApiResponse> => {
  return apiClient.get(request, `${config.endpointExceptionManagementDataService}api/${config.exceptionManagementService}`);
};

export const getValidationExceptions = (
  request: APIRequestContext,
  params?: {
    exceptionId?: number;
    lastId?: number;
    exceptionStatus?: number;
    sortOrder?: number;
    exceptionCategory?: number;
  }
): Promise<any> => {
  return apiClient.get(
    request,
    `${config.endpointBsSelectGetValidationExceptions}${config.routeGetValidationExceptions}`,
    params
  );
};
