#!/bin/bash

set -euo pipefail

cd "$(git rev-parse --show-toplevel)"

# This file is for you! Edit it to call your unit test suite. Note that the same
# file will be called if you run it locally as if you run it on CI.

# Replace the following line with something like:
#
#   rails test:unit
#   python manage.py test
#   npm run test
#
# or whatever is appropriate to your project. You should *only* run your fast
# tests from here. If you want to run other test suites, see the predefined
# tasks in scripts/test.mk.

UnitDir="tests/"
# ResDir="$UnitDir"results-unit
Format="trx"

find "$UnitDir" -name '*.csproj' | while read -r file; do
    dotnet test --no-build "$file" --logger $Format
done

# mkdir -p "$ResDir"
# find "$UnitDir" -name "*.$Format" -not -path "$ResDir/*" | while read -r resfile; do
#     mv "$resfile" "$ResDir"
# done

# echo "Unit tests are not yet implemented. See scripts/tests/unit.sh for more."
