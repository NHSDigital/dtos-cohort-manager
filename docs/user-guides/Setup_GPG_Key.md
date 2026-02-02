# Guide: Setup GPG Key

- [Guide: Setup GPG Key](#guide-setup-gpg-key)
  - [Overview](#overview)
  - [Checking for existing key and creating a new one](#checking-for-existing-key-and-creating-a-new-one)
  - [Creating your GPG key](#creating-your-gpg-key)
  - [Adding the GPG Key to your GitHub account](#adding-the-gpg-key-to-your-github-account)
  - [Setting your GPG key in your local Git config](#setting-your-gpg-key-in-your-local-git-config)

## Overview

This document will guide you to setup a new GPG key if you don't not have one and assign it to your git account.

## Checking for existing key and creating a new one

Run the below command in your terminal.

```shell
gpg --list-secret-keys --keyid-format=long
```

If you get no output then you do not have any GPG keys and will need to create one. If you do have any GPG keys you should see an output similar to the below.

```shell
[keyboxd]
---------
sec   ed25519/D2482B82913C2ADB 2023-06-30 [SC]
      8B6BAB298F871E1CE593EF33D2613B82913C2ADB
uid                 [ultimate] Your Name (GPG Key) <Your Email>
ssb   cv25519/4A689C5C2D914521 2023-06-30 [E]
```

## Creating your GPG key

First check if you have the GPG command line tool installed by running the below command.

```shell
gpg --version
```

If you already have the GPG command line tool installed you will see an output similar to the below telling you the information about your installed version. If you do not get an output then you will have to install the GPG command line tool for your operating system, you can find this here - <https://www.gnupg.org/download/>.

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

To list your new GPG key in its long form use the below command.

```shell
gpg --list-secret-keys --keyid-format=long
```

The output should be similar to the below.

```shell
gpg --list-secret-keys --keyid-format=long
/Users/hubot/.gnupg/secring.gpg
------------------------------------
sec   4096R/3AA5C34371567BD2 2016-03-10 [expires: 2017-03-10]
uid                          Hubot <hubot@example.com>
ssb   4096R/4BB6D45482678BE3 2016-03-10
```

Copy the long form of the GPG Key ID you'd like to use, in this case it would be `3AA5C34371567BD2`

Once you have your GPG key you can run the below command to print the GPG key ID, in ASCII armor format. Replace `3AA5C34371567BD2` with your GPG key id.

```shell
gpg --armor --export 3AA5C34371567BD2
```

Copy your GPG key, beginning with -----BEGIN PGP PUBLIC KEY BLOCK----- and ending with -----END PGP PUBLIC KEY BLOCK-----.

## Adding the GPG Key to your GitHub account

Go to your GitHub account settings, then navigate to 'Access'.
Under GPG Keys, Click New GPG Key.
Create a name for your key in the 'Title' field.
in the 'Key' field paste your GPG Key Which you copied above, then click 'Add GPG Key'
Once you have authenticated, your GPG Key should now be added to your account.

## Setting your GPG key in your local Git config

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

Now when you commit to a branch, you should be prompted for the password you set when creating your GPG key. Once you have done this and committed you should see that the commit is `verified` in git.

If you get an error when you try to commit for the first time after setting your GPG key there is a quick fix by running the below command.

```shell
export GPG_TTY=$(tty)
```

This should set the correct local variables and if you try to commit again it should prompt you for your password as it should.

## Troubleshooting

### Error: `gpg: signing failed: No secret key`

Steps to resolve:

  1. Check your secret key:
  
      ```shell
      gpg --list-secret-keys --keyid-format=long
      ```

      Make sure the key exists and note the GPG key ID.
  
  2. Verify Git signing key configuration:
  
      ```shell
      git config --global user.signingkey <your-gpg-key-id>
      ```

  3. Ensure GPG program is set correctly (especially on Windows):

      ```shell
      git config --global gpg.program "C:/Program Files (x86)/GnuPG/bin/gpg.exe"
      ```
  
      Adjust the path if GPG is installed elsewhere.
