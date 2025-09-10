Feature: Not raised exceptions page

    Background:
        Given I sign in with a test account
      When I go to the page "/participant-information/2028"
      Then I should see the heading "Exception information"
      And I see the text "Local reference (exception ID): 2028"

    Scenario: Check for accessibility issues as a signed in user
      Given I should see the heading "Exception information"
      Then I should expect 0 accessibility issues

    Scenario: Check for the portal form used
        Given I should see the heading "Exception information"
        Then I see the text "Portal form: Request to amend incorrect patient PDS record data"

    Scenario: Check for not raised exception participant details
        Given I see the text "Not raised breast screening exceptions"
        Then I should see the secondary heading "Participant details"

    Scenario: Check for not raised exception details
        Given I see the text "Not raised breast screening exceptions"
        Then I should see the secondary heading "Exception details"

    Scenario: Check for exception status section
        Given I see the text "Exception information"
        Then I should see the secondary heading "Exception status"
        And I see the text input with label "Enter ServiceNow Case ID"
        And I see the button "Save and continue"

    Scenario: Check for the portal form used for a CaaS exception
        When I go to the page "/participant-information/2020"
        Given I see the text "Exception information"
        Then I see the text "Portal form: Raise with Cohorting as a Service (CaaS)"

    Scenario: Check for the portal form used for a BSS exception
        When I go to the page "/participant-information/2034"
        Given I see the text "Exception information"
        Then I see the text "Portal form: Raise with Breast Screening Select (BSS)"

    Scenario: Check for breadcrumb navigation back to Not raised breast screening exceptions page
      When I go to the page "/participant-information/2032"
      Then I see the link "Home"
      Then I see the link "Not raised breast screening exceptions"
      When I click the link "Not raised breast screening exceptions"
      Then I should see the heading "Not raised breast screening exceptions"

    Scenario: Invalid ServiceNow Case ID input shows error message
        Given I go to the page "/participant-information/2028"
        And I fill the input with label "Enter ServiceNow Case ID" with "<input>"
        And I click the button "Save and continue"
        Then I should see the error summary with message "<error>"
        And I should see the inline error message "<error>"

        Examples:
            | input         | error                                                                 |
            |              | ServiceNow case ID is required                                        |
            | CS06191      | ServiceNow case ID must be nine characters or more                    |
            | CS0619153A   | ServiceNow case ID must start with two letters followed by at least seven digits (e.g. CS0619153) |
            | CS 0619153   | ServiceNow case ID must not contain spaces                            |
            | C$0619153    | ServiceNow case ID must only contain letters and numbers              |
