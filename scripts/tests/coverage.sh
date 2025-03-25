#!/bin/bash
set -euo pipefail

# Move to the root directory of the repository.
cd "$(git rev-parse --show-toplevel)"
ROOT_DIR="$PWD"

# Run the tests and generate the coverage report.
dotnet test --collect:"XPlat Code Coverage" \
            /p:CollectCoverage=true \
            /p:CoverletOutput="${ROOT_DIR}/coverage/coverage.xml" \
            /p:CoverletOutputFormat=opencover

# Set the report output environment variable.
REPORT_OUTPUT="${ROOT_DIR}/coverage/coverage.xml"
export REPORT_OUTPUT

# Echo the path for the workflow to pick up.
echo "${REPORT_OUTPUT}"
