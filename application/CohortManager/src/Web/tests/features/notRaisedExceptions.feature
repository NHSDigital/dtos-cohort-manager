Feature: Not raised exceptions page

  Background:
    Given I am signed in as "test@nhs.net" with password "Password123"
    When I go to the page "/exceptions"
    Then I should see the heading "Not raised breast screening exceptions"

  Scenario: Page has no accessibility issues
    Then I should expect 0 accessibility issues

  Scenario: Table shows 18 "Not raised" exceptions with expected columns
    Then I see the text "Showing 1 to 10 of 18 results" in the element "not-raised-exception-count"
    And the table "exceptions-table" has 10 rows
    And every row in the table "exceptions-table" has status "Not raised"
    And the first row in the table "exceptions-table" has exception ID "2073"

    Scenario: Sort the not raised exceptions table by "Date exception created (oldest first)"
      Given I should see the heading "Not raised breast screening exceptions"
      When I sort the table by "Date exception created (oldest first)"
      Then the table "exceptions-table" has 10 rows
      And the first row in the table "exceptions-table" has exception ID "2033"

    Scenario: Sort the not raised exceptions table by "Date exception created (newest first)"
      Given I should see the heading "Not raised breast screening exceptions"
      When I sort the table by "Date exception created (newest first)"
      Then the table "exceptions-table" has 10 rows
      And the first row in the table "exceptions-table" has exception ID "2073"

  Scenario: Breadcrumb back to homepage
    Then I see the link "Home"
    When I click the link "Home"
    Then I should see the heading "Breast screening"

  Scenario: Change exception to raised and back to not raised
    Given I go to the page "/participant-information/2028"
    When I fill the input with label "Enter ServiceNow Case ID" with "CS1234567"
    And I click the button "Save and continue"
    Then I should see the heading "Not raised breast screening exceptions"
    And the table "exceptions-table" does not contain exception ID "2028"

    When I go to the page "/participant-information/2028"
    Then I see the text "CS1234567"
    When I click the link "Change"
    And I fill the input with label "Enter ServiceNow Case ID" with ""
    And I click the button "Save and continue"
    Then I should see the heading "Raised breast screening exceptions"
    And the table "exceptions-table" does not contain exception ID "2028"
