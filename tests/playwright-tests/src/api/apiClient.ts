
import { APIRequestContext } from '@playwright/test';
import { ApiResponse, Headers, QueryParams } from './core/types';
import { buildUrl, parseResponse } from './core/requestClient';


export const get = async <T = any>(
  request: APIRequestContext,
  endpoint: string,
  params?: QueryParams,
  headers?: Headers
): Promise<ApiResponse<T>> => {
  const url = buildUrl(endpoint, params);
  const response = await request.get(url, { headers });
  return parseResponse<T>(response);
};

export const post = async <T = any>(
  request: APIRequestContext,
  endpoint: string,
  data: any,
  headers?: Headers
): Promise<ApiResponse<T>> => {
  const response = await request.post(endpoint, { data, headers });
  return parseResponse<T>(response);
};

export const postWithQuery = async <T = any>(
  request: APIRequestContext,
  endpoint: string,
  params?: QueryParams,
  headers?: Headers
): Promise<ApiResponse<T>> => {
  const url = buildUrl(endpoint, params);
  const response = await request.post(url, { headers });
  return parseResponse<T>(response);
};
