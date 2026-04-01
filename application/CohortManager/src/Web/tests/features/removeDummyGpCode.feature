Feature: Remove Dummy GP Code page

  Background:
    Given I sign in with a test account

  Scenario: Form is visible with expected fields and actions
    When I go to the page "/remove-dummy-gp-code"
    Then I should see the heading "Remove Dummy GP Code"
    And I see the link "Home" with the href "/"
    And I see the input with label "NHS Number"
    And I see the text "Forename"
    And I see the text "Surname"
    And I see the text "Date of Birth"
    And I see the text "Service Now Ticket Number"
    And I see the button "Submit"

  Scenario: Page has no accessibility issues when signed in
    When I go to the page "/remove-dummy-gp-code"
    Then I should see the heading "Remove Dummy GP Code"
    And I should expect 0 accessibility issues

  Scenario: Empty submit shows the NHS Number validation error
    When I go to the page "/remove-dummy-gp-code"
    And I click the button "Submit"
    Then I should see the error summary with message "NHS Number is required"
    And I should see the inline error message "NHS Number is required"

  Scenario: Success confirmation panel is shown when success query parameter is present
    When I go to the page "/remove-dummy-gp-code?success=true"
    Then I should see the heading "Request submitted successfully"
    And I see the text "Your request to remove the dummy GP Code has been accepted."
