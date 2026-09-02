using BenTools.Mathematics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TitanCore.Net.Packets.Models;
using Utils.NET;
using Utils.NET.Geometry;
using Utils.NET.Logging;
using Utils.NET.Pathfinding;
using Utils.NET.Utils;
using WorldGen.Nodes;
using WorldGen.Rasterization;

namespace WorldGen
{
    public class World
    {
        private const float Perlin_Scale = 2f;

        /// <summary>
        /// The width of the world in tiles
        /// </summary>
        public int width;

        /// <summary>
        /// The height of the world in tiles
        /// </summary>
        public int height;

        /// <summary>
        /// The random object for this world
        /// </summary>
        public Random random;

        /// <summary>
        /// The random used to generate the map shape
        /// </summary>
        private Random shapeRandom;

        public List<Center> centers = new List<Center>();

        public List<Corner> corners = new List<Corner>();

        public List<Edge> edges = new List<Edge>();

        public List<Center> landCenters = new List<Center>();

        public List<Corner> landCorners = new List<Corner>();

        public HashSet<Corner> rivers = new HashSet<Corner>();

        public HashSet<List<Vec2>> roads = new HashSet<List<Vec2>>();

        public HashSet<Center> towns = new HashSet<Center>();

        private Center[] pixelMap;

        private int centerIds;

        private int cornerIds;

        private int edgeIds;

        private Vec2 elevationOffset;

        public World(int width, int height, int seed)
        {
            this.width = width;
            this.height = height;
            random = new Random(seed);
            shapeRandom = new Random(seed + 1);
        }

        public void Generate(int pointCount, int relaxations)
        {
            var offset = new Vec2((float)(random.NextDouble()) * 100000, (float)(random.NextDouble()) * 100000);
            elevationOffset = new Vec2((float)(random.NextDouble()) * 100000, (float)(random.NextDouble()) * 100000);

            var points = new Vector[pointCount];
            for (int i = 0; i < points.Length; i++)
                points[i] = new Vector(shapeRandom.NextDouble() * width, shapeRandom.NextDouble() * height);

            GenerateNodes(points);
            for (int i = 0; i < relaxations; i++)
            {
                GenerateNodes(RelaxPoints());
            }

            GeneratePixelMap();

            Assign(offset);
        }

        /// <summary>
        /// Builds the island shape without towns, rivers, or difficulty bands.
        /// Those extra passes are for the old preview tool; live overworld only needs land plus biome blobs.
        /// </summary>
        public void GenerateIsland(int pointCount, int relaxations)
        {
            // Keep the Perlin sample in a small range. Offsets of ~100000 make adjacent
            // tiles round to the same float, which crenellates the beach into 1-tile teeth.
            var offset = new Vec2(
                8f + (float)(random.NextDouble()) * 40f,
                8f + (float)(random.NextDouble()) * 40f);

            var points = new Vector[pointCount];
            for (int i = 0; i < points.Length; i++)
                points[i] = new Vector(shapeRandom.NextDouble() * width, shapeRandom.NextDouble() * height);

            GenerateNodes(points);
            for (int i = 0; i < relaxations; i++)
            {
                GenerateNodes(RelaxPoints());
            }

            GeneratePixelMap();
            landOffset = offset;
            AssignLand(offset);
        }

        /// <summary>
        /// Perlin offset used by the island land test. Rasterization samples the same function per tile
        /// so the beach follows a smooth curve instead of Voronoi cell edges.
        /// </summary>
        public Vec2 landOffset;

        /// <summary>
        /// Tile the player (and Fireside) should spawn on. Set by TryAssignRealmBiomes.
        /// </summary>
        public Int2 spawnTile;

        private static readonly OverworldBiomeType[] interiorBiomes = new OverworldBiomeType[]
        {
            OverworldBiomeType.Meadows,
            OverworldBiomeType.Wilderness,
            OverworldBiomeType.Dunes,
            OverworldBiomeType.Shallows,
            OverworldBiomeType.Peaks,
            OverworldBiomeType.Tundra
        };

        /// <summary>
        /// Layout matching the old painted island: Meadows in the south-east (spawn),
        /// Wilderness south-west, Dunes in the middle, Shallows north of that,
        /// Peaks north-west and Tundra north-east. Values are island-bounding-box UVs (y=0 is south).
        /// </summary>
        private static readonly Vec2[] interiorSeedUvs = new Vec2[]
        {
            new Vec2(0.68f, 0.20f),
            new Vec2(0.28f, 0.28f),
            new Vec2(0.50f, 0.46f),
            new Vec2(0.50f, 0.60f),
            new Vec2(0.30f, 0.80f),
            new Vec2(0.72f, 0.80f)
        };

        private const int Min_Cells_Per_Biome = 25;
        private const int Spawn_Edge_Margin = 40;

        /// <summary>
        /// Paints each Voronoi cell as a compact realm blob and picks a south-Meadows beach spawn.
        /// Returns false when the island is too small or a biome would be missing — caller should retry with a new seed.
        /// </summary>
        public bool TryAssignRealmBiomes()
        {
            if (landCenters == null || landCenters.Count == 0) return false;

            var interior = new List<Center>();
            foreach (var center in landCenters)
            {
                if (center.water) continue;
                if (center.coast) continue;
                interior.Add(center);
            }

            if (interior.Count < interiorBiomes.Length * Min_Cells_Per_Biome) return false;

            float minX = float.MaxValue, minY = float.MaxValue, maxX = float.MinValue, maxY = float.MinValue;
            foreach (var center in landCenters)
            {
                if (center.position.x < minX) minX = center.position.x;
                if (center.position.y < minY) minY = center.position.y;
                if (center.position.x > maxX) maxX = center.position.x;
                if (center.position.y > maxY) maxY = center.position.y;
            }

            float sizeX = maxX - minX;
            float sizeY = maxY - minY;
            if (sizeX < 1 || sizeY < 1) return false;

            var seedCenters = new Center[interiorBiomes.Length];
            var usedIds = new HashSet<int>();
            for (int i = 0; i < interiorBiomes.Length; i++)
            {
                float jitterX = (float)(random.NextDouble() * 0.08 - 0.04);
                float jitterY = (float)(random.NextDouble() * 0.08 - 0.04);
                var uv = interiorSeedUvs[i];
                var target = new Vec2(minX + (uv.x + jitterX) * sizeX, minY + (uv.y + jitterY) * sizeY);
                var pick = interior.Closest(_ => _.position.SqrDistanceTo(target));
                if (pick == null || !usedIds.Add(pick.id)) return false;
                seedCenters[i] = pick;
            }

            foreach (var center in centers)
            {
                if (center.water)
                {
                    center.biomeType = OverworldBiomeType.Ocean;
                    continue;
                }

                var closest = seedCenters[0];
                float closestDistance = center.position.SqrDistanceTo(seedCenters[0].position);
                for (int i = 1; i < seedCenters.Length; i++)
                {
                    var distance = center.position.SqrDistanceTo(seedCenters[i].position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closest = seedCenters[i];
                    }
                }

                for (int i = 0; i < seedCenters.Length; i++)
                {
                    if (seedCenters[i] == closest)
                    {
                        center.biomeType = interiorBiomes[i];
                        break;
                    }
                }
            }

            // The south coast is the spawn landing. Force it to Meadows so Fireside is not in dunes/wilderness.
            var southLand = landCenters.OrderBy(_ => _.position.y).Take(24).ToArray();
            foreach (var cell in southLand)
                cell.biomeType = OverworldBiomeType.Meadows;

            var counts = new Dictionary<OverworldBiomeType, int>();
            foreach (var biome in interiorBiomes)
                counts[biome] = 0;
            foreach (var center in landCenters)
            {
                if (center.coast || center.water) continue;
                if (!counts.ContainsKey(center.biomeType)) continue;
                counts[center.biomeType] = counts[center.biomeType] + 1;
            }
            foreach (var biome in interiorBiomes)
            {
                if (counts[biome] < Min_Cells_Per_Biome) return false;
            }

            if (!TryPickSpawn(southLand)) return false;

            Log.Write($"Overworld biomes Meadows={counts[OverworldBiomeType.Meadows]} Wilderness={counts[OverworldBiomeType.Wilderness]} Dunes={counts[OverworldBiomeType.Dunes]} Shallows={counts[OverworldBiomeType.Shallows]} Peaks={counts[OverworldBiomeType.Peaks]} Tundra={counts[OverworldBiomeType.Tundra]}");
            return true;
        }

        private bool TryPickSpawn(Center[] southLand)
        {
            Center best = null;
            foreach (var cell in southLand)
            {
                if (cell.biomeType != OverworldBiomeType.Meadows) continue;
                int x = (int)cell.position.x;
                int y = (int)cell.position.y;
                if (x < Spawn_Edge_Margin || y < 8 || x >= width - Spawn_Edge_Margin || y >= height - 48)
                    continue;
                if (best == null || cell.position.y < best.position.y)
                    best = cell;
            }

            if (best == null)
            {
                foreach (var cell in landCenters)
                {
                    if (cell.biomeType != OverworldBiomeType.Meadows) continue;
                    int x = (int)cell.position.x;
                    int y = (int)cell.position.y;
                    if (x < Spawn_Edge_Margin || y < 8 || x >= width - Spawn_Edge_Margin || y >= height - 48)
                        continue;
                    if (best == null || cell.position.y < best.position.y)
                        best = cell;
                }
            }

            if (best == null) return false;

            spawnTile = new Int2((int)best.position.x, (int)best.position.y);
            return true;
        }

        /// <summary>
        /// Same land test as Voronoi cells, but at a tile. Used so the shoreline is a curve instead of cell polygons.
        /// </summary>
        public bool IsLandAt(int x, int y)
        {
            return IsLandAt(new Vec2(x + 0.5f, y + 0.5f), landOffset);
        }

        private bool IsLand(Center center, Vec2 offset)
        {
            return IsLandAt(center.position, offset);
        }

        private bool IsLandAt(Vec2 position, Vec2 offset)
        {
            // Doubles so neighboring tiles actually get different noise samples.
            // Float math with a large offset used to quantize the shoreline into 1-tile jags.
            double px = (position.x - width * 0.5) / width * 2.0;
            double py = (position.y - height * 0.5) / width * 2.0;
            double perlin = Perlin.Noise(offset.x + px * Perlin_Scale, offset.y + py * Perlin_Scale, 0) - 0.5;
            double n = -Math.Sqrt(px * 1.3 * px * 1.3 + py * py) + 0.65 + perlin * 0.9;
            return n > 0;
        }

        private void GenerateNodes(Vector[] points)
        {
            var voronoi = Fortune.ComputeVoronoiGraph(points);
            GenerateRelations(points, voronoi);
        }
        
        private Vector[] RelaxPoints()
        {
            var points = new Vector[centers.Count];
            int index = 0;
            foreach (var center in centers)
            {
                // Hull cells often have no finite corners. Averaging an empty set is NaN,
                // and Fortune then throws "same key (NaN;NaN)" on the next relaxation.
                if (center.corners.Count == 0)
                {
                    points[index++] = new Vector(center.position.x, center.position.y);
                    continue;
                }

                Vec2 p = new Vec2(0, 0);
                foreach (var corner in center.corners)
                    p += corner.position;
                p /= center.corners.Count;
                if (float.IsNaN(p.x) || float.IsNaN(p.y) || float.IsInfinity(p.x) || float.IsInfinity(p.y))
                    p = center.position;
                points[index++] = new Vector(p.x, p.y);
            }
            return points;
        }

        private static bool IsInvalidPoint(Vector point)
        {
            if (point == null) return true;
            if (point == Fortune.VVUnkown || point == Fortune.VVInfinite) return true;
            return double.IsNaN(point[0]) || double.IsNaN(point[1])
                || double.IsInfinity(point[0]) || double.IsInfinity(point[1]);
        }
        
        private void GenerateRelations(Vector[] points, VoronoiGraph voronoi)
        {
            centers.Clear();
            corners.Clear();
            edges.Clear();

            var centerMap = new Dictionary<Vector, Center>();
            var cornerMap = new Dictionary<Vector, Corner>();
            var edgeMap = new Dictionary<VoronoiEdge, Edge>();

            foreach (var point in points)
            {
                if (IsInvalidPoint(point)) continue;
                if (centerMap.ContainsKey(point)) continue;
                centerMap.Add(point, new Center(new Vec2((float)point[0], (float)point[1]), centerIds++));
            }
            foreach (var point in voronoi.Vertizes)
            {
                if (IsInvalidPoint(point)) continue;
                if (cornerMap.ContainsKey(point)) continue;
                cornerMap.Add(point, new Corner(new Vec2((float)point[0], (float)point[1]), cornerIds++));
            }

            foreach (var edge in voronoi.Edges)
            {
                if (edge.IsPartlyInfinite || edge.VVertexA == Fortune.VVUnkown || edge.VVertexB == Fortune.VVUnkown) continue;
                if (IsInvalidPoint(edge.VVertexA) || IsInvalidPoint(edge.VVertexB)) continue;
                if (!centerMap.ContainsKey(edge.LeftData) || !centerMap.ContainsKey(edge.RightData)) continue;
                if (!cornerMap.ContainsKey(edge.VVertexA) || !cornerMap.ContainsKey(edge.VVertexB)) continue;

                var newEdge = new Edge(edgeIds++);
                edgeMap.Add(edge, newEdge);

                newEdge.d0 = centerMap[edge.LeftData];
                newEdge.d0.borders.Add(newEdge);

                newEdge.d1 = centerMap[edge.RightData];
                newEdge.d1.borders.Add(newEdge);

                newEdge.d0.neighbors.Add(newEdge.d1);
                newEdge.d1.neighbors.Add(newEdge.d0);

                newEdge.v0 = cornerMap[edge.VVertexA];
                newEdge.v0.protrudes.Add(newEdge);
                newEdge.v0.touches.Add(newEdge.d0);
                newEdge.v0.touches.Add(newEdge.d1);

                newEdge.v1 = cornerMap[edge.VVertexB];
                newEdge.v1.protrudes.Add(newEdge);
                newEdge.v1.touches.Add(newEdge.d0);
                newEdge.v1.touches.Add(newEdge.d1);

                newEdge.v0.adjacent.Add(newEdge.v1);
                newEdge.v1.adjacent.Add(newEdge.v0);

                newEdge.d0.corners.Add(newEdge.v0);
                newEdge.d0.corners.Add(newEdge.v1);

                newEdge.d1.corners.Add(newEdge.v0);
                newEdge.d1.corners.Add(newEdge.v1);
            }

            centers.AddRange(centerMap.Values);
            edges.AddRange(edgeMap.Values);
            corners.AddRange(cornerMap.Values);
        }

        public Center GetCenterNear(int x, int y)
        {
            if (x < 0) x = 0;
            if (x >= width) x = width - 1;
            if (y < 0) y = 0;
            if (y >= height) y = height - 1;

            return pixelMap[y * width + x];
        }

        private void Assign(Vec2 offset)
        {
            AssignLand(offset);
            AssignDistanceFromBeach();
            AssignElevation();
            AssignRivers(random.Next(5, 7));
            AssignDistanceFromLowest();
            AssignDifficulty();

            AssignCenterAverages();

            AssignTowns();
        }

        private void AssignLand(Vec2 offset)
        {
            foreach (var center in centers)
            {
                center.water = !IsLand(center, offset);
            }

            foreach (var center in centers)
            {
                center.coast = !center.water && center.neighbors.Any(_ => _.water);
            }

            RemoveIslands();

            foreach (var corner in corners)
            {
                corner.water = corner.touches.All(_ => _.water);
                corner.coast = !corner.water && corner.touches.Any(_ => _.water);
            }

            landCenters = centers.Where(_ => !_.water).ToList();
            landCorners = corners.Where(_ => !_.water).ToList();
        }

        private void RemoveIslands()
        {
            // Pixel lookup can miss when a cell has no finite corners, which used to
            // enqueue null and NRE on neighbors. Start from the nearest actual land cell.
            Center first = null;
            float best = float.MaxValue;
            var mapCenter = new Vec2(width / 2f, height / 2f);
            foreach (var center in centers)
            {
                if (center == null || center.water) continue;
                if (float.IsNaN(center.position.x) || float.IsNaN(center.position.y)) continue;
                float d = center.position.SqrDistanceTo(mapCenter);
                if (d < best)
                {
                    best = d;
                    first = center;
                }
            }
            if (first == null) return;

            HashSet<Center> mainland = new HashSet<Center>();
            mainland.Add(first);

            Queue<Center> toAssign = new Queue<Center>();
            toAssign.Enqueue(first);

            while (toAssign.Count > 0)
            {
                var center = toAssign.Dequeue();
                if (center == null) continue;
                foreach (var neighbor in center.neighbors)
                {
                    if (neighbor == null || neighbor.water) continue;
                    if (mainland.Add(neighbor))
                        toAssign.Enqueue(neighbor);
                }
            }

            foreach (var center in centers)
            {
                if (mainland.Contains(center)) continue;

                center.water = true;
                center.coast = false;
            }
        }

        private void AssignDistanceFromBeach()
        {
            Queue<Corner> toAssign = new Queue<Corner>();

            var firstCorner = corners.First(_ => _.water);
            firstCorner.distanceFromBeach = 0;
            toAssign.Enqueue(firstCorner);

            while (toAssign.Count > 0)
            {
                var corner = toAssign.Dequeue();
                var distance = corner.distanceFromBeach + 1;
                foreach (var neighbor in corner.adjacent)
                {
                    if (neighbor.water)
                    {
                        if (neighbor.distanceFromBeach == 0) continue;
                        neighbor.distanceFromBeach = 0;
                        toAssign.Enqueue(neighbor);
                        continue;
                    }

                    if (neighbor.distanceFromBeach <= 0)
                    {
                        neighbor.distanceFromBeach = distance;
                        toAssign.Enqueue(neighbor);
                        continue;
                    }

                    if (neighbor.distanceFromBeach > distance)
                    {
                        neighbor.distanceFromBeach = distance;
                        toAssign.Enqueue(neighbor);
                        continue;
                    }
                }
            }

            var max = corners.Max(_ => _.distanceFromBeach);
            foreach (var corner in corners)
                corner.distanceFromBeach /= max;
        }

        private void AssignElevation()
        {
            foreach (var corner in landCorners)
            {
                var p = (corner.position / width) * 9;
                var perlin = (float)Perlin.Noise(elevationOffset.x + p.x, elevationOffset.y + p.y, 0);
                corner.elevation = corner.distanceFromBeach * 1.2f + perlin;
            }

            var max = landCorners.Max(_ => _.elevation);
            foreach (var corner in landCorners)
                corner.elevation /= max;

            foreach (var corner in landCorners)
                corner.elevation *= corner.elevation;

            foreach (var corner in landCorners)
            {
                corner.downSlope = corner.adjacent.Closest(_ => _.elevation);
                if (corner.downSlope.elevation > corner.elevation)
                {
                    corner.downSlope = corner.adjacent.Closest(_ => _.distanceFromBeach);
                }
            }
        }

        private void AssignRivers(int count)
        {
            var starts = landCorners.Where(_ => _.elevation > 0.5f).ToArray();
            for (int i = 0; i < count; i++)
            {
                Corner corner = null;

                int bc = 0;
                do corner = starts[random.Next(starts.Length)];
                while ((rivers.Contains(corner) || corner.downSlope == null) && bc++ < 100);

                if (bc >= 100) return;

                rivers.Add(corner);

                while (corner.downSlope != null && !corner.coast)
                {
                    if (corner.river != null) break;
                    corner.river = corner.downSlope;
                    corner = corner.river;
                }
            }
        }

        private void AssignDistanceFromLowest()
        {
            Queue<Corner> toAssign = new Queue<Corner>();
            var lowest = landCorners.Min((a, b) => a.position.y < b.position.y ? a : b);
            lowest.distanceFromLowest = 0;
            toAssign.Enqueue(lowest);

            while (toAssign.Count > 0)
            {
                var corner = toAssign.Dequeue();
                var distance = corner.distanceFromLowest + 1;
                foreach (var neighbor in corner.adjacent)
                {
                    if (neighbor.water) continue;

                    if (neighbor.distanceFromLowest <= 0)
                    {
                        neighbor.distanceFromLowest = distance;
                        toAssign.Enqueue(neighbor);
                        continue;
                    }

                    if (neighbor.distanceFromLowest > distance)
                    {
                        neighbor.distanceFromLowest = distance;
                        toAssign.Enqueue(neighbor);
                        continue;
                    }
                }
            }

            var max = landCorners.Max(_ => _.distanceFromLowest);
            foreach (var corner in landCorners)
                corner.distanceFromLowest /= max;
        }

        private void AssignDifficulty()
        {
            float max = landCorners.Max(_ =>
            {
                _.difficulty = _.distanceFromBeach + _.distanceFromLowest * 2.5f;
                return _.difficulty;
            });
            foreach (var corner in landCorners)
                corner.difficulty /= max;
        }

        private void AssignCenterAverages()
        {
            foreach (var center in centers)
            {
                center.distanceFromBeach = center.corners.Average(_ => _.distanceFromBeach);
                center.distanceFromLowest = center.corners.Average(_ => _.distanceFromLowest);
                center.difficulty = center.corners.Average(_ => _.difficulty);
                center.elevation = center.corners.Average(_ => _.elevation);
            }
        }

        private void AssignTowns()
        {
            AssignTownAt(0);
            AssignTownAt(0.3f, _ => _.coast);
            AssignTownAt(0.6f);
            AssignTownAt(0.9f);

            AssignTownRegions();
            AssignRoads();
        }

        private void AssignTownAt(float difficulty, Func<Center, bool> constraint = null)
        {
            IEnumerable<Center> collection = landCenters;
            if (constraint != null)
                collection = landCenters.Where(constraint);
            var center = collection.Closest(_ => Math.Abs(_.difficulty - difficulty));
            center.town = true;
            towns.Add(center);
        }

        private void AssignTownRegions()
        {
            foreach (var center in landCenters)
            {
                center.townRegion = towns.Closest(_ => _.position.DistanceTo(center.position));
            }
        }

        private void AssignRoads()
        {
            var towns = this.towns.OrderBy(_ => _.difficulty).ToArray();
            for (int i = 0; i < towns.Length - 1; i++)
            {
                AssignRoad(towns[i], towns[i + 1]);
            }
        }

        private void AssignRoad(Center a, Center b)
        {
            var closestACorner = a.corners.Closest(_ => _.coast ? float.MaxValue : (_.position - b.position).SqrLength);
            var closestBCorner = b.corners.Closest(_ => _.coast ? float.MaxValue : (_.position - a.position).SqrLength);
            var path = AStar.Pathfind(closestACorner, closestBCorner);
            if (path == null)
            {
                Log.Write("Failed to pathfind road");
                return;
            }

            List<Vec2> road = new List<Vec2>();
            road.Add(a.position);
            road.AddRange(path.Select(_ => _.position));
            road.Add(b.position);

            roads.Add(road);
        }

        private void GeneratePixelMap()
        {
            pixelMap = new Center[width * height];

            // Bucket sites by position and search nearby cells. Cheaper than a partition
            // query per tile (those copied HashSets on every pixel).
            const int cell = 32;
            int gw = (width + cell - 1) / cell;
            int gh = (height + cell - 1) / cell;
            var buckets = new List<Center>[gw * gh];
            for (int i = 0; i < buckets.Length; i++)
                buckets[i] = new List<Center>(8);

            foreach (var center in centers)
            {
                if (center == null) continue;
                if (float.IsNaN(center.position.x) || float.IsNaN(center.position.y)) continue;
                int gx = (int)center.position.x / cell;
                int gy = (int)center.position.y / cell;
                if (gx < 0) gx = 0;
                else if (gx >= gw) gx = gw - 1;
                if (gy < 0) gy = 0;
                else if (gy >= gh) gy = gh - 1;
                buckets[gy * gw + gx].Add(center);
            }

            System.Threading.Tasks.Parallel.For(0, height, y =>
            {
                int gy = y / cell;
                for (int x = 0; x < width; x++)
                {
                    int gx = x / cell;
                    Center closest = FindClosestCenter(buckets, gw, gh, gx, gy, x, y, 1);
                    if (closest == null)
                        closest = FindClosestCenter(buckets, gw, gh, gx, gy, x, y, 3);
                    pixelMap[y * width + x] = closest;
                }
            });
        }

        private static Center FindClosestCenter(List<Center>[] buckets, int gw, int gh, int gx, int gy, int x, int y, int radius)
        {
            Center closest = null;
            float best = float.MaxValue;
            for (int oy = -radius; oy <= radius; oy++)
            {
                int cy = gy + oy;
                if (cy < 0 || cy >= gh) continue;
                for (int ox = -radius; ox <= radius; ox++)
                {
                    int cx = gx + ox;
                    if (cx < 0 || cx >= gw) continue;
                    var bucket = buckets[cy * gw + cx];
                    for (int i = 0; i < bucket.Count; i++)
                    {
                        var c = bucket[i];
                        float dx = c.position.x - x;
                        float dy = c.position.y - y;
                        float d = dx * dx + dy * dy;
                        if (d < best)
                        {
                            best = d;
                            closest = c;
                        }
                    }
                }
            }
            return closest;
        }

        #region Rasterization

        public MapTile[,] Rasterize(WorldDefinition definition)
        {
            var offset = new Vec2(Rand.FloatValue(), Rand.FloatValue());

            var tiles = new MapTile[width, height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var center = GetCenterNear(x, y);
                    var biome = (center.water ? definition.ocean : (center.coast ? definition.beach : definition.biomes[center.difficulty])) ?? definition.beach;

                    float biomePerlin = (float)Perlin.Noise((offset.x + x / (float)width) * biome.perlinScale, (offset.y + y / (float)height) * biome.perlinScale, 0);
                    var tile = new MapTile((ushort)x, (ushort)y, biome.GetTile(biomePerlin), biome.GetObject(biomePerlin));
                    tiles[x, y] = tile;
                }
            }
            return tiles;
        }

        #endregion
    }
}
