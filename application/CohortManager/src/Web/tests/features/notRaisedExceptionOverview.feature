Feature: testing Breast screening - Not raisedexception overview page

  Background:
    Given the user navigate to not raised exception overview page

  @epic_4a
  Scenario: verify  column headers on Raised exception overview page
    Then the exception summary table should have the following columns:
      | Local reference  Exception ID |
      | NHS number                    |
      | Date exception created        |
      | Short description             |
      | Exception status              |

  Scenario: verify that the Sorting is out of scope on table Headers.
    Then the exception summary table headers should not be sortable

  Scenario: navigation to exception information page
    When the user clicks on exception ID link
    Then they should navigate to 'Exception information - Cohort Manager - NHS'

  Scenario: verify navigation to Home screen from Raised exception overview page
    When the user clicks on Home link
    Then they should navigate to 'Breast screening - Cohort Manager - NHS'

  Scenario: verify navigation to Contact us screen
    And the user clicks on contact us link
    Then they should navigate to 'Get help with Cohort Manager - Cohort Manager - NHS'

  Scenario: verify navigation to Terms and conditions screen
    And the user clicks on Terms and conditions link
    Then they should navigate to 'Terms and conditions - Cohort Manager - NHS'

  Scenario: verify navigation to cookies screen
    And the user clicks on cookies link
    Then they should navigate to 'Cookies on Cohort Manager - Cohort Manager - NHS'
