// Builds and updates the score UI during gameplay, then reuses the same text object to render the results screen.
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(TMP_Text))]
public class ScoreKeeper : MonoBehaviour
{
    private const int MaxMultiplier = 4;

    public static ScoreKeeper Singleton;

    private TMP_Text scoreDisplay;
    private AudioSource scoreAudio;
    private TMP_Text comboDisplay;
    private TMP_Text statsDisplay;
    private TMP_Text promptDisplay;
    private int shownMultiplier = 1;

    void Start()
    {
        Singleton = this;
        scoreDisplay = GetComponent<TMP_Text>();
        scoreAudio = GetComponent<AudioSource>();

        UiFactory.ConfigureCanvas(GetComponentInParent<Canvas>());

        // One script handles both scenes so the HUD and results screen stay visually consistent.
        if (SceneManager.GetActiveScene().name == GameDirector.MainSceneName)
        {
            BuildMainHud();
        }
        else
        {
            BuildResultsScreen();
        }
    }

    void Update()
    {
        // The active scene determines whether this script behaves like an in-run HUD or a post-run summary screen.
        if (SceneManager.GetActiveScene().name == GameDirector.MainSceneName)
        {
            UpdateMainHud();
        }
        else
        {
            UpdateResultsScreen();
        }
    }

    private void BuildMainHud()
    {
        // The main score text is anchored into the top-left corner and acts as the visual template for cloned labels.
        UiFactory.SetRect(scoreDisplay.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(56f, -44f), new Vector2(420f, 72f));
        scoreDisplay.fontSize = 44f;
        scoreDisplay.enableWordWrapping = false;
        scoreDisplay.alignment = TextAlignmentOptions.TopLeft;
        scoreDisplay.color = new Color(0.95f, 0.97f, 1f);

        comboDisplay = UiFactory.CreateText(
            "ComboDisplay",
            transform.parent,
            scoreDisplay,
            new Vector2(0f, 1f),
            new Vector2(56f, -108f),
            new Vector2(1080f, 56f),
            28f,
            TextAlignmentOptions.TopLeft,
            false);

        UiFactory.SetRect(comboDisplay.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(56f, -108f), new Vector2(1080f, 56f));
        comboDisplay.enableWordWrapping = false;

        comboDisplay.color = new Color(0.8f, 0.9f, 1f);
    }

    private void UpdateMainHud()
    {
        GameDirector director = GameDirector.Instance;

        if (director == null)
        {
            return;
        }

        int multiplier = director.CurrentMultiplier;
        string shield = director.RemainingGrace > 0f ? $"  Shield {director.RemainingGrace:0.0}s" : string.Empty;
        int nextThreshold = Mathf.Min((multiplier * 4) + 1, ((MaxMultiplier - 1) * 4) + 1);
        int catchesNeeded = Mathf.Max(0, nextThreshold - director.Combo);

        // The combo line explains both current streak value and exactly how far the player is from the next reward tier.
        scoreDisplay.text = $"Score {director.CurrentScore}";
        if (director.Combo <= 0)
        {
            comboDisplay.text = $"Streak 0  Multiplier x1  Next bonus in 5 catches{shield}";
        }
        else if (multiplier >= MaxMultiplier)
        {
            comboDisplay.text = $"Streak {director.Combo}  Multiplier x{multiplier}  Max bonus reached{shield}";
        }
        else
        {
            string catchLabel = catchesNeeded == 1 ? "catch" : "catches";
            comboDisplay.text = $"Streak {director.Combo}  Multiplier x{multiplier}  Next x{multiplier + 1} in {catchesNeeded} {catchLabel}{shield}";
        }

        float ramp = (multiplier - 1f) / 3f;
        comboDisplay.color = Color.Lerp(new Color(0.76f, 0.94f, 1f), new Color(1f, 0.82f, 0.4f), ramp);

        // A short audio cue fires only when the visible multiplier increases.
        if (multiplier > shownMultiplier && scoreAudio != null && scoreAudio.clip != null)
        {
            scoreAudio.pitch = 0.95f + (0.08f * multiplier);
            scoreAudio.PlayOneShot(scoreAudio.clip, 0.8f);
        }

        shownMultiplier = multiplier;
    }

    private void BuildResultsScreen()
    {
        TMP_Text title = GameObject.Find("GameOver")?.GetComponent<TMP_Text>();

        if (title != null)
        {
            // The existing scene text is reformatted here so the results scene can stay simple in the editor.
            UiFactory.SetRect(title.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 170f), new Vector2(1000f, 96f));
            title.text = "Run Complete";
            title.fontSize = 72f;
            title.alignment = TextAlignmentOptions.Center;
            title.enableWordWrapping = false;
            title.margin = Vector4.zero;
            title.color = new Color(1f, 0.89f, 0.6f);
        }

        UiFactory.SetRect(scoreDisplay.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 5f), new Vector2(1000f, 250f));
        scoreDisplay.fontSize = 38f;
        scoreDisplay.alignment = TextAlignmentOptions.Center;
        scoreDisplay.enableWordWrapping = false;
        scoreDisplay.margin = Vector4.zero;
        scoreDisplay.color = new Color(0.96f, 0.97f, 1f);

        promptDisplay = UiFactory.CreateText(
            "ReplayPrompt",
            transform.parent,
            scoreDisplay,
            new Vector2(0.5f, 0.5f),
            new Vector2(0f, -170f),
            new Vector2(900f, 50f),
            26f,
            TextAlignmentOptions.Center,
            false);

        UiFactory.SetRect(promptDisplay.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -170f), new Vector2(900f, 50f));
        promptDisplay.margin = Vector4.zero;
        promptDisplay.color = new Color(0.85f, 0.9f, 0.98f);
    }

    private void UpdateResultsScreen()
    {
        int bestScore = RunStats.BestScore;

        // A compact stat block so the player can scan score, combo, and accuracy at a glance.
        if (RunStats.HasResults)
        {
            scoreDisplay.text =
                $"<size=52>Score {RunStats.LastScore}</size>\n" +
                $"Best {bestScore}\n" +
                $"Max Combo {RunStats.LastMaxCombo}\n" +
                $"Accuracy {RunStats.LastAccuracy:0}%\n" +
                $"Caught {RunStats.LastCaught} / Missed {RunStats.LastMissed}";
        }
        else
        {
            scoreDisplay.text = $"<size=50>Best Score {bestScore}</size>\nPlay a run from MainScene to generate results.";
        }

        if (promptDisplay != null)
        {
            promptDisplay.text = "Press Enter, Space, or R to play again";
        }

        // The results screen accepts several common confirm inputs so restarting feels immediate.
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter) ||
            Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.R))
        {
            SceneManager.LoadScene(GameDirector.MainSceneName);
        }
    }
}
