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

# Debug information about environment
echo "===== DEBUG INFORMATION ====="
echo "GitHub event: $GITHUB_EVENT_NAME"
echo "GitHub ref: $GITHUB_REF"
echo "GitHub SHA: $GITHUB_SHA"
echo "Coverage path: $COVERAGE_PATH"

# Check if coverage directory exists
echo "Checking coverage directory..."
if [ -d "$COVERAGE_PATH" ]; then
  echo "Coverage directory exists."
  echo "Coverage files found:"
  find "$COVERAGE_PATH" -type f -name "*.xml" | sort
  echo "Coverage file sizes:"
  find "$COVERAGE_PATH" -type f -name "*.xml" -exec ls -lh {} \;
else
  echo "WARNING: Coverage directory does not exist!"
  mkdir -p "$COVERAGE_PATH"
  echo "Created empty coverage directory."
fi

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

# Debug info
echo "GitHub event: $GITHUB_EVENT_NAME"
echo "PR arguments: ${PR_ARGS}"
echo "=========================="

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
/d:sonar.cs.cobertura.reportsPaths="${COVERAGE_PATH}/cobertura.xml" \
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

# Debug coverage files after build/test
echo "Checking coverage files after build/test..."
find "${COVERAGE_PATH}" -type f -name "*.xml" | sort
echo "Coverage file details:"
find "${COVERAGE_PATH}" -type f -name "*.xml" -exec ls -lh {} \;

# End SonarScanner
echo "Ending SonarScanner analysis..."
dotnet sonarscanner end /d:sonar.token="${SONAR_TOKEN}"

echo "Analysis complete."