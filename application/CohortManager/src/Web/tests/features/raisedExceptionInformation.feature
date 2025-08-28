Feature: testing Breast screening - Raised exception information page

  Background:
    Given the user navigate to raised exception overview page
    When the user clicks on exception ID link

  @epic_4a @req_6328 @test_10066
  Scenario: verify exception information
    Then they should navigate to 'Exception information - Cohort Manager - NHS'
    And the participant details section should have the following fields:
      | NHS number               |
      | Superseded by NHS number |
      | Surname                  |
      | Forename                 |
      | Date of birth            |
      | Gender                   |
      | Current address          |
      | Registered practice code |
    And the Exception details section should have the following fields:
      | Date exception created |
      | More detail            |
    And the following labels should be present on top of the page:
      | Portal form used   |
      | Exception status   |
      | ServiceNow Case ID |

  @epic_4a @req_6328 @test_10072
  Scenario: navigation to exception overview page
    When the user clicks on raised breast screening exceptions link
    Then they should navigate to 'Raised breast screening exceptions - Cohort Manager - NHS'

  @epic_4a @req_6328 @test_10073
  Scenario: verify navigation to Home screen from Raised exception information page
    When the user clicks on Home link
    Then they should navigate to 'Breast screening - Cohort Manager - NHS'

  @epic_4a @req_6328 @test_10075
  Scenario: verify navigation to Contact us screen from raised exception information page
    And the user clicks on contact us link
    Then they should navigate to 'Get help with Cohort Manager - Cohort Manager - NHS'

  @epic_4a @req_6328 @test_10074
  Scenario: verify navigation to Terms and conditions screen from raised exception information page
    And the user clicks on Terms and conditions link
    Then they should navigate to 'Terms and conditions - Cohort Manager - NHS'

  @epic_4a @req_6328 @test_10076
  Scenario: verify navigation to cookies screen from raised exception information page
    And the user clicks on cookies link
    Then they should navigate to 'Cookies on Cohort Manager - Cohort Manager - NHS'
