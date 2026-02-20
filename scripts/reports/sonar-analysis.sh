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

# Store absolute path to coverage directory
COVERAGE_FULL_PATH="$(pwd)/${COVERAGE_PATH}"

# Restore dependencies (targeted to reduce disk usage)
if [[ "${FULL_RESTORE:-false}" == "true" ]]; then
  find . -name "*.sln" -exec dotnet restore {} \;
else
  if [[ -f "application/CohortManager/src/Functions/Functions.sln" ]]; then
    dotnet restore application/CohortManager/src/Functions/Functions.sln
  fi
  dotnet restore "${UNIT_TEST_DIR}/ConsolidatedTests.csproj"
fi

# Begin SonarScanner with coverage configuration and PR information
dotnet sonarscanner begin \
  /k:"${SONAR_PROJECT_KEY}" \
  /o:"${SONAR_ORGANISATION_KEY}" \
  /d:sonar.token="${SONAR_TOKEN}" \
  /d:sonar.host.url="https://sonarcloud.io" \
  /d:sonar.cs.opencover.reportsPaths="${COVERAGE_PATH}/**/*.xml" \
  /d:sonar.python.version="3.8" \
  /d:sonar.typescript.lcov.reportPaths="${COVERAGE_PATH}/lcov.info" \
  /d:sonar.exclusions="\
**/Migrations/**\
  " \
  /d:sonar.coverage.inclusions="**/*.cs" \
  /d:sonar.coverage.exclusions="\
**/*Tests.cs,\
**/Tests/**/*.cs,\
**/Program.cs,\
**/Model/**/*.cs,\
**/Set-up/**/*.cs,\
**/scripts/**/*.cs,\
**/*Config.cs,\
**/HealthCheckFunction.cs,\
**/bin/**/*.cs,\
**/obj/**/*.cs,\
**/Properties/**/*.cs,\
**/*.generated.cs,\
**/*.Designer.cs,\
**/*.g.cs,\
**/*.GlobalUsings.g.cs,\
**/Migrations/**,\
**/*.AssemblyInfo.cs\
" \
  /d:sonar.tests="tests" \
  /d:sonar.test.inclusions="**/*.spec.ts,**/*.test.ts,**/tests/**/*.ts" \
  /d:sonar.verbose=true \
  /d:sonar.scm.provider=git \
  /d:sonar.scm.revision=${GITHUB_SHA} \
  /d:sonar.scanner.scanAll=true \
  ${PR_ARGS}

# Build only the consolidated test project for coverage
dotnet build "${UNIT_TEST_DIR}/ConsolidatedTests.csproj" --no-restore

# Run consolidated tests to generate coverage
# This is critical - tests must run between SonarScanner begin and end commands
dotnet test "${UNIT_TEST_DIR}/ConsolidatedTests.csproj" \
  --no-restore \
  --results-directory "${COVERAGE_PATH}" \
  --logger "trx;LogFileName=TestResults.trx" \
  --collect:"XPlat Code Coverage;Format=opencover;Include=**/*.cs;ExcludeByFile=**/*Tests.cs,**/Tests/**/*.cs,**/Program.cs,**/Model/**/*.cs,**/Set-up/**/*.cs,**/scripts/**/*.cs,**/HealthCheckFunction.cs,**/*Config.cs,**/bin/**/*.cs,**/obj/**/*.cs,**/Properties/**/*.cs,**/*.generated.cs,**/*.Designer.cs,**/*.g.cs,**/*.GlobalUsings.g.cs,**/*.AssemblyInfo.cs" \
  --verbosity normal

# Run frontend tests to generate lcov coverage
echo "Running frontend tests to generate coverage"
if [ -d "application/CohortManager/src/Web" ]; then
  (
    cd application/CohortManager/src/Web || exit 1
    # Configure npm to better handle transient 429s and avoid extra work
    export npm_config_audit=false
    export npm_config_fund=false
    export npm_config_fetch_retries=5
    export npm_config_fetch_retry_factor=2
    export npm_config_fetch_retry_mintimeout=20000
    export npm_config_fetch_retry_maxtimeout=120000
    export npm_config_prefer_offline=true
    # Retry npm ci with simple backoff to survive registry rate limits
    n=0
    until [ "$n" -ge 3 ]
    do
      npm ci --ignore-scripts && break
      n=$((n+1))
      sleep $((n*15))
    done
    if [ "$n" -ge 3 ]; then
      echo "npm ci failed after retries" >&2
      exit 1
    fi
    npm run test:unit:coverage
    mkdir -p "${COVERAGE_FULL_PATH}"
    cp coverage/lcov.info "${COVERAGE_FULL_PATH}/lcov.info"
  )
  echo "Frontend test coverage generated at ${COVERAGE_PATH}/lcov.info"
else
  echo "Frontend directory not found, skipping frontend tests"
fi

# End SonarScanner
dotnet sonarscanner end /d:sonar.token="${SONAR_TOKEN}"
