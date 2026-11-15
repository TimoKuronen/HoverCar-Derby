using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Points per participant id, plus the rules for turning dealt damage into points.
/// Ids are OwnerClientId for players and NetworkObjectId for bots.
/// </summary>
public class ScoreLedger
{
    private readonly Dictionary<ulong, int> pointsById = new Dictionary<ulong, int>();

    /// <summary>
    /// Registers a participant with zero points. Returns false if already registered.
    /// </summary>
    public bool TryAddPlayer(ulong id)
    {
        if (pointsById.ContainsKey(id))
            return false;

        pointsById[id] = 0;
        return true;
    }

    /// <summary>
    /// Adds points to a registered participant and returns the new total. Returns false for unknown ids.
    /// </summary>
    public bool TryAward(ulong id, int points, out int total)
    {
        if (!pointsById.TryGetValue(id, out total))
            return false;

        total += points;
        pointsById[id] = total;
        return true;
    }

    public bool TryGetPoints(ulong id, out int points)
    {
        return pointsById.TryGetValue(id, out points);
    }

    /// <summary>
    /// Participants ordered by points, highest first.
    /// </summary>
    public List<KeyValuePair<ulong, int>> GetRanked()
    {
        var list = new List<KeyValuePair<ulong, int>>(pointsById);
        list.Sort((a, b) => b.Value.CompareTo(a.Value));
        return list;
    }

    /// <summary>
    /// Points earned for dealing damage. False when the damage is not scorable: non-positive,
    /// or without an attacker (ulong.MaxValue).
    /// </summary>
    public static bool TryGetDamagePoints(float damage, ulong attackerId, out int points)
    {
        points = 0;
        if (damage <= 0f || attackerId == ulong.MaxValue)
            return false;

        points = Mathf.RoundToInt(damage);
        return true;
    }
}
