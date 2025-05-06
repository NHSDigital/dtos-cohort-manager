import { APIRequestContext } from '@playwright/test';
import * as apiClient from '../apiClient';
import { config } from '../../config/env';
import { ApiResponse } from '../core/types';


export const getRecordsFromCohortDistributionService = (
  request: APIRequestContext
): Promise<ApiResponse> => {
  return apiClient.get(request, `${config.endpointCohortDistributionDataService}${config.cohortDistributionService}`);
};

export async function filterUsing3PointCheck(records: any, participantRecord: any ) {
  const matchingParticipant = records.filter((item: Record<string, any>) =>
    item["NHSNumber"] == participantRecord.nhsNumber &&
    item["DateOfBirth"] == participantRecord.dateOfBirth &&
    item["LastName"] == participantRecord.lastName
  );
  return matchingParticipant;
}


