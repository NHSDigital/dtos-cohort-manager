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

# Maximum number of parallel processes
# Adjust this based on your system's capabilities
MAX_PARALLEL=8

# Clean up existing coverage directory
rm -rf "$COVERAGE_DIR"

# Create required directories
mkdir -p "$COVERAGE_DIR/.temp"
mkdir -p "$ResDir"

# Clean up any existing test results to avoid confusion
rm -rf "$ResDir"/*

# Function to run a single test project
run_test() {
    local file="$1"
    local base=$(basename "$file" .csproj)

    echo -e "\nRunning unit tests for:\n$file"

    # Define individual coverage file
    local coverage_file="${COVERAGE_DIR}/.temp/${base}_coverage.cobertura.xml"

    # Create a separate directory for each test run to avoid conflicts
    local test_result_dir="$ResDir/$base"
    mkdir -p "$test_result_dir"

    # Run tests with Cobertura output
    set +e
    dotnet test "$file" --results-directory "$test_result_dir" --logger "trx;LogFileName=${base}.trx" --verbosity quiet \
    --collect:"XPlat Code Coverage"
    local test_exit=$?
    set -e

    # If test command failed for a reason other than the file error, report it
    if [ $test_exit -ne 0 ]; then
        echo "Warning: Test execution for $base exited with code $test_exit"
        # Continue anyway - we want to generate as much coverage as possible
    fi

    # Find the coverage file from the test run - searching recursively through the directory
    # Use find to get the coverage file, taking just the first one if multiple exist
    local coverage_search=$(find "$test_result_dir" -type f -name "coverage.cobertura.xml" | head -1)
    if [ -n "$coverage_search" ] && [ -f "$coverage_search" ]; then
        echo "Found coverage file: $coverage_search"
        cp "$coverage_search" "$coverage_file"
        echo "Copied to: $coverage_file"
    else
        echo "Warning: No coverage file generated for $file"
    fi
}

# Find all test projects
test_projects=($(find "$UnitDir" -name '*.csproj'))
total_tests=${#test_projects[@]}

echo "Found $total_tests test projects to run"

# Array to keep track of background processes
pids=()

# Counter for completed tests
completed=0

# Run tests in parallel with a maximum number of concurrent processes
for file in "${test_projects[@]}"; do
    # If we've reached MAX_PARALLEL, wait for one to finish before starting more
    if [ ${#pids[@]} -ge $MAX_PARALLEL ]; then
        # Use a more compatible approach without wait -n
        while [ ${#pids[@]} -ge $MAX_PARALLEL ]; do
            # Check each process to see if it has completed
            new_pids=()
            for pid in "${pids[@]}"; do
                if kill -0 $pid 2>/dev/null; then
                    # Process is still running, keep it in the array
                    new_pids+=($pid)
                else
                    # Process completed
                    ((completed++))
                    echo "A test has completed. Progress: $completed/$total_tests"
                fi
            done

            # If we found a completed process, break the loop
            if [ ${#new_pids[@]} -lt ${#pids[@]} ]; then
                pids=("${new_pids[@]}")
                break
            fi

            # Replace the old array with the new one anyway
            pids=("${new_pids[@]}")

            # If we didn't find any completed processes, sleep briefly and try again
            sleep 0.5
        done

        echo "Progress: $completed/$total_tests tests processed"
    fi

    # Run test in background
    run_test "$file" &

    # Store the PID
    pids+=($!)
done

# Wait for all remaining processes to finish
if [ ${#pids[@]} -gt 0 ]; then
    echo "Waiting for remaining tests to complete..."
    for pid in "${pids[@]}"; do
        wait $pid || true
        ((completed++))
    done
    echo "Progress: $completed/$total_tests tests processed"
fi

echo "All tests completed!"

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
