using System.Collections.Generic;
using Utils.NET.Collections;
using Utils.NET.Utils;

namespace WorldGen.Rasterization
{
    /// <summary>
    /// Named overworld realms. Interior biomes are assigned as blobs (not difficulty rings)
    /// so each one has a real middle for waypoint placement.
    /// </summary>
    public enum OverworldBiomeType
    {
        Ocean,
        Beach,
        Meadows,
        Wilderness,
        Dunes,
        Shallows,
        Peaks,
        Tundra
    }

    /// <summary>
    /// One scenery object that only stamps when the tile matches, matching WorldCreator's export rules.
    /// </summary>
    public struct OverworldBiomeObject
    {
        public ushort tileType;
        public ushort[] objectTypes;

        public OverworldBiomeObject(ushort tileType, params ushort[] objectTypes)
        {
            this.tileType = tileType;
            this.objectTypes = objectTypes;
        }

        public ushort Get(ushort tileType)
        {
            if (objectTypes == null || objectTypes.Length == 0) return 0;
            if (this.tileType != 0 && this.tileType != tileType) return 0;
            return objectTypes.Random();
        }
    }

    /// <summary>
    /// Tile and object tables for one realm. Copied from the Unity WorldCreator biomes that
    /// produced the old static overworld, so spawn tables, music, and waypoint tile lists still match.
    /// </summary>
    public class OverworldBiome
    {
        public float perlinScale;
        public RangeMap<ushort> tiles = new RangeMap<ushort>();
        public RangeMap<OverworldBiomeObject> objects = new RangeMap<OverworldBiomeObject>();
    }

    /// <summary>
    /// Production biome catalog used when rasterizing a generated island.
    /// </summary>
    public static class OverworldBiomes
    {
        public static OverworldBiome Get(OverworldBiomeType type)
        {
            return biomes[type];
        }

        // Built before the biome table so lily stamps are never a null type list.
        private static readonly ushort[] ShallowLilyTypes = BuildShallowLilyTypes();
        private static readonly Dictionary<OverworldBiomeType, OverworldBiome> biomes = Create();

        private static Dictionary<OverworldBiomeType, OverworldBiome> Create()
        {
            var map = new Dictionary<OverworldBiomeType, OverworldBiome>();

            map[OverworldBiomeType.Ocean] = new OverworldBiome
            {
                perlinScale = 1,
                tiles = new RangeMap<ushort>(new[]
                {
                    new RangePair<ushort>(new Range(float.MinValue, float.MaxValue), 0xb03)
                }),
                objects = new RangeMap<OverworldBiomeObject>(new[]
                {
                    new RangePair<OverworldBiomeObject>(new Range(0.985f, float.MaxValue), new OverworldBiomeObject(0, 0xa08))
                })
            };

            map[OverworldBiomeType.Beach] = new OverworldBiome
            {
                perlinScale = 1,
                tiles = new RangeMap<ushort>(new[]
                {
                    new RangePair<ushort>(new Range(float.MinValue, float.MaxValue), 0xb04)
                }),
                objects = new RangeMap<OverworldBiomeObject>(new[]
                {
                    new RangePair<OverworldBiomeObject>(new Range(0.979f, 0.988f), new OverworldBiomeObject(0, 0xa08)),
                    new RangePair<OverworldBiomeObject>(new Range(0.988f, 0.997f), new OverworldBiomeObject(0, 0xa01)),
                    new RangePair<OverworldBiomeObject>(new Range(0.997f, float.MaxValue), new OverworldBiomeObject(0, 0xa49))
                })
            };

            map[OverworldBiomeType.Meadows] = new OverworldBiome
            {
                perlinScale = 80,
                tiles = new RangeMap<ushort>(new[]
                {
                    new RangePair<ushort>(new Range(float.MinValue, float.MaxValue), 0xb02)
                }),
                objects = new RangeMap<OverworldBiomeObject>(new[]
                {
                    new RangePair<OverworldBiomeObject>(new Range(0.9f, 0.92f), new OverworldBiomeObject(0xb02, 0xa0b)),
                    new RangePair<OverworldBiomeObject>(new Range(0.92f, 0.96f), new OverworldBiomeObject(0xb02, 0xa0a)),
                    new RangePair<OverworldBiomeObject>(new Range(0.96f, float.MaxValue), new OverworldBiomeObject(0xb02, 0xa07))
                })
            };

            map[OverworldBiomeType.Wilderness] = new OverworldBiome
            {
                perlinScale = 300,
                tiles = new RangeMap<ushort>(new[]
                {
                    new RangePair<ushort>(new Range(float.MinValue, 0.7f), 0xb05),
                    new RangePair<ushort>(new Range(0.7f, float.MaxValue), 0xb06)
                }),
                objects = new RangeMap<OverworldBiomeObject>(new[]
                {
                    new RangePair<OverworldBiomeObject>(new Range(0.78f, 0.82f), new OverworldBiomeObject(0xb05, 0xa09)),
                    new RangePair<OverworldBiomeObject>(new Range(0.82f, 0.9f), new OverworldBiomeObject(0xb05, 0xa02)),
                    new RangePair<OverworldBiomeObject>(new Range(0.9f, 0.92f), new OverworldBiomeObject(0xb05, 0xa03)),
                    new RangePair<OverworldBiomeObject>(new Range(0.95f, float.MaxValue), new OverworldBiomeObject(0, 0xa04))
                })
            };

            map[OverworldBiomeType.Dunes] = new OverworldBiome
            {
                perlinScale = 100,
                tiles = new RangeMap<ushort>(new[]
                {
                    new RangePair<ushort>(new Range(float.MinValue, 0.5f), 0xb07),
                    new RangePair<ushort>(new Range(0.5f, 0.88f), 0xb29),
                    new RangePair<ushort>(new Range(0.88f, float.MaxValue), 0xb08)
                }),
                objects = new RangeMap<OverworldBiomeObject>(new[]
                {
                    new RangePair<OverworldBiomeObject>(new Range(float.MinValue, 0.01f), new OverworldBiomeObject(0xb07, 0xa05)),
                    new RangePair<OverworldBiomeObject>(new Range(0.01f, 0.015f), new OverworldBiomeObject(0xb07, 0xa06)),
                    new RangePair<OverworldBiomeObject>(new Range(0.985f, 0.99f), new OverworldBiomeObject(0xb29, 0xa06)),
                    new RangePair<OverworldBiomeObject>(new Range(0.99f, float.MaxValue), new OverworldBiomeObject(0xb29, 0xa05))
                })
            };

            map[OverworldBiomeType.Shallows] = new OverworldBiome
            {
                perlinScale = 100,
                tiles = new RangeMap<ushort>(new[]
                {
                    new RangePair<ushort>(new Range(float.MinValue, 0.32f), 0xb24),
                    new RangePair<ushort>(new Range(0.32f, 0.71f), 0xb25),
                    new RangePair<ushort>(new Range(0.71f, 0.75f), 0xb26),
                    new RangePair<ushort>(new Range(0.75f, 0.78f), 0xb27),
                    new RangePair<ushort>(new Range(0.78f, float.MaxValue), 0xb28)
                }),
                objects = new RangeMap<OverworldBiomeObject>(new[]
                {
                    new RangePair<OverworldBiomeObject>(new Range(float.MinValue, 0.01f), new OverworldBiomeObject(0xb24, 0xa43)),
                    new RangePair<OverworldBiomeObject>(new Range(0.01f, 0.02f), new OverworldBiomeObject(0xb24, 0xa42)),
                    new RangePair<OverworldBiomeObject>(new Range(0.02f, 0.025f), new OverworldBiomeObject(0xb24, ShallowLilyTypes)),
                    new RangePair<OverworldBiomeObject>(new Range(0.04f, 0.05f), new OverworldBiomeObject(0xb25, 0xa43)),
                    new RangePair<OverworldBiomeObject>(new Range(0.05f, 0.06f), new OverworldBiomeObject(0xb25, 0xa42)),
                    new RangePair<OverworldBiomeObject>(new Range(0.06f, 0.065f), new OverworldBiomeObject(0xb25, ShallowLilyTypes)),
                    new RangePair<OverworldBiomeObject>(new Range(0.96f, float.MaxValue), new OverworldBiomeObject(0xb28, 0xa44))
                })
            };

            map[OverworldBiomeType.Peaks] = new OverworldBiome
            {
                perlinScale = 100,
                tiles = new RangeMap<ushort>(new[]
                {
                    new RangePair<ushort>(new Range(float.MinValue, 0.88f), 0xb0d),
                    new RangePair<ushort>(new Range(0.88f, float.MaxValue), 0xb0e)
                }),
                objects = new RangeMap<OverworldBiomeObject>(new[]
                {
                    new RangePair<OverworldBiomeObject>(new Range(0.985f, float.MaxValue), new OverworldBiomeObject(0xb0d, 0xa25))
                })
            };

            map[OverworldBiomeType.Tundra] = new OverworldBiome
            {
                perlinScale = 100,
                tiles = new RangeMap<ushort>(new[]
                {
                    new RangePair<ushort>(new Range(float.MinValue, 0.32f), 0xb21),
                    new RangePair<ushort>(new Range(0.32f, 0.75f), 0xb1e),
                    new RangePair<ushort>(new Range(0.75f, 0.80f), 0xb1f),
                    new RangePair<ushort>(new Range(0.80f, float.MaxValue), 0xb20)
                }),
                objects = new RangeMap<OverworldBiomeObject>(new[]
                {
                    new RangePair<OverworldBiomeObject>(new Range(0.59f, 0.6f), new OverworldBiomeObject(0xb1e, 0xa3e, 0xa3f, 0xa40, 0xa41)),
                    new RangePair<OverworldBiomeObject>(new Range(0.6f, float.MaxValue), new OverworldBiomeObject(0xb20, 0xa3a, 0xa3b, 0xa3c, 0xa3d))
                })
            };

            return map;
        }

        // 49 white, 49 pink, 2 blue — 2% blue without a tiny dedicated spawn band.
        private static ushort[] BuildShallowLilyTypes()
        {
            var types = new ushort[100];
            for (int i = 0; i < 49; i++)
            {
                types[i * 2] = 0xa4a;
                types[i * 2 + 1] = 0xa4b;
            }
            types[98] = 0xa4c;
            types[99] = 0xa4c;
            return types;
        }
    }
}
