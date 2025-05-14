
import { expect } from '@playwright/test';
import { ApiResponse, ResponseValidator } from '../api/core/types';

export const expectStatus = <T>(expectedStatus: number): ResponseValidator<ApiResponse<T>> =>
  async (response: ApiResponse<T>): Promise<void> => {
    expect(response.status).toBe(expectedStatus);
    console.info(`✅\t Verified Status code to be : ${response.status}`);
  };

export const validateResponseByStatus = <T>(): ResponseValidator<ApiResponse<T>> =>
  async (response: ApiResponse<T>): Promise<void> => {
    if (response.status === 200) {
      expect(response.data).toBeDefined();
      expect(Array.isArray(response.data)).toBe(true);
    }

    if (response.status === 204) {
      expect(response.data).toBe(null);
    }

  };

export const composeValidators = <T>(...validators: ResponseValidator<T>[]): ResponseValidator<T> =>
  async (response: T): Promise<void> => {
    for (const validator of validators) {
      await validator(response);
    }
  };
