Feature: Epic2_AutomatedRegressionSuite
DTOSS Regression TEST PACK.

  Background:
    Given the database is cleaned of all records for NHS Numbers: 1111110662, 2222211794,2312514176,2612314172,2612514171
    And the application is properly configured

  @DTOSS-5104 @Regression
  Scenario: Verify eligibility flag is set to true for add participant
    Given file <AddFileName> exists in the configured location for "Add" with NHS numbers : <NhsNumbers>
    And the file is uploaded to the Blob Storage container
    And the NHS numbers in the database should match the file data
    Then the Participant_Management table should contain an eligibility flag set to true
      | FieldName        | FieldValue |
      | ELIGIBILITY_FLAG |          1 |

    Examples:
      | AddFileName                                       | NhsNumbers |
      | ADD_1B8F53_-_CAAS_BREAST_SCREENING_COHORT.parquet | 2612514171 |

  @DTOSS-5104 @Regression
  Scenario: Verify eligibility flag is set to true for AMENDED records
    Given file <AddFileName> exists in the configured location for "Add" with NHS numbers : <NhsNumbers>
    And the file is uploaded to the Blob Storage container
    And the NHS numbers in the database should match the file data
    And file <AmendedFileName> exists in the configured location for "Amended" with NHS numbers : <NhsNumbers>
    When the file is uploaded to the Blob Storage container
    Then verify the NhsNumbers in Participant_Management table should match <RecordType>
    Then the Participant_Management table should contain an eligibility flag set to true
      | FieldName        | FieldValue |
      | ELIGIBILITY_FLAG |          1 |

    Examples:
      | AddFileName                                        | AmendedFileName                                        | NhsNumbers | RecordType |
      | ADD1_1B8F53_-_CAAS_BREAST_SCREENING_COHORT.parquet | AMENDED1_1B8F53_-_CAAS_BREAST_SCREENING_COHORT.parquet | 2312514176 | Amended    |
