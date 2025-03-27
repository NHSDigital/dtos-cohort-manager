#!/bin/bash
set -euo pipefail
# Go to the root of the repository
cd "$(git rev-parse --show-toplevel)"
dir="$PWD"
UnitDir="$dir/tests/UnitTests/"
ResDir="${UnitDir}results-unit"
Format="trx"
COVERAGE_DIR="${dir}/coverage"
FINAL_COVERAGE_COBERTURA="${COVERAGE_DIR}/functions.coverage.cobertura.xml"

# Clean up existing coverage directory
rm -rf "$COVERAGE_DIR"

# Create required directories
mkdir -p "$COVERAGE_DIR/.temp"
mkdir -p "$ResDir"

# Clean up any existing test results to avoid confusion
rm -rf "$ResDir"/*

# Run tests and generate separate Cobertura reports
find "$UnitDir" -name '*.csproj' | while read -r file; do
  echo -e "\nRunning unit tests for:\n$file"
  base=$(basename "$file" .csproj)

  # Define individual coverage file
  coverage_file="${COVERAGE_DIR}/.temp/${base}_coverage.cobertura.xml"

  # Create a separate directory for each test run to avoid conflicts
  test_result_dir="$ResDir/$base"
  mkdir -p "$test_result_dir"

  # Run tests with Cobertura output
  set +e
  dotnet test "$file" --results-directory "$test_result_dir" --logger "trx;LogFileName=${base}.trx" --verbosity quiet \
    --collect:"XPlat Code Coverage"
  test_exit=$?
  set -e

  # If test command failed for a reason other than the file error, report it
  if [ $test_exit -ne 0 ]; then
    echo "Warning: Test execution for $base exited with code $test_exit"
    # Continue anyway - we want to generate as much coverage as possible
  fi

  # Find the coverage file from the test run - searching recursively through the directory
  # Use find to get the coverage file, taking just the first one if multiple exist
  coverage_search=$(find "$test_result_dir" -type f -name "coverage.cobertura.xml" | head -1)

  if [ -n "$coverage_search" ] && [ -f "$coverage_search" ]; then
    echo "Found coverage file: $coverage_search"
    cp "$coverage_search" "$coverage_file"
    echo "Copied to: $coverage_file"
  else
    echo "Warning: No coverage file generated for $file"
  fi
done

# Merge all Cobertura reports into one if any exist
if ls "${COVERAGE_DIR}/.temp"/*.cobertura.xml 1> /dev/null 2>&1; then
  echo -e "\nMerging Cobertura reports..."
  # Ensure the report generator tool is available
  dotnet tool install -g dotnet-reportgenerator-globaltool || true

  # Create a temporary file to filter the output
  TEMP_LOG=$(mktemp)

  # Run ReportGenerator and filter out Azure Functions temporary file errors
  reportgenerator -reports:"${COVERAGE_DIR}/.temp/*.cobertura.xml" -targetdir:"${COVERAGE_DIR}/.temp" -reporttypes:Cobertura 2>&1 | tee "$TEMP_LOG" | grep -v "does not exist (any more)"

  # Check if the merged report was generated
  if [ -f "${COVERAGE_DIR}/.temp/Cobertura.xml" ]; then
    mv "${COVERAGE_DIR}/.temp/Cobertura.xml" "$FINAL_COVERAGE_COBERTURA"
    echo "Generated merged coverage report: $FINAL_COVERAGE_COBERTURA"
    # Clean up temporary files, leaving only the final report
    rm -rf "${COVERAGE_DIR}/.temp"
  else
    echo "Warning: Failed to generate merged coverage report"
    # Show the full log if we failed
    echo "Full ReportGenerator log:"
    cat "$TEMP_LOG"
  fi

  # Clean up
  rm -f "$TEMP_LOG"
else
  echo "Warning: No coverage files found to merge"
fi

# Collect all TRX files into the main results directory
find "$ResDir" -name "*.${Format}" -type f | while read -r resfile; do
  # Copy to main results directory (preserving all TRX files)
  basename_file=$(basename "$resfile")
  cp "$resfile" "$ResDir/$basename_file"
done

# List created TRX result files for verification.
echo -e "\nCreated TRX result files in ${ResDir}:\n"
find "$ResDir" -maxdepth 1 -name "*.${Format}"
echo "Test execution complete."
