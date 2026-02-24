#!/bin/bash

# Exit immediately if a command exits with a non-zero status.
# Treat unset variables as an error and prevent errors in a pipeline from being masked.
set -euo pipefail

echo "🚀 Starting post-build setup..."

# --- Package Installation ---
# Update package lists and install necessary dependencies.
echo "📦 Updating packages and installing dependencies (gnupg2, git, make)..."
sudo apt-get update
sudo apt-get install -y --no-install-recommends gnupg2 git make

# --- ASDF Installation ---
ASDF_DIR="$HOME/.asdf"
ASDF_VERSION="v0.15.0"

echo " cloning asdf version ${ASDF_VERSION}..."
git clone https://github.com/asdf-vm/asdf.git "$ASDF_DIR" --branch "$ASDF_VERSION"

# --- Shell Configuration ---
BASHRC_FILE="$HOME/.bashrc"

echo "✍️ Adding asdf configuration to ${BASHRC_FILE}..."
echo -e "\n# --- ASDF Setup ---" >> "$BASHRC_FILE"
echo '. "$HOME/.asdf/asdf.sh"' >> "$BASHRC_FILE"
echo '. "$HOME/.asdf/completions/asdf.bash"' >> "$BASHRC_FILE"

# Source asdf for the current script session to make it available immediately.
. "$HOME/.asdf/asdf.sh"

# --- Git Configuration ---
# Add the current working directory to Git's safe directories.
echo "🔒 Configuring Git safe directory..."
git config --global --add safe.directory "$PWD"

# --- Install Azure CLI ---
echo "⚙️ Installing Azure CLI..."
curl -sL https://aka.ms/InstallAzureCLIDeb --proto "=https" | sudo bash

# --- Project Setup ---
echo "⚙️ Running project-specific configuration..."
make config

# --- GPG Key Verification ---
# Making sure the GPG key sharing from your host is working as expected.
# This will fail if no secret key is available, causing the script to exit.
echo "🔑 Verifying GPG key access..."
echo "test" | gpg --clearsign > /dev/null

echo "✅ Post-build setup completed successfully!"
