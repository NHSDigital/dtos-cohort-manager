# Test Files

This directory is used to store test files required for integration tests, there are sub-directories for add, update and remove test scenarios.

## Instructions

1. Place your test files in this directory (ensuring that all test data is dummy data).
2. Ensure files are named correctly with the correct file extension and naming convention.
3. Do not commit any test files to the repository: CSV and Parquet files have been intentionally excluded from version control and added to the '.gitignore' this ensures that sensitive or large test data files are not accidentally committed. 

## Example

Place your valid test data file into tests/Integration/TestFiles/add before running an automated happy path test.