import { test, expect } from '../../fixtures/test-fixtures';
import { cleanupDatabaseFromAPI, validateSqlDatabaseFromAPI } from "../../steps/steps";

function getToday(): string {
  const today = new Date();
  return today.toISOString().split("T")[0];
}

test.describe('@DTOSS-5396-01 - Transformation - Invalid Flag triggers Reason for Removal logic', () => {
  test('should apply correct transformations when invalidFlag is true', async ({ request, testData }) => {
    const validations = testData.checkInDatabase;
    const nhsNumbers = testData.nhsNumbers;

    await cleanupDatabaseFromAPI(request, nhsNumbers);

    await validateSqlDatabaseFromAPI(request, validations);

    const record = validations.find((v: { validations: { NHSNumber: string } }) =>
      v.validations.NHSNumber === "9991234567"
    )?.validations;

    expect(record).toBeTruthy();

    expect(record!.PrimaryCareProvider).toBeNull();
    expect(record!.ReasonForRemoval).toBe("ORR");
    expect(record!.ReasonForRemovalBusinessEffectiveDate).toBe(getToday());
  });
});
