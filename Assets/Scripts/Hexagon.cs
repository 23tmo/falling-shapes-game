// Defines the hexagon's balanced, mid-speed movement and score values.
using UnityEngine;

public class Hexagon : FallingShapeBase
{
    protected override ShapeTuning BuildTuning()
    {
        // Hexagons are steady mid-tier targets with wider sway, which adds motion variety without extreme difficulty.
        return new ShapeTuning
        {
            Label = "Hexagon",
            Points = 6,
            BaseFallSpeed = 4.45f,
            EndSpeedMultiplier = 1.28f,
            SwayAmplitude = 1.55f,
            SwayFrequency = 1.7f,
            SpinSpeed = 42f,
            TelegraphDuration = 0f,
            TelegraphSpeedScale = 1f,
            SpawnBias = 1.1f,
            ComboGraceSeconds = 0f,
            HitStopDuration = 0.024f,
            PopupColor = new Color(0.5f, 0.8f, 1f)
        };
    }
}
