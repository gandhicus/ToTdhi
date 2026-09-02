using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using TitanCore.Core;
using TitanCore.Files;
using Utils.NET.Geometry;
using Utils.NET.Logging;
using Utils.NET.Utils;
using WorldGen.Rasterization;
using static TitanCore.Files.MapElementFile;

namespace WorldGen
{
    /// <summary>
    /// Builds a fresh overworld island at server start. Replaces the baked overworld.mef
    /// with blob-shaped biomes so existing waypoint, spawn, and music systems keep working.
    /// </summary>
    public static class OverworldGenerator
    {
        public const int Map_Size = 2048;
        public const int Point_Count = 5000;
        public const int Relaxations = 3;
        private const int Max_Attempts = 12;

        /// <summary>
        /// How far inland the sand rim reaches. Painted from a per-tile distance field so it
        /// follows the rounded shoreline instead of whole Voronoi cells.
        /// </summary>
        private const int Beach_Width = 18;

        /// <summary>
        /// How far inland from the water the Nexus sits, on the same rim as the rest of the island.
        /// </summary>
        private const int Spawn_Water_Distance = 8;

        public const int Fireside_Offset_X = -7;
        public const int Fireside_Offset_Y = 5;

        /// <summary>
        /// How far around the Nexus and Fireside to strip scenery. Keep this small so
        /// beach rocks and meadow trees still appear next to the landing.
        /// </summary>
        private const int Spawn_Clear_Radius = 4;

        public static MapElementFile Generate()
        {
            return Generate(Rand.IntValue());
        }

        public static MapElementFile Generate(int seed)
        {
            Exception lastError = null;
            for (int attempt = 0; attempt < Max_Attempts; attempt++)
            {
                int attemptSeed = seed + attempt;
                try
                {
                    var stopwatch = Stopwatch.StartNew();
                    var world = new World(Map_Size, Map_Size, attemptSeed);
                    world.GenerateIsland(Point_Count, Relaxations);
                    if (!world.TryAssignRealmBiomes())
                    {
                        Log.Write($"Overworld seed {attemptSeed} rejected: biomes or spawn did not fit");
                        continue;
                    }

                    var map = Rasterize(world);
                    Log.Write($"Overworld generated seed={attemptSeed} spawn=({world.spawnTile.x},{world.spawnTile.y}) in {stopwatch.Elapsed.TotalSeconds:0.0}s");
                    return map;
                }
                catch (Exception e)
                {
                    lastError = e;
                    Log.Write($"Overworld seed {attemptSeed} failed: {e.Message}");
                }
            }

            throw new Exception("Failed to generate an overworld island after " + Max_Attempts + " attempts", lastError);
        }

        /// <summary>
        /// Ken Perlin in this project repeats every 256 units. If sample coordinates
        /// cross a multiple of 256, sand/water blobs get a straight seam and look like
        /// their top and bottom halves were cut apart. Keep the whole map inside one period.
        /// </summary>
        private const float Max_Noise_Span = 200f;
        private const float Noise_Offset_Range = 24f;

        private static MapElementFile Rasterize(World world)
        {
            int width = world.width;
            int height = world.height;
            var land = BuildLandMask(world, width, height);
            var waterDistance = BuildWaterDistance(land, width, height);

            var tiles = new MapTileElement[width, height];
            var noiseOffset = new Vec2(
                (float)world.random.NextDouble() * Noise_Offset_Range,
                (float)world.random.NextDouble() * Noise_Offset_Range);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var biomeType = BiomeAt(world, land, waterDistance, x, y);
                    var biome = OverworldBiomes.Get(biomeType);

                    // Ocean and beach are a single tile type. Skip Perlin there — it was a big chunk of gen time.
                    ushort tileType;
                    if (biomeType == OverworldBiomeType.Ocean)
                        tileType = 0xb03;
                    else if (biomeType == OverworldBiomeType.Beach)
                        tileType = 0xb04;
                    else
                    {
                        float scale = biome.perlinScale <= 0 ? 1 : biome.perlinScale;
                        if (scale > Max_Noise_Span)
                            scale = Max_Noise_Span;

                        float noise = (float)Perlin.Noise(
                            noiseOffset.x + (x / (float)width) * scale,
                            noiseOffset.y + (y / (float)height) * scale,
                            0);

                        tileType = biome.tiles[noise];
                        if (tileType == 0)
                            tileType = 0xb04;
                    }

                    ushort objectType = 0;
                    var spec = biome.objects.Get((float)world.random.NextDouble());
                    if (spec.objectTypes != null)
                        objectType = spec.Get(tileType);

                    tiles[x, y] = new MapTileElement
                    {
                        tileType = tileType,
                        objectType = objectType
                    };
                }
            }

            world.spawnTile = SnapSpawnToBeach(tiles, world.spawnTile, waterDistance, width, height);
            ClearSpawn(tiles, world.spawnTile, width, height);

            return new MapElementFile
            {
                width = width,
                height = height,
                tiles = tiles,
                entities = new MapEntityElement[0],
                regions = new MapRegionElement[]
                {
                    new MapRegionElement
                    {
                        x = (uint)world.spawnTile.x,
                        y = (uint)world.spawnTile.y,
                        regionType = Region.Spawn
                    }
                }
            };
        }

        private static OverworldBiomeType BiomeAt(World world, bool[,] land, int[,] waterDistance, int x, int y)
        {
            if (!land[x, y])
                return OverworldBiomeType.Ocean;

            if (waterDistance[x, y] <= Beach_Width)
                return OverworldBiomeType.Beach;

            var center = world.GetCenterNear(x, y);
            if (center == null || center.water || center.biomeType == OverworldBiomeType.Ocean)
                return OverworldBiomeType.Meadows;
            if (center.biomeType == OverworldBiomeType.Beach)
                return OverworldBiomeType.Meadows;
            return center.biomeType;
        }

        private static bool[,] BuildLandMask(World world, int width, int height)
        {
            var land = new bool[width, height];
            Parallel.For(0, height, y =>
            {
                for (int x = 0; x < width; x++)
                    land[x, y] = world.IsLandAt(x, y);
            });

            SmoothLandMask(land, width, height, 2);
            KeepMainland(land, width, height);
            return land;
        }

        // A 1-tile sand tooth into the ocean usually has 3 land neighbors behind it.
        // Requiring 4 kills those crenellations. Water with 5 land neighbors is a 1-tile
        // notch and gets filled, so the rim is large bays instead of a square-wave edge.
        private const int Land_Keep_Neighbors = 4;
        private const int Water_Fill_Neighbors = 5;

        private static void SmoothLandMask(bool[,] land, int width, int height, int passes)
        {
            var next = new bool[width, height];
            for (int pass = 0; pass < passes; pass++)
            {
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int landNeighbors = 0;
                        for (int oy = -1; oy <= 1; oy++)
                        {
                            for (int ox = -1; ox <= 1; ox++)
                            {
                                if (ox == 0 && oy == 0) continue;
                                int nx = x + ox;
                                int ny = y + oy;
                                if (nx < 0 || ny < 0 || nx >= width || ny >= height) continue;
                                if (land[nx, ny]) landNeighbors++;
                            }
                        }

                        if (land[x, y])
                            next[x, y] = landNeighbors >= Land_Keep_Neighbors;
                        else
                            next[x, y] = landNeighbors >= Water_Fill_Neighbors;
                    }
                }

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                        land[x, y] = next[x, y];
                }
            }
        }

        private static void KeepMainland(bool[,] land, int width, int height)
        {
            int startX = width / 2;
            int startY = height / 2;
            if (!land[startX, startY])
            {
                bool found = false;
                for (int r = 1; r < Math.Min(width, height) / 2 && !found; r++)
                {
                    for (int y = startY - r; y <= startY + r && !found; y++)
                    {
                        for (int x = startX - r; x <= startX + r && !found; x++)
                        {
                            if (x < 0 || y < 0 || x >= width || y >= height) continue;
                            if (!land[x, y]) continue;
                            startX = x;
                            startY = y;
                            found = true;
                        }
                    }
                }
                if (!found) return;
            }

            var keep = new bool[width, height];
            var queue = new Queue<Int2>();
            keep[startX, startY] = true;
            queue.Enqueue(new Int2(startX, startY));
            while (queue.Count > 0)
            {
                var p = queue.Dequeue();
                TryEnqueueLand(land, keep, queue, width, height, p.x + 1, p.y);
                TryEnqueueLand(land, keep, queue, width, height, p.x - 1, p.y);
                TryEnqueueLand(land, keep, queue, width, height, p.x, p.y + 1);
                TryEnqueueLand(land, keep, queue, width, height, p.x, p.y - 1);
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                    land[x, y] = keep[x, y];
            }
        }

        private static void TryEnqueueLand(bool[,] land, bool[,] keep, Queue<Int2> queue, int width, int height, int x, int y)
        {
            if (x < 0 || y < 0 || x >= width || y >= height) return;
            if (!land[x, y] || keep[x, y]) return;
            keep[x, y] = true;
            queue.Enqueue(new Int2(x, y));
        }

        private static int[,] BuildWaterDistance(bool[,] land, int width, int height)
        {
            var dist = new int[width, height];
            var queue = new Queue<Int2>();
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (land[x, y])
                    {
                        dist[x, y] = int.MaxValue;
                        continue;
                    }
                    dist[x, y] = 0;
                    queue.Enqueue(new Int2(x, y));
                }
            }

            while (queue.Count > 0)
            {
                var p = queue.Dequeue();
                int next = dist[p.x, p.y] + 1;
                TryEnqueueDistance(land, dist, queue, width, height, p.x + 1, p.y, next);
                TryEnqueueDistance(land, dist, queue, width, height, p.x - 1, p.y, next);
                TryEnqueueDistance(land, dist, queue, width, height, p.x, p.y + 1, next);
                TryEnqueueDistance(land, dist, queue, width, height, p.x, p.y - 1, next);
            }
            return dist;
        }

        private static void TryEnqueueDistance(bool[,] land, int[,] dist, Queue<Int2> queue, int width, int height, int x, int y, int next)
        {
            if (x < 0 || y < 0 || x >= width || y >= height) return;
            if (!land[x, y]) return;
            if (next >= dist[x, y]) return;
            dist[x, y] = next;
            queue.Enqueue(new Int2(x, y));
        }

        private static Int2 SnapSpawnToBeach(MapTileElement[,] tiles, Int2 hint, int[,] waterDistance, int width, int height)
        {
            Int2 best = hint;
            int bestScore = int.MaxValue;
            int search = 120;
            int minX = Math.Max(1, hint.x - search);
            int maxX = Math.Min(width - 2, hint.x + search);
            int minY = Math.Max(1, hint.y - search);
            int maxY = Math.Min(height - 2, hint.y + search);

            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    if (tiles[x, y].tileType != 0xb04) continue;

                    int dist = waterDistance[x, y];
                    if (dist < 6 || dist > Beach_Width - 6) continue;

                    int fx = x + Fireside_Offset_X;
                    int fy = y + Fireside_Offset_Y;
                    if (fx < 1 || fy < 1 || fx >= width - 1 || fy >= height - 1) continue;
                    if (tiles[fx, fy].tileType == 0xb03) continue;

                    int dx = x - hint.x;
                    int dy = y - hint.y;
                    int distPenalty = dist - Spawn_Water_Distance;
                    if (distPenalty < 0) distPenalty = -distPenalty;
                    int score = dx * dx + dy * dy + distPenalty * 40;
                    if (score < bestScore)
                    {
                        bestScore = score;
                        best = new Int2(x, y);
                    }
                }
            }

            return best;
        }

        private static void ClearSpawn(MapTileElement[,] tiles, Int2 spawn, int width, int height)
        {
            int fx = spawn.x + Fireside_Offset_X;
            int fy = spawn.y + Fireside_Offset_Y;

            ClearSpawnPad(tiles, width, height, spawn.x, spawn.y, Spawn_Clear_Radius, true);
            ClearSpawnPad(tiles, width, height, fx + 1, fy, 3, false);
        }

        private static void ClearSpawnPad(MapTileElement[,] tiles, int width, int height, int cx, int cy, int radius, bool forceSand)
        {
            int r2 = radius * radius;
            int minX = Math.Max(0, cx - radius);
            int maxX = Math.Min(width - 1, cx + radius);
            int minY = Math.Max(0, cy - radius);
            int maxY = Math.Min(height - 1, cy + radius);
            for (int y = minY; y <= maxY; y++)
            {
                for (int x = minX; x <= maxX; x++)
                {
                    int dx = x - cx;
                    int dy = y - cy;
                    if (dx * dx + dy * dy > r2) continue;

                    var tile = tiles[x, y];
                    tile.objectType = 0;
                    if (forceSand && tile.tileType != 0xb03)
                        tile.tileType = 0xb04;
                    tiles[x, y] = tile;
                }
            }
        }
    }
}
