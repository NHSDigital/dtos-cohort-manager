Feature: Overview page

    Scenario: Check for page heading
        When I go to the page "/"
        Then I see the heading "Cohort Manager"

    Scenario: Check for accessibility issues as a signed out user
        When I go to the page "/"
        Then I should expect "0" accessibility issues

    Scenario: Check for the NHS login button
        When I go to the page "/"
        Then I see the button "Log in with my Care Identity"

    Scenario: Sign in as a test user
      Given I am signed in as "test@nhs.net" with password "Password123"
      Then I should see the heading "Breast screening"

    Scenario: Check for accessibility issues as a signed in user
      Given I should see the heading "Breast screening"
      Then I should expect "0" accessibility issues

    Scenario: Check for not raised exceptions
      Given I should see the link "Not raised" with the URL "/exceptions/not-raised"
      Then I should see the text "Exceptions to be raised with teams"
      Then I should expect "18" not raised exceptions

    Scenario: Check for raised exceptions
      Given I should see the link "Raised" with the URL "/exceptions/raised"
      Then I should see the text "Access and amend previously raised exceptions"
      Then I should expect "10" raised exceptions

    Scenario: Check for reports
      Given I should see the link "Reports" with the URL "/reports"
      Then I should see the text "To manage investigations into demographic changes"
      Then I should expect "28"
