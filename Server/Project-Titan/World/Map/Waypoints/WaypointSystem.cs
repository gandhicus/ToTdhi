using System;
using System.Collections.Generic;
using System.Text;
using TitanCore.Core;
using TitanCore.Data;
using TitanCore.Data.Map;
using Utils.NET.Geometry;
using Utils.NET.Logging;
using World.Map.Objects.Map;
using World.Worlds;

namespace World.Map.Waypoints
{
    public class WaypointSystem
    {
        private class WaypointDefinition
        {
            public ushort objectType;
            public string name;
            public ushort[] tileTypes;
            public Int2? overridePosition;
        }

        private static readonly WaypointDefinition[] definitions = new WaypointDefinition[]
        {
            new WaypointDefinition
            {
                objectType = 0xaad,
                name = "Weeping Wilderness",
                tileTypes = new ushort[] { 0xb05, 0xb06 },
            },
            new WaypointDefinition
            {
                objectType = 0xaae,
                name = "Desolate Dunes",
                tileTypes = new ushort[] { 0xb07, 0xb08, 0xb29 },
            },
            new WaypointDefinition
            {
                objectType = 0xaaf,
                name = "Sanguine Shallows",
                tileTypes = new ushort[] { 0xb24, 0xb25, 0xb26, 0xb27, 0xb28 },
            },
            new WaypointDefinition
            {
                objectType = 0xab0,
                name = "Treacherous Tundra",
                tileTypes = new ushort[] { 0xb1e, 0xb1f, 0xb20, 0xb21, 0xb22, 0xb23 },
            },
            new WaypointDefinition
            {
                objectType = 0xab1,
                name = "Perilous Peaks",
                tileTypes = new ushort[] { 0xb0d, 0xb0e },
            },
        };

        public List<Waypoint> waypoints = new List<Waypoint>();

        private readonly Overworld world;

        public WaypointSystem(Overworld world)
        {
            this.world = world;
        }

        public void Spawn()
        {
            var buckets = BucketTilesByDefinition();

            foreach (var definition in definitions)
            {
                var position = definition.overridePosition ?? FindBiomeCenter(world, buckets[definition.objectType], definition.name);
                if (!position.HasValue)
                {
                    Log.Write($"Warning: no tiles found for waypoint '{definition.name}', skipping");
                    continue;
                }

                if (!GameData.objects.TryGetValue(definition.objectType, out var info) || !(info is WaypointInfo))
                {
                    Log.Write($"Warning: missing WaypointInfo for object type {definition.objectType}, skipping '{definition.name}'");
                    continue;
                }

                var waypoint = new Waypoint();
                waypoint.Initialize(info);
                waypoint.waypointName.Value = definition.name;
                waypoint.position.Value = position.Value.ToVec2() + 0.5f;
                ClearPad(position.Value, 2);
                world.objects.AddObject(waypoint);
                world.spawnSystem.AddNoSpawnZone(waypoint.position.Value, 10);
                waypoints.Add(waypoint);

                Log.Write($"Waypoint '{definition.name}' placed at {position.Value}");
            }
        }

        private Dictionary<ushort, List<Int2>> BucketTilesByDefinition()
        {
            var tileToObjectType = new Dictionary<ushort, ushort>();
            foreach (var definition in definitions)
            {
                foreach (var tileType in definition.tileTypes)
                    tileToObjectType[tileType] = definition.objectType;
            }

            var buckets = new Dictionary<ushort, List<Int2>>();
            foreach (var definition in definitions)
                buckets[definition.objectType] = new List<Int2>();

            for (int y = 0; y < world.height; y++)
            {
                for (int x = 0; x < world.width; x++)
                {
                    var tile = world.tiles.GetTile(x, y);
                    if (!tileToObjectType.TryGetValue(tile.tileType, out var objectType)) continue;
                    buckets[objectType].Add(new Int2(x, y));
                }
            }

            return buckets;
        }

        private static long PackTile(int x, int y) => ((long)x << 32) | (uint)y;

        private Int2? FindBiomeCenter(Overworld world, List<Int2> tiles, string name)
        {
            if (tiles.Count == 0) return null;

            var tileSet = new HashSet<long>();
            foreach (var tile in tiles)
                tileSet.Add(PackTile(tile.x, tile.y));

            var visited = new HashSet<long>();
            List<Int2> largestComponent = null;

            foreach (var start in tiles)
            {
                var startKey = PackTile(start.x, start.y);
                if (visited.Contains(startKey)) continue;

                var component = new List<Int2>();
                var queue = new Queue<Int2>();
                queue.Enqueue(start);
                visited.Add(startKey);

                while (queue.Count > 0)
                {
                    var current = queue.Dequeue();
                    component.Add(current);

                    foreach (var adjacent in current.Adjacent)
                    {
                        var key = PackTile(adjacent.x, adjacent.y);
                        if (!tileSet.Contains(key) || visited.Contains(key)) continue;
                        visited.Add(key);
                        queue.Enqueue(adjacent);
                    }
                }

                if (largestComponent == null || component.Count > largestComponent.Count)
                    largestComponent = component;
            }

            float centroidX = 0;
            float centroidY = 0;
            foreach (var tile in largestComponent)
            {
                centroidX += tile.x;
                centroidY += tile.y;
            }
            centroidX /= largestComponent.Count;
            centroidY /= largestComponent.Count;

            Int2? best = null;
            float bestDistance = float.MaxValue;
            Int2? fallback = null;
            float fallbackDistance = float.MaxValue;

            foreach (var tile in largestComponent)
            {
                var dx = tile.x - centroidX;
                var dy = tile.y - centroidY;
                var distance = dx * dx + dy * dy;

                if (IsOpenPad(tile) && distance < bestDistance)
                {
                    bestDistance = distance;
                    best = tile;
                }

                if (world.tiles.CanWalk(tile.x + 0.5f, tile.y + 0.5f) && distance < fallbackDistance)
                {
                    fallbackDistance = distance;
                    fallback = tile;
                }
            }

            return best ?? fallback;
        }

        private bool IsOpenPad(Int2 tile)
        {
            if (!world.tiles.PlayerCanWalk(tile.x + 0.5f, tile.y + 0.5f))
                return false;

            var mapTile = world.tiles.GetTile(tile.x, tile.y);
            if (mapTile.tileType == 0xb0e) return false;

            var objInfo = mapTile.GetObjectInfo();
            if (objInfo == null) return true;
            if (objInfo is Object3dInfo) return false;
            if (objInfo is StaticObjectInfo staticObj && staticObj.collidable) return false;
            return true;
        }

        private void ClearPad(Int2 center, int radius)
        {
            int r2 = radius * radius;
            for (int y = center.y - radius; y <= center.y + radius; y++)
            {
                for (int x = center.x - radius; x <= center.x + radius; x++)
                {
                    if (x < 0 || y < 0 || x >= world.width || y >= world.height) continue;
                    int dx = x - center.x;
                    int dy = y - center.y;
                    if (dx * dx + dy * dy > r2) continue;
                    var tile = world.tiles.GetTile(x, y);
                    if (tile.tileType == 0) continue;
                    tile.objectType = 0;
                    world.tiles.SetTile(tile);
                }
            }
        }
    }
}
