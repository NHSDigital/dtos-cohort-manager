
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
  console.info(`✅\t Log API response for GET ${url} with params ${JSON.stringify(params)}; received Response Status : ${response.status()} and Body ${JSON.stringify(response.body())}`);
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

export const post = async <T = any>(
  request: APIRequestContext,
  endpoint: string,
  data: any,
  headers?: Headers
): Promise<ApiResponse<T>> => {
  const response = await request.post(endpoint, { data, headers });
  console.info(`✅\t Log API response for POST ${endpoint} with Body ${JSON.stringify(data)}; received Response Status : ${response.status()} and Body ${JSON.stringify(response.body())}`);
  return parseResponse<T>(response);
};
