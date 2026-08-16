using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Utils.NET.Collections;
using Utils.NET.Geometry;
using Utils.NET.Pathfinding;
using Utils.NET.Utils;

namespace TitanCore.Gen
{
    [Flags]
    public enum MapElementType
    {
        Empty = 0,
        Ground = 1,
        Wall = 1 << 1,
        SetPiece = 1 << 2,
        Boss = 1 << 3,
        Spawn = 1 << 4,
        BossPath = 1 << 5,
        Ocean = 1 << 6,
        Tag1 = 1 << 7,
        Tag2 = 1 << 8,
        Tag3 = 1 << 9,
        Tag4 = 1 << 10,
        Tag5 = 1 << 11,
        Tag6 = 1 << 12,
        Tag7 = 1 << 13,
        Tag8 = 1 << 14,
    }

    public class Map
    {
        public readonly int width;

        public readonly int height;

        protected MapElementType[,] data;

        private Int2[] sizeChecker;

        public Map(int width, int height)
        {
            this.width = width;
            this.height = height;
            data = new MapElementType[width, height];
            CreateSizeChecker(Math.Min(48, Math.Max(width, height) / 2));
        }

        public Map(int width, int height, Map source)
        {
            this.width = width;
            this.height = height;
            data = new MapElementType[width, height];
            CreateSizeChecker(Math.Min(48, Math.Max(width, height) / 2));
            LoadSource(source, new Int2((width - source.width) / 2, (height - source.height) / 2));
        }

        public Map(int width, int height, Map source, Int2 sourcePosition)
        {
            this.width = width;
            this.height = height;
            data = new MapElementType[width, height];
            CreateSizeChecker(Math.Min(48, Math.Max(width, height) / 2));
            LoadSource(source, sourcePosition);
        }

        private void LoadSource(Map source, Int2 sourcePosition)
        {
            foreach (var point in source.EachPoint())
            {
                Set(sourcePosition + point, source.Get(point));
            }
        }

        public MapElementType Get(Int2 point)
        {
            if (!InBounds(point)) return MapElementType.Empty;
            return data[point.x, point.y];
        }

        public void Set(Int2 point, MapElementType type)
        {
            if (!InBounds(point)) return;
            data[point.x, point.y] = type;
        }

        public bool InBounds(Int2 point)
        {
            return point.x >= 0 && point.y >= 0 && point.x < width && point.y < height;
        }

        public Map Clamp()
        {
            var min = new Int2(int.MaxValue, int.MaxValue);
            var max = new Int2(int.MinValue, int.MinValue);

            foreach (var point in EachPoint())
            {
                var type = data[point.x, point.y];
                if (type == MapElementType.Empty) continue;
                min.x = Math.Min(point.x, min.x);
                min.y = Math.Min(point.y, min.y);
                max.x = Math.Max(point.x, max.x);
                max.y = Math.Max(point.y, max.y);
            }

            var rect = new IntRect(min, max - min);
            var map = new Map(rect.width, rect.height);
            foreach (var point in map.EachPoint())
                map.Set(point, Get(min + point));

            return map;
        }

        public Map And(Map other)
        {
            var newMap = new Map(width, height);
            foreach (var point in EachPoint())
                newMap.Set(point, Get(point) & other.Get(point));
            return newMap;
        }

        public Map Or(Map other)
        {
            var newMap = new Map(width, height);
            foreach (var point in EachPoint())
                newMap.Set(point, Get(point) | other.Get(point));
            return newMap;
        }

        public IEnumerable<Int2> EachPoint()
        {
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                    yield return new Int2(x, y);
        }
        public IEnumerable<Queue<Int2>> GetMasses(MapElementType types)
        {
            var checkedPoints = new bool[width, height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    if (checkedPoints[x, y]) continue;

                    var pointType = data[x, y];
                    if ((pointType != MapElementType.Empty && (types & pointType) > 0) || types == pointType)
                    {
                        var foundQueue = new Queue<Int2>();
                        var checkQueue = new Queue<Int2>();
                        checkQueue.Enqueue(new Int2(x, y));

                        while (checkQueue.Count > 0)
                        {
                            var point = checkQueue.Dequeue();
                            if (!InBounds(point)) continue;
                            if (checkedPoints[point.x, point.y]) continue;
                            checkedPoints[point.x, point.y] = true;

                            pointType = data[point.x, point.y];
                            if ((pointType != MapElementType.Empty && (types & pointType) > 0) || types == pointType)
                            {
                                foundQueue.Enqueue(point);
                                checkQueue.Enqueue(point + new Int2(1, 0));
                                checkQueue.Enqueue(point + new Int2(-1, 0));
                                checkQueue.Enqueue(point + new Int2(0, 1));
                                checkQueue.Enqueue(point + new Int2(0, -1));
                            }
                        }

                        yield return foundQueue;
                    }
                }
        }

        public IEnumerable<Queue<Int2>> GetLandmasses(int maxCount)
        {
            int count = 0;
            foreach (var mass in GetMasses(MapElementType.Ground | MapElementType.Wall).OrderByDescending(_ => _.Count))
            {
                if (count >= maxCount)
                {
                    foreach (var point in mass)
                        data[point.x, point.y] = MapElementType.Empty;
                }
                else
                    yield return mass;
                count++;
            }
        }

        public void Extrude(MapElementType type, IEnumerable<Queue<Int2>> masses)
        {
            foreach (var mass in masses)
            {
                foreach (var point in mass)
                {
                    for (int y = -1; y <= 1; y++)
                        for (int x = -1; x <= 1; x++)
                            if (x != 0 || y != 0)
                            {
                                var p = point + new Int2(x, y);
                                if (!InBounds(p)) continue;
                                var t = data[p.x, p.y];
                                if (t == MapElementType.Empty)
                                    data[p.x, p.y] = type;
                            }
                }
            }
        }

        private void CreateSizeChecker(int radius)
        {
            var points = new List<Int2>();
            for (int y = -radius; y <= radius; y++)
                for (int x = -radius; x <= radius; x++)
                {
                    points.Add(new Int2(x, y));
                }
            sizeChecker = points.OrderBy(_ => _.SqrLength).ToArray();
        }

        public List<Int2[]> GetBiggestAreas(int count, int maxLandmass)
        {
            return GetBiggestAreas(count, maxLandmass, GetLandmasses(maxLandmass));
        }

        public List<Int2[]> GetBiggestAreas(int count, int maxLandmass, IEnumerable<Queue<Int2>> masses)
        {
            var clearance = GetGroundClearanceField();
            var ranked = new List<Int2>();

            foreach (var mass in masses)
            {
                var best = Int2.zero;
                var bestClearance = -1;
                var any = false;
                foreach (var point in mass)
                {
                    var value = clearance[point.x, point.y];
                    if (value == int.MaxValue)
                        value = 0;
                    if (!any || value > bestClearance)
                    {
                        bestClearance = value;
                        best = point;
                        any = true;
                    }
                }
                if (any)
                    ranked.Add(best);
            }

            ranked.Sort((a, b) => clearance[b.x, b.y].CompareTo(clearance[a.x, a.y]));
            if (ranked.Count > count)
                ranked.RemoveRange(count, ranked.Count - count);

            return ranked.Select(point => GetPointArea(point).ToArray()).ToList();
        }

        private int[,] GetGroundClearanceField()
        {
            var field = new int[width, height];
            var queue = new Queue<Int2>();
            var dirs = new Int2[]
            {
                new Int2(1, 0), new Int2(-1, 0), new Int2(0, 1), new Int2(0, -1)
            };

            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    if ((data[x, y] & MapElementType.Ground) == MapElementType.Ground)
                    {
                        field[x, y] = int.MaxValue;
                    }
                    else
                    {
                        field[x, y] = 0;
                        queue.Enqueue(new Int2(x, y));
                    }
                }

            while (queue.Count > 0)
            {
                var p = queue.Dequeue();
                var distance = field[p.x, p.y];
                foreach (var dir in dirs)
                {
                    var n = p + dir;
                    if (!InBounds(n))
                        continue;
                    var next = distance + 1;
                    if (next < field[n.x, n.y])
                    {
                        field[n.x, n.y] = next;
                        queue.Enqueue(n);
                    }
                }
            }

            return field;
        }

        public IEnumerable<Int2> GetPointArea(Int2 point)
        {
            foreach (var check in sizeChecker)
            {
                var p = point + check;
                if ((Get(p) & MapElementType.Ground) == MapElementType.Ground)
                    yield return p;
                else
                    yield break;
            }
        }

        public IEnumerable<Int2> Spread(Int2 point)
        {
            foreach (var check in sizeChecker)
            {
                var p = point + check;
                yield return p;
            }
        }

        public int[,] GetDistanceField(Int2 start, out int maxDistance)
        {
            var field = new int[width, height];
            foreach (var p in EachPoint())
                field[p.x, p.y] = -1;

            maxDistance = 0;
            if (!InBounds(start) || (Get(start) & MapElementType.Ground) == 0)
                return field;

            var queue = new Queue<Int2>();
            field[start.x, start.y] = 0;
            queue.Enqueue(start);

            var dirs = new Int2[]
            {
                new Int2(1, 0), new Int2(-1, 0), new Int2(0, 1), new Int2(0, -1)
            };

            while (queue.Count > 0)
            {
                var point = queue.Dequeue();
                var distance = field[point.x, point.y];
                if (distance > maxDistance)
                    maxDistance = distance;

                foreach (var dir in dirs)
                {
                    var next = point + dir;
                    if (!InBounds(next) || field[next.x, next.y] != -1)
                        continue;
                    if ((Get(next) & MapElementType.Ground) == 0)
                        continue;

                    field[next.x, next.y] = distance + 1;
                    queue.Enqueue(next);
                }
            }

            return field;
        }

        public Map Scale(float scale)
        {
            var newMap = new Map((int)(width * scale), (int)(height * scale));
            foreach (var point in newMap.EachPoint())
            {
                newMap.Set(point, Get((point.ToVec2() / scale).ToInt2()));
            }
            return newMap;
        }

        public Map Scale(Vec2 scale)
        {
            var newMap = new Map((int)(width * scale.x), (int)(height * scale.y));
            foreach (var point in newMap.EachPoint())
            {
                newMap.Set(point, Get((point.ToVec2() / scale).ToInt2()));
            }
            return newMap;
        }

        public Int2 RandomDistancePoint(int[,] distanceField, Range distance)
        {
            var eligableSpots = new List<Int2>();
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    var d = distanceField[x, y];
                    if (d >= distance.min && d <= distance.max)
                    {
                        eligableSpots.Add(new Int2(x, y));
                    }
                }
            return eligableSpots[Rand.Next(eligableSpots.Count)];
        }

        public int[,] GenerateTypeDistanceField(int maxDistance, MapElementType type)
        {
            var field = new int[width, height];
            var queue = new Queue<Int2>();
            var offsets = new Int2[]
            {
                new Int2(-1, -1), new Int2(0, -1), new Int2(1, -1),
                new Int2(-1, 0),                   new Int2(1, 0),
                new Int2(-1, 1),  new Int2(0, 1),  new Int2(1, 1),
            };

            foreach (var p in EachPoint())
            {
                if ((Get(p) & type) == type)
                {
                    field[p.x, p.y] = 0;
                    queue.Enqueue(p);
                }
                else
                {
                    field[p.x, p.y] = int.MaxValue;
                }
            }

            while (queue.Count > 0)
            {
                var p = queue.Dequeue();
                var distance = field[p.x, p.y];
                if (distance >= maxDistance)
                    continue;

                foreach (var offset in offsets)
                {
                    var n = p + offset;
                    if (!InBounds(n))
                        continue;

                    var next = distance + 1;
                    if (next < field[n.x, n.y])
                    {
                        field[n.x, n.y] = next;
                        queue.Enqueue(n);
                    }
                }
            }

            return field;
        }

        private struct MapPathNode : IPathNode<MapPathNode>
        {
            public Vec2 Position => intPosition;

            private static Int2[] offsets = new Int2[]
            {
                new Int2(0, -1),
                new Int2(-1, 0),
                new Int2(1, 0),
                new Int2(0, 1),
            };

            public IEnumerable<MapPathNode> Adjacent
            {
                get
                {
                    foreach (var offset in offsets)
                    {
                        var p = intPosition + offset;
                        if ((map.Get(p) & MapElementType.Ground) != MapElementType.Ground) continue;
                        yield return new MapPathNode(map, p);
                    }
                }
            }

            private Map map;

            public Int2 intPosition;

            public MapPathNode(Map map, Int2 intPosition)
            {
                this.map = map;
                this.intPosition = intPosition;
            }

            public override bool Equals(object obj)
            {
                if (obj is MapPathNode pathNode)
                    return pathNode.intPosition == intPosition;
                return base.Equals(obj);
            }
        }

        public IEnumerable<Int2> Pathfind(Int2 start, Int2 end)
        {
            var path = AStar.Pathfind(new MapPathNode(this, start), new MapPathNode(this, end));
            if (path == null)
                return null;
            return path.Select(_ => _.intPosition);
        }

        public void Smooth(Int2[] smoothingDirections)
        {
            var newData = new MapElementType[width, height];
            for (int y = 0; y < height; y++)
                for (int x = 0; x < width; x++)
                {
                    int numEmptyNeighbors = 0;
                    foreach (var direction in smoothingDirections)
                    {
                        var point = new Int2(x + direction.x, y + direction.y);
                        MapElementType neighborType;
                        if (!InBounds(point))
                            neighborType = MapElementType.Empty;
                        else
                            neighborType = data[point.x, point.y];

                        if (neighborType == MapElementType.Empty)
                            numEmptyNeighbors++;
                    }

                    if (numEmptyNeighbors > (int)Math.Ceiling(smoothingDirections.Length / 2.0f))
                        newData[x, y] = MapElementType.Empty;
                    else if (numEmptyNeighbors < (int)Math.Floor(smoothingDirections.Length / 2.0f))
                        newData[x, y] = MapElementType.Ground;
                    else
                        newData[x, y] = data[x, y];
                }
            data = newData;
        }
    }
}
