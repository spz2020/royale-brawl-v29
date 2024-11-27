
# Royale Brawl v29

A Brawl Stars private server for 29.231 based on royale Brawl

## Download the client [here](https://mega.nz/file/h4JGEYxa#cVTFiG1lIFqFxsfl-VzAabzwMy-sDvqMVkN3OaE-1bA)
![Logo](https://github.com/Erder00/royale-brawl-v29/blob/main/screenshots/lobby.png?raw=true)


## Features


- offline battles
- report system
- discord integration
- online game rooms
- friends & clubs
- brawl pass and trophy road
- global, club and brawler leaderboards
- random events
- slash commands in clubs

#### discord bot commands

- !help - shows all available commands
- !status - show server status
- !ping - will respond with pong
- !resetseason - resets the season
- !reports - sneds a link to all reported messages
- !ban - ban an account (!ban [TAG])
- !unban - unban an account (!unban [TAG])
- !mute - mute a player (!mute [TAG])
- !unmute - unmute a player (!unmute [TAG])
- !userinfo - show player info (!userinfo [TAG])
- !settrophies - set trophies for all brawlers (!settrophies [TAG] [Trophies])
- !addgems - grant gems to a player (!addgems [TAG] [Gems])

#### club commands

- /help - lists all available commands
- /status - shows the server status
## Installation

requirements:

[dotnet 8.0](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)  
[mysql](https://dev.mysql.com/downloads/)

on linux (and termux/userland) install both via your package manager
```bash
apt install mysql-server dotnet-sdk-8.0
```
clone the repository (install git via `apt install git` if needed)
```bash
git clone https://github.com/Erder00/royale-brawl-v29
```
cd into the the correct directory to compile
```bash
cd royale-brawl-v29/
```
start mysql
```bash
service mysql start
```
mysql shell
```bash
mysql
```
set mysql root password
```bash
ALTER USER 'root'@'localhost' IDENTIFIED WITH caching_sha2_password BY 'YOUR_PASSWORD';
```
create a new mysql database
```bash
CREATE DATABASE databasename;
```
exit mysql
```bash
exit;
```
import database.sql
```bash
mysql -u root -p databasename < database.sql
```
cd into the project
```bash
cd src/Supercell.Laser.Server
```
compile the project
```bash
dotnet publish
```
if you get an error saying

"GC heap initialization failed with error 0x8007000E
Failed to create CoreCLR, HRESULT: 0x8007000E"

run:
```bash
export DOTNET_GCHeapHardLimit=1C0000000 
```
or add `export DOTNET_GCHeapHardLimit=1C0000000` to your .bashrc

now cd into the path the the compiled dll
```bash
cd bin/Release/net8.0/
```
edit the config file with your prefered text editor, for example vim:
```bash
vim config.json
```
change the `database_password` to the password you set, and the `database_name` to the database you created. also edit `BotToken` and `ChannelId` if you want to use the discord bot

finally run the server
```bash
dotnet Supercell.Laser.Server.dll
```
![alt text](https://i.imgur.com/wvrVbsi.png)

now connect to the server using the [pre-made client](https://mega.nz/file/h4JGEYxa#cVTFiG1lIFqFxsfl-VzAabzwMy-sDvqMVkN3OaE-1bA)

decompile the apk and replace the ip in `lib/armeabi-v7a/liberder.script.so` with your own


## TODO

- add setup tutorials for android, windows and linux
- fix random events sometimes crashing
- fix brawl pass / collete and surge mess
- add more club commands
- add creator codes