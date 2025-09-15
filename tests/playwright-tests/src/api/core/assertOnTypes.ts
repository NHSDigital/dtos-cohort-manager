import { expect } from "@playwright/test";

export function  assertOnCounts(matchingObject: any, nhsNumber: any, matchingObjects: any, fieldName: string, expectedValue: unknown) {
 console.info(`🚧 Count check with expected value ${expectedValue} for NHS Number ${nhsNumber}`);

    let actualCount = 0;
    if (matchingObjects && Array.isArray(matchingObjects)) {
      actualCount = matchingObjects.length;
    } else if (matchingObjects === null || matchingObjects === undefined) {
      actualCount = 0;
      console.warn(`⚠️ matchingObjects is ${matchingObjects === null ? 'null' : 'undefined'} for NHS Number ${nhsNumber}`);
    } else {
      actualCount = 1;
      console.warn(`⚠️ matchingObjects is not an array for NHS Number ${nhsNumber}, treating as single object`);
    }

    console.info(`📊 Actual count: ${actualCount}, Expected count: ${expectedValue} for NHS Number ${nhsNumber}`);

    const expectedCount = Number(expectedValue);

    if (isNaN(expectedCount)) {
      throw new Error(`❌ expectedCount value '${expectedValue}' is not a valid number for NHS Number ${nhsNumber}`);
    }

    // Perform the assertion
    try {
      expect(actualCount).toBe(expectedCount);
      console.info(`✅ Count check completed for field ${fieldName} with value ${expectedValue} for NHS Number ${nhsNumber}`);
    } catch (error) {
      console.error(`❌ Count check failed for NHS Number ${nhsNumber}: Expected ${expectedCount}, but got ${actualCount}`);
      throw error;
    }
}


export function assertOnNhsNumber(expectedValue: unknown , nhsNumber: string) 
{  
  console.info(`🚧 Validating NHS Number field for 204 response`);

  // For 204 responses, validate that we searched for the correct NHS number
  const expectedNhsNumber = Number(expectedValue);
  const actualNhsNumber = Number(nhsNumber);

  try {
    expect(actualNhsNumber).toBe(expectedNhsNumber);
    console.info(`✅ NHS Number validation completed: searched for ${actualNhsNumber}, expected ${expectedNhsNumber}`);
  } catch (error) {
    console.error(`❌ NHS Number validation failed: searched for ${actualNhsNumber}, expected ${expectedNhsNumber}`);
    throw error;
  }
}

export function assertOnRecordDateTimes(fieldName: string, expectedValue: unknown, nhsNumber: string,  matchingObject: any ) {    
       console.info(`🚧 Validating timestamp field ${fieldName} for NHS Number ${nhsNumber}`);

      if (!matchingObject && expectedValue !== null && expectedValue !== undefined) {
        throw new Error(`❌ No matching object found for NHS Number ${nhsNumber} but expected to validate field ${fieldName}`);
      }

      if (!matchingObject && (expectedValue === null || expectedValue === undefined)) {
        console.info(`ℹ️ Skipping validation for ${fieldName} as no matching object found and no expected value for NHS Number ${nhsNumber}`);
        return;
      }

      expect(matchingObject).toHaveProperty(fieldName);
      const actualValue = matchingObject[fieldName];

      if (typeof expectedValue === 'string' && expectedValue.startsWith('PATTERN:')) {
        const pattern = expectedValue.substring('PATTERN:'.length);
        console.info(`Validating timestamp against pattern: ${pattern}`);

        const formatMatch = validateTimestampFormat(actualValue, pattern);

        if (formatMatch) {
          console.info(`✅ Timestamp matches pattern for ${fieldName}`);
        } else {
          console.error(`❌ Timestamp doesn't match pattern for ${fieldName}`);
          expect(formatMatch).toBe(true);
        }
      } else {
        if (expectedValue === actualValue) {
          console.info(`✅ Timestamp exact match for ${fieldName}`);
        } else {
          try {
            const expectedDate = new Date(expectedValue as string);
            const actualDate = new Date(actualValue);

            const expectedTimeWithoutMs = new Date(expectedDate);
            expectedTimeWithoutMs.setMilliseconds(0);
            const actualTimeWithoutMs = new Date(actualDate);
            actualTimeWithoutMs.setMilliseconds(0);

            if (expectedTimeWithoutMs.getTime() === actualTimeWithoutMs.getTime()) {
              console.info(`✅ Timestamp matches (ignoring milliseconds) for ${fieldName}`);
            } else {
              const timeDiff = Math.abs(expectedDate.getTime() - actualDate.getTime());
              const oneMinute = 60 * 1000;

              if (timeDiff <= oneMinute) {
                console.info(`✅ Timestamp within acceptable range (±1 minute) for ${fieldName}`);
              } else {
                expect(actualValue).toBe(expectedValue);
              }
            }
          } catch (e) {
            console.error(`Error validating timestamp: ${e}`);
            expect(actualValue).toBe(expectedValue);
          }
        }
      }

      console.info(`✅ Validation completed for timestamp field ${fieldName} for NHS Number ${nhsNumber}`);
}




export function MatchOnRuleDescriptionDynamic(matchingObject: any, nhsNumber: string) {
    const actualValue = matchingObject['RuleDescription'];
      console.info(`Actual RuleDescription: "${actualValue}"`);

      // Regex based on message requirement
      const dynamicPattern = /Unable to add to cohort distribution\. As participant \d+ has triggered a validation exception/;

      try {
        expect(actualValue).toMatch(dynamicPattern);
        console.info(`✅ Dynamic message validation passed for NHS Number ${nhsNumber}`);
      } catch (error) {
        console.info(`❌ Dynamic message validation failed!`);
        throw error;
      }
}

export function MatchDynamicType(matchingObject: any, nhsNumber: string, expectedValue: any, fieldName: string) {
        console.info(`🚧 Validating field ${fieldName} with expected value ${expectedValue} for NHS Number ${nhsNumber}`);

      if (!matchingObject && expectedValue !== null && expectedValue !== undefined) {
        throw new Error(`❌ No matching object found for NHS Number ${nhsNumber} but expected to validate field ${fieldName}`);
      }

      if (!matchingObject && (expectedValue === null || expectedValue === undefined)) {
        console.info(`ℹ️ Skipping validation for ${fieldName} as no matching object found and no expected value for NHS Number ${nhsNumber}`);
        return;
      }

      expect(matchingObject).toHaveProperty(fieldName);
      expect(matchingObject[fieldName]).toBe(expectedValue);
      console.info(`✅ Validation completed for field ${fieldName} with value ${expectedValue} for NHS Number ${nhsNumber}`);
    }


function validateTimestampFormat(timestamp: string, pattern: string): boolean {
  if (!timestamp) {
    return false;
  }
  console.info(`Actual timestamp: ${timestamp}`);

  if (pattern === 'yyyy-MM-ddTHH:mm:ss' || pattern === 'yyyy-MM-ddTHH:mm:ss.SSS') {
    return /^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(\.\d+)?$/.test(timestamp);
  }
  else if (pattern === 'yyyy-MM-dd') {
    return /^\d{4}-\d{2}-\d{2}$/.test(timestamp);
  }
  else {
    return !isNaN(new Date(timestamp).getTime());
  }
}


