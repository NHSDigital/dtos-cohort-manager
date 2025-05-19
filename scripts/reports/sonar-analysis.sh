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

# Ensure coverage directory exists
mkdir -p "$COVERAGE_PATH"

# Restore solution dependencies
find . -name "*.sln" -exec dotnet restore {} \;

# Begin SonarScanner with coverage configuration and PR information
dotnet sonarscanner begin \
/k:"${SONAR_PROJECT_KEY}" \
/o:"${SONAR_ORGANISATION_KEY}" \
/d:sonar.token="${SONAR_TOKEN}" \
/d:sonar.host.url="https://sonarcloud.io" \
/d:sonar.cs.opencover.reportsPaths="${COVERAGE_PATH}/**/*.xml" \
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
find . -name "*.sln" -exec dotnet build {} --no-restore \;

# Run consolidated tests to generate coverage
# This is critical - tests must run between SonarScanner begin and end commands
dotnet test "${UNIT_TEST_DIR}/ConsolidatedTests.csproj" \
  --results-directory "${COVERAGE_PATH}" \
  --logger "trx;LogFileName=TestResults.trx" \
  --collect:"XPlat Code Coverage;Format=opencover" \
  --verbosity normal

# End SonarScanner
dotnet sonarscanner end /d:sonar.token="${SONAR_TOKEN}"