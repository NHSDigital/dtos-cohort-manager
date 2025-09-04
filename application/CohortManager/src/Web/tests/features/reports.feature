Feature: Reports page

    Scenario: Check for page heading
        When I go to the page "/reports"
        Then I see the heading "Reports"

    Scenario: Check for accessibility issues
        When I go to the page "/reports"
        Then I should expect "0" accessibility issues

    Scenario: Check for reports data
      Given I should see the heading "Reports"
      Then I should see the text "Showing reports for" in the element "reports-date-range"
      And the table "reports-table" should have columns:
        | Date        |
        | Demographic change      |
        | Action           |
      And the table "reports-table" should have "28" rows
      And all the reports in the table "reports-table" should have a link "View report"

    Scenario: Check for the first report (NHS number change) in the table
      Given I should see the heading "Reports"
      When I click the link "View report" in the first row of the table "reports-table"
      Then I see the heading "NHS number change"
      And I see the table "report-details-table" with columns:
        | Patient name       |
        | Date of birth      |
        | NHS number           |
        | Superseded by NHS number        |
      And the table "report-details-table" should have "1" rows

    Scenario: Check for the second report (Possible confusion over NHS number) in the table
      Given I should see the heading "Reports"
      When I click the link "View report" in the second row of the table "reports-table"
      Then I see the heading "Possible confusion"
      And I see the table "report-details-table" with columns:
        | Patient name       |
        | Date of birth      |
        | NHS number           |
      And the table "report-details-table" should have "1" rows

    Scenario: Check for breadcrumb navigation back to homepage
      When I go to the page "/reports"
      Then I see the link "Home" in the breadcrumb navigation
      When I click the link "Home" in the breadcrumb navigation
      Then I see the heading "Cohort Manager"

     Scenario: Check for breadcrumb navigation back to reports page
      When I go to the page "/reports/2025-09-04?category=13"
      Then I see the link "Home" in the breadcrumb navigation
      Then I see the link "Reports" in the breadcrumb navigation
      When I click the link "Reports" in the breadcrumb navigation
      Then I see the heading "Reports"
