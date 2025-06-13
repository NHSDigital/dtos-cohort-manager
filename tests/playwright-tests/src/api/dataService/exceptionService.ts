import { APIRequestContext } from '@playwright/test';
import * as apiClient from '../apiClient';
import { config } from '../../config/env';
import { ApiResponse } from '../core/types';

export const getRecordsFromExceptionService = (
  request: APIRequestContext
): Promise<ApiResponse> => {
  return apiClient.get(request, `${config.endpointExceptionManagementDataService}api/${config.exceptionManagementService}`);
};
