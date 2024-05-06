#!/bin/bash

# Prompt the user for the name
echo "Please enter your name:"
read user_name

# Prompt the user for their email address
echo "Please enter your email address (the same as the email address in your GitHub account):"
read user_email

# Prompt the user for their passphrase in a secure way
echo "Please enter your GPG Key passphrase (memorize it or add it to a password manager):"
read -s gpg_pass

# Setting the proper git config, local and global
git config user.name $user_name
git config user.email $user_email

git config --global user.name $user_name
git config --global user.email $user_email

git config --global commit.gpgsign true

# Generating the GPG key

file=$(echo $user_email | sed "s/[^[:alpha:]]/-/g")

mkdir -p "$HOME/.gnupg"
chmod 0700 "$HOME/.gnupg"
cd "$HOME/.gnupg"
cat > "$file.gpg-key.script" <<EOF
  %echo Generating a GPG key
  Key-Type: ECDSA
  Key-Curve: nistp256
  Subkey-Type: ECDH
  Subkey-Curve: nistp256
  Name-Real: $user_name
  Name-Email: $user_email
  Expire-Date: 0
  Passphrase: $gpg_pass
  %commit
  %echo done
EOF
gpg --batch --generate-key "$file.gpg-key.script"
rm "$file.gpg-key.script"

# Adding a proper environmental variable to bash profile
[ -f ~/.bashrc ] && echo -e '\nexport GPG_TTY=$(tty)' >> ~/.bashrc
source ~/.bashrc

# Finished, remainings steps as below:
echo "*** Key generated, remember about: "
echo "- activating it in local git config: [ git config --global user.signingkey YOUR_LONG_KEY ]"
echo "- adding your GPG key in the GitHub Account settings: [ pg --armor --export YOUR_LONG_KEY ]"
