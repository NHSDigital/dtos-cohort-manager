Feature: File Upload and Cohort Distribution

DTOSS SMOKE TEST PACK.

    Background:
        Given the database is cleaned of all records for NHS Numbers: 1111110662, 2222211794,2312514176,2612314172,2612514171
        And the application is properly configured

@DTOSS-6256
Scenario: 01. Verify file upload and cohort distribution process
	Given file <FileName> exists in the configured location for "Add" with NHS numbers : <NhsNumbers>
	When the file is uploaded to the Blob Storage container
	Then the NHS numbers in the database should match the file data
    When the file is uploaded to the Blob Storage container
    Then the NHS numbers in the database should match the file data

    Examples:
        | FileName                                             | RecordType | NhsNumbers             |
        | ADD_2_RECORDS_-_CAAS_BREAST_SCREENING_COHORT.parquet | Add        | 1111110662, 2222211794 |
        

@DTOSS-6257
Scenario: 02. Verify file upload and cohort distribution process for amended records
    Given file <AddFileName> exists in the configured location for "Add" with NHS numbers : <NhsNumbers>
    And the file is uploaded to the Blob Storage container
    And the NHS numbers in the database should match the file data
    And  file <AmendedFileName> exists in the configured location for "Amended" with NHS numbers : <NhsNumbers>
    When the file is uploaded to the Blob Storage container
    Then there should be 2 records for the NHS Number in the database
    And the database should match the amended <AmendedGivenName> for the NHS Number
    
    Examples:
        | AddFileName                                        | AmendedFileName                                        | NhsNumbers | AmendedGivenName |
        | ADD1_1B8F53_-_CAAS_BREAST_SCREENING_COHORT.parquet | AMENDED1_1B8F53_-_CAAS_BREAST_SCREENING_COHORT.parquet | 2312514176 | AMENDEDNewTest1  |

@DTOSS-6406
Scenario: 03.Verify file upload handles invalid GP Practice Code Exception
    Given file <AddFileName> exists in the configured location for "Add" with NHS numbers : <NhsNumbers>
    When the file is uploaded to the Blob Storage container
	Then the Exception table should contain the below details for the NHS Number
        | FieldName        | FieldValue                                     |
        | RULE_ID          | 36                                             |
        | RULE_DESCRIPTION | Invalid primary care provider GP practice code |
    
    Examples:
	| AddFileName                                             | NhsNumbers | RULE_ID | RuleDescription                                |
	| Exception_1B8F53_-_CAAS_BREAST_SCREENING_COHORT.parquet | 2612314172 | 36      | Invalid primary care provider GP practice code |


@DTOSS-6407
Scenario: 04.Verify file upload handles EmptyDOB Exception
    Given file <AddFileName> exists in the configured location for "Add" with NHS numbers : <NhsNumbers>
    And the file is uploaded to the Blob Storage container
    And the NHS numbers in the database should match the file data
    And file <AmendedFileName> exists in the configured location for "Amended" with NHS numbers : <NhsNumbers>
    When the file is uploaded to the Blob Storage container
    Then the Exception table should contain the below details for the NHS Number
        | FieldName        | FieldValue                                     |
        | RULE_ID          | 17                                             |
        | RULE_DESCRIPTION | Date of birth invalid |
  
    Examples:
	| AddFileName                                       | AmendedFileName                                       | NhsNumbers | RULE_ID | RuleDescription                                |
	| ADD_1B8F53_-_CAAS_BREAST_SCREENING_COHORT.parquet | AMENDED_1B8F53_-_CAAS_BREAST_SCREENING_COHORT.parquet | 2612514171 | 36      | Invalid primary care provider GP practice code |