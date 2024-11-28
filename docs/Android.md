## Android
1: get it in [Github Actions](https://github.com/spz2020/royale-brawl-v29/actions)

2: install [Termux](https://github.com/termux/termux-app)

3: upgrade Packages
```
pkg update && pkg upgrade
```
4: install glibc
```
apt install glibc-repo wget
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
7: create new terminal sessions and run [mariadb-secure-installation](https://mariadb.com/kb/en/mariadb-secure-installation/)
```
'/data/data/com.termux/files/usr/bin/mariadb-secure-installation'
```
8: start glibc runner
```
grun -s

```
9: Download dotnet and install 
```
wget https://download.visualstudio.microsoft.com/download/pr/501c5677-1a80-4232-9223-2c1ad336a304/867b5afc628837835a409cf4f465211d/dotnet-runtime-8.0.11-linux-arm64.tar.gz
mkdir .dotnet
tar xvf dotnet-runtime-8.0.11-linux-arm64.tar.gz -C .dotnet
grun -f .dotnet/dotnet
grun -c .dotnet/dotnet
```

### Run 
terminal 1
```
cd '/data/data/com.termux/files/usr' ; /data/data/com.termux/files/usr/bin/mariadbd-safe --datadir='/data/data/com.termux/files/usr/var/lib/mysql'
```
terminal 2 
```
cd Server
~/.dotnet/dotnet Supercell.Laser.Server.dll
```
