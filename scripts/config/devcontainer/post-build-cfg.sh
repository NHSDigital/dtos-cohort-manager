echo "Post build commands..."
sudo apt-get update
sudo apt-get install gnupg2 -y
git clone https://github.com/asdf-vm/asdf.git ~/.asdf --branch v0.14.0
echo '. "$HOME/.asdf/asdf.sh"' >> ~/.bashrc
echo '. "$HOME/.asdf/completions/asdf.bash"' >> ~/.bashrc
source ~/.bashrc
git config --global --add safe.directory $PWD
make config
