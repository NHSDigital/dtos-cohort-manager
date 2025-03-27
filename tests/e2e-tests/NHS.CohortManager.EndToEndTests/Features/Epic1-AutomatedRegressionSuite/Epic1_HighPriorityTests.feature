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
    Then the NHS Number should have the following records count
      | TableName               | ExpectedCountInTable |
      | Participant_Management  |                    1 |
      | Participant_Demographic |                    1 |

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
    Then verify the NhsNumbers in Participant_Management table should match <RecordType>
    Then the NHS Number should have the following records count
      | TableName               | ExpectedCountInTable |
      | Participant_Management  |                    1 |
      | Participant_Demographic |                    1 |
      | BS_Cohort_Distribution  |                    2 |

    Examples:
      | AddFileName                                        | AmendedFileName                                        | NhsNumbers | RecordType |
      | ADD1_1B8F53_-_CAAS_BREAST_SCREENING_COHORT.parquet | AMENDED1_1B8F53_-_CAAS_BREAST_SCREENING_COHORT.parquet | 2312514176 | AMENDED    |

  @DTOSS-7585 @Regression
  Scenario: Verify ADD records that trigger a non-fatal validation rule reach internal participant tables but not Cohort distribution
    Given file <AddFileName> exists in the configured location for "Add" with NHS numbers : <NhsNumbers>
    When the file is uploaded to the Blob Storage container
    Then the Exception table should contain the below details for the NHS Number
      | FieldName        | FieldValue                                     |
      | RULE_ID          |                                             36 |
      | RULE_DESCRIPTION | Invalid primary care provider GP practice code |
    Then the NHS Number should have the following records count
      | TableName               | ExpectedCountInTable |
      | Participant_Management  |                    1 |
      | Participant_Demographic |                    1 |
      | BS_Cohort_Distribution  |                    0 |

    Examples:
      | AddFileName                                             | NhsNumbers |
      | Exception_1B8F53_-_CAAS_BREAST_SCREENING_COHORT.parquet | 2612314172 |

  @DTOSS-7585 @Regression
  Scenario: Verify AMENDED records with non-fatal validation issues reach participant tables with partial Cohort distribution entries
    Given file <AddFileName> exists in the configured location for "Add" with NHS numbers : <NhsNumbers>
    And the file is uploaded to the Blob Storage container
    And the NHS numbers in the database should match the file data
    And file <AmendedFileName> exists in the configured location for "Amended" with NHS numbers : <NhsNumbers>
    When the file is uploaded to the Blob Storage container
    Then the Exception table should contain the below details for the NHS Number
      | FieldName        | FieldValue            |
      | RULE_ID          |                    17 |
      | RULE_DESCRIPTION | Date of birth invalid |
    Then the NHS Number should have the following records count
      | TableName               | ExpectedCountInTable |
      | Participant_Management  |                    1 |
      | Participant_Demographic |                    1 |
      | BS_Cohort_Distribution  |                    1 |

    Examples:
      | AddFileName                                       | AmendedFileName                                       | NhsNumbers |
      | ADD_1B8F53_-_CAAS_BREAST_SCREENING_COHORT.parquet | AMENDED_1B8F53_-_CAAS_BREAST_SCREENING_COHORT.parquet | 2612514171 |

  @DTOSS-7589 @Regression
  Scenario: Verify AMENDED records is processed without any Exception
    Given file <AddFileName> exists in the configured location for "Add" with NHS numbers : <NhsNumbers>
    And the file is uploaded to the Blob Storage container
    And the NHS numbers in the database should match the file data
    And file <AmendedFileName> exists in the configured location for "Amended" with NHS numbers : <NhsNumbers>
    When the file is uploaded to the Blob Storage container
    Then verify the NhsNumbers in Participant_Management table should match <RecordType>
    Then the NHS Number should have the following records count
      | TableName               | ExpectedCountInTable |
      | Participant_Management  |                    1 |
      | Participant_Demographic |                    1 |
      | BS_Cohort_Distribution  |                    2 |
      | Exception_Managment     |                    0 |

    Examples:
      | AddFileName                                        | AmendedFileName                                        | NhsNumbers | RecordType |
      | ADD1_1B8F53_-_CAAS_BREAST_SCREENING_COHORT.parquet | AMENDED1_1B8F53_-_CAAS_BREAST_SCREENING_COHORT.parquet | 2312514176 | AMENDED    |

  @DTOSS-7590 @Regression
  Scenario: Verify ADD records is processed without any Exception
    Given file <FileName> exists in the configured location for "Add" with NHS numbers : <NhsNumbers>
    When the file is uploaded to the Blob Storage container
    Then verify the NhsNumbers in Participant_Management table should match <RecordType>
    Then the NHS Number should have the following records count
      | TableName               | ExpectedCountInTable |
      | Participant_Management  |                    1 |
      | Participant_Demographic |                    1 |
      | BS_Cohort_Distribution  |                    1 |
      | Exception_Managment     |                    0 |

    Examples:
      | FileName                                            | RecordType | NhsNumbers             |
      | ADD2_records_-_CAAS_BREAST_SCREENING_COHORT.parquet | Add        | 1111110662, 2222211794 |

  @DTOSS-7587 @Regression
  Scenario: Verify a file is uploaded to blobstorage successfully
    Given file <FileName> exists in the configured location for "Add" with NHS numbers : <NhsNumbers>
    When the file is uploaded to the Blob Storage container
    Then the uploaded file should exist in blob storage
    And the file content should match the original

    Examples:
      | FileName                                             | RecordType | NhsNumbers             |
      | ADD_2_RECORDS_-_CAAS_BREAST_SCREENING_COHORT.parquet | Add        | 1111110662, 2222211794 |

  @DTOSS-7588 @Regression
  Scenario: Verify that a file with an invalid name creates a validation exception
    Given file <FileName> exists in the configured location for "Add" with NHS numbers : <NhsNumbers>
    When the file is uploaded to the Blob Storage container
    Then the exception table should contain the below details
      | FieldName        | FieldValue                                                                           |
      | ERROR_RECORD     | File name is invalid. File name: Exception_1B8F53_-_CAAS_BREAST_screening_'@.parquet |
      | RULE_DESCRIPTION | The file failed file validation. Check the file Exceptions blob store.               |

    Examples:
      | FileName                                            | RecordType | NhsNumbers             |
      | Exception_1B8F53_-_CAAS_BREAST_screening_'@.parquet | Add        | 1111110662, 2222211794 |

  @DTOSS-7586 @Regression
  Scenario: Verify Successful Data Processing and Storage in Cohort Manager for Add"
    Given file <FileName> exists in the configured location for "Add" with NHS numbers : <NhsNumbers>
    When the file is uploaded to the Blob Storage container
    Then verify the NhsNumbers in Participant_Management table should match <RecordType>
    Then the NHS Number should have the following records count
      | TableName               | ExpectedCountInTable |
      | Participant_Management  |                    1 |
      | Participant_Demographic |                    1 |
      | Exception_Management    |                    0 |
    And verify Parquet file data matches the data in cohort distribution

    Examples:
      | FileName                                           | RecordType | NhsNumbers |
      | ADD1_1B8F53_-_CAAS_BREAST_SCREENING_COHORT.parquet | Add        | 2312514176 |
