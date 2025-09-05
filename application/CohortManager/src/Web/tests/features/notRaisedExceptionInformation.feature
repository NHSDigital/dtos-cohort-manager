Feature: Not raised exceptions page

    Background:
      Given I am signed in as "test@nhs.net" with password "Password123"
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
