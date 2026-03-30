// Displays the run timer, end-of-run urgency cues, and the short gameplay hint shown during the main scene.
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(TMP_Text))]
public class Timer : MonoBehaviour
{
    public float totalTime = 75f;

    private TMP_Text timerDisplay;
    private AudioSource audioSource;
    private TMP_Text hintDisplay;
    private int lastWarningSecond = int.MaxValue;

    // GameDirector reads this property so the timer text in the scene also acts as the editable run-length setting.
    public float ConfiguredDuration => totalTime;

    void Start()
    {
        timerDisplay = GetComponent<TMP_Text>();
        audioSource = GetComponent<AudioSource>();

        if (SceneManager.GetActiveScene().name != GameDirector.MainSceneName)
        {
            return;
        }

        // The timer only builds HUD elements in the gameplay scene; the results scene uses a different layout.
        UiFactory.ConfigureCanvas(GetComponentInParent<Canvas>());
        UiFactory.SetRect(timerDisplay.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-56f, -44f), new Vector2(320f, 72f));
        timerDisplay.fontSize = 44f;
        timerDisplay.enableWordWrapping = false;
        timerDisplay.alignment = TextAlignmentOptions.TopRight;
        BuildHud();
    }

    void Update()
    {
        if (SceneManager.GetActiveScene().name != GameDirector.MainSceneName)
        {
            return;
        }

        GameDirector director = GameDirector.Instance;

        if (director == null)
        {
            return;
        }

        int secondsRemaining = Mathf.CeilToInt(director.TimeRemaining);
        timerDisplay.text = $"Time {secondsRemaining}";

        // Color shifts toward warm tones as time runs out so there's more urgency.
        float urgency = 1f - Mathf.Clamp01(director.TimeRemaining / 15f);
        timerDisplay.color = Color.Lerp(new Color(0.9f, 0.96f, 1f), new Color(1f, 0.56f, 0.44f), urgency);

        // A warning ping plays once per second during the final countdown.
        if (audioSource != null && audioSource.clip != null && secondsRemaining <= 10 && secondsRemaining > 0 &&
            secondsRemaining != lastWarningSecond)
        {
            audioSource.pitch = 1f + ((10 - secondsRemaining) * 0.04f);
            audioSource.PlayOneShot(audioSource.clip, 0.6f);
            lastWarningSecond = secondsRemaining;
        }

        if (secondsRemaining > 10)
        {
            // Reset the sentinel once the danger window has passed so the countdown can trigger again on a restart.
            lastWarningSecond = int.MaxValue;
        }
    }

    private void BuildHud()
    {
        // The hint line teaches the core controls and the star/shield rule without needing a separate tutorial screen.
        hintDisplay = UiFactory.CreateText(
            "RunHint",
            transform.parent,
            timerDisplay,
            new Vector2(0.5f, 0f),
            new Vector2(0f, 8f),
            new Vector2(1400f, 44f),
            22f,
            TextAlignmentOptions.Center,
            false);

        UiFactory.SetRect(hintDisplay.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(1400f, 44f));
        hintDisplay.text = "Arrow keys or A / D move. Stars protect your combo. Missing a shape costs 2 points. R restarts.";
        hintDisplay.color = new Color(0.82f, 0.88f, 0.96f);
    }
}
