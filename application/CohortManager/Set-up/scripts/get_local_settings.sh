#!/bin/bash

# Define the root directory as the parent of the current directory
ROOT_DIR="$(dirname "$(dirname "$(pwd)")")"

# Define the target directory as the parent directory of the current directory
TARGET_DIR="../"

# Define the output script file name
OUTPUT_SCRIPT="recreate_local_settings.sh"

# Start the output script with a shebang
echo "#!/bin/bash" > $OUTPUT_SCRIPT

# Function to process files in the root directory
process_files() {
    local current_dir="$1"
    local target_dir="$2"

    # Find all files named local.settings.json in the current directory
    local files=$(find "$current_dir" -type f -name "local.settings.json")
    local file_count=$(echo "$files" | wc -l)
    local index=0

    for file in $files; do
        index=$((index+1))
        # Calculate the relative path from the root to the file
        relative_path="${file#$ROOT_DIR/}"
        # Calculate the target path for the file
        target_file="$target_dir/$relative_path"
        # Add a command to the output script to create the target directory and file with its content
        echo "mkdir -p \"$(dirname "$target_file")\"" >> $OUTPUT_SCRIPT
        # Specify the content for the local.settings.json file
        echo "cat > \"$target_file\" << EOF" >> $OUTPUT_SCRIPT
        # Read the content of the found local.settings.json file and echo it into the target file
        cat "$file" >> $OUTPUT_SCRIPT
        # Check if this is the last file and add EOF accordingly
        if [ $index -lt $file_count ]; then
            echo -e "\nEOF" >> $OUTPUT_SCRIPT
        fi
    done
}

# Start processing from the root directory
process_files "$ROOT_DIR" "$TARGET_DIR"

# Make the output script executable
chmod +x $OUTPUT_SCRIPT

echo "Script $OUTPUT_SCRIPT created. Run it to recreate the structure and files with their content in the parent directory."
