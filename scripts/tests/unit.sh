#!/bin/bash

set -euo pipefail

cd "$(git rev-parse --show-toplevel)"

dir="$PWD"
UnitDir="$dir/tests/"
ResDir="$UnitDir"results-unit
Format="trx"

find "$UnitDir" -name '*.csproj' | while read -r file; do
    echo -e "\nRunning unit tests for:\n$file"
    dotnet test "$file" --logger $Format --verbosity quiet
done

mkdir -p "$ResDir"
find "$UnitDir" -name "*.$Format" -not -path "$ResDir/*" | while read -r resfile; do
    mv "$resfile" "$ResDir"
done

echo -e "\nCreated result files:\n"
find "$ResDir" -name "*.$Format"

# echo "Unit tests are not yet implemented. See scripts/tests/unit.sh for more."
