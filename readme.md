# Royale Brawl v29

A Brawl Stars private server for 29.270 based on royale Brawl

why did I create this server? because all public v29 servers suck

## Download the client [here](https://mega.nz/file/zmxDRCzL#k5bdy8w3cAta11dWtzpfGZQXwUtsX0z0jzI_fPyXXdg)
![Logo](https://github.com/Erder00/royale-brawl-v29/blob/main/docs/screenshots/lobby.png?raw=true)


## Features


- offline battles
- vip / premium system
- report system
- discord integration
- online game rooms
- friends & clubs
- brawl pass and trophy road
- global, club and brawler leaderboards
- random events
- slash commands in clubs
- anti-ddos

#### discord bot commands

- !help - shows all available commands
- !status - show server status
- !ping - will respond with pong
- !resetseason - resets the season
- !reports - sneds a link to all reported messages
- !givepremium - give premium to an account for one month (!givepremium [TAG])
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

How to install:

[Android](https://github.com/Erder00/royale-brawl-v29/blob/main/docs/Android.md)
[Linux](https://github.com/Erder00/royale-brawl-v29/blob/main/docs/Linux.md)
[Windows](https://github.com/Erder00/royale-brawl-v29/blob/main/docs/Windows.md)

then connect to the server using the [pre-made client](https://mega.nz/file/zmxDRCzL#k5bdy8w3cAta11dWtzpfGZQXwUtsX0z0jzI_fPyXXdg)

decompile the apk and replace the ip in `lib/armeabi-v7a/liberder.script.so` with your own

## Acknowledgements

 - based on [royale brawl](https://github.com/Erder00/royale-brawl) from [xeon](https://git.xeondev.com/xeon)
 - using [netcord](https://netcord.dev) for the discord stuff (it's really cool, check it out)
 - [spz](https://github.com/spz2020) and [santer](https://github.com/SANS3R66) for the pull requests <3

## TODO

- better anti-cheat (check csv shas and apk integrity in script, detect if battles are very short)
- fix pin packs
- add more club commands
- add creator codes
