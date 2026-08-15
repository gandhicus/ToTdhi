# ToTdhi

ToTdhi is a singleplayer/small group-focused overhaul mod for Trials of Titan based on javritan's "Trials of Titan Local" fork.

This mod seeks to greatly enhance the game's experience, introducing various balance changes and features.

AI tools were used as a substantial aid to add features and edit the codebase. Design, including item names and descriptions, balancing, spriting, etc. was done entirely by Gandhicus without AI assistance.

# Trials of Titan Local

## Play The Release

You do not need Unity or the source code to play the packaged release.

1. Open the GitHub repository page.
2. Go to **Releases**.
3. Download `TrialsOfTitanLocal.zip`.
4. Extract the zip into any folder.
5. Run one of the included `.bat` files.

### Solo Play

For single-player/local play:

1. Leave `LocalServer.txt` as:

   ```txt
   ip: 127.0.0.1
   ```

2. Leave `ServerSettings.txt` as:

   ```txt
   ip: 127.0.0.1
   admin: false
   anticheat: true
   lootBoost: 1
   ```

3. Run `StartAllLocal.bat`.
4. Keep the database and server console windows open while playing.

### Join A Friend

If another player is hosting:

1. Ask the host for their LAN/Hamachi IPv4 address.
2. Edit `LocalServer.txt`.
3. Set `ip` to the host address, for example:

   ```txt
   ip: 25.12.34.56
   ```

4. Run `StartClient.bat`.
5. Do not run `StartAllLocal.bat` unless you want to host your own server.

### Host For Friends

To host a private LAN/Hamachi server:

1. Install Hamachi, Porthole, or connect all players to the same LAN/VPN.
2. Edit `ServerSettings.txt`.
3. Set `ip` to the LAN/Hamachi/Porthole address that friends can reach, for example:

   ```txt
   ip: 25.12.34.56
   admin: false
   anticheat: true
   lootBoost: 1
   ```

4. Run `StartAllLocal.bat`.
5. In Windows Firewall, allow inbound TCP traffic for ports `8443` and `12000`.
6. Send the same IP address to friends.
7. Friends should edit only their `LocalServer.txt` and run `StartClient.bat`.

### Porthole Hosting

When using Porthole, open/share the required ports in Porthole itself:

```txt
tcp 8443
tcp 12000
```

Use the Porthole lobby/host IP address that other players can reach. On the host
PC, put that reachable IP in `ServerSettings.txt`:

```txt
ip: 25.12.34.56
```

Friends put the same reachable IP in their `LocalServer.txt` and run only
`StartClient.bat`:

```txt
ip: 25.12.34.56
```

Do not add `:8443` or `:12000` to `LocalServer.txt`; the client already uses
`8443` for the web/login server and `12000` for the game connection. If Porthole
shows a different local endpoint for players, use the IP/address from that
endpoint instead.

Do not expose DynamoDB Local port `8000`. Player accounts and character progress
are stored on the host PC.

## Player Config

The release package has two config files. Keep them separate:

- `LocalServer.txt` is client-only. It tells this client which server to connect
  to.
- `ServerSettings.txt` is server-only. It controls the local server started by
  `StartServer.bat`.

Server settings:

- `ip`: server address. Use `127.0.0.1` for solo play, or the host LAN/Hamachi
  IPv4 address for private multiplayer.
- `admin`: when `true`, admin commands are enabled only for the local host client
  connected from the same PC.
- `anticheat`: when `false`, disables the local server's suspicious-hit
  disconnects.
- `lootBoost`: drop chance multiplier from `1` to `100`. Use `1` for normal
  loot, or `10` for the previously boosted local loot.

## Player Progress

Local progress is stored outside the extracted game folder:

```txt
%LOCALAPPDATA%\TrialsOfTitanLocal\DynamoDb
```

Because progress lives in `%LOCALAPPDATA%`, extracting a new release zip over an
old game folder should not overwrite player accounts or characters. The release
package also excludes local `*.db`, `*.db-shm`, `*.db-wal`, and `*.db-journal`
files.

## Requirements For Players

- Windows.
- Java Runtime Environment for DynamoDB Local, unless a portable Java runtime is
  included in `Runtime\Java`.
- .NET runtime for the local server, unless a portable runtime is included in
  `Runtime\DotNet` or a self-contained server executable is included.

## Developers

Use this section only if you want to work with the source code or create a new
release package.

### Source Layout

- `Client\Project-Titan-Client`: Unity client project.
- `Server\Project-Titan`: local web/game server source.
- `Library\TitanCore`: shared game data and networking code.
- `Database\DynamoDb`: DynamoDB Local files.

### Developer Requirements

- Unity Editor. The local build script looks for Unity `6000.3.11f1` by default,
  but you can override the path with `UNITY_EXE`.
- .NET SDK/runtime.
- Java Runtime Environment.

### Run From Source

1. Run `Database\DynamoDb\run.bat`.
2. Open `Server\Project-Titan\Project-Titan.sln` as administrator.
3. Run project `Run.Local.All`.
4. Open `Client\Project-Titan-Client` in Unity.
5. Run the client from the Unity Editor.

### Build A Release Zip

Run these scripts from `Client\Project-Titan-Client\LocalRun`:

1. Close this Unity project in the Unity Editor.
2. Run `BuildClient.bat`.
3. Run `BuildServer.bat`.
4. Check the default player-facing config files:

   ```txt
   PackageTemplate\LocalServer.txt
   PackageTemplate\ServerSettings.txt
   ```

5. Run `PackageLocalZip.bat`.

The finished archive is created at:

```txt
Client\Project-Titan-Client\Builds\TrialsOfTitanLocal.zip
```

`PackageLocalZip.bat` copies the package template, the built client, the
published server, and DynamoDB Local into the zip. It does not include local
DynamoDB database files.
