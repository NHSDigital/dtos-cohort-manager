#!/bin/bash
set -e
# Get input parameters
SONAR_PROJECT_KEY="$1"
SONAR_ORGANISATION_KEY="$2"
SONAR_TOKEN="$3"
COVERAGE_PATH="${4:-coverage}"
GITHUB_TOKEN="$5"
GITHUB_EVENT_NAME="$6"
GITHUB_HEAD_REF="$7"
GITHUB_BASE_REF="$8"
GITHUB_EVENT_PULL_REQUEST_NUMBER="$9"
GITHUB_REPOSITORY="${10}"
GITHUB_REF="${11}"
GITHUB_SHA="${12}"
UNIT_TEST_DIR="${13:-tests/UnitTests}"

# Debug information about environment
echo "===== DEBUG INFORMATION ====="
echo "GitHub event: $GITHUB_EVENT_NAME"
echo "GitHub ref: $GITHUB_REF"
echo "GitHub SHA: $GITHUB_SHA"
echo "Coverage path: $COVERAGE_PATH"
echo "Unit test directory: $UNIT_TEST_DIR"

# Get PR information for SonarCloud
if [[ "$GITHUB_EVENT_NAME" == "pull_request" || "$GITHUB_EVENT_NAME" == "pull_request_target" ]]; then
  PR_BRANCH="$GITHUB_HEAD_REF"
  PR_BASE="$GITHUB_BASE_REF"
  PR_KEY="$GITHUB_EVENT_PULL_REQUEST_NUMBER"
  echo "Running analysis for PR #${PR_KEY} from ${PR_BRANCH} into ${PR_BASE}"
  PR_ARGS="/d:sonar.pullrequest.key=${PR_KEY} /d:sonar.pullrequest.branch=${PR_BRANCH} /d:sonar.pullrequest.base=${PR_BASE} /d:sonar.pullrequest.github.repository=${GITHUB_REPOSITORY}"
else
  BRANCH_NAME="${GITHUB_REF#refs/heads/}"
  echo "Running analysis for branch ${BRANCH_NAME}"
  PR_ARGS="/d:sonar.branch.name=${BRANCH_NAME}"
fi

echo "PR arguments: ${PR_ARGS}"
echo "=========================="

# Ensure coverage directory exists
mkdir -p "$COVERAGE_PATH"

# Restore solution dependencies
echo "Restoring .NET dependencies..."
find . -name "*.sln" -exec dotnet restore {} \;

# Show explicit SonarCloud coverage paths to verify configuration
OPENCOVER_PATH="${COVERAGE_PATH}/*.xml"
VSCOVERAGE_PATH="${COVERAGE_PATH}/coverage.xml"
COBERTURA_PATH="${COVERAGE_PATH}/coverage.cobertura.xml"

echo "SonarCloud will look for coverage at these paths:"
echo "OpenCover: $OPENCOVER_PATH"
echo "VSCoverage: $VSCOVERAGE_PATH"
echo "Cobertura: $COBERTURA_PATH"

# Begin SonarScanner with coverage configuration and PR information
echo "Starting SonarScanner..."
dotnet sonarscanner begin \
/k:"${SONAR_PROJECT_KEY}" \
/o:"${SONAR_ORGANISATION_KEY}" \
/d:sonar.token="${SONAR_TOKEN}" \
/d:sonar.host.url="https://sonarcloud.io" \
/d:sonar.cs.opencover.reportsPaths="${OPENCOVER_PATH}" \
/d:sonar.cs.vscoveragexml.reportsPaths="${VSCOVERAGE_PATH}" \
/d:sonar.cs.cobertura.reportsPaths="${COBERTURA_PATH}" \
/d:sonar.python.version="3.8" \
/d:sonar.typescript.lcov.reportPaths="${COVERAGE_PATH}/lcov.info" \
/d:sonar.coverage.exclusions="**/*Tests.cs,**/Tests/**/*.cs,**/test/**/*.ts,**/tests/**/*.ts,**/*.spec.ts,**/*.test.ts" \
/d:sonar.tests="tests" \
/d:sonar.test.inclusions="**/*.spec.ts,**/*.test.ts,**/tests/**/*.ts" \
/d:sonar.verbose=true \
/d:sonar.log.level=DEBUG \
/d:sonar.scm.provider=git \
/d:sonar.scm.revision=${GITHUB_SHA} \
/d:sonar.scanner.scanAll=true \
${PR_ARGS}

# Build all solutions
echo "Building solutions..."
find . -name "*.sln" -exec dotnet build {} --no-restore \;

# Run consolidated tests to generate coverage
echo "Running consolidated tests to generate coverage..."
dotnet test "${UNIT_TEST_DIR}/ConsolidatedTests.csproj" \
--results-directory "${COVERAGE_PATH}" \
--logger "trx;LogFileName=TestResults.trx" \
--collect:"XPlat Code Coverage" \
--verbosity normal

# Debug coverage files after test run
echo "===== COVERAGE FILE VERIFICATION ====="
echo "Checking coverage files after test run..."
find "${COVERAGE_PATH}" -type f -name "*.xml" | sort
echo "Coverage file details:"
find "${COVERAGE_PATH}" -type f -name "*.xml" -exec ls -lh {} \;

# Look for coverage XML files specifically
echo "Looking for coverage report formats..."
find "${COVERAGE_PATH}" -type f -name "*coverage*.xml" | sort

# Check content of XML files to verify they contain coverage data
echo "Verifying coverage file content (first 10 lines):"
find "${COVERAGE_PATH}" -type f -name "*coverage*.xml" -exec sh -c "echo '=== {} ==='; head -n 10 {}" \;

# Check if SonarCloud expected files exist
echo "Checking if SonarCloud expected paths exist:"
[ -f "${COVERAGE_PATH}/coverage.xml" ] && echo "VSCoverage file exists: ${COVERAGE_PATH}/coverage.xml" || echo "VSCoverage file MISSING: ${COVERAGE_PATH}/coverage.xml"
[ -f "${COVERAGE_PATH}/coverage.cobertura.xml" ] && echo "Cobertura file exists: ${COVERAGE_PATH}/coverage.cobertura.xml" || echo "Cobertura file MISSING: ${COVERAGE_PATH}/coverage.cobertura.xml"
echo "OpenCover files:"
find "${COVERAGE_PATH}" -type f -name "*.xml" | grep -i opencover || echo "No OpenCover files found"
echo "===============================

# Rename coverage files to match SonarCloud expected paths if needed
echo "Renaming coverage files to match expected paths..."
for xml_file in $(find "${COVERAGE_PATH}" -name "*coverage*.xml"); do
  if [ ! -f "${COVERAGE_PATH}/coverage.xml" ] && [[ "$xml_file" == *coverage*.xml ]]; then
    echo "Copying $xml_file to ${COVERAGE_PATH}/coverage.xml for VSCoverage report"
    cp "$xml_file" "${COVERAGE_PATH}/coverage.xml"
  fi
  if [ ! -f "${COVERAGE_PATH}/coverage.cobertura.xml" ] && [[ "$xml_file" == *cobertura*.xml ]]; then
    echo "Copying $xml_file to ${COVERAGE_PATH}/coverage.cobertura.xml for Cobertura report"
    cp "$xml_file" "${COVERAGE_PATH}/coverage.cobertura.xml"
  fi
done

# Check coverage files again after renaming
echo "Coverage files after renaming:"
find "${COVERAGE_PATH}" -type f -name "*.xml" | sort

# End SonarScanner
echo "Ending SonarScanner analysis (with file verification)..."
dotnet sonarscanner end /d:sonar.token="${SONAR_TOKEN}" | tee sonar_end_output.log

# Check SonarScanner logs for coverage imports
echo "===== CHECKING SONARSCANNER LOGS FOR COVERAGE ====="
grep -i "coverage" sonar_end_output.log || echo "No coverage mentions in logs"
grep -i "parse" sonar_end_output.log || echo "No parsing mentions in logs"
grep -i "import" sonar_end_output.log || echo "No import mentions in logs"
grep -i "error" sonar_end_output.log || echo "No errors found in logs"
grep -i "warn" sonar_end_output.log || echo "No warnings found in logs"
echo "===============================

echo "Analysis complete."