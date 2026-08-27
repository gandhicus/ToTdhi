using System.Collections.Generic;
using Utils.NET.Logging;
using World.Logic.Reader;
using World.Map.Objects.Entities;
using World.Worlds.Gates;

namespace World.Logic.Actions.Death.Gates
{
    /// <summary>
    /// Death action that opens a dungeon portal where the enemy died.
    ///
    /// Written in a .ls script as:
    ///     death: { create_gate(type: "dumir") }
    ///
    /// The valid type values are whatever GateRegistry knows about, which now includes
    /// dungeons defined in dungeons.xml. This class used to carry its own hard-coded
    /// switch of dungeon names that had to be kept in step with a second copy in
    /// GateSpawner; it now just passes the key straight through.
    /// </summary>
    public class CreateGate : DeathAction
    {
        /// <summary>
        /// The dungeon key from the script. Stored as the raw string rather than a
        /// resolved type because scripts are parsed at startup, before data-driven
        /// dungeons have been registered.
        /// </summary>
        private string gateKey;

        public override bool ReadParameterValue(string name, LogicScriptReader reader)
        {
            switch (name)
            {
                case "type":
                    gateKey = reader.ReadString();
                    return true;
            }
            return false;
        }

        public override void OnDeath(Enemy enemy, Player killer, List<Damager> damagers)
        {
            if (string.IsNullOrEmpty(gateKey))
            {
                Log.Error("[CreateGate] A create_gate action has no 'type' set, so no dungeon was opened.");
                return;
            }

            // No portal duration is passed, so the dungeon's own PortalTime applies.
            // A null return means the key was wrong; GateSpawner has already logged it,
            // and the only consequence is that no portal appears.
            GateSpawner.SpawnGate(enemy.world, gateKey, enemy.position.Value);
        }
    }
}
