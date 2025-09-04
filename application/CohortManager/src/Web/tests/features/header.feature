Feature: Header component

    Scenario: Check for page heading
        When I go to the page "/"
        Then I see the heading "Cohort Manager"

    Scenario: Sign in as a test user
      Given I am signed in as "test@nhs.net" with password "Password123"
      Then I should see the heading "Breast screening"

    Scenario: Check for header links
        When I go to the page "/"
        Then I see the link "Cohort Manager" with the URL "/"
        And I see the text "Test User"
        And I see the link "Account and settings" with the URL "/account"
        And I see the link "Log out"
