# Snapshot Tests Guide

Snapshot tests are used to validate how changes are impacting the expected output of the application.
These tests are not signing that the code is correct, But ensuring that the change has not had any unexpected consequences.

These tests will run a set of regression files, and will compare the output of these against the previous approved output (the snapshot).
If there are differences, these can be validated and checked using a diff tool.

- If the changes are expected then the differences can be accepted.
- If these are unexpected the changes should be rejected and investigation in to unexpected chances should be carried out and resolved before running the tests again.

## Prerequistes

the below tool will need installing to easily verify the changes to the snapshot
`dotnet tool install -g verify.tool`

## Process

1 - Clear down the database such that there is no unexpected data in the database
2 - Ensure all the functions are running
3 - Run `Dotnet Test` from the /snapshot-tests Directory
4a - If the Tests pass, This indicates that there have been no functional changes have been made and you are good to go!
4b - If the Tests fail you can run `dotnet verify review` this will show the differences between the verified(previous) and received (New)

## ToDo

- Clear down and load the test files are part of the test. Clear the database once all the tests are completed?
  - Maybe only clear the NHS Numbers that are part of the smoke test file and only filter on that when doing the comparison
- Test the api responses by calling the APIs from the Snapshot tests
  - Such as retrieve cohort Distribution and the Exception API
  - Do this after each file is run?
  - Do this in addition to the database checks?
- Create new test classes for each of the smoke test files
- Work on how this would work in the pipeline
- Ensure the above process works as expected
