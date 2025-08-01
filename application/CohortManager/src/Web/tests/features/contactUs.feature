Feature: testing to verify all ac's on Legal pages story-DTOSS-9551

  Background:
    Given the User navigate to contact us page

  Scenario: verify navigation to Terms and conditions screen
    When the user clicks on Terms and conditions link
    Then they should navigate to 'Terms and conditions - Cohort Manager - NHS'

  Scenario: verify navigation to Contact us screen
    And the user clicks on contact us link
    Then they should navigate to 'Get help with Cohort Manager - Cohort Manager - NHS'

  Scenario: verify navigation to cookies screen
    When the user clicks on cookies link
    Then they should navigate to 'Cookies on Cohort Manager - Cohort Manager - NHS'

  Scenario: verify navigation to technical support and general enquiries
    When the user clicks on technical support and general enquiries link
    Then they should navigate to NHS National IT Customer Support Portal page

  Scenario: verify navigation to Report an incident
    When the user clicks on Report an incident link
    Then they should navigate to NHS National IT Customer Support Portal page

  Scenario: verify navigation to back to previous screen from contactus page
    When the user clicks on Back to previous screen link
    Then they should navigate to 'Cohort Manager - NHS'
