import { APIRequestContext } from '@playwright/test';
export type Headers = Record<string, string>;
export type QueryParams = Record<string, string | number | boolean>;
export type ResponseValidator<T> = (response: T) => Promise<void>;
export type RequestFunc = (request: APIRequestContext, ...args: any[]) => Promise<any>;

export interface ApiResponse<T = any> {
  status: number;
  data: T | null;
  headers: Record<string, string>;
  text: string;
}
