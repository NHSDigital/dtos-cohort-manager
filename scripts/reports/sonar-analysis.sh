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

# Begin SonarScanner with coverage configuration and PR information
echo "Starting SonarScanner..."
dotnet sonarscanner begin \
/k:"${SONAR_PROJECT_KEY}" \
/o:"${SONAR_ORGANISATION_KEY}" \
/d:sonar.token="${SONAR_TOKEN}" \
/d:sonar.host.url="https://sonarcloud.io" \
/d:sonar.cs.opencover.reportsPaths="${COVERAGE_PATH}/*.xml" \
/d:sonar.cs.vscoveragexml.reportsPaths="${COVERAGE_PATH}/coverage.xml" \
/d:sonar.cs.cobertura.reportsPaths="${COVERAGE_PATH}/coverage.cobertura.xml" \
/d:sonar.python.version="3.8" \
/d:sonar.typescript.lcov.reportPaths="${COVERAGE_PATH}/lcov.info" \
/d:sonar.coverage.exclusions="**/*Tests.cs,**/Tests/**/*.cs,**/test/**/*.ts,**/tests/**/*.ts,**/*.spec.ts,**/*.test.ts" \
/d:sonar.tests="tests" \
/d:sonar.test.inclusions="**/*.spec.ts,**/*.test.ts,**/tests/**/*.ts" \
/d:sonar.verbose=true \
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

# Debug coverage files and directories after test run
echo "===== COVERAGE PATH CONTENT ====="
echo "Listing all directories in ${COVERAGE_PATH}:"
find "${COVERAGE_PATH}" -type d | sort

echo "Listing all files in ${COVERAGE_PATH} recursively:"
find "${COVERAGE_PATH}" -type f | sort

# Find and copy coverage files to expected locations
echo "Finding and copying coverage files to SonarCloud expected locations..."

# Find all XML files and show their file sizes
echo "All XML files found:"
find "${COVERAGE_PATH}" -name "*.xml" -type f -exec ls -lh {} \;

# Find coverage files generated in subdirectories
# The coverage files are typically in GUID-named folders under the results directory
echo "Locating coverage files in GUID subdirectories..."

# Look for cobertura XML files
COBERTURA_FILE=$(find "${COVERAGE_PATH}" -name "coverage.cobertura.xml" | head -n 1)
if [ -n "$COBERTURA_FILE" ]; then
  echo "Found Cobertura file: ${COBERTURA_FILE}"
  cp "${COBERTURA_FILE}" "${COVERAGE_PATH}/coverage.cobertura.xml"
  echo "Copied to ${COVERAGE_PATH}/coverage.cobertura.xml"
else
  echo "No Cobertura file found"
fi

# Look for regular coverage XML files (might be named differently)
COVERAGE_XML=$(find "${COVERAGE_PATH}" -name "*.xml" -not -name "coverage.cobertura.xml" -not -name "TestResults.xml" | head -n 1)
if [ -n "$COVERAGE_XML" ]; then
  echo "Found coverage XML file: ${COVERAGE_XML}"
  cp "${COVERAGE_XML}" "${COVERAGE_PATH}/coverage.xml"
  echo "Copied to ${COVERAGE_PATH}/coverage.xml"
else
  echo "No coverage XML file found"
fi

# Check if the coverage files now exist at expected locations
echo "Checking if SonarCloud expected paths exist after copying:"
[ -f "${COVERAGE_PATH}/coverage.xml" ] && echo "VSCoverage file exists: ${COVERAGE_PATH}/coverage.xml" || echo "VSCoverage file MISSING: ${COVERAGE_PATH}/coverage.xml"
[ -f "${COVERAGE_PATH}/coverage.cobertura.xml" ] && echo "Cobertura file exists: ${COVERAGE_PATH}/coverage.cobertura.xml" || echo "Cobertura file MISSING: ${COVERAGE_PATH}/coverage.cobertura.xml"

# After copying coverage files, before ending SonarScanner
echo "===== DETAILED COVERAGE FILE INFORMATION ====="
echo "Complete listing of all XML files in ${COVERAGE_PATH}:"
find "${COVERAGE_PATH}" -name "*.xml" -type f -exec ls -lh {} \; | tee coverage_files.log

echo "Content sample of coverage.cobertura.xml (if exists):"
if [ -f "${COVERAGE_PATH}/coverage.cobertura.xml" ]; then
  head -n 20 "${COVERAGE_PATH}/coverage.cobertura.xml"
  echo "File size: $(du -h ${COVERAGE_PATH}/coverage.cobertura.xml | cut -f1)"
  echo "Check if it's a valid XML file:"
  xmllint --noout "${COVERAGE_PATH}/coverage.cobertura.xml" && echo "Valid XML" || echo "Invalid XML"
else
  echo "File does not exist"
fi

echo "Content sample of coverage.xml (if exists):"
if [ -f "${COVERAGE_PATH}/coverage.xml" ]; then
  head -n 20 "${COVERAGE_PATH}/coverage.xml"
  echo "File size: $(du -h ${COVERAGE_PATH}/coverage.xml | cut -f1)"
  echo "Check if it's a valid XML file:"
  xmllint --noout "${COVERAGE_PATH}/coverage.xml" && echo "Valid XML" || echo "Invalid XML"
else
  echo "File does not exist"
fi

# Capture verbose SonarScanner output 
echo "Ending SonarScanner analysis with full debug output..."
dotnet sonarscanner end /d:sonar.token="${SONAR_TOKEN}" /d:sonar.verbose=true | tee sonar_full_output.log
grep -i "coverage\|cobertura\|opencover\|vscoverage" sonar_full_output.log || echo "No coverage-related terms found in logs"

# End SonarScanner
echo "Ending SonarScanner analysis..."
dotnet sonarscanner end /d:sonar.token="${SONAR_TOKEN}"

echo "Analysis complete."