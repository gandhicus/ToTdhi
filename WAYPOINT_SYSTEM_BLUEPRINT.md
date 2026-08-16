# Waypoint Statue System — Implementation Blueprint

## Goal

Five interactable-free "waypoint statues" placed at the center of five overworld biomes. They are permanently visible on the minimap / mobile map for every player in the world, and clicking/tapping one on the map teleports the player to it using the existing teleport pipeline.

Hard requirements:

1. Statues are **not** players or enemies and must **not** affect `playerCount` / `MaxPlayerCount`.
2. One statue per biome, positioned at the center of that biome, in the `Overworld` only.
3. Statues are visible on the map at all times regardless of player distance or fog-of-war.
4. Teleporting to a statue uses the same `Goto` / `TnGoto` / `TnGotoAck` flow used for player teleport, including the existing 10-second cooldown.

Biomes and sprites (sprites already exist in `Client/Project-Titan-Client/Assets/Sprites/Waypoints/`):

| Biome | Sprite asset name | Overworld tile types |
|---|---|---|
| Weeping Wilderness | `Weeping Wilderness Waypoint` | `0xb05`, `0xb06` |
| Desolate Dunes | `Desolate Dunes Waypoint` | `0xb07`, `0xb08`, `0xb29` |
| Sanguine Shallows | `Sanguine Shallows Waypoint` | `0xb24`, `0xb25`, `0xb26`, `0xb27`, `0xb28` |
| Treacherous Tundra | `Treacherous Tundra Waypoint` | `0xb1e`, `0xb1f`, `0xb20`, `0xb21`, `0xb22`, `0xb23` |
| Perilous Peaks | `Perilous Peaks Waypoint` | `0xb0d`, `0xb0e` |

Tile IDs are taken from `Client/Project-Titan-Client/Assets/Scripts/UI/Map/BiomeTitle.cs` (`biomeAreaTiles`), which is the existing source of truth for biome naming.

---

## Design decisions (do not deviate)

1. **New object type.** Add `GameObjectType.Waypoint` plus a `WaypointInfo : StaticObjectInfo` data class and 5 XML entries. Because `ObjectManager.AddObject` only increments `players` for `GameObjectType.Player`, a `Waypoint` cannot affect realm player count. No change to player-count logic is needed or allowed.
2. **Placement is computed at runtime from the loaded map**, not baked into `overworld.mef`. There is no biome-bounds or biome-center data anywhere in the codebase today, so it must be derived by scanning `world.tiles`. Placement runs once in `Overworld.DoInitWorld()`.
3. **Always-synced via the existing global-object mechanism.** `ObjectManager` already force-syncs titans to every player when `world.AllowGlobalObjects` is true (and `Overworld.AllowGlobalObjects` is `true`). Generalize this to a `globalObjects` list so waypoints are always present client-side, which is what makes their minimap indicator always render. Do **not** invent a new "map marker" packet or a separate marker layer.
4. **Teleport reuses `/tpobj`.** `World/Commands/TeleportCommand.cs` already contains `TpObjCommand` (`/tpobj {object id}`, `Rank.Player`) which calls `player.Teleport(gameObject)` and succeeds for any object whose `Teleportable` is true. Setting `Waypoint.Teleportable => true` makes teleporting work with **zero new packets and zero new server handlers**. Client sends `new TnChat("/tpobj " + gameId)`, mirroring the existing `World.Teleport(Character)` which sends `"/teleport " + name`.
5. **No unlock/attunement, no persistence, no proximity interaction.** Statues are always usable by everyone. They are decorative in-world objects; all teleporting happens from the map UI.
6. Statues do **not** collide. Collision in this codebase is tile-derived (`TileManager`), and spawned `GameObject`s do not participate. This is accepted.

---

## Part 1 — Shared data layer (`Library/TitanCore`)

### 1.1 `Library/TitanCore/Data/GameObjectType.cs`

Append a new value at the **end** of the enum (the enum is serialized by ordinal in object stats/XML parsing, so never insert in the middle):

```csharp
    NomadCharm,

    Waypoint
```

### 1.2 New file `Library/TitanCore/Data/Map/WaypointInfo.cs`

Model exactly on `WardrobeInfo.cs`. `GameDataFile` uses `TypeFactory<GameObjectType, GameObjectInfo>` reflection, so simply existing is enough to register it.

```csharp
using System;
using System.Collections.Generic;
using System.Text;

namespace TitanCore.Data.Map
{
    public class WaypointInfo : StaticObjectInfo
    {
        public override GameObjectType Type => GameObjectType.Waypoint;
    }
}
```

### 1.3 XML entries

Add the following to **both** copies of the static object data (they are separate files and both must be edited):

- `Library/TitanCore/Data/Xmls/staticobjects.xml` (server canonical)
- `Client/Project-Titan-Client/Assets/Data/staticobjects.xml` (client copy, loaded as a `TextAsset` by `GameDataLoader`)

Highest existing id is `0xaac`, so use `0xaad`–`0xab1`. Insert before the closing `</Objects>`:

```xml
  <Object id="0xaad" name="Weeping Wilderness Waypoint">
    <Type>Waypoint</Type>
    <Sprite>Weeping Wilderness Waypoint</Sprite>
    <Size>1.4</Size>
  </Object>

  <Object id="0xaae" name="Desolate Dunes Waypoint">
    <Type>Waypoint</Type>
    <Sprite>Desolate Dunes Waypoint</Sprite>
    <Size>1.4</Size>
  </Object>

  <Object id="0xaaf" name="Sanguine Shallows Waypoint">
    <Type>Waypoint</Type>
    <Sprite>Sanguine Shallows Waypoint</Sprite>
    <Size>1.4</Size>
  </Object>

  <Object id="0xab0" name="Treacherous Tundra Waypoint">
    <Type>Waypoint</Type>
    <Sprite>Treacherous Tundra Waypoint</Sprite>
    <Size>1.4</Size>
  </Object>

  <Object id="0xab1" name="Perilous Peaks Waypoint">
    <Type>Waypoint</Type>
    <Sprite>Perilous Peaks Waypoint</Sprite>
    <Size>1.4</Size>
  </Object>
```

`<Sprite>` names must match the PNG asset names exactly, spaces included (`TextureManager` keys sprites by asset name).

---

## Part 2 — Server (`Server/Project-Titan`)

### 2.1 New file `Server/Project-Titan/World/Map/Objects/Map/Waypoint.cs`

Model on `World/Map/Objects/Map/Portal.cs` (it uses the same `ObjectStat<string>` name pattern that produces a client ground label).

```csharp
using System;
using System.Collections.Generic;
using System.Text;
using TitanCore.Data;
using TitanCore.Net.Packets.Models;
using World.GameState;

namespace World.Map.Objects.Map
{
    public class Waypoint : GameObject
    {
        public override GameObjectType Type => GameObjectType.Waypoint;

        public override bool Ticks => false;

        public override bool Global => true;

        public override bool Teleportable => true;

        public ObjectStat<string> waypointName = new ObjectStat<string>(ObjectStatType.Name, ObjectStatScope.Public, "", "");

        protected override void GetStats(List<ObjectStat> list)
        {
            base.GetStats(list);

            list.Add(waypointName);
        }
    }
}
```

Notes:
- `Teleportable => true` is what enables `/tpobj`. `Player.Teleport` still enforces the 10s `lastTeleport` cooldown and still routes through `Goto` → `TnGoto` → `TnGotoAck`, so invulnerability and projectile-block behaviour are unchanged.
- `Ticks => false` means the object costs nothing per tick.

### 2.2 `Server/Project-Titan/World/Map/Objects/GameObject.cs`

Add a virtual flag next to the existing `Teleportable`:

```csharp
        public virtual bool Teleportable => false;

        /// <summary>
        /// If true, this object is synced to every player in the world regardless of sight
        /// </summary>
        public virtual bool Global => false;
```

### 2.3 `Server/Project-Titan/World/Map/ObjectManager.cs`

Generalize the existing titan global-sync path.

**a)** Add the list next to `public List<Enemy> titans`:

```csharp
        public List<GameObject> globalObjects = new List<GameObject>();
```

**b)** In `AddObject`, after the `switch (obj.Type)` block and before `obj.OnAddToWorld();`:

```csharp
            if (obj.Global)
                globalObjects.Add(obj);
```

**c)** In `RemoveObject`, before/after the `switch`:

```csharp
            if (obj.Global)
                globalObjects.Remove(obj);
```

**d)** In `NetworkTickPlayer`, extend the existing global block:

```csharp
            if (world.AllowGlobalObjects)
            {
                foreach (var titan in titans)
                    player.ProcessObject(titan, ref time);

                foreach (var obj in globalObjects)
                    player.ProcessObject(obj, ref time);
            }
```

Do **not** add global objects to `TickLogic` — `Waypoint.Ticks` is false. `PlayerGameState.ProcessObject` already de-duplicates by `processedObjectIds`, so a waypoint that is also inside the player's sight rect is not sent twice. `Overworld.LimitSight` is `false`, so the fog-of-war gate in `ProcessObject` does not suppress waypoints.

### 2.4 New file `Server/Project-Titan/World/Map/Waypoints/WaypointSystem.cs`

Responsible for computing biome centers and spawning the statues. Runs once, synchronously, during world init.

**Definition table** (static, inside `WaypointSystem`):

```csharp
private class WaypointDefinition
{
    public ushort objectType;      // 0xaad .. 0xab1
    public string name;            // "Weeping Wilderness"
    public ushort[] tileTypes;
    public Int2? overridePosition; // optional hand-placed override, null = compute
}
```

Populate with the five rows from the table at the top of this document. `name` is the biome name without the word "Waypoint" (`"Weeping Wilderness"`, `"Desolate Dunes"`, `"Sanguine Shallows"`, `"Treacherous Tundra"`, `"Perilous Peaks"`) — this string becomes `waypointName` and therefore the in-world ground label and the map UI label. Leave `overridePosition` as `null` for all five; it exists so positions can be hand-tuned later without changing the algorithm.

**Placement algorithm** — `Int2 FindBiomeCenter(World world, ushort[] tileTypes)`:

1. Build a `HashSet<ushort>` of the biome's tile types.
2. Single pass over `x in [0, world.width)`, `y in [0, world.height)` reading `world.tiles.GetTile(x, y).tileType`. Collect every matching coordinate into a `List<Int2>` (this mirrors `SpawnSystem.TileSpawnData.FindTiles`).
3. Because a biome type appears in multiple disjoint Voronoi cells across the map, split the matches into **connected components** (4-way BFS) using a reusable `bool[,] visited` array sized `world.width x world.height`. Keep only the **largest** component — that is "the biome".
4. Compute the arithmetic centroid of the largest component.
5. The centroid may land outside the component (concave shapes) or on unwalkable tiles (e.g. `0xb24` deep lake in Sanguine Shallows). Select the final tile as the member of the largest component that is closest to the centroid **and** satisfies `world.tiles.CanWalk(x + 0.5f, y + 0.5f)`. If no member is walkable, fall back to the closest member regardless.
6. Return that `Int2`. Log it: `Log.Write($"Waypoint '{name}' placed at {position}")`.

Performance: the overworld map is up to 2048×2048. One full scan per biome (5 scans) plus BFS at world init is acceptable, but prefer a **single** map scan that buckets all five biomes' tiles at once, then run BFS per biome. Reuse one `visited` array across biomes by clearing only the coordinates touched.

**Spawn** — `public void Spawn()`:

For each definition:
1. Resolve position: `definition.overridePosition ?? FindBiomeCenter(...)`. If no tiles were found for the biome, log a warning and skip that waypoint (do not throw — gate maps and future map revisions must not crash world init).
2. `GameData.objects.TryGetValue(definition.objectType, out var info)`; require `info is WaypointInfo`.
3. Construct and add:

```csharp
var waypoint = new Waypoint();
waypoint.Initialize(info);
waypoint.waypointName.Value = definition.name;
waypoint.position.Value = position.ToVec2() + 0.5f;
world.objects.AddObject(waypoint);
```

4. Register a no-spawn zone so enemies do not crowd the statue: `world.spawnSystem.AddNoSpawnZone(waypoint.position.Value, 10);`
5. Keep spawned waypoints in a `public List<Waypoint> waypoints` for the optional `/waypoints` command below.

### 2.5 `Server/Project-Titan/World/Worlds/Overworld.cs`

Add a field and a call in `DoInitWorld()`. It must run **after** `spawnSystem` is constructed so no-spawn zones apply:

```csharp
        public WaypointSystem waypointSystem;
```

```csharp
            spawnSystem = new SpawnSystem(this);
            spawnSystem.AddNoSpawnZone(spawn, 20);

            waypointSystem = new WaypointSystem(this);
            waypointSystem.Spawn();

            overworldCycle = new OverworldCycle(this);
```

Only `Overworld` gets waypoints. Do not touch `Nexus` or any gate world.

### 2.6 Optional but recommended: `Server/Project-Titan/World/Commands/WaypointsCommand.cs`

A `Rank.Player` command `/waypoints` that returns each waypoint's name, position, and `gameId` via `ChatData.Info`. This is the fastest way to verify placement and to test teleporting before the UI is wired. Model on the existing command classes in `World/Commands/`.

---

## Part 3 — Client (`Client/Project-Titan-Client`)

### 3.1 Sprite atlas

The `Assets/Sprites/Waypoints` folder (guid `1f81f20a52ba3b04fb69ada41ed5f374`) is **not** currently packed into `Assets/Sprites/GameObjects.spriteatlas`. `TextureManager.Init` only indexes sprites that are inside the passed atlases, so `TextureManager.GetSprite("Weeping Wilderness Waypoint")` returns null until this is fixed.

Add this line to the `packables:` list in `Assets/Sprites/GameObjects.spriteatlas`:

```yaml
    - {fileID: 102900000, guid: 1f81f20a52ba3b04fb69ada41ed5f374, type: 3}
```

The existing waypoint `.png.meta` importers are already correct for world objects (`spritePixelsToUnits: 8`, `spritePivot: {x: 0.5, y: 0.1}`), matching other game objects. Leave them alone.

### 3.2 New file `Assets/Scripts/World/WorldObjects/Map/Waypoint.cs`

Model on `Assets/Scripts/World/WorldObjects/Map/Portal.cs` (ground label from the `Name` stat) plus the titan branch of `Entities/Enemy.cs` (custom map indicator sprite).

```csharp
using TitanCore.Data;
using TitanCore.Net.Packets.Models;
using UnityEngine;

public class Waypoint : SpriteWorldObject
{
    public override GameObjectType ObjectType => GameObjectType.Waypoint;

    public string waypointName { get; private set; } = "";

    public override void LoadObjectInfo(GameObjectInfo info)
    {
        base.LoadObjectInfo(info);

        name = info.name;

        indicator.spriteRenderer.sprite = TextureManager.GetDisplaySprite(info);
        indicator.spriteRenderer.color = Color.white;
        indicator.sizeAdjustment = 0.35f;
    }

    protected override void ProcessStat(NetStat stat, bool first)
    {
        base.ProcessStat(stat, first);

        switch (stat.type)
        {
            case ObjectStatType.Name:
                waypointName = (string)stat.value;
                ShowGroundLabel(waypointName);
                break;
        }
    }
}
```

`indicator` is the protected field on `SpriteWorldObject`, resolved in `Awake` via `GetComponentInChildren<Indicator>()`, which is why the prefab must contain an `Indicator` child (next step).

### 3.3 Prefab `Assets/Prefabs/World/Map/Waypoint.prefab`

Duplicate `Assets/Prefabs/World/Map/Portal.prefab` (it already has the `Indicator` child correctly wired) and:
- Replace the `Portal` script component with the new `Waypoint` script.
- Confirm the `Indicator` child's `obj` reference points at the root and that `spriteRenderer`/`circleSprite` are still assigned.
- Leave the indicator on the minimap-camera layer used by `Portal.prefab` — that is what makes it render into the minimap and mobile map render texture.

### 3.4 Register the prefab

In `Assets/Prefabs/World/Game.prefab`, add the new `Waypoint.prefab` to the `ObjectManager.objectPrefabs` array. `ObjectManager.SortPrefabs` keys the pool by `WorldObject.ObjectType`, so a missing entry silently makes `TryGetObject` return false and no statue will ever appear.

### 3.5 `Assets/Scripts/World/World.cs`

Add a waypoint list and a teleport entry point next to the existing `Teleport(Character)`:

```csharp
    [HideInInspector]
    public List<Waypoint> waypoints = new List<Waypoint>();
```

```csharp
    public void TeleportToWaypoint(Waypoint waypoint)
    {
        if (waypoint == null) return;
        gameManager.client.SendAsync(new TnChat("/tpobj " + waypoint.gameId));
    }
```

Maintain the list in `SortObject` / `UnsortObject`, alongside the existing `containers` / `interactables` handling:

```csharp
        if (worldObject is Waypoint waypoint)
            waypoints.Add(waypoint);
```

```csharp
        if (worldObject is Waypoint waypoint)
            waypoints.Remove(waypoint);
```

`NewMap` already clears all objects through `RemoveObject`, so the list drains on world change.

### 3.6 PC map click — `Assets/Scripts/UI/Map/Minimap.cs` + `UI/Social/TeleportPanel.cs` + `UI/Social/TeleportOption.cs`

`Minimap.OnPointerClick` already converts the click to a world position and calls `teleportPanel.Show(worldPos, mapSize * 0.4f)`. Extend the panel rather than adding a second panel.

**`TeleportPanel.Show(Vector2 worldPosition, float sampleRadius)`:**
- Build the candidate list as it does today (characters within `sampleRadius`, nearest first).
- Additionally collect `world.waypoints` within `sampleRadius`, nearest first.
- Present **waypoints first**, then characters, then fill remaining `options` with `Setup(null)`. Waypoints are large fixed landmarks; a click near a statue should prefer the statue.
- Keep the current early-return when there is nothing to show.

**`TeleportOption`:** add a `Setup(Waypoint waypoint)` overload that stores the waypoint (clearing the character reference), shows the waypoint's display sprite via `TextureManager.GetDisplaySprite(waypoint.info)` where the class preview currently goes, and sets the label to `waypoint.waypointName`. In `Select()`, branch: if a waypoint is set call `world.TeleportToWaypoint(waypoint)`, otherwise the existing `world.Teleport(character)`.

Increase the number of `TeleportOption` slots in the `TeleportPanel` prefab if five options is too few once waypoints share the list.

### 3.7 Mobile map tap — `Assets/Scripts/UI/Map/MobileMap.cs`

`OnPointerUp` already filters out drags/long-presses and converts the tap to `worldPos`, then finds the nearest `Character` within `heightView * 0.06f`.

Add a waypoint check **before** the character check, using the same radius:
- Find the nearest `world.waypoints` entry within `heightView * 0.06f`.
- If one is found, hide any open tooltip and show a new lightweight `WaypointTooltipMobile` (modeled on `Assets/Scripts/UI/Tooltips/Mobile/PlayerTooltipMobile.cs`): waypoint sprite, `waypointName`, and a single **Teleport** button that calls `world.TeleportToWaypoint(waypoint)` then closes the tooltip and the side menu — exactly what `PlayerTooltipMobile.Teleport()` does today.
- Only fall through to the existing character/tooltip logic when no waypoint was hit.

Create the matching prefab under `Assets/Prefabs/UI/` next to the other mobile tooltips and wire it into `MobileGameUI` the same way `PlayerTooltipMobile` is.

---

## Part 4 — Explicitly out of scope

- No new packet types (`TnPacketType` is untouched; next free value would be `50` if a future change needs one).
- No changes to `playerCount`, `MaxPlayerCount`, `NetConstants.Max_Overworld_Players`, or `InstanceManager`.
- No changes to `overworld.mef`, `WorldCreator`, `MapEditor`, or `MapElementFile`.
- No changes to fog-of-war / `TnTiles` / tile discovery.
- No account persistence, unlock gating, or per-player waypoint state.
- No changes to `Player.Teleport`, `Player.Goto`, `GotoAckHandler`, or the 10s cooldown.

---

## Part 5 — File checklist

**Modified**
- `Library/TitanCore/Data/GameObjectType.cs`
- `Library/TitanCore/Data/Xmls/staticobjects.xml`
- `Client/Project-Titan-Client/Assets/Data/staticobjects.xml`
- `Server/Project-Titan/World/Map/Objects/GameObject.cs`
- `Server/Project-Titan/World/Map/ObjectManager.cs`
- `Server/Project-Titan/World/Worlds/Overworld.cs`
- `Client/Project-Titan-Client/Assets/Sprites/GameObjects.spriteatlas`
- `Client/Project-Titan-Client/Assets/Prefabs/World/Game.prefab`
- `Client/Project-Titan-Client/Assets/Scripts/World/World.cs`
- `Client/Project-Titan-Client/Assets/Scripts/UI/Social/TeleportPanel.cs`
- `Client/Project-Titan-Client/Assets/Scripts/UI/Social/TeleportOption.cs`
- `Client/Project-Titan-Client/Assets/Scripts/UI/Map/MobileMap.cs`

**Added**
- `Library/TitanCore/Data/Map/WaypointInfo.cs`
- `Server/Project-Titan/World/Map/Objects/Map/Waypoint.cs`
- `Server/Project-Titan/World/Map/Waypoints/WaypointSystem.cs`
- `Server/Project-Titan/World/Commands/WaypointsCommand.cs` (optional)
- `Client/Project-Titan-Client/Assets/Scripts/World/WorldObjects/Map/Waypoint.cs`
- `Client/Project-Titan-Client/Assets/Prefabs/World/Map/Waypoint.prefab`
- `Client/Project-Titan-Client/Assets/Scripts/UI/Tooltips/Mobile/WaypointTooltipMobile.cs` + prefab

---

## Part 6 — Verification

1. Server boots locally (`WorldModule` starts `Nexus` + `Overworld`) with five `Waypoint '<name>' placed at <x, y>` log lines and no exceptions.
2. `/waypoints` in the overworld lists five entries with plausible, well-separated coordinates.
3. Enter the overworld from spawn without exploring: all five statue icons are already visible on the PC minimap when zoomed fully out (`mapCentered`), and on the mobile full-screen map. This confirms global sync is working.
4. Clicking a statue icon on the PC minimap opens `TeleportPanel` with the statue as the first option; selecting it teleports the player next to the statue.
5. Immediately teleporting again is rejected for ~10 seconds with the existing "Unable to teleport" chat error, confirming the shared cooldown.
6. Tapping a statue icon on the mobile map opens the waypoint tooltip and its Teleport button works, closing the side menu.
7. On arrival the statue renders with the correct biome sprite and shows its biome name as a ground label.
8. `playerCount` reported to the Nexus portal label (`Overworld ({n}/75)`) is unchanged by the presence of the statues — spawn statues, then confirm the count equals the number of connected players.
9. Enemies do not spawn on top of the statues (no-spawn zone).
