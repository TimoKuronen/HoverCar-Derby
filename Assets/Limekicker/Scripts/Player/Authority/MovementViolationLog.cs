using System.Collections.Generic;

/// <summary>
/// Counts violations inside a sliding time window so a client is removed only after repeated offences,
/// not after a single network hiccup.
/// </summary>
public class MovementViolationLog
{
    private readonly int limit;
    private readonly float windowSeconds;
    private readonly Queue<float> times = new Queue<float>();

    public MovementViolationLog(int limit, float windowSeconds)
    {
        this.limit = limit;
        this.windowSeconds = windowSeconds;
    }

    /// <summary>
    /// Records a violation at the given time. Returns true once the number of violations inside the window
    /// reaches the limit. A time earlier than recorded entries (clock restart) drops them.
    /// </summary>
    public bool Register(float now)
    {
        while (times.Count > 0 && (now < times.Peek() || now - times.Peek() > windowSeconds))
        {
            times.Dequeue();
        }

        times.Enqueue(now);
        return times.Count >= limit;
    }
}
