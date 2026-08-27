using System;
using System.Threading.Tasks;
using TitanCore.Data;
using Utils.NET.Geometry;
using Utils.NET.Logging;
using World.Map.Objects.Map;

namespace World.Worlds.Gates
{
    /// <summary>
    /// Creates a dungeon instance and drops its entry portal into an existing world.
    ///
    /// This is the only code path that opens a dungeon. It used to be duplicated inside
    /// the CreateGate death action, which meant portal setup existed twice and could
    /// drift; both callers now come through here.
    /// </summary>
    public static class GateSpawner
    {
        /// <summary>
        /// How long a portal opened by a gate key lasts. Keys are consumed immediately,
        /// so this is deliberately short - it is a "everyone in, now" moment rather than
        /// a portal that sits around.
        /// </summary>
        public const int KeyPortalDurationSeconds = 30;

        /// <summary>
        /// Opens the dungeon registered under the given key, at the given position.
        ///
        /// Returns null if the key is not registered, so the caller can tell the player
        /// something useful. Passing -1 for the duration leaves the dungeon's own
        /// PortalTime in charge.
        /// </summary>
        public static Gate SpawnGate(World world, string gateKey, Vec2 position, int portalDurationSeconds = -1)
        {
            var gate = GateRegistry.Create(gateKey);
            if (gate == null)
            {
                Log.Error($"[GateSpawner] No dungeon is registered under the key '{gateKey}'.");
                return null;
            }

            return SpawnGate(world, gate, position, portalDurationSeconds);
        }

        /// <summary>
        /// Opens an already-constructed dungeon. Kept separate from the key lookup so
        /// callers that build a gate themselves can still share the portal logic.
        /// </summary>
        public static Gate SpawnGate(World world, Gate gate, Vec2 position, int portalDurationSeconds = -1)
        {
            if (world == null || gate == null) return null;

            if (portalDurationSeconds >= 0)
                gate.portalDurationOverride = portalDurationSeconds;

            gate.worldId = world.manager.GetWorldId();
            AddGate(world, gate, position);
            return gate;
        }

        /// <summary>
        /// Builds the dungeon's map off the tick thread, then registers it and spawns the
        /// portal players click.
        ///
        /// This is async void on purpose: map generation for the procedural dungeons is
        /// slow enough that doing it inline would stall the 20-tick-per-second world
        /// loop. The trade-off is that nothing can await the result, so every failure has
        /// to be caught and logged here or it would be lost entirely - an unobserved
        /// exception in an async void method can take the process down.
        /// </summary>
        private static async void AddGate(World world, Gate gate, Vec2 position)
        {
            try
            {
                await Task.Run(() => gate.InitWorld());

                world.manager.AddWorld(gate);

                // The portal object itself must exist in game data. A dungeon pointing at
                // a portal id that was never defined would throw out of a dictionary
                // lookup, so it is checked rather than indexed.
                if (!GameData.objects.TryGetValue(gate.PreferredPortal, out var portalInfo))
                {
                    Log.Error($"[GateSpawner] '{gate.WorldName}' wants portal object 0x{gate.PreferredPortal:x} but no such object exists in the game data. No portal was created.");
                    return;
                }

                var portal = new Portal(gate.worldId);
                portal.worldName.Value = gate.WorldName;
                portal.position.Value = position;
                portal.Initialize(portalInfo);

                gate.portal = portal;
                world.PushTickAction(() =>
                {
                    world.objects.AddObject(portal);
                });
            }
            catch (Exception e)
            {
                // Reaching here means the dungeon failed to build. Consequence: no portal
                // appears and the player sees nothing happen. The world they are standing
                // in is untouched, which is the important part.
                Log.Error($"[GateSpawner] Failed to open dungeon '{gate.WorldName}': {e}");
            }
        }
    }
}
