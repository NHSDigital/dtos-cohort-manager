Feature: testing Breast screening - Cohort Manager home page

Background:
 Given the User has logged in to the Cohort manager exceptions UI
 When they land on the 'Breast screening - Cohort Manager'

@DTOSS-9777 @DTOSS-9779 @DTOSS-9782
Scenario: verify  total number, Raised link, text on Raised card
 Then they should see Raised as link on raised card
 And the total number should be displayed on raised
 And they should be able to view 'Access and amend previously raised exceptions' text under the Raised card
