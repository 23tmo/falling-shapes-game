// Defines the diamond movement and scoring profile.
using UnityEngine;

public class Diamond : FallingShapeBase
{
    protected override ShapeTuning BuildTuning()
    {
        // Diamonds are intended to feel valuable and slightly dangerous, so they move fast and pay well.
        return new ShapeTuning
        {
            Label = "Diamond",
            Points = 12,
            BaseFallSpeed = 6.8f,
            EndSpeedMultiplier = 1.32f,
            SwayAmplitude = 0.8f,
            SwayFrequency = 2.9f,
            SpinSpeed = 140f,
            TelegraphDuration = 0.45f,
            TelegraphSpeedScale = 0.12f,
            SpawnBias = 1.18f,
            ComboGraceSeconds = 0f,
            HitStopDuration = 0.035f,
            PopupColor = new Color(0.98f, 0.52f, 1f)
        };
    }
}
