Feature: Epic1_AutomatedRegressionSuite

DTOSS Regression TEST PACK.

Background:
        Given the database is cleaned of all records for NHS Numbers: 1111110662, 2222211794,2312514176,2612314172,2612514171
        And the application is properly configured

@DTOSS-7583 @Regression
Scenario:Verify NHS data propagation across participant tables after file upload
	Given file <FileName> exists in the configured location for "Add" with NHS numbers : <NhsNumbers>
	When the file is uploaded to the Blob Storage container
	Then verify the NHS numbers in Participant_Management and Participant_Demographic table should match the file data

    Examples:
        | FileName                                             | RecordType | NhsNumbers             |
        | ADD_2_RECORDS_-_CAAS_BREAST_SCREENING_COHORT.parquet | Add        | 1111110662, 2222211794 |
