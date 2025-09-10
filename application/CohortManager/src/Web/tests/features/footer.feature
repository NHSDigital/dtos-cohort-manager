Feature: Check footer component for links on every page

  Scenario Outline: Footer contains expected links
    When I go to the page "/"
    Then I see the link "<text>" with the href "<href>"

    Examples:
      | text                  | href                        |
      | Accessibility statement | /accessibility-statement   |
      | Contact us             | /contact-us                |
      | Cookies                | /cookies-policy            |
      | Terms and conditions   | /terms-and-conditions      |
