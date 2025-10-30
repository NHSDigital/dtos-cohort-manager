Feature: Reports section

  Background:
    Given I sign in with a test account
    When I go to the page "/reports"
    Then I should see the heading "Reports"

  Scenario: Page meets accessibility standards
    Then I should expect 0 accessibility issues

  Scenario: User can open the first report in the list
    When I click the first "View report" link
    Then I should see the heading "NHS Number Change"
    And I see the table heading "Patient name"
    And I see the table heading "Date of birth"
    And I see the table heading "NHS number"
    And I see the table heading "Superseded by NHS number"

  Scenario: User can open the second report in the list
    When I click the second "View report" link
    Then I should see the heading "Possible Confusion"
    And I see the table heading "Patient name"
    And I see the table heading "Date of birth"
    And I see the table heading "NHS number"

  Scenario: User can navigate back to the homepage via breadcrumb
    Then I see the link "Home"
    When I click the link "Home"
    Then I should see the heading "Breast screening"

  Scenario: User can navigate back to the reports page via breadcrumb
    Given I go to the page "/reports/2025-09-04?category=13"
    Then I see the link "Home"
    And I see the link "Reports"
    When I click the link "Reports"
    Then I should see the heading "Reports"
