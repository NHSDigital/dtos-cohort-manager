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
/d:sonar.cs.opencover.reportsPaths="${COVERAGE_PATH}/coverage.xml" \
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
  --collect:"XPlat Code Coverage;Format=opencover" \
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

# Look for OpenCover XML files and copy to standard location
OPENCOVER_FILE=$(find "${COVERAGE_PATH}" -name "coverage.opencover.xml" | head -n 1)
if [ -n "$OPENCOVER_FILE" ]; then
  echo "Found OpenCover file: ${OPENCOVER_FILE}"
  cp "${OPENCOVER_FILE}" "${COVERAGE_PATH}/coverage.xml"
  echo "Copied to ${COVERAGE_PATH}/coverage.xml"
else
  echo "No OpenCover file found, looking for any XML..."
  
  # Fallback to any XML file if no specific format found
  ANY_XML=$(find "${COVERAGE_PATH}" -name "*.xml" -not -name "TestResults.xml" | head -n 1)
  if [ -n "$ANY_XML" ]; then
    echo "Found XML file: ${ANY_XML}"
    cp "${ANY_XML}" "${COVERAGE_PATH}/coverage.xml"
    echo "Copied to ${COVERAGE_PATH}/coverage.xml"
  else
    echo "No suitable XML coverage file found"
  fi
fi

# Check if the coverage files now exist
echo "Checking if coverage file exists at expected path:"
[ -f "${COVERAGE_PATH}/coverage.xml" ] && echo "Coverage file exists: ${COVERAGE_PATH}/coverage.xml" || echo "Coverage file MISSING: ${COVERAGE_PATH}/coverage.xml"

# Fix any XML issues
echo "Fixing potential XML issues..."
if [ -f "${COVERAGE_PATH}/coverage.xml" ]; then
  # Create a backup
  cp "${COVERAGE_PATH}/coverage.xml" "${COVERAGE_PATH}/coverage.xml.bak"
  
  # Fix common XML issues - remove BOM and invalid characters
  # This uses 'sed' to remove the BOM marker if present
  sed -i '1s/^\xEF\xBB\xBF//' "${COVERAGE_PATH}/coverage.xml" || true
  
  # Only keep valid XML characters
  tr -cd '\11\12\15\40-\176' < "${COVERAGE_PATH}/coverage.xml.bak" > "${COVERAGE_PATH}/coverage.xml.tmp" || true
  mv "${COVERAGE_PATH}/coverage.xml.tmp" "${COVERAGE_PATH}/coverage.xml" || true
  
  # Verify if fixed
  echo "Checking if XML is now valid:"
  xmllint --noout "${COVERAGE_PATH}/coverage.xml" && echo "✅ XML is now valid" || echo "❌ XML still has issues - continuing anyway"
fi

# Log coverage file info
echo "===== COVERAGE FILE DETAILS ====="
echo "Coverage file details:"
ls -lh "${COVERAGE_PATH}/coverage.xml" || echo "File not found"
echo "First 20 lines of coverage file:"
head -n 20 "${COVERAGE_PATH}/coverage.xml" || echo "Cannot display file content"

# End SonarScanner - REMOVED the duplicate command and the verbose parameter
echo "Ending SonarScanner analysis..."
dotnet sonarscanner end /d:sonar.token="${SONAR_TOKEN}" | tee sonar_output.log

# Check for coverage mentions in output
echo "Checking for coverage mentions in SonarScanner output:"
grep -i "coverage\|parsing\|report" sonar_output.log || echo "No coverage-related terms found in logs"

echo "Analysis complete."