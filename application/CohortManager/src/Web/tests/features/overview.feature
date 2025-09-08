Feature: Overview page

  # Signed-out checks
  Scenario: Page heading is shown
    When I go to the page "/"
    Then I should see the heading "Cohort Manager"

  Scenario: Page has no accessibility issues when signed out
    When I go to the page "/"
    Then I should expect 0 accessibility issues

  Scenario: NHS login button is shown
    When I go to the page "/"
    Then I see the button "Log in with my Care Identity"

  # Signed-in checks
  Scenario: Page has no accessibility issues when signed in
    Given I sign in with a test account
    When I go to the page "/"
    Then I should expect 0 accessibility issues

  Scenario Outline: Overview tiles are visible with correct links, text and counts
    Given I sign in with a test account
    When I go to the page "/"
    Then I see the link "<label>" with the href "<href>"
    And I see the text "<description>"
    And I see the number in the first card (Not raised) is greater than or equal to 0
    And I see the number in the second card (Raised) is greater than or equal to 0
    And I see the number in the third card (Reports) is greater than or equal to 0

    Examples:
      | label      | href               | description                                   |
      | Not raised | /exceptions        | Exceptions to be raised with teams            |
      | Raised     | /exceptions/raised | Access and amend previously raised exceptions |
      | Reports    | /reports           | To manage investigations into demographic changes |
