using UnityEngine;

public class RangerArrows : Effect
{
    // Pooling enables the object (and plays) before radius/color are known.
    // Clear that premature burst so we only emit once at the real settings.
    protected override void OnEnable()
    {
    }

    public void SetInfo(float radius, Color? color = null)
    {
        if (system == null)
            system = GetComponent<ParticleSystem>();
        if (system == null)
            return;

        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var shape = system.shape;
        shape.radius = radius;
        if (color.HasValue)
        {
            var options = system.main;
            options.startColor = color.Value;
        }

        system.Play();
    }
}
