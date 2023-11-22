# ASF Achievement Manager

# DISCLAIMER
This plugin is provided on AS-IS basis, without any guarantee at all. Author is not responsible for any harm, direct or indirect, that may be caused by using this plugin. You use this plugin at your own risk.

## Introduction 
This plugin for [ASF](https://github.com/JustArchiNET/ArchiSteamFarm/) allows you to view, set and reset achievements in steam games, similar to [SAM](https://github.com/gibbed/SteamAchievementManager). Works only with ASF v4.0+ (make sure to check actual required version in release notes). 

## Installation
- download .zip file from [latest release](https://github.com/Rudokhvist/ASF-Achievement-Manager/releases/latest), in most cases you need `ASF-Achievement-Manager.zip`, but if you use ASF-generic-netf.zip (you really need a strong reason to do that) download `ASF-Achievement-Manager-netf.zip`.
- unpack downloaded .zip file to `plugins` folder inside your ASF folder.
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

You can get support for this plugin in https://steamcommunity.com/groups/Ryzheplugins (or just use github issues).

---

# Менеджер достижений для ASF

# ОТКАЗ ОТ ОТВЕТСВЕННОСТИ
Этот плагин предоставляется по принципу "КАК ЕСТЬ", без каких-либо гарантий. Автор не несёт никакой ответсвенности за любой прямой или непрямой ущерб, который может быть следствием использования этого плагина. Вы используете этот плагин на свой страх и риск.

## Введение 
Этот плагин для [ASF](https://github.com/JustArchiNET/ArchiSteamFarm/) позволяет просматривать, включать и выключать достижения в играх Steam, аналогично программе [SAM](https://github.com/gibbed/SteamAchievementManager).  Работает только в ASF 4.0+ (не забудьте проверить реально требуемую версию в информации о релизе).

## Установка
- скачайте файл .zip из [последнего релиза](https://github.com/Rudokhvist/ASF-Achievement-Manager/releases/latest), в большинстве случаев вам нужен файл `ASF-Achievement-Managerm.zip`, не если вы по какой-то причине пользуетесь ASF-generic-netf.zip (а для этого нужны веские причины) - скачайте `ASF-Achievement-Manager-netf.zip`.
- распакуйте скачанный файл .zip в папку `plugins` внутри вашей папки с ASF.
- (пере)запустите ASF, вы должны получить сообщение что плагин успешно загружен. 

## Использовение
После установки, вы можете использовать следующие команды (только с аккаунта с правами Master+):

### `alist <bots> <appids>`
Отображает список и текущее состояние достижений в указанных играх для заданных ботов. Вы можете указать нескольких ботов и несколько appid.<br/>
Пример работы программы:<br/>
![пример работы alist](https://i.imgur.com/IiRnH81.png)<br/>
Открытые достижения отмечены как ✅, ещё закрытые - как ❌. Если рядом с достижением стоит отметка ⚠️ - это означает что это серверное достижение, и его состояние невозможно переключать с помощью этого плагина.<br/>
Examples:<br/>
  `alist bot1 370910,730`<br/>
  `alist bot1,bot2 370910`
  
### `aset <bots> <appid> <achievements>`
Включает (открывает) достижения с указанными номерами в заданной игре на заданных ботах. Обратите внимание, что в отличии от команды `alist`, вы можете указать только один appid. Номера достижений соответствуют тем, которые выводит команда `alist`. Вы можете вместо списка с номерами достижений указать `*` чтобы включить все доступные достижения.<br/>
Примеры:<br/>
  `aset bot1 370910 1,2,5`<br/>
  `aset bot1,bot2 370910 1`<br/>
  `aset bot1 370910 *`
  
### `areset <bots> <appid> <achievements>`
Выключает (закрывает) достижения с указанными номерами в заданной игре на заданных ботах. Обратите внимание, что в отличии от команды `alist`, вы можете указать только один appid. Номера достижений соответствуют тем, которые выводит команда `alist`. Вы можете вместо списка с номерами достижений указать `*` чтобы выключить все доступные достижения.<br/>
Примеры:<br/>
  `areset bot1 370910 1,2,5`<br/>
  `areset bot1..bot3 370910 2`<br/>
  `areset bot1 370910 *`
  
 Помощь по этому плагину вы можете получить в https://steamcommunity.com/groups/Ryzheplugins (или просто используйте раздел issues)

![downloads](https://img.shields.io/github/downloads/Rudokhvist/ASF-Achievement-Manager/total.svg?style=social)
