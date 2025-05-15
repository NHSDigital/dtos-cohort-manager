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

# Get PR information for SonarCloud
if [[ "$GITHUB_EVENT_NAME" == "pull_request" || "$GITHUB_EVENT_NAME" == "pull_request_target" ]]; then
  PR_BRANCH="$GITHUB_HEAD_REF"
  PR_BASE="$GITHUB_BASE_REF"
  PR_KEY="$GITHUB_EVENT_PULL_REQUEST_NUMBER"
  echo "Running analysis for PR #${PR_KEY} from ${PR_BRANCH} into ${PR_BASE}"
  PR_ARGS="/d:sonar.pullrequest.key=${PR_KEY} /d:sonar.pullrequest.branch=${PR_BRANCH} /d:sonar.pullrequest.base=${PR_BASE} /d:sonar.pullrequest.github.repository=${GITHUB_REPOSITORY}"
else
  BRANCH_NAME="${GITHUB_REF#refs/heads/}"
  if [[ "$BRANCH_NAME" != "main" && "$BRANCH_NAME" != "master" ]]; then
    echo "Running analysis for branch ${BRANCH_NAME}"
    PR_ARGS="/d:sonar.branch.name=${BRANCH_NAME}"
  else
    echo "Running analysis for main branch"
    PR_ARGS=""
  fi
fi

# Debug info
echo "GitHub event: $GITHUB_EVENT_NAME"
echo "PR arguments: ${PR_ARGS}"

# Restore solution dependencies
find . -name "*.sln" -exec dotnet restore {} \;

# Begin SonarScanner with coverage configuration and PR information
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
  ${PR_ARGS}

# Build all solutions
find . -name "*.sln" -exec dotnet build {} --no-restore \;

# End SonarScanner
dotnet sonarscanner end /d:sonar.token="${SONAR_TOKEN}"