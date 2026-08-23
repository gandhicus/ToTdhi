using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TitanCore.Core;
using TMPro;
using UnityEngine;

public class BarStat : Stat
{
    private int max;

    private int value;

    public RectTransform valueBar;

    public RectTransform containerBar;

    public override void SetStat(int value, int extra, Player player)
    {
        max = value + extra;
        if (statType == StatType.MaxHealth)
            max = StatFunctions.ClampPlayerMaxHealth(max);
        CheckMax(value, player);
        Resize();
    }

    public void SetValue(int value)
    {
        this.value = value;
        stat.text = value.ToString();
        Resize();
    }

    private void Resize()
    {
        float percent = max <= 0 ? 0f : (float)value / max;
        valueBar.anchorMax = new Vector2(Mathf.Clamp01(percent), 1);
        valueBar.offsetMax = new Vector2(0, 0);
    }
}