Feature: Epic1_AutomatedRegressionSuite
DTOSS Regression TEST PACK.

  Background:
    Given the database is cleaned of all records for NHS Numbers: 1111110662, 2222211794,2312514176,2612314172,2612514171
    And the application is properly configured

  @DTOSS-7583 @Regression
  Scenario: Verify ADD records reach the participant tables
    Given file <FileName> exists in the configured location for "Add" with NHS numbers : <NhsNumbers>
    When the file is uploaded to the Blob Storage container
    Then verify the NHS numbers in Participant_Management and Participant_Demographic table should match the file data

    Examples:
      | FileName                                             | RecordType | NhsNumbers             |
      | ADD_2_RECORDS_-_CAAS_BREAST_SCREENING_COHORT.parquet | Add        | 1111110662, 2222211794 |

  @DTOSS-7583 @Regression
  Scenario: Verify AMENDED records reach the participant tables
    Given file <AddFileName> exists in the configured location for "Add" with NHS numbers : <NhsNumbers>
    And the file is uploaded to the Blob Storage container
    And the NHS numbers in the database should match the file data
    And file <AmendedFileName> exists in the configured location for "Amended" with NHS numbers : <NhsNumbers>
    When the file is uploaded to the Blob Storage container
    Then verify the NhsNumbers in Participant_Management table should match <RecordType>
    And the Participant_Demographic table should match the <AmendedGivenName> for the NHS Number

    Examples:
      | AddFileName                                        | AmendedFileName                                        | NhsNumbers | AmendedGivenName | RecordType |
      | ADD1_1B8F53_-_CAAS_BREAST_SCREENING_COHORT.parquet | AMENDED1_1B8F53_-_CAAS_BREAST_SCREENING_COHORT.parquet | 2312514176 | AMENDEDNewTest1  | Amended    |

  @DTOSS-7584 @Regression
  Scenario: Confirm NHS Number Count Integrity Across Participant Tables After Processing for ADD record
    Given file <FileName> exists in the configured location for "Add" with NHS numbers : <NhsNumbers>
    When the file is uploaded to the Blob Storage container
    Then the NHS Number should have exactly 1 record in Participant_Management
    And the NHS Number should have exactly 1 record in Participant_Demographic

    Examples:
      | FileName                                             | RecordType | NhsNumbers             |
      | ADD_2_RECORDS_-_CAAS_BREAST_SCREENING_COHORT.parquet | Add        | 1111110662, 2222211794 |

  @DTOSS-7584 @Regression
  Scenario: Confirm NHS Number Count Integrity Across Participant Tables After Processing for AMENDED record
    Given file <AddFileName> exists in the configured location for "Add" with NHS numbers : <NhsNumbers>
    And the file is uploaded to the Blob Storage container
    And the NHS numbers in the database should match the file data
    And file <AmendedFileName> exists in the configured location for "Amended" with NHS numbers : <NhsNumbers>
    When the file is uploaded to the Blob Storage container
    Then the NHS Number should have exactly 1 record in Participant_Management
    And the NHS Number should have exactly 1 record in Participant_Demographic

    Examples:
      | AddFileName                                        | AmendedFileName                                        | NhsNumbers |
      | ADD1_1B8F53_-_CAAS_BREAST_SCREENING_COHORT.parquet | AMENDED1_1B8F53_-_CAAS_BREAST_SCREENING_COHORT.parquet | 2312514176 |

  @DTOSS-7585 @Regression
  Scenario: Verify exception records doesn't end up in Participant_Demographic and Participant_Management table for ADD record
    Given file <AddFileName> exists in the configured location for "Add" with NHS numbers : <NhsNumbers>
    When the file is uploaded to the Blob Storage container
    Then the Exception table should contain the below details for the NHS Number
      | FieldName        | FieldValue                                     |
      | RULE_ID          |                                             36 |
      | RULE_DESCRIPTION | Invalid primary care provider GP practice code |
    Then the NHS Number should have exactly 1 record in Participant_Management
    And the NHS Number should have exactly 1 record in Participant_Demographic
    And the NHS Number should have exactly 0 record in Cohort_Distribution table

    Examples:
      | AddFileName                                             | NhsNumbers |
      | Exception_1B8F53_-_CAAS_BREAST_SCREENING_COHORT.parquet | 2612314172 |

  @DTOSS-7585 @Regression
  Scenario: Verify exception records doesn't end up in Participant_Demographic and Participant_Management table for AMENDED record
    Given file <AddFileName> exists in the configured location for "Add" with NHS numbers : <NhsNumbers>
    And the file is uploaded to the Blob Storage container
    And the NHS numbers in the database should match the file data
    And file <AmendedFileName> exists in the configured location for "Amended" with NHS numbers : <NhsNumbers>
    When the file is uploaded to the Blob Storage container
    Then the Exception table should contain the below details for the NHS Number
      | FieldName        | FieldValue            |
      | RULE_ID          |                    17 |
      | RULE_DESCRIPTION | Date of birth invalid |
    Then the NHS Number should have exactly 1 record in Participant_Management
    And the NHS Number should have exactly 1 record in Participant_Demographic
    And the NHS Number should have exactly 1 record in Cohort_Distribution table

    Examples:
      | AddFileName                                       | AmendedFileName                                       | NhsNumbers |
      | ADD_1B8F53_-_CAAS_BREAST_SCREENING_COHORT.parquet | AMENDED_1B8F53_-_CAAS_BREAST_SCREENING_COHORT.parquet | 2612514171 |
