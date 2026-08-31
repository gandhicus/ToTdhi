using System;
using System.Collections.Generic;
using TitanCore.Core;
using UnityEngine;

public class StatusEffects : MonoBehaviour
{
    private const float Sprite_Spacing = 0.62f;

    public Entity toFollow;

    private static StatusEffect[] effectTypes = (StatusEffect[])Enum.GetValues(typeof(StatusEffect));

    private Dictionary<StatusEffect, SpriteRenderer> effects = new Dictionary<StatusEffect, SpriteRenderer>();

    private void LateUpdate()
    {
        if (toFollow == null) return;

        var height = toFollow.GetHeight();
        var position = toFollow.transform.position;
        position.z -= height + 0.06f;
        transform.position = position;

        var parentScale = transform.parent.localScale.x;
        float scale;
        if (parentScale != 0)
            scale = 0.7f / parentScale;
        else
            scale = 0;
        transform.localScale = new Vector3(scale, scale, scale);

        UpdateEffects();
    }

    private void UpdateEffects()
    {
        for (int i = 0; i < effectTypes.Length; i++)
        {
            var effect = effectTypes[i];
            if (toFollow.HasStatusEffect(effect) && HasIconSprite(effect))
            {
                if (effects.ContainsKey(effect)) continue;
                var effectSprite = toFollow.world.gameManager.objectManager.GetStatusEffectSprite(this, effect);
                effects.Add(effect, effectSprite);
            }
            else
            {
                if (!effects.TryGetValue(effect, out var effectSprite)) continue;
                effects.Remove(effect);
                toFollow.world.gameManager.objectManager.ReturnStatusEffectSprite(effectSprite);
            }
        }

        LayoutVisibleIcons();
    }

    private static bool HasIconSprite(StatusEffect effect)
    {
        return TextureManager.GetStatusEffect(effect) != null;
    }

    private static bool IsVisibleIcon(SpriteRenderer sprite)
    {
        return sprite != null && sprite.sprite != null && sprite.enabled;
    }

    private void LayoutVisibleIcons()
    {
        int visible = 0;
        foreach (var sprite in effects.Values)
        {
            if (IsVisibleIcon(sprite))
                visible++;
        }

        if (visible == 0) return;

        float spread = (visible - 1) * Sprite_Spacing;
        int index = 0;
        foreach (var sprite in effects.Values)
        {
            if (!IsVisibleIcon(sprite)) continue;
            sprite.transform.localPosition = new Vector3(-spread / 2f + Sprite_Spacing * index, 0, 0);
            index++;
        }
    }
}
