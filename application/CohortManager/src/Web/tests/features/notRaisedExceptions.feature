Feature: Not raised exceptions page

    Scenario: Check for page heading
        When I go to the page "/exceptions"
        Then I see the heading "Not raised breast screening exceptions"

    Scenario: Check for accessibility issues as a signed in user
      Given I should see the heading "Not raised breast screening exceptions"
      Then I should expect "0" accessibility issues

    Scenario: Check for not raised exceptions data
      Given I should see the heading "Not raised breast screening exceptions"
      Then I should see the text "Showing 18 of 18 results" in the element "app-results-text"
      And the table "exceptions-table" should have "18" rows
      And all the exceptions in the table "exceptions-table" should have status "Not raised"
      And the table "exceptions-table" should have columns:
        | Exception ID             |
        | NHS Number               |
        | Date exception created   |
        | Short description        |
        | Exception status         |
      And the first row of the table "exceptions-table" should contain exception ID "2028"

    Scenario: Sort the not raised exceptions table by "Date exception created (oldest first)"
      Given I should see the heading "Not raised breast screening exceptions"
      When I sort the table "exceptions-table" by "Date exception created (oldest first)"
      Then the table "exceptions-table" should have "18" rows
      And the first row of the table "exceptions-table" should contain exception ID "2033"

    Scenario: Sort the not raised exceptions table by "Date exception created (newest first)"
      Given I should see the heading "Not raised breast screening exceptions"
      When I sort the table "exceptions-table" by "Date exception created (newest first)"
      Then the table "exceptions-table" should have "18" rows
      And the first row of the table "exceptions-table" should contain exception ID "2028"

    Scenario: Check for breadcrumb navigation back to homepage
        When I go to the page "/exceptions"
        Then I see the link "Home" in the breadcrumb navigation
        When I click the link "Home" in the breadcrumb navigation
        Then I see the heading "Cohort Manager"

    Scenario: Change not raised exception to raised and back to not raised
      Given I go to the page "/participant-information/2028"
      When I fill in the text input with label "Enter ServiceNow Case ID" with "CS1234567"
      And I click the button "Save and continue"
      Then I should see the heading "Not raised breast screening exceptions"
      And I should not see the Exception ID "2028" in the table "exceptions-table"
      When I go to the page "/participant-information/2028"
      Then I should see the text "CS1234567" in the the ServiceNow Case ID field
      When I click the Change link next to the ServiceNow Case ID field
      And I fill in the text input with label "Enter ServiceNow Case ID" with ""
      And I click the button "Save and continue"
      Then I should see the heading "Not raised breast screening exceptions"
      And I should see the Exception ID "2028" in the table "exceptions-table"
