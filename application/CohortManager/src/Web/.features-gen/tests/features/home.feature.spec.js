// Generated from: tests/features/home.feature
import { test } from "../../../tests/features/steps/fixtures.ts";

test.describe('testing Breast screening - Cohort Manager home page', () => {

  test.beforeEach('Background', async ({ Given, page, When }) => {
    await Given('the User has logged in to the Cohort manager exceptions UI', null, { page }); 
    await When('they land on the \'Breast screening - Cohort Manager\'', null, { page }); 
  });
  
  test('verify  total number, Raised link, text on Raised card', { tag: ['@DTOSS-9777', '@DTOSS-9779', '@DTOSS-9782'] }, async ({ Then, And }) => { 
    await Then('they should see Raised as link on raised card'); 
    await And('the total number should be displayed on raised'); 
    await And('they should be able to view \'Access and amend previously raised exceptions\' text under the Raised card'); 
  });

});

// == technical section ==

test.use({
  $test: ({}, use) => use(test),
  $uri: ({}, use) => use('tests/features/home.feature'),
  $bddFileData: ({}, use) => use(bddFileData),
});

const bddFileData = [ // bdd-data-start
  {"pwTestLine":11,"pickleLine":8,"tags":["@DTOSS-9777","@DTOSS-9779","@DTOSS-9782"],"steps":[{"pwStepLine":7,"gherkinStepLine":4,"keywordType":"Context","textWithKeyword":"Given the User has logged in to the Cohort manager exceptions UI","isBg":true,"stepMatchArguments":[]},{"pwStepLine":8,"gherkinStepLine":5,"keywordType":"Action","textWithKeyword":"When they land on the 'Breast screening - Cohort Manager'","isBg":true,"stepMatchArguments":[{"group":{"start":17,"value":"'Breast screening - Cohort Manager'","children":[{"children":[{"children":[]}]},{"start":18,"value":"Breast screening - Cohort Manager","children":[{"children":[]}]}]},"parameterTypeName":"string"}]},{"pwStepLine":12,"gherkinStepLine":9,"keywordType":"Outcome","textWithKeyword":"Then they should see Raised as link on raised card","stepMatchArguments":[]},{"pwStepLine":13,"gherkinStepLine":10,"keywordType":"Outcome","textWithKeyword":"And the total number should be displayed on raised","stepMatchArguments":[]},{"pwStepLine":14,"gherkinStepLine":11,"keywordType":"Outcome","textWithKeyword":"And they should be able to view 'Access and amend previously raised exceptions' text under the Raised card","stepMatchArguments":[{"group":{"start":28,"value":"'Access and amend previously raised exceptions'","children":[{"children":[{"children":[]}]},{"start":29,"value":"Access and amend previously raised exceptions","children":[{"children":[]}]}]},"parameterTypeName":"string"}]}]},
]; // bdd-data-end