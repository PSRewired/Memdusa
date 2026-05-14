# Memdusa "Exploit"

[![Memdusa Exploit](https://img.youtube.com/vi/LdkU-zksBig/0.jpg)](https://www.youtube.com/watch?v=LdkU-zksBig)

## Requirements

- [.Net 10](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/runtime-desktop-10.0.5-windows-x64-installer)
- DNS Server or DNS proxy ([Retro DNS](https://github.com/PSRewired/RetroDNS))

## Usage
Start the server and have your PS2's DNS address pointed to your DNS server / proxy.
Select a game that is supported and connect online. ulaunch should load.
This is a heavily cut down version of it and only has USB support. Make sure whatever homebrew you want to load after is on a USB drive and plugged into the PS2.


## Supported Games

|Game		|GameID		|AppID	|Domain 								|
|-----------|-----------|-------|---------------------------------------|
|Killzone	|SCUS_974.02|10724	|killzone-prod.muis.pdonline.scea.com	|
|SOCOM II US Navy Seals Patch r0004|SCUS_972.75|10472|socom2-prod.muis.pdonline.scea.com socom2-prod.pdonline.scea.com|
SOCOM 3 US Navy Seals Base Game and 2.3|SCUS_974.74|20004|socom3-prod.pdonline.scea.com socom3-prod.muis.pdonline.scea.com|
SOCOM Combined Assault Base game and Patch 1.4|SCUS_975.45|20604|socomca.ps2.online.scea.com|
|Syphon Filter - The Omega Strain|SCUS_972.64|10411|syphonfilter-prod.pdonline.scea.com|
|Syphon Filter - The Omega Strain|SCES_520.33|10414|sfos-palmaster.online.scee.com|

## Game specific notes

### Killzone
You will have to wait for the game to give you the disconnect message. This can take up to 45 seconds to happen. Once it happens press Triangle and you will be brought back to the Network configuration selection. Pick one and press X to select it. LaunchElf will load.

### SOCOM 2

You will have to wait for the game to give you the disconnect message. This can take about 30 seconds. Once that happens press X and LaunchElf will load.

### SOCOM 3 / Combined Assault
The version of the payload you need to use will depend on what version of the game you are currently on. On the main menu you will see OCN vx.x and a date. You need to rename the file in the Mods folder. For example you have SOCOM 3 on version 2.3. You will rename 20004-2.3.txt to 20004.txt. This same thing applies for Combined Assault except the file name starts with 20604.

For both of these games you will connect and wait for a Connection Failure message to appear at the Universe Selection screen. Once you see this, wait about 10 seconds and then press X to continue. LaunchElf will now load.

### Syphon Filter - The Omega Strain
Nothing special needs to take place. Just connect and as soon as the payload goes down the game will load LaunchElf.

## Using Retro DNS as a proxy
If you plan on using Retro DNS as your proxy you will need to edit the 99-domains.txt file to include the following:
```
killzone-prod.muis.pdonline.scea.com=ip://replaceme
socom2-prod.pdonline.scea.com=ip://replaceme
socom2-prod.muis.pdonline.scea.com=ip://replaceme
socom3-prod.muis.pdonline.scea.com=ip://replaceme
syphonfilter-prod.pdonline.scea.com=ip://replaceme
sfos-palmaster.online.scee.com=ip://replaceme
socomca.ps2.online.scea.com=ip://replaceme

// Catch all, unless you are running a DNAS repo you'll need this so it gets to ours
*=dns://67.222.156.250
```
Make sure you replace the "replaceme" text with the IP address that retro DNS shows you. Once you edit and save you can run retro DNS and click Start.

On your PS2 you will need to edit your network configurations. Killzone does not have this functionality so you will need another game that does. You will need to set your DNS 1 IP to the one that you got from retro DNS.


## How this works
> The majority of the games that used Medius for online play have a packet that allows the server to write anything you want to anywhere you want in memory. This takes advantage of that. Once the payload is written to memory the very next request the game makes to the server will load the ELF.

## FAQ

- Why isn't 'game name' supported?
> Either the game requires a DNAS bypass to even get online or the game lacks the functionality to write the payload to memory. Games that require a DNAS bypass also tend to require you to be able to load up a patched backup of the game which means you would need to able to run homebrew.

- Can I run this on a remote server and have anyone use this?
> At the moment no. The payload in general is pretty big and most of the games will time out when doing this to a remote server. All I did was gut a working elf launcher to get it to work locally. If someone wants to work on making a smaller loader please do. 

- Can I get this to work with any elf?
> Yes and no. From what I've seen most ELF files exist in the 0010000 memory range and most games have running code in that area. You would need to recompile the ELF to load from a different memory location which is what I did with LaunchElf. I found 01CF3400 range to be empty in these games but that's not always the case. The butchered source code to LaunchElf in included in the repo.

- Why is it called Memdusa
> This utilizes the memory write packet and I froze my console far too many times testing this.
