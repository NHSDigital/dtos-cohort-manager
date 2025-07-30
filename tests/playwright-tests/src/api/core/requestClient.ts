
import { ApiResponse, QueryParams } from './types';

export const buildUrl = (endpoint: string, params?: QueryParams): string => {
  if (!params || Object.keys(params).length === 0) {
    return endpoint;
  }

  const searchParams = new URLSearchParams();
  Object.entries(params).forEach(([key, value]) => {
    searchParams.append(key, String(value));
  });

  return `${endpoint}?${searchParams.toString()}`;
};

export const parseResponse = async <T>(response: any): Promise<ApiResponse<T>> => {
  const status = response.status();
  const headers = response.headers();

  let data = null;
  if (status === 200) {
    try {
      data = await response.json();
      console.info(`✅\t Log API response data: ${JSON.stringify(data)}`);
    }

    catch {
      data = await response.text();
      console.info(`✅\t Log API response text: ${data}`);
    }
  }

  return {
    status,
    data,
    headers
  };
};
