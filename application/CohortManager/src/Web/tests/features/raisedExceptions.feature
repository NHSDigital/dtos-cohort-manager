Feature: Raised exceptions page

    Background:
      Given I sign in with a test account
      When I go to the page "/exceptions/raised"
      Then I should see the heading "Raised breast screening exceptions"

    Scenario: Check for accessibility issues as a signed in user
      Given I should see the heading "Raised breast screening exceptions"
      Then I should expect 0 accessibility issues

    Scenario: Check for raised exceptions data
      Given I should see the heading "Raised breast screening exceptions"
      Then I see text containing "Showing 1 to 10" in the element "raised-exception-count"
      And the table "exceptions-table" has 10 rows
      And every row in the table "exceptions-table" has status "Raised"
      And the first row in the table "exceptions-table" has exception ID "3010"

    Scenario: Sort the raised exceptions table by "Date exception created (oldest first)"
      Given I should see the heading "Raised breast screening exceptions"
      When I sort the table by "Date exception created (oldest first)"
      Then the table "exceptions-table" has 10 rows
      And the first row in the table "exceptions-table" has exception ID "2083"

    Scenario: Sort the raised exceptions table by "Date exception created (newest first)"
      Given I should see the heading "Raised breast screening exceptions"
      When I sort the table by "Date exception created (newest first)"
      Then the table "exceptions-table" has 10 rows
      And the first row in the table "exceptions-table" has exception ID "3010"

    Scenario: Sort the raised exceptions table by "Exception ID (ascending)"
      Given I should see the heading "Raised breast screening exceptions"
      When I sort the table by "Exception ID (ascending)"
      Then the table "exceptions-table" has 10 rows
      And the first row in the table "exceptions-table" has exception ID "2083"

    Scenario: Sort the raised exceptions table by "NHS Number (descending)"
      Given I should see the heading "Raised breast screening exceptions"
      When I sort the table by "NHS Number (descending)"
      Then the table "exceptions-table" has 10 rows
      And the first row in the table "exceptions-table" has exception ID "3010"

    Scenario: Check for breadcrumb navigation back to homepage
      When I go to the page "/exceptions"
      Then I see the link "Home"
      When I click the link "Home"
      Then I should see the heading "Breast screening"
