Feature: Unauthenticated users can't access protected pages

  Background:
    Given I am not authenticated

  Scenario Outline: Protected pages show unauthenticated view
    When I go to the page "<path>"
    Then I should see the heading "Cohort Manager"

    Examples:
      | path                                   |
      | /exceptions                            |
      | /exceptions/raised                     |
      | /participant-information/2028          |
      | /reports                               |
      | /reports/2025-09-04?category=13        |
