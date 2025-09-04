Feature: Check footer component for links on every page

    Scenario: Check for footer links
        When I go to the page "/"
        Then I see the link "Accessibility statement"
        And I see the link "Contact us"
        And I see the link "Cookies"
        And I see the link "Terms and conditions"
        And the link "Accessibility statement" should have href "/accessibility-statement"
        And the link "Contact us" should have href "/contact-us"
        And the link "Cookies" should have href "/cookies-policy"
        And the link "Terms and conditions" should have href "/terms-and-conditions"
