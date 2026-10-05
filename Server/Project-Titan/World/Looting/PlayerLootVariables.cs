using System;
using System.Collections.Generic;
using System.Text;
using TitanCore.Core;
using World.Map.Objects.Entities;

namespace World.Looting
{
    public struct PlayerLootVariables
    {
        public ulong ownerId;

        public int damage;

        public float damagePercent;

        public int fortune;

        public PlayerLootVariables(ulong ownerId, int damage, float damagePercent, int fortune)
        {
            this.ownerId = ownerId;
            this.damage = damage;
            this.damagePercent = damagePercent;
            this.fortune = fortune;
        }

        public static int GetFortune(Player player)
        {
            // Missing player state leaves the drop at its base chance.
            if (player?.gameState?.playerState == null)
                return 0;

            var state = player.gameState.playerState;
            return state.currentSnapshot.GetAlternateStat(AlternateStatType.Fortune)
                + state.GetTimedAlternateStatBonus(AlternateStatType.Fortune);
        }
    }
}
