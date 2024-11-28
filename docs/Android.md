## Android
1: get it in [Github Actions](https://github.com/spz2020/royale-brawl-v29/actions)

2: install [Termux](https://github.com/termux/termux-app)

3: upgrade Packages
```
pkg update && pkg upgrade
```
4: install glibc
```
apt install glibc-repo
apt install glibc-runner libicu-glibc
```
5: install mariadb
```
pkg install mariadb
```
6: start mariadb
```
cd '/data/data/com.termux/files/usr' ; /data/data/com.termux/files/usr/bin/mariadbd-safe --datadir='/data/data/com.termux/files/usr/var/lib/mysql'
```
