Feature: Static pages are reachable, correctly headed, and accessible

    Scenario: Check accessibility statement page
        When I go to the page "/accessibility-statement"
        Then I see the heading "Accessibility statement"
        And I should expect "0" accessibility issues

    Scenario: Check contact us page
        When I go to the page "/contact-us"
        Then I see the heading "Contact us"
        And I should expect "0" accessibility issues

    Scenario: Check cookies policy page
        When I go to the page "/cookies-policy"
        Then I see the heading "Cookies on Cohort Manager"
        And I should expect "0" accessibility issues

    Scenario: Check terms and conditions page
        When I go to the page "/terms-and-conditions"
        Then I see the heading "Terms and conditions"
        And I should expect "0" accessibility issues
