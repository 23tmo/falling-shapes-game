// Defines the square's high-risk, high-reward movement and scoring profile.
using UnityEngine;

public class Square : FallingShapeBase
{
    protected override ShapeTuning BuildTuning()
    {
        // Squares are premium targets, so they drop quickly, score big, and give only a tiny warning window.
        return new ShapeTuning
        {
            Label = "Square",
            Points = 24,
            BaseFallSpeed = 8.25f,
            EndSpeedMultiplier = 1.35f,
            SwayAmplitude = 0.65f,
            SwayFrequency = 4.5f,
            SpinSpeed = -48f,
            TelegraphDuration = 0.1f,
            TelegraphSpeedScale = 0.75f,
            SpawnBias = 1.3f,
            ComboGraceSeconds = 0f,
            HitStopDuration = 0.05f,
            PopupColor = new Color(1f, 0.5f, 0.42f)
        };
    }
}
