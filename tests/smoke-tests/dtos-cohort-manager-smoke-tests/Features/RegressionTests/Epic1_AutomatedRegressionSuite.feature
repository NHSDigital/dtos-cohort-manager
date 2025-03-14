Feature: Epic1_AutomatedRegressionSuite

DTOSS Regression TEST PACK.

Background:
        Given the database is cleaned of all records for NHS Numbers: 1111110662, 2222211794,2312514176
        And the application is properly configured

@DTOSS-7583 @Regression
Scenario: Verify NHS data propagation across participant tables after file upload for ADD record
	Given file <FileName> exists in the configured location for "Add" with NHS numbers : <NhsNumbers>
	When the file is uploaded to the Blob Storage container
	Then verify the NHS numbers in Participant_Management and Participant_Demographic table should match the file data

    Examples:
        | FileName                                             | RecordType | NhsNumbers             |
        | ADD_2_RECORDS_-_CAAS_BREAST_SCREENING_COHORT.parquet | Add        | 1111110662, 2222211794 |

@DTOSS-7583 @Regression
Scenario:  Verify NHS data propagation across participant tables after file upload for AMENDED record
    Given file <AddFileName> exists in the configured location for "Add" with NHS numbers : <NhsNumbers>
    And the file is uploaded to the Blob Storage container
    And the NHS numbers in the database should match the file data
	And file <AmendedFileName> exists in the configured location for "Amended" with NHS numbers : <NhsNumbers>
    When the file is uploaded to the Blob Storage container
	Then verify the NhsNumbers in Participant_Management table should match <RecordType>
    And the Participant_Demographic table should match the <AmendedGivenName> for the NHS Number

    Examples:
       | AddFileName                                        | AmendedFileName                                        | NhsNumbers | AmendedGivenName | RecordType |
       | ADD1_1B8F53_-_CAAS_BREAST_SCREENING_COHORT.parquet | AMENDED1_1B8F53_-_CAAS_BREAST_SCREENING_COHORT.parquet | 2312514176 | AMENDEDNewTest1  |  Amended   |

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
Scenario:  Confirm NHS Number Count Integrity Across Participant Tables After Processing for AMENDED record
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
