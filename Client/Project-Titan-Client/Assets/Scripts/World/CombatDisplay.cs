using TitanCore.Core;
using UnityEngine;

public static class CombatDisplay
{
    public static readonly Color TrueDamageColor = Color.white;
    public static readonly Color CriticalColor = new Color(1f, 0.5f, 0f);
    public static readonly Color AbsorbedColor = Color.green;

    public static Color GetHitColor(HitResultType type)
    {
        switch (type)
        {
            case HitResultType.TrueDamage:
                return TrueDamageColor;
            case HitResultType.Critical:
                return CriticalColor;
            default:
                return Color.red;
        }
    }

    public static void ShowHitResult(WorldObject obj, DamageResult result, bool aggregatePlayerDamage = false)
    {
        switch (result.type)
        {
            case HitResultType.Blocked:
                obj.ShowAlert("Blocked", TrueDamageColor, true);
                break;
            case HitResultType.Absorbed:
                obj.ShowAlert("ABSORBED", AbsorbedColor, true);
                break;
            default:
                if (result.damage <= 0) break;
                var color = GetHitColor(result.type);
                if (aggregatePlayerDamage && obj is Enemy enemy)
                    enemy.ShowPlayerDamageAlert(result.damage, color);
                else
                    obj.ShowAlert("-" + result.damage, color);
                break;
        }
    }
}
