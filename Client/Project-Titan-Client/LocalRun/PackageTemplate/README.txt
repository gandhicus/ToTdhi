Trials of Titan Local Package
=============================

Quick start: play alone or host

1. Extract the zip.
2. Run StartAllLocal.bat.
3. Keep the database and server console windows open while playing.

Quick start: join a friend's server

1. Extract the zip.
2. Edit LocalServer.txt.
3. Set ip to the host's LAN/Hamachi IPv4 address, for example:
   ip: 25.12.34.56
4. Run StartClient.bat.
5. Do not run StartAllLocal.bat unless you want to host your own local server.

What is included:

- Client\TrialsOfTitan.exe
- Server\Run.Local.All.dll or Server\Run.Local.All.exe
- Database\DynamoDb
- Start scripts

Progress storage:

- Character/account progress is stored by DynamoDB Local in:
  %LOCALAPPDATA%\TrialsOfTitanLocal\DynamoDb
- This folder is outside the extracted game folder, so extracting a new zip over the old package should not overwrite progress.
- If you played an older package that stored test_us-east-1.db inside Database\DynamoDb, StartDatabase.bat will copy that file to the new progress folder the first time it runs.
- Do not delete the %LOCALAPPDATA%\TrialsOfTitanLocal\DynamoDb folder unless you intentionally want to reset local progress.

Config files:

LocalServer.txt is client-only. It controls which server this client connects to.
Friends who only join another host should edit only this file:

ip: 127.0.0.1

ServerSettings.txt is server-only. It controls the local server started by StartServer.bat.
Only the host needs to edit this file:

ip: 127.0.0.1
admin: false
anticheat: true
lootBoost: 1

Settings:

- ip: the LAN/Hamachi IPv4 address that the server advertises, or the IP a client connects to.
- admin: true allows admin commands only for the local host client connected from this same PC.
- anticheat: false disables the local server's suspicious-hit disconnects.
- lootBoost: drop chance multiplier from 1 to 100. Use 1 for normal loot, 10 for the previously boosted local loot.

LAN / Hamachi / Porthole play:

Solo/local play:

1. Leave LocalServer.txt as:
   ip: 127.0.0.1
2. Leave ServerSettings.txt as:
   ip: 127.0.0.1
3. Run StartAllLocal.bat.

Hosting for friends over Hamachi, Porthole, or a LAN:

1. Install Hamachi/Porthole or connect all players to the same LAN/VPN.
2. On the host PC, set ServerSettings.txt ip to the host address that friends can reach, for example:
   ip: 25.12.34.56
3. On the host PC, run StartAllLocal.bat.
4. In Windows Firewall on the host PC, allow inbound TCP traffic for:
   8443
   12000
5. Send the same IP address to friends.
6. Friends should set their LocalServer.txt to that host IP and run StartClient.bat only:
   ip: 25.12.34.56

Porthole setup:

1. Create or join the Porthole lobby.
2. In Porthole, share/open both TCP ports:
   8443
   12000
3. Use the Porthole lobby/host IP address that players can reach.
4. Put that IP in the host ServerSettings.txt:
   ip: 25.12.34.56
5. Friends put the same reachable IP in their LocalServer.txt:
   ip: 25.12.34.56
6. Do not add :8443 or :12000 to LocalServer.txt. The client uses 8443 for web/login and 12000 for the game automatically.

Do not expose DynamoDB Local port 8000. Player accounts and character progress are stored on the host PC.

Ports:

- DynamoDB Local: localhost:8000
- Web server:     localhost/Hamachi/Porthole IP:8443
- Game server:    localhost/Hamachi/Porthole IP:12000

Requirements:

- Unity is not required to play this package.
- Java is required for DynamoDB Local unless a portable Java runtime is included at Runtime\Java.
- .NET runtime is required for Server\Run.Local.All.dll unless a portable runtime is included at Runtime\DotNet or a self-contained Server\Run.Local.All.exe is included.

If the server asks for administrator rights, allow it. The local web server uses HttpListener on http://*:8443/.
