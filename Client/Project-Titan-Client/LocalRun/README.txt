Trials of Titan local launcher
==============================

Use these scripts from this folder:

1. BuildClient.bat
   Builds the Unity Windows client to:
   Builds\Windows\TrialsOfTitan.exe

2. BuildServer.bat
   Publishes the local server to:
   Builds\LocalServer\Run.Local.All.dll

3. StartDatabase.bat
   Starts DynamoDB Local on port 8000.

4. StartServer.bat
   Starts the local web/game server.
   Web port:  8443
   Game port: 12000

5. StartClient.bat
   Starts the built Windows client.

6. StartAllLocal.bat
   Starts database, server, then client.

7. PackageLocalZip.bat
   Creates:
   Builds\TrialsOfTitanLocal.zip

Recommended first-time flow:

1. Close the Unity Editor for this project.
2. Run BuildClient.bat.
3. Run BuildServer.bat.
4. Run StartAllLocal.bat.

Sharing flow:

1. Run BuildClient.bat.
2. Run BuildServer.bat.
3. Check the default release config files in PackageTemplate:
   PackageTemplate\LocalServer.txt
   PackageTemplate\ServerSettings.txt
4. Run PackageLocalZip.bat.
5. Send Builds\TrialsOfTitanLocal.zip.

After code changes:

- Re-run BuildClient.bat if client, Unity scene, UI, asset, or shared library code changed.
- Re-run BuildServer.bat if server or shared library code changed.
- Re-run PackageLocalZip.bat after both builds are up to date.

Player progress:

Local progress is stored by DynamoDB Local in:
%LOCALAPPDATA%\TrialsOfTitanLocal\DynamoDb

PackageLocalZip.bat does not include local *.db files. This keeps player progress outside the extracted package so a new zip does not overwrite it.

Config files:

LocalServer.txt is client-only. It controls which server this client connects to:

ip: 127.0.0.1

ServerSettings.txt is server-only. It controls the local server started by StartServer.bat:

ip: 127.0.0.1
admin: false
anticheat: true
lootBoost: 1

PackageTemplate contains the default config files that will be copied into the release zip.
Change LocalRun\PackageTemplate\LocalServer.txt and LocalRun\PackageTemplate\ServerSettings.txt before packaging if you want different defaults in the archive.

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

Unity path override:

If BuildClient.bat cannot find Unity, set UNITY_EXE first:

set UNITY_EXE=D:\Unity\Editor\6000.3.11f1\Editor\Unity.exe
