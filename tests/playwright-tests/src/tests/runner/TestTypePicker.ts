import { fail } from "assert";
import { runnerBasedEpic1TestScenariosAmend } from "../e2e/epic1-highpriority-tests/epic1-high-priority-testsuite-migrated";
import { runnerBasedEpic123TestScenariosAddAmend } from "../e2e/epic123-smoke-tests/epic123-smoke-tests-migrated";
import { runnerBasedEpic2TestScenariosAmend } from "../e2e/epic2-highpriority-tests/epic2-high-priority-testsuite-migrated";
import { runnerBasedEpic2MedTestScenariosAmend } from "../e2e/epic2-medpriority-tests/epic2-med-priority-testsuite-migrated";
import { runnerBasedEpic3TestScenariosAmend } from "../e2e/epic3-highpriority-tests/epic3-high-priority-testsuite-migrated";
import { runnerBasedEpic3MedTestScenariosAmend } from "../e2e/epic3-medpriority-tests/epic3-med-priority-testsuite-migrated";
import { runnerBasedEpic4dTestScenariosAmend } from "../e2e/epic4d-validation-tests/epic4d-6045-validation-testsuite-migrated";


export function TestTypePicker(
  TEST_TYPE: string
) 
{
    switch(TEST_TYPE) {
    case 'RegressionEpic1': 
    return runnerBasedEpic1TestScenariosAmend;
    case 'RegressionEpic2':
        return runnerBasedEpic2TestScenariosAmend;   
    case 'RegressionEpic2Med': 
        return runnerBasedEpic2MedTestScenariosAmend;
    case 'RegressionEpic3':
        return runnerBasedEpic3TestScenariosAmend;
    case 'RegressionEpic3Med': 
        return runnerBasedEpic3MedTestScenariosAmend;
    case 'RegressionEpic4d':
        return runnerBasedEpic4dTestScenariosAmend;
    case 'RegressionEpic4c': 
        return runnerBasedEpic4dTestScenariosAmend;   
    default:   
        return runnerBasedEpic123TestScenariosAddAmend;   
    }
    
}
