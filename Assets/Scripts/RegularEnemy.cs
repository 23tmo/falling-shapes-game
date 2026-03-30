// Defines the common circle target that establishes the base rhythm of the game.
using UnityEngine;

public class RegularEnemy : FallingShapeBase
{
    protected override ShapeTuning BuildTuning()
    {
        // Circles are the low-risk baseline spawn. They're cheaper, slower, and calmer than the bonus shapes.
        return new ShapeTuning
        {
            Label = "Circle",
            Points = 1,
            BaseFallSpeed = 3.2f,
            EndSpeedMultiplier = 1.18f,
            SwayAmplitude = 0.25f,
            SwayFrequency = 1.2f,
            SpinSpeed = 55f,
            TelegraphDuration = 0f,
            TelegraphSpeedScale = 1f,
            SpawnBias = 0.95f,
            ComboGraceSeconds = 0f,
            HitStopDuration = 0.015f,
            PopupColor = new Color(0.62f, 1f, 0.82f)
        };
    }
}
