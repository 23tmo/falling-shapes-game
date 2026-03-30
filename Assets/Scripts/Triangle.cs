// Defines the triangle's spinning motion and modest score value.
using UnityEngine;

public class Triangle : FallingShapeBase
{
    protected override ShapeTuning BuildTuning()
    {
        // Triangles are a moderate-value target with fast rotation and a very narrow sway pattern.
        return new ShapeTuning
        {
            Label = "Triangle",
            Points = 4,
            BaseFallSpeed = 5.25f,
            EndSpeedMultiplier = 1.26f,
            SwayAmplitude = 0.1f,
            SwayFrequency = 0.9f,
            SpinSpeed = -130f,
            TelegraphDuration = 0f,
            TelegraphSpeedScale = 1f,
            SpawnBias = 1.08f,
            ComboGraceSeconds = 0f,
            HitStopDuration = 0.02f,
            PopupColor = new Color(1f, 0.62f, 0.31f)
        };
    }
}
