using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

public class RangerArrows : Effect
{
    public void SetInfo(float radius, Color? color = null)
    {
        var shape = system.shape;
        shape.radius = radius;
        if (color.HasValue)
        {
            var options = system.main;
            options.startColor = color.Value;
        }
    }
}
