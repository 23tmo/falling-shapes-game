// Stores the latest run summary and persistent best score so the results screen can survive scene changes.
using UnityEngine;

public static class RunStats
{
    // PlayerPrefs is only used for the all-time best score; the rest of the data is per-session.
    private const string BestScoreKey = "falling_shapes_best_score";

    public static int LastScore { get; private set; }
    public static int LastBestScore { get; private set; }
    public static int LastMaxCombo { get; private set; }
    public static int LastCaught { get; private set; }
    public static int LastMissed { get; private set; }
    public static float LastAccuracy { get; private set; }
    public static bool HasResults { get; private set; }

    public static int BestScore => PlayerPrefs.GetInt(BestScoreKey, 0);

    public static void BeginRun()
    {
        // Starting a run clears transient results while preserving the current best score for comparison later.
        HasResults = false;
        LastScore = 0;
        LastMaxCombo = 0;
        LastCaught = 0;
        LastMissed = 0;
        LastAccuracy = 100f;
        LastBestScore = BestScore;
    }

    public static void CompleteRun(int score, int maxCombo, int caught, int missed)
    {
        // This snapshot is what the GameOver scene reads to build its summary text.
        LastScore = score;
        LastMaxCombo = maxCombo;
        LastCaught = caught;
        LastMissed = missed;
        LastAccuracy = (caught + missed) == 0 ? 100f : (caught / (float)(caught + missed)) * 100f;
        HasResults = true;

        // Persist the best score immediately so closing the game after a run still keeps the record.
        int best = Mathf.Max(BestScore, score);
        PlayerPrefs.SetInt(BestScoreKey, best);
        PlayerPrefs.Save();
        LastBestScore = best;
    }
}
