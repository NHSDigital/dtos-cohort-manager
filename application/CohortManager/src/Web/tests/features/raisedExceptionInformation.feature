Feature: Raised exceptions page

    Scenario: Check for page heading
        When I go to the page "/participant-information/2028"
        Then I see the heading "Exception information"
        And I see the text "Local reference (exception ID): 2028"

    Scenario: Check for accessibility issues as a signed in user
      Given I should see the heading "Exception information"
      Then I should expect "0" accessibility issues

    Scenario: Check for the raised exception information
        Given I should see the text "Raised breast screening exceptions"
        Then I should see the text "Portal form used" with the text "Request to amend incorrect patient PDS record data"
        And I should see the text "Exception status" with the tag "Raised"
        And I should see the text "ServiceNow Case ID"
        And I should see the link "Change" with the URL "/participant-information/4001?edit=true#exception-status"

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

    Scenario: Check to make sure the exception status section is not present
        Given I should see the text "Not raised breast screening exceptions"
        Then I should not see the heading "Exception status"
        And I should not see the text input with label "Enter ServiceNow Case ID"
        And the button "Save and continue" should not be present

    Scenario: Check for the change link functionality
        Given I should see the link "Change" with the URL "/participant-information/4001?edit=true#exception-status"
        When I go to the page "/participant-information/4001?edit=true#exception-status"
        Then I should see the heading "Exception information"
        And I should see the text "Local reference (exception ID): 4001"
        And I should see the heading "Exception status"
        And I should see the text input with label "Enter ServiceNow Case ID"
        And I should see the button "Save and continue"

    Scenario: Check for breadcrumb navigation back to Raised breast screening exceptions page
      When I go to the page "/participant-information/3001"
      Then I see the link "Home" in the breadcrumb navigation
      Then I see the link "Raised breast screening exceptions" in the breadcrumb navigation
      When I click the link "Raised breast screening exceptions" in the breadcrumb navigation
      Then I see the heading "Raised breast screening exceptions"
