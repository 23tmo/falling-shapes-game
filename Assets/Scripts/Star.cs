// Defines the star, which grants temporary combo protection instead of high point value.
using UnityEngine;

public class Star : FallingShapeBase
{
    protected override ShapeTuning BuildTuning()
    {
        // Stars trade raw score for utility by granting a short grace window that absorbs one missed shape.
        return new ShapeTuning
        {
            Label = "Star",
            Points = 2,
            BaseFallSpeed = 3.8f,
            EndSpeedMultiplier = 1.24f,
            SwayAmplitude = 0.45f,
            SwayFrequency = 2.3f,
            SpinSpeed = 95f,
            TelegraphDuration = 0f,
            TelegraphSpeedScale = 1f,
            SpawnBias = 1f,
            ComboGraceSeconds = 1.75f,
            HitStopDuration = 0.02f,
            PopupColor = new Color(1f, 0.93f, 0.46f)
        };
    }
}
