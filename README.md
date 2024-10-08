# ASF Achievement Manager

# DISCLAIMER
This plugin is provided on AS-IS basis, without any guarantee at all. Author is not responsible for any harm, direct or indirect, that may be caused by using this plugin. You use this plugin at your own risk.

## Introduction 
This plugin for [ASF](https://github.com/JustArchiNET/ArchiSteamFarm/) allows you to view, set and reset achievements in steam games, similar to [SAM](https://github.com/gibbed/SteamAchievementManager). Works only with ASF v4.0+ (make sure to check actual required version in release notes). 

## Installation
- download `ASFAchievementManager.zip` file from [latest release](https://github.com/CatPoweredPlugins/ASF-Achievement-Manager/releases/latest).
- create new folder (for example, `ASFAchievementManager`) in the `plugins` folder of your ASF installation
- unpack downloaded .zip file to the folder you just created.
- optionally, configure plugin properties in ASF.json file (see below).
- (re)start ASF, you should get a message indicating that plugin loaded successfully. 

## Usage
After installation, you can use those commands (only for accounts with Master+ permissions):

### `alist <bots> <appids>`
Displays current status of achievements in specified games on given bots. You can specify multiple bots and multiple appids.<br/>
Example of output:<br/>
![alist output example](https://i.imgur.com/IiRnH81.png)<br/>
Unlocked achievements are marked as ✅, still locked as ❌. If achievement has mark ⚠️ next to it - it means this achievement is server-side, and can't be switched with this plugin.<br/>
Examples:<br/>
  `alist bot1 370910,730`<br/>
  `alist bot1,bot2 370910`
  
### `aset <bots> <appid> <achievements>`
Sets (unlocks) achievements with specified numbers in provided appid on given bots. Please note that, unlike `alist`, you can provide only one appid here. Achievement numbers corresponds to the ones showed by `alist` command. You can provide `*` instead of achievements list to set all available achievements.<br/>
Examples:<br/>
  `aset bot1 370910 1,2,5`<br/>
  `aset bot1,bot2 370910 1`<br/>
  `aset bot1 370910 *`
  
### `areset <bots> <appid> <achievements>`
Resets (locks) achievements with specified numbers in provided appid on given bots. Please note that, unlike `alist`, you can provide only one appid here. Achievement numbers corresponds to the ones showed by `alist` command. You can provide `*` instead of achievements list to reset all available achievements.<br/>
Examples:<br/>
  `areset bot1 370910 1,2,5`<br/>
  `areset bot1..bot3 370910 2`<br/>
  `areset bot1 370910 *`

## Configuration
This plugins adds one additional property in global ASF config (ASF.json).

### `Rudokhvist.AchievementsCulture`
Property of `string` type, with default value equal to `null`, that determines in what language you want to see achievement names, if available. If value is `null`, or invalid culture is provided, plugin will fallback to the global culture set in ASF (see [`CurrentCulture`](https://github.com/JustArchiNET/ArchiSteamFarm/wiki/Configuration#currentculture)).
Accepted values are the same as for [`CurrentCulture`](https://github.com/JustArchiNET/ArchiSteamFarm/wiki/Configuration#currentculture) global ASF property, but please note that only languages actually provided in steam could be displayed, if there is no specified language provided in Steam - plugin will fallback to English names.

Example:

`"Rudokhvist.AchievementsCulture":"uk-UA"`


![downloads](https://img.shields.io/github/downloads/CatPoweredPlugins/ASF-Achievement-Manager/total.svg?style=social)
