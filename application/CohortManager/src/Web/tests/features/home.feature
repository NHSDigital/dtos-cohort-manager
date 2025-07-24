Feature: testing Breast screening - Cohort Manager home page

  Background:
    Given the User has logged in to the Cohort manager exceptions UI
    When they land on the 'Breast screening - Cohort Manager - NHS'

  @DTOSS-9777 @DTOSS-9779 @DTOSS-9782
  Scenario: verify  total number, Raised link, text on Raised card
    Then they should see Raised as link on raised card
    And the total number should be displayed on raised
    And they should be able to view 'Access and amend previously raised exceptions' text under the Raised card

  @DTOSS-9778 @DTOSS-9780
  Scenario: verify  total number, Not Raised link, text on Not Raised card
    Then they should see Not Raised as link on not raised card
    And the total number should be displayed on Not raised
    And they should be able to view 'Exceptions to be raised with teams' text under Not Raised card

  Scenario: verify  total number, Report link, text on Report card
    Then they should see Report as link on Report card
    And the total number should be displayed on Report card
    And they should be able to view 'To manage investigations into demographic changes' text under Report card

  Scenario: verify navigation to Raised exception Summary screen
    And the user clicks on Raised link
    Then they should navigate to 'Raised breast screening exceptions - Cohort Manager - NHS'
    And they should see 'Raised breast screening exceptions' on raised exception screen

  Scenario: verify navigation to Not Raised exception Summary screen
    And the user clicks on Not Raised link
    Then they should directed to 'Not raised breast screening exceptions - Cohort Manager - NHS'
    And they should see 'Not raised breast screening exceptions' on not raised exception screen

  Scenario: verify navigation to Report Summary screen
    And the user clicks on Report link
    Then they should lands on 'Reports - Cohort Manager - NHS'
    And they should see 'Reports' on Report screen

  Scenario: verify navigation to Contact us screen
    And the user clicks on contact us link
    Then they should navigate to contact us page

  Scenario: verify navigation to Terms and conditions screen
    And the user clicks on Terms and conditions link
    Then they should navigate to Terms and conditions page

  Scenario: verify navigation to cookies screen
    And the user clicks on cookies link
    Then they should navigate to cookies page
