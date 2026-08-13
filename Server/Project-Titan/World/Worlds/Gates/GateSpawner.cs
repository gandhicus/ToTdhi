using System;
using System.Threading.Tasks;
using TitanCore.Data;
using Utils.NET.Geometry;
using World.Map.Objects.Map;

namespace World.Worlds.Gates
{
    public static class GateSpawner
    {
        public const int KeyPortalDurationSeconds = 30;

        public static Type ResolveGateType(string gateType)
        {
            switch (gateType)
            {
                case "forge":
                    return typeof(ValdoksForge);
                case "dumir":
                    return typeof(Dumir);
                case "bubra":
                    return typeof(BhogninsGate);
                case "woods":
                    return typeof(RictornsGate);
                case "fortress":
                    return typeof(MannahsFortress);
                default:
                    return null;
            }
        }

        public static Gate SpawnGate(World world, Type gateType, Vec2 position, int portalDurationSeconds = -1)
        {
            if (gateType == null) return null;

            var gate = (Gate)Activator.CreateInstance(gateType);
            if (portalDurationSeconds >= 0)
                gate.portalDurationOverride = portalDurationSeconds;

            gate.worldId = world.manager.GetWorldId();
            AddGate(world, gate, position);
            return gate;
        }

        private static async void AddGate(World world, Gate gate, Vec2 position)
        {
            await Task.Run(() =>
            {
                gate.InitWorld();
            });
            world.manager.AddWorld(gate);

            var portal = new Portal(gate.worldId);
            portal.worldName.Value = gate.WorldName;
            portal.position.Value = position;
            portal.Initialize(GameData.objects[gate.PreferredPortal]);

            gate.portal = portal;
            world.PushTickAction(() =>
            {
                world.objects.AddObject(portal);
            });
        }
    }
}
