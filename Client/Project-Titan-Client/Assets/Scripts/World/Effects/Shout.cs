using UnityEngine;

public class Shout : Effect
{
    public override void Init(EffectManager manager, World world)
    {
        base.Init(manager, world);

        var options = system.main;
        options.simulationSpace = ParticleSystemSimulationSpace.Custom;
        options.customSimulationSpace = world.transform;
    }

    public void SetInfo(float spreadDeg, float angleDeg, float radius)
    {
        float arc = Mathf.Clamp(spreadDeg, 1f, 360f);
        transform.localEulerAngles = new Vector3(0, 0, angleDeg - arc / 2f);

        var shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Circle;
        shape.arc = arc;
        shape.arcMode = ParticleSystemShapeMultiModeValue.BurstSpread;
        shape.radius = 0.25f;
        shape.radiusThickness = 0f;

        var emission = system.emission;
        var bursts = new ParticleSystem.Burst[Mathf.Max(1, emission.burstCount)];
        emission.GetBursts(bursts);
        bursts[0] = new ParticleSystem.Burst(0f, (short)Mathf.Clamp(Mathf.RoundToInt(arc * 1.2f), 24, 96));
        emission.SetBursts(bursts);

        var speed = system.velocityOverLifetime;
        speed.speedModifier = radius / 0.4f;

        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        system.Play();
    }
}
