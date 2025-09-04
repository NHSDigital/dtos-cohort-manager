Feature: Not raised exceptions page

    Scenario: Check for page heading
        When I go to the page "/participant-information/2028"
        Then I see the heading "Exception information"
        And I see the text "Local reference (exception ID): 2028"

    Scenario: Check for accessibility issues as a signed in user
      Given I should see the heading "Exception information"
      Then I should expect "0" accessibility issues

    Scenario: Check for the portal form used
        Given I should see the text "Raised breast screening exceptions"
        Then I should see the text "Portal form: Request to amend incorrect patient PDS record data"

    Scenario: Check for not raised exception participant details
        Given I should see the text "Not raised breast screening exceptions"
        Then I should see the heading "Participant details"
        And I should see the values in the "participant-details-section" list:
          | NHS Number       |
          | Surname   |
          | Forename  |
          | Date of birth      |
          | Gender            |
          | Registered practice code       |

    Scenario: Check for not raised exception details
        Given I should see the text "Not raised breast screening exceptions"
        Then I should see the heading "Exception details"
        And I should see the values in the "exception-details-section" list:
          | Date exception created |
          | More detail	       |
          | ServiceNowId  |
        And the ServiceNow ID should have the text "Not raised"

    Scenario: Check for exception status section
        Given I should see the text "Not raised breast screening exceptions"
        Then I should see the heading "Exception status"
        And I should see the text input with label "Enter ServiceNow Case ID"
        And the button "Save and continue" should be present

    Scenario: Check for the portal form used for a CaaS exception
        When I go to the page "/participant-information/2020"
        Given I should see the text "Raised breast screening exceptions"
        Then I should see the text "Portal form: Raise with Cohorting as a Service (CaaS)"

    Scenario: Check for the portal form used for a BSS exception
        When I go to the page "/participant-information/2034"
        Given I should see the text "Raised breast screening exceptions"
        Then I should see the text "Portal form: Raise with Breast Screening Select (BSS)"

    Scenario: Check for breadcrumb navigation back to Not raised breast screening exceptions page
      When I go to the page "/participant-information/2032"
      Then I see the link "Home" in the breadcrumb navigation
      Then I see the link "Not raised breast screening exceptions" in the breadcrumb navigation
      When I click the link "Not raised breast screening exceptions" in the breadcrumb navigation
      Then I see the heading "Not raised breast screening exceptions"
