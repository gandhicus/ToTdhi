using System;
using System.Collections.Generic;
using Utils.NET.Geometry;
using Utils.NET.Utils;
using World.Map;

namespace World.Worlds
{
    public partial class Overworld
    {
        // Roll once per meadow sector so large meadow regions can contain several rare clearings.
        private const float GiantTreeSetPieceChance = 0.05f;
        private const int GiantTreeSectorSize = 64;
        private const int GiantTreePlacementAttemptsPerSector = 12;
        private const int GiantTreeSpacing = 48;
        private const int GiantTreeSpawnClearance = 40;
        private const int GiantTreeWaypointClearance = 20;
        private const ushort MeadowTileType = 0xb02;

        private void PlaceMeadowSetPieces()
        {
            var setPiece = SetPiece.Load("meadow/gianttree.mef");
            int maxX = width - setPiece.file.width;
            int maxY = height - setPiece.file.height;
            if (maxX < 0 || maxY < 0) return;

            var placedCenters = new List<Int2>();
            for (int sectorY = 0; sectorY < height; sectorY += GiantTreeSectorSize)
            {
                for (int sectorX = 0; sectorX < width; sectorX += GiantTreeSectorSize)
                {
                    int sectorWidth = Math.Min(GiantTreeSectorSize, width - sectorX);
                    int sectorHeight = Math.Min(GiantTreeSectorSize, height - sectorY);
                    int sectorCenterX = sectorX + sectorWidth / 2;
                    int sectorCenterY = sectorY + sectorHeight / 2;
                    if (tiles.GetTile(sectorCenterX, sectorCenterY).tileType != MeadowTileType ||
                        Rand.FloatValue() >= GiantTreeSetPieceChance)
                        continue;

                    for (int attempt = 0; attempt < GiantTreePlacementAttemptsPerSector; attempt++)
                    {
                        int centerX = sectorX + Rand.Next(sectorWidth);
                        int centerY = sectorY + Rand.Next(sectorHeight);
                        int x = centerX - setPiece.file.width / 2;
                        int y = centerY - setPiece.file.height / 2;
                        if (x < 0 || y < 0 || x > maxX || y > maxY) continue;

                        int spawnDx = centerX - spawn.x;
                        int spawnDy = centerY - spawn.y;
                        if (spawnDx * spawnDx + spawnDy * spawnDy < GiantTreeSpawnClearance * GiantTreeSpawnClearance)
                            continue;

                        bool tooClose = false;
                        foreach (var waypoint in waypointSystem.waypoints)
                        {
                            float dx = centerX - waypoint.position.Value.x;
                            float dy = centerY - waypoint.position.Value.y;
                            if (dx * dx + dy * dy < GiantTreeWaypointClearance * GiantTreeWaypointClearance)
                            {
                                tooClose = true;
                                break;
                            }
                        }
                        if (tooClose) continue;

                        foreach (var placed in placedCenters)
                        {
                            int dx = centerX - placed.x;
                            int dy = centerY - placed.y;
                            if (dx * dx + dy * dy < GiantTreeSpacing * GiantTreeSpacing)
                            {
                                tooClose = true;
                                break;
                            }
                        }
                        if (tooClose) continue;

                        bool allMeadow = true;
                        for (int localY = 0; localY < setPiece.file.height && allMeadow; localY++)
                            for (int localX = 0; localX < setPiece.file.width; localX++)
                                if (tiles.GetTile(x + localX, y + localY).tileType != MeadowTileType)
                                {
                                    allMeadow = false;
                                    break;
                                }
                        if (!allMeadow) continue;

                        ApplySetPiece(setPiece, new Int2(x, y));
                        placedCenters.Add(new Int2(centerX, centerY));
                        break;
                    }
                }
            }
        }
    }
}
