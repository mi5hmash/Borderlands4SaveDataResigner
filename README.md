[![License: MIT](https://img.shields.io/badge/License-MIT-blueviolet.svg)](https://opensource.org/license/mit)
[![Release Version](https://img.shields.io/github/v/tag/mi5hmash/Borderlands4SaveDataResigner?label=Version)](https://github.com/mi5hmash/Borderlands4SaveDataResigner/releases/latest)
[![Visual Studio 2026](https://custom-icon-badges.demolab.com/badge/Visual%20Studio%202026-F0ECF8.svg?&logo=visual-studio-26)](https://visualstudio.microsoft.com/)
[![.NET10](https://img.shields.io/badge/.NET%2010-512BD4?logo=dotnet&logoColor=fff)](#)

> [!IMPORTANT]
> **This software is free and open source. If someone asks you to pay for it, it's likely a scam.**

This command-line utility can **encrypt and decrypt SaveData files** from the Borderlands 4 game. It can also **resign SaveData files** with your own UserID so you can **load them on your User Account**.

## Supported platforms
It only supports PC SaveData files from the Steam and Epic Games Store (EGS).

## 🔄 Note about the conversion between the Steam and EGS platforms
To convert SaveData files between the two platforms, use the resign mode and provide the correct User IDs for both the source and target platforms.

# 🤯 Why was it created :interrobang:
I wanted to share a SaveData file with a friend, but it isn't possible by default.

# :scream: Is it safe?
The short answer is: **No.** 
> [!CAUTION]
> If you unreasonably edit your SaveData files, you risk corrupting them or getting banned from playing online. In both cases, you will lose your progress.

> [!IMPORTANT]
> Always back up the files you intend to edit before editing them.

> [!IMPORTANT]
> Disable the Steam Cloud before you replace any SaveData files.

You have been warned, and now that you are completely aware of what might happen, you may proceed to the next chapter.

# :scroll: How to use this tool
## [GUI] - 🪟 Windows 
> [!IMPORTANT]
> If you’re working on Linux or macOS, skip this chapter and move on to the next one.

On Windows, you can use either the CLI or the GUI version, but in this chapter I’ll describe the latter.

<img src="https://github.com/mi5hmash/Borderlands4SaveDataResigner/blob/main/.resources/images/MainWindow-v2.png" alt="MainWindow-v2"/>

### BASIC OPERATIONS

#### 1. Setting the Input Directory
You can set the input folder in whichever way feels most convenient:
- **Drag & drop:** Drop SaveData file - or the folder containing it - onto the TextBox **(2)**.
- **Pick a folder manually:** Click the button **(3)** to open a folder‑picker window and browse to the directory where SaveData file is.
- **Type it in:** If you already know the path, simply enter it directly into the TextBox **(2)**.

#### 2. Entering the User ID
In the case of Steam, your User ID is 64-bit SteamID.  
One way to find it is by using the SteamDB calculator at [steamdb.info](https://steamdb.info/calculator/).
In the case of Epic Games Store, your User ID is 32‑character lowercase hexadecimal string. To find your Epic Games Account ID, log in to the Epic Games website, hover over your display name, and click "Account" to view it under "Account Information".

#### 3. Re-signing SaveData files
If you want to re‑sign your SaveData file/s so it works on another Steam account, type the User ID of the account that originally created that SaveData file/s into the TextBox **(4)**. Then enter the User ID of the account that should be allowed to use that SaveData file/s into the TextBox **(7)**. Finally, press the **"Re-sign All"** button **(11)**.

> [!NOTE]
> The re‑signed files will be placed in a newly created folder within the ***"Borderlands4SaveDataResigner/_OUTPUT/"*** folder.

#### 4. Accessing modified files
Modified files are being placed in a newly created folder within the ***"Borderlands4SaveDataResigner/_OUTPUT/"*** folder. You may open this directory in a new File Explorer window by pressing the button **(12)**.

> [!NOTE]
> After you locate the modified files, you can copy them into your save‑game folder:
***"%USERPROFILE%\Documents\My Games\Borderlands 4\Saved\SaveGames"***.
You can open this directory in a new window by pressing the button **(1)**.

### ADVANCED OPERATIONS

#### Enabling SuperUser Mode

> [!WARNING]
> This mode is for advanced users only.

If you really need it, you can enable SuperUser mode by triple-clicking the version number label **(13)**.

#### Decrypting SaveData files

> [!IMPORTANT]  
> This button is visible only when the SuperUser Mode is Enabled. 

If you want to decrypt SaveData file\s to read its content, type the User ID of the account that originally created that SaveData file/s into the TextBox **(4)**, and press the **"Decrypt All"** button **(8)**.

#### Encrypting SaveData files

> [!IMPORTANT]  
> This button is visible only when the SuperUser Mode is Enabled. 

If you want to encrypt the decrypted SaveData file\s, enter the User ID of the account that should be allowed to use that SaveData file/s into the TextBox **(7)**, and press the **"Encrypt All"** button **(9)**.

### OTHER BUTTONS
Button **(5)** uses a brute‑force approach to find the correct key for source SaveData file, but it works only with a SteamID.
Button **(6)** swaps the values in the **"User ID (INPUT)"** and **"User ID (OUTPUT)"** TextBoxes.
Button **(10)** cancels the currently running operation.

## [CLI] - 🪟 Windows | 🐧 Linux | 🍎 macOS

```plaintext
Usage: .\bl4-savedata-resigner-cli.exe -m <mode> [options]

Modes:
  -m d  Decrypt SaveData files
  -m e  Encrypt SaveData files
  -m r  Re-sign SaveData files
  -m b  Bruteforce Steam ID for a SaveData file

Options:
  -p <path>     Path to folder containing SaveData files or path to a single SaveData file (used in Bruteforce mode)
  -u <user_id>  User ID (used in decrypt/encrypt modes)
  -uI <old_id>  Original User ID (used in re-sign mode)
  -uO <new_id>  New User ID (used in re-sign mode)
  -v            Verbose output
  -h            Show this help message
```

### Examples
#### Decrypt
```bash
.\bl4-savedata-resigner-cli.exe -m d -p ".\InputDirectory" -u 76561197960265729
```
#### Encrypt
```bash
.\bl4-savedata-resigner-cli.exe -m e -p ".\InputDirectory" -u 76561197960265730
```
#### Re-sign
```bash
.\bl4-savedata-resigner-cli.exe -m r -p ".\InputDirectory" -uI 76561197960265729 -uO 76561197960265730
```
#### Bruteforce
```bash
.\bl4-savedata-resigner-cli.exe -m b -p ".\InputDirectory\1.sav"
```

> [!NOTE]
> Modified files are being placed in a newly created folder within the ***"Borderlands4SaveDataResigner/_OUTPUT/"*** folder.

# :fire: Issues
All the problems I've encountered during my tests have been fixed on the go. If you find any other issues (which I hope you won't) feel free to report them [there](https://github.com/mi5hmash/Borderlands4SaveDataResigner/issues).

> [!TIP]
> This application creates a log file that may be helpful in troubleshooting.  
It can be found in the same directory as the executable file.  
Application stores up to two log files from the most recent sessions.
