Feature: testing Breast screening - Not raised exception information page

  Background:
    Given the user navigate to not raised exception overview page
    When the user clicks on exception ID link
    And they should navigate to 'Exception information - Cohort Manager - NHS'

  @regression @req_3913 @test_10077
  Scenario: verify exception information
    Then they should navigate to 'Exception information - Cohort Manager - NHS'
    And the participant details section should have the following fields:
      | NHS number               |
      | Surname                  |
      | Forename                 |
      | Date of birth            |
      | Gender                   |
      | Current address          |
      | Contact details          |
      | Registered practice code |
    And the Exception details section should have the following fields:
      | Date exception created |
      | More detail            |
      | ServiceNow ID          |
    And the Exception status have 'Enter ServiceNow Case ID'
    And the Exception status have 'save and continue' button

  @req_3913 @test_10083
  Scenario: navigation to exception information page
    When the user clicks on Not raised breast screening exceptions link
    Then they should navigate to 'Not raised breast screening exceptions - Cohort Manager - NHS'

  @req_3913 @test_10084
  Scenario: verify navigation to Home screen from Raised exception overview page
    When the user clicks on Home link
    Then they should navigate to 'Breast screening - Cohort Manager - NHS'

  @req_3913 @test_10087
  Scenario: verify navigation to Contact us screen
    And the user clicks on contact us link
    Then they should navigate to 'Get help with Cohort Manager - Cohort Manager - NHS'

  @req_3913 @test_10085
  Scenario: verify navigation to Terms and conditions screen
    And the user clicks on Terms and conditions link
    Then they should navigate to 'Terms and conditions - Cohort Manager - NHS'

  @req_3913 @test_10086
  Scenario: verify navigation to cookies screen
    And the user clicks on cookies link
    Then they should navigate to 'Cookies on Cohort Manager - Cohort Manager - NHS'
