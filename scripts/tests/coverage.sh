#!/bin/bash

set -euo pipefail

# Move to the root directory of the repository.
cd "$(git rev-parse --show-toplevel)"

# Run the tests and generate the coverage report.
dotnet test --collect:"XPlat Code Coverage" \
            /p:CollectCoverage=true \
            /p:CoverletOutput="coverage/coverage.xml" \
            /p:CoverletOutputFormat=opencover

# Set the report output environment variable.
REPORT_OUTPUT="$(pwd)/coverage/coverage.xml"
export REPORT_OUTPUT

# Echo the path for the workflow to pick up.
echo "${REPORT_OUTPUT}"
