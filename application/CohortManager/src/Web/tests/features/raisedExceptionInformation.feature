Feature: Raised exceptions page

    Background:
      Given I am signed in as "test@nhs.net" with password "Password123"
      When I go to the page "/participant-information/4001"
      Then I should see the heading "Exception information"
      And I see the text "Local reference (exception ID): 4001"

    Scenario: Check for accessibility issues as a signed in user
      Given I should see the heading "Exception information"
      Then I should expect 0 accessibility issues

    Scenario: Check for raised exception information
        Given I see the text "Exception information"
        Then I see the text "Portal form used"
        And I see the text "Request to amend incorrect patient PDS record data"
        And I see the text "Exception status"
        Then I see the tag "Raised"
        Then I see the text "ServiceNow Case ID" in the element "service-now-case-label"
        And I see the link "Change ServiceNow Case ID" with the href "?edit=true#exception-status"

    Scenario: Check to make sure the exception status section is not present
        Given I should not see the secondary heading "Exception status"
        And I should not see the text input with label "Enter ServiceNow Case ID"
        And the button "Save and continue" should not be present

    Scenario: Check for the change link functionality
        Given I see the link "Change ServiceNow Case ID" with the href "?edit=true#exception-status"
        When I go to the page "/participant-information/4001?edit=true#exception-status"
        And I see the text "Local reference (exception ID): 4001"
        And I should see the secondary heading "Exception status"
        And I see the text input with label "Enter ServiceNow Case ID"
        And I see the button "Save and continue"

    Scenario: Check for breadcrumb navigation back to Raised breast screening exceptions page
      When I go to the page "/participant-information/3001"
      Then I see the link "Home"
      Then I see the link "Raised breast screening exceptions"
      When I click the link "Raised breast screening exceptions"
      Then I should see the heading "Raised breast screening exceptions"
