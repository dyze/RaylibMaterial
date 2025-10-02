
namespace Editor.Helpers;

public class Timer(DateTime startedOn, TimeSpan duration)
{
    public bool IsElapsed(DateTime now)
    {
        return now - startedOn >= duration;
    }

    /// <summary>
    /// Returns the percentage of completion
    /// </summary>
    /// <param name="now"></param>
    /// <returns>0 to 1</returns>
    public float Completion(DateTime now)
    {
        var currentDuration = now - startedOn;

        return Math.Min(0, (float)currentDuration.Seconds / duration.Seconds);
    }
}