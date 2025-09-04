Feature: Raised exceptions page

    Scenario: Check for page heading
        When I go to the page "/exceptions/raised"
        Then I see the heading "Raised breast screening exceptions"

    Scenario: Check for accessibility issues as a signed in user
      Given I should see the heading "Raised breast screening exceptions"
      Then I should expect "0" accessibility issues

    Scenario: Check for raised exceptions data
      Given I should see the heading "Raised breast screening exceptions"
      Then I should see the text "Showing 10 of 10 results" in the element "app-results-text"
      And the table "exceptions-table" should have "10" rows
      And all the exceptions in the table "exceptions-table" should have status "Raised"
      And the table "exceptions-table" should have columns:
        | Exception ID             |
        | NHS Number               |
        | Date exception created   |
        | Short description        |
        | Exception status         |
      And the first row of the table "exceptions-table" should contain exception ID "2028"

    Scenario: Sort the raised exceptions table by "Status last updated (oldest first)"
      Given I should see the heading "Raised breast screening exceptions"
      When I sort the table "exceptions-table" by "Status last updated (oldest first)"
      Then the table "exceptions-table" should have "10" rows
      And the first row of the table "exceptions-table" should contain exception ID "2084"

    Scenario: Sort the raised exceptions table by "Date exception created (newest first)"
      Given I should see the heading "Raised breast screening exceptions"
      When I sort the table "exceptions-table" by "Date exception created (newest first)"
      Then the table "exceptions-table" should have "10" rows
      And the first row of the table "exceptions-table" should contain exception ID "4001"

    Scenario: Check for breadcrumb navigation back to homepage
      When I go to the page "/exceptions"
      Then I see the link "Home" in the breadcrumb navigation
      When I click the link "Home" in the breadcrumb navigation
      Then I see the heading "Cohort Manager"
