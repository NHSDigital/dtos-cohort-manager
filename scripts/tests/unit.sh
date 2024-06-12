#!/bin/bash

set -euo pipefail

cd "$(git rev-parse --show-toplevel)"

dir="$PWD"
UnitDir="$dir/tests/"
ResDir="$UnitDir"results-unit
Format="trx"

# Find all *.csproj files and execute dotnet test, with build for now
find "$UnitDir" -name '*.csproj' | while read -r file; do
    echo -e "\nRunning unit tests for:\n$file"
    dotnet test "$file" --logger $Format --verbosity quiet
done

# Move all trx result files into a separate folder, for easier reporting
mkdir -p "$ResDir"
find "$UnitDir" -name "*.$Format" -not -path "$ResDir/*" | while read -r resfile; do
    mv "$resfile" "$ResDir"
done

# List created results
echo -e "\nCreated result files:\n"
find "$ResDir" -name "*.$Format"

# echo "Unit tests are not yet implemented. See scripts/tests/unit.sh for more."
