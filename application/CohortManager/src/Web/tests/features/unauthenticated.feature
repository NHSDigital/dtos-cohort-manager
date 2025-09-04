Feature: Unauthenticated users can't access protected pages

  Background:
    Given I am not authenticated
    Then I should not be able to access protected pages

    Scenario: Check access to the exceptions page as an unauthenticated user
        When I go to the page "/exceptions"
        Then I should see the heading "Cohort Manager"

    Scenario: Check access to the raised exceptions page as an unauthenticated user
        When I go to the page "/exceptions/raised"
        Then I should see the heading "Cohort Manager"

    Scenario: Check access to participant information page
        When I go to the page "/participant-information/2028"
        Then I should see the heading "Cohort Manager"

    Scenario: Check access to reports page
        When I go to the page "/reports"
        Then I should see the heading "Cohort Manager"

    Scenario: Check access to reports information page
        When I go to the page "/reports/2025-09-04?category=13"
        Then I should see the heading "Cohort Manager"
