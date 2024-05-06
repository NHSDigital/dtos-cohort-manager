#!/bin/bash
# Read the recommendations from the extensions.json file
recommendations=$(jq -r '.recommendations[]' ../../../.vscode/extensions.json)

# Convert the recommendations to a bash array
extensions=($recommendations)

# Start the JSON array string
ext_array='['

# Add each extension to the JSON array string
for extension in "${extensions[@]}"; 
  do
    ext_array+="\"$extension\"," 
  done

# Remove the trailing comma and finish the JSON array string
ext_array="${ext_array%,}]"

# jq --argjson ext_array "$ext_array" '.customizations.vscode.extensions = $ext_array' devcontainer.json > modified.json
jq --argjson ext_array "$ext_array" '.customizations.vscode.extensions = $ext_array' ../../../.devcontainer/devcontainer.json > devcontainer_extensions.json