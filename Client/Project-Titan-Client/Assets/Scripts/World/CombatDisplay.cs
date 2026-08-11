using TitanCore.Core;
using UnityEngine;

public static class CombatDisplay
{
    public static readonly Color TrueDamageColor = Color.white;

    public static void ShowHitResult(WorldObject obj, DamageResult result, bool aggregatePlayerDamage = false)
    {
        switch (result.type)
        {
            case HitResultType.Blocked:
                obj.ShowAlert("Blocked", TrueDamageColor, true);
                break;
            case HitResultType.TrueDamage:
                if (result.damage <= 0) break;
                if (aggregatePlayerDamage && obj is Enemy enemy)
                    enemy.ShowPlayerDamageAlert(result.damage, TrueDamageColor);
                else
                    obj.ShowAlert("-" + result.damage, TrueDamageColor);
                break;
            default:
                if (result.damage <= 0) break;
                if (aggregatePlayerDamage && obj is Enemy enemyNormal)
                    enemyNormal.ShowPlayerDamageAlert(result.damage, Color.red);
                else
                    obj.ShowAlert("-" + result.damage, Color.red);
                break;
        }
    }
}
