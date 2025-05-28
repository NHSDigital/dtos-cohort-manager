import { APIRequestContext, expect } from '@playwright/test';
import * as apiClient from '../apiClient';
import { config } from '../../config/env';
import { ApiResponse } from '../core/types';

export const getRecordsFromExceptionService = async (
  request: APIRequestContext
): Promise<ApiResponse> => {

  const url = `${config.endpointExceptionManagementDataService}api/${config.exceptionManagementService}`;
  const response = await apiClient.get(request, url);

  return response;
};


