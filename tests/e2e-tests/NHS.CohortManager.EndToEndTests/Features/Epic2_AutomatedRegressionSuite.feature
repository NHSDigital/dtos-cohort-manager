Feature: Epic2_AutomatedRegressionSuite
DTOSS Regression TEST PACK.

  Background:
    Given the database is cleaned of all records for NHS Numbers: 1111110662, 2222211794,2312514176,2612314172,2612514171
    And the application is properly configured

  @DTOSS-5104 @Regression
  Scenario: 01.Verify eligibility flag is set to true for add participant
    Given file <AddFileName> exists in the configured location for "Add" with NHS numbers : <NhsNumbers>
    And the file is uploaded to the Blob Storage container
    And the NHS numbers in the database should match the file data
    Then the Participant Management table should contain an eligibility flag set to true
      | FieldName        | FieldValue |
      | ELIGIBILITY_FLAG |          1 |

    Examples:
      | AddFileName                                       | NhsNumbers |
      | ADD_1B8F53_-_CAAS_BREAST_SCREENING_COHORT.parquet | 2612514171 |
