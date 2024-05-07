# Guide: Sign Git commits

- [Guide: Sign Git commits](#guide-sign-git-commits)
  - [Overview](#overview)
  - [Signing commits using GPG](#signing-commits-using-gpg)
    - [Generate GPG key](#generate-gpg-key)
    - [Importing your GPG key](#importing-your-gpg-key)
    - [Configure Git](#configure-git)
    - [Configure GitHub](#configure-github)
    - [Troubleshooting](#troubleshooting)
    - [Additional settings](#additional-settings)
  - [Signing commits using SSH](#signing-commits-using-ssh)
    - [Generate SSH key](#generate-ssh-key)
    - [Configure Git](#configure-git-1)
    - [Configure GitHub](#configure-github-1)
  - [Testing](#testing)

## Overview

Signing Git commits is a good practice and ensures the correct web of trust has been established for the distributed version control management, e.g. [Bitwarden](https://bitwarden.com/).

There are two ways to sign commits in GitHub, using a GPG or an SSH signature. Detailed information about this can be found in the following [documentation](https://docs.github.com/en/authentication/managing-commit-signature-verification/about-commit-signature-verification). It is recommended to use the GPG method for signing commits as GPG keys can be set to expire or be revoked if needed. Below is a step-by-step guide on how to set it up.

## Signing commits using GPG

### Generate GPG key

First check if you have the GPG command line tool installed by running the below command.

```shell
gpg --version
```

If you already have the GPG command line tool installed you will see an output similar to the below telling you the information about your installed version. If you do not get an output then you will have to install the GPG command line tool for your operating system, you can find this here - <https://www.gnupg.org/download/>. If you are using Windows os, you can download the required GPG command line tool from winget package manager here - <https://gpg4win.org/download.html>.

```shell
gpg (GnuPG) 2.4.5
libgcrypt 1.10.3
Copyright (C) 2024 g10 Code GmbH
License GNU GPL-3.0-or-later <https://gnu.org/licenses/gpl.html>
This is free software: you are free to change and redistribute it.
There is NO WARRANTY, to the extent permitted by law.

Home: /.gnupg
Supported algorithms:
Pubkey: RSA, ELG, DSA, ECDH, ECDSA, EDDSA
Cipher: IDEA, 3DES, CAST5, BLOWFISH, AES, AES192, AES256, TWOFISH,
        CAMELLIA128, CAMELLIA192, CAMELLIA256
Hash: SHA1, RIPEMD160, SHA256, SHA384, SHA512, SHA224
Compression: Uncompressed, ZIP, ZLIB, BZIP2
```

Now Generate a new GPG key pair. Since there are multiple versions of GPG, you need to use different commands for key generation.

If you are on GPG version 2.1.17 or newer then use the below command

```shell
gpg --full-generate-key
```

At the prompt, specify the kind of key you want, or press Enter to accept the default and at the next prompt, specify the key size you want, or press Enter to accept the default. Enter the length of time you want the key valid for and press Enter.

If you are not on GPG version 2.1.17 or newer then use the below command to generate your GPG Key.

```shell
gpg --default-new-key-algo rsa4096 --gen-key
```

Enter the length of time you want the key valid for and press Enter.

Enter your user ID information.

Make note of the ID and save the keys.

```shell
gpg --list-secret-keys --keyid-format LONG $USER_EMAIL
```

You should see a similar output to this:

```shell
gpg --list-secret-keys --keyid-format=long
/Users/hubot/.gnupg/secring.gpg
------------------------------------
sec   4096R/3AA5C34371567BD2 2016-03-10 [expires: 2017-03-10]
uid                          Hubot <hubot@example.com>
ssb   4096R/4BB6D45482678BE3 2016-03-10
```

Copy the long form of the GPG Key ID you'd like to use, in this case it would be `3AA5C34371567BD2`

Once you have your GPG key you can run the below command to print the GPG key ID, in ASCII armor format. Replace `3AA5C34371567BD2` with your gpg key id.

```shell
gpg --armor --export 3AA5C34371567BD2
```

Copy your GPG key, beginning with -----BEGIN PGP PUBLIC KEY BLOCK----- and ending with -----END PGP PUBLIC KEY BLOCK-----.

### Importing your GPG key

Import your private key. GPG keys are stored in the `~/.gnupg` directory. You can import the key using the uid, in this case it is `hubot`

```shell
gpg --import Hubot.gpg
```

Remove keys from the GPG agent if no longer needed, firs list the keys.

To list public keys on both Linux and Mac os:

```shell
gpg --list-keys
```

To list private keys on both Linux and Mac os:

```shell
gpg --list-secret-keys
```

For your keys to be deleted properly, make sure you delete the private key before deleting the public one.
There are a few ways you can delete your gpg keys.

Delete keys for a single user:
  Private Key:
    ```ssh
    gpg --delete-secret-key [uid]
    ```
  Public Key:
    ```ssh
    gpg --delete-key [uid]
    ```

This will now ask you if you are suire you want to delte the key, press `y` for yes the you will get a pop-up that again asks if you are sure, click delete key and your key will be deleted.

### Configure Git

If you have multiple GPG Keys, you need to tell Git which one to use.

To start with, just to make sure, you need to unset the current GPG key, you can do this with the below command.

```shell
git config --global --unset gpg.format
```

Use the below command to list the long form of the GPG keys for which you have both a public and private key. A private key is required for signing commits or tags.

```shell
gpg --list-secret-keys --keyid-format=long
```

You should have an output similar to the below.

```shell
$ gpg --list-secret-keys --keyid-format=long
/Users/hubot/.gnupg/secring.gpg
------------------------------------
sec   4096R/3AA5C34371567BD2 2016-03-10 [expires: 2017-03-10]
uid                          Hubot <hubot@example.com>
ssb   4096R/4BB6D45482678BE3 2016-03-10
```

From the list of GPG keys, copy the long form of the GPG key ID you'd like to use. In this example, the GPG key ID is `3AA5C34371567BD2`.

To set your primary GPG key you will need to run the below command but replacing `3AA5C34371567BD2` with your key ID.

If you wish to configure git to sign all commits by default then run the below command.

```shell
git config --global commit.gpgsign true
```

You need to ensure that your local git is using the same email as the email used for your github account. To do this run the below command to see which email your local git is configured to. The `.git/config` should be your path to your git config file

```shell
 cat .git/config
```

This wil output something similar to the following which will show all your git credentials.

```shell
[filter "lfs"]
        required = true
        clean = git-lfs clean -- %f
        smudge = git-lfs smudge -- %f
        process = git-lfs filter-process
[user]
        name = First_Name Last_Name
        email = Name@example.com
        signingkey = some-value-here
[commit]
        gpgsign = true
[core]
        editor = vim
```

If any of the above details are incorrect you can amend them using the following commands if you want to set them globally.
To set your username:

```shell
git config --global user.name "FIRST_NAME LAST_NAME"
```

To sety your email:

```shell
git config --global user.email "MY_NAME@example.com"
```

If you want to set the credentials for the specific repository, make sureyou are in the repoositary directory and run the following commands.

```shell
it config user.name "FIRST_NAME LAST_NAME"
```

```shell
git config user.email "MY_NAME@example.com"
```

Now verify that these have been set correctly by again running the below. The `.git/config` should be your path to your git config file

```shell
 cat .git/config
 ```

### Configure GitHub

To [add your GPG public key to your GitHub account](https://docs.github.com/en/authentication/managing-commit-signature-verification/adding-a-gpg-key-to-your-github-account) follow these steps:

1. Navigate to your GitHub account settings.
2. From the sidebar, click on "**SSH and GPG keys**".
3. Click on the "**New GPG key**" button.
4. In the "**Title**" field, enter a descriptive name for the key, like "My GitHub signing key".
5. Copy the contents of your public key file and paste it into the "**Key**" field.

   ```shell
   cat $file.gpg-key.pub
   ```

6. Click "**Add GPG key**" to save.

After completing these steps, your new signing key will be listed in the "**SSH and GPG keys**" section of your GitHub profile.

To verify that your key is working you can do the following, change the working directory to the folder where your file and signature are saved.

```shell
gpg --verify [signature-file] [file]
```

For example if you have the tor browser bundle file and the signature file you would use the following command.

```shell
gpg --verify tor-browser.tar.gz.asc tor-browser.tar.gz
```

This method works on Linux, Windows and Mac operating systems.

### Troubleshooting

If you receive the error message `error: gpg failed to sign the data`, make sure you added `export GPG_TTY=$(tty)` to your `~/.zshrc` or `~/.bashrc`, and restarted your terminal.

```shell
sed -i '/^export GPG_TTY/d' ~/.exports
echo "export GPG_TTY=\$TTY" >> ~/.exports
```

### Additional settings

Configure caching git commit signature passphrase for 3 hours

```shell
source ~/.zshrc # or ~/.bashrc
mkdir -p ~/.gnupg
sed -i '/^pinentry-program/d' ~/.gnupg/gpg-agent.conf 2>/dev/null ||:
echo "pinentry-program $(whereis -q pinentry)" >> ~/.gnupg/gpg-agent.conf
sed -i '/^default-cache-ttl/d' ~/.gnupg/gpg-agent.conf
echo "default-cache-ttl 10800" >> ~/.gnupg/gpg-agent.conf
sed -i '/^max-cache-ttl/d' ~/.gnupg/gpg-agent.conf
echo "max-cache-ttl 10800" >> ~/.gnupg/gpg-agent.conf
gpgconf --kill gpg-agent
git config --global credential.helper cache
#git config --global --unset credential.helper
```

## Signing commits using SSH

### Generate SSH key

You should not do this if you already have GPG signing set up. One or the other is fine, but not both.

If you do not already have SSH key access set up on your GitHub account, first [generate a new SSH key](https://docs.github.com/en/authentication/connecting-to-github-with-ssh/generating-a-new-ssh-key-and-adding-it-to-the-ssh-agent). To create a new SSH key, you need to run the following command. This will generate a new SSH key of the type `ed25519` and associate it with your email address. Please replace your.name@email with your actual email address.

```shell
ssh-keygen -t ed25519 -C "your.name@email" -f "~/.ssh/github-signing-key"
```

When you run this command, it will ask you to enter a passphrase. Choose a strong passphrase and make sure to remember it, as you will need to provide it when your key is loaded by the SSH agent.

### Configure Git

If you are signing commits locally using an SSH key, you need to [configure Git](https://docs.github.com/en/authentication/managing-commit-signature-verification/telling-git-about-your-signing-key#telling-git-about-your-ssh-key) accordingly since it is not the default method.

Run the following command to instruct Git to use the SSH signing key format, instead of the default GPG:

```shell
git config --global gpg.format ssh
```

Next, specify the private key for Git to use:

```shell
git config --global user.signingkey ~/.ssh/github-signing-key
```

Lastly, instruct Git to sign all of your commits:

```shell
git config --global commit.gpgsign true
```

### Configure GitHub

To [add your SSH public key to your GitHub account](https://docs.github.com/en/authentication/connecting-to-github-with-ssh/adding-a-new-ssh-key-to-your-github-account) follow these steps:

1. Navigate to your GitHub account settings.
2. From the sidebar, click on "**SSH and GPG keys**".
3. Click on the "**New SSH key**" button.
4. In the "**Title**" field, enter a descriptive name for the key, like "My GitHub signing key".
5. Copy the contents of your public key file and paste it into the "**Key**" field.

   ```shell
   cat ~/.ssh/github-signing-key.pub
   ```

6. Ensure to select "**Signing Key**" from the "**Key type**" dropdown.
7. Click "**Add SSH key**" to save.

After completing these steps, your new signing key will be listed in the "**SSH and GPG keys**" section of your GitHub profile.

## Testing

To ensure your configuration works as expected, make a commit to a branch locally and push it to GitHub. When you view the commit history of the branch on GitHub, [your latest commit](https://docs.github.com/en/authentication/managing-commit-signature-verification/about-commit-signature-verification#about-commit-signature-verification) should now display a `Verified` tag, which indicates successful signing with your GPG or SSH key.
