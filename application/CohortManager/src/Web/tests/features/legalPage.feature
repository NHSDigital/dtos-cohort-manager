@legalPage
Feature: testing Breast screening - Cohort Manager Legal pages

  Background:
    Given the User has login and navigate to terms and conditions page

  @smoke
  Scenario: verify navigation to Terms and conditions screen
    When the user clicks on Terms and conditions link
    Then they should navigate to Terms and conditions page

  Scenario: verify navigation to cookies screen
    When the user clicks on cookies link
    Then they should navigate to cookies page

  Scenario: verify navigation to Contact us screen
    And the user clicks on contact us link
    Then they should navigate to contact us page

  Scenario: verify navigation to Care Identity Service (CIS)
    When the user clicks on Care Identity Service link
    Then they should navigate to Care Identity Service - NHS England Digital page

  Scenario: verify navigation to terms and conditions for Care Identity Service and NHS Spine users
    When the user clicks on CIS and NHS Spine terms and conditions link
    Then they should navigate to terms and conditions for Care Identity Service and NHS Spine users page

  Scenario: verify navigation cookies page
    When the user clicks on cookies policy link
    Then they should navigate to cookies page
