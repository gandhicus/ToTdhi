using System;
using System.Collections.Generic;
using System.Text;
using Utils.NET.Geometry;
using Utils.NET.Pathfinding;
using WorldGen.Rasterization;

namespace WorldGen.Nodes
{
    public class Center : IPathNode<Center>
    {
        public int id;

        public Vec2 position;

        public HashSet<Center> neighbors = new HashSet<Center>();

        public HashSet<Edge> borders = new HashSet<Edge>();

        public HashSet<Corner> corners = new HashSet<Corner>();

        public bool water = false;

        public bool coast = false;

        public float distanceFromBeach = -1;

        public float distanceFromLowest = -1;

        public float difficulty = -1;

        public float elevation = 0;

        public bool town = false;

        public Center townRegion;

        /// <summary>
        /// Named overworld realm this cell belongs to. Ocean/Beach are the shoreline ring;
        /// interior cells are compact biome blobs so waypoints can sit in each biome's middle.
        /// </summary>
        public OverworldBiomeType biomeType = OverworldBiomeType.Ocean;

        public Center(Vec2 position, int id)
        {
            this.position = position;
            this.id = id;
        }

        public Vec2 Position => position;

        public IEnumerable<Center> Adjacent => neighbors;
    }
}
