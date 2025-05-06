using System.Collections.Generic;

public static class LeaderboardCache
{
    public static Dictionary<string, int> scores = new Dictionary<string, int>();
    public static Dictionary<string, float> times = new Dictionary<string, float>();

    public static void Clear()
    {
        scores.Clear();
        times.Clear();
    }
}
