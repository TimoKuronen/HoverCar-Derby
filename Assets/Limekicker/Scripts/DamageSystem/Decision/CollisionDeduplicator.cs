using System.Collections.Generic;

/// <summary>
/// Remembers which car pairs collided recently so one physical collision is processed once,
/// even though both cars (and the owner report path) observe it.
/// </summary>
public class CollisionDeduplicator
{
    private readonly Dictionary<ulong, float> startTimes = new Dictionary<ulong, float>();
    private readonly List<ulong> expiredKeys = new List<ulong>();

    /// <summary>
    /// Order-independent key for a pair of participant ids.
    /// </summary>
    public static ulong KeyFor(ulong idA, ulong idB)
    {
        return idA < idB ? (idA << 32) | idB : (idB << 32) | idA;
    }

    /// <summary>
    /// True while a registered collision for this key is younger than the cooldown. A start time
    /// ahead of now (the clock restarted) counts as expired.
    /// </summary>
    public bool IsRecent(ulong key, float now, float cooldown)
    {
        return startTimes.TryGetValue(key, out float start) && now >= start && now - start < cooldown;
    }

    /// <summary>
    /// Records a collision for the key and drops entries whose cooldown has passed.
    /// </summary>
    public void Register(ulong key, float now, float cooldown)
    {
        expiredKeys.Clear();
        foreach (KeyValuePair<ulong, float> entry in startTimes)
        {
            if (now < entry.Value || now - entry.Value >= cooldown)
                expiredKeys.Add(entry.Key);
        }

        for (int i = 0; i < expiredKeys.Count; i++)
            startTimes.Remove(expiredKeys[i]);

        startTimes[key] = now;
    }
}
