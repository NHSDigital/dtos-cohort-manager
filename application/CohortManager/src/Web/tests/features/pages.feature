Feature: Legal and contact pages are reachable and accessible

  Scenario Outline: Static page is reachable, headed correctly, and has no a11y issues
    When I go to the page "<path>"
    Then I should see the heading "<heading>"
    And I should expect 0 accessibility issues

    Examples:
      | path                     | heading                       |
      | /accessibility-statement | Accessibility statement       |
      | /contact-us              | Get help with Cohort Manager  |
      | /cookies-policy          | Cookies on Cohort Manager     |
      | /terms-and-conditions    | Terms and conditions          |

  Scenario: Check for 404 page
    When I go to the page "/thispagedoesnotexist"
    Then I should see the heading "Page not found"
    And I see the link "Return to the homepage" with the href "/"
