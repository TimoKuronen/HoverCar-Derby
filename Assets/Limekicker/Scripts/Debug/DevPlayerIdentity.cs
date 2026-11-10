using System;
using System.Text.RegularExpressions;
using UnityEngine;

/// <summary>
/// Editor-only view of the current Multiplayer Play Mode player: whether it is a virtual player,
/// a stable authentication profile name for it, and its configured tags.
/// </summary>
public static class DevPlayerIdentity
{
    public const string ClientTag = "Client";

    /// <summary>True in a Multiplayer Play Mode virtual player (not the main editor).</summary>
    public static bool IsVirtualPlayer
    {
        get
        {
#if UNITY_EDITOR
            return !Unity.Multiplayer.PlayMode.CurrentPlayer.IsMainEditor;
#else
            return false;
#endif
        }
    }

    /// <summary>
    /// Authentication profile unique to this virtual player's project copy. Virtual players share the
    /// main editor's Company/Product identity, so without a profile they reuse its cached anonymous session.
    /// </summary>
    public static string GetAuthProfileName()
    {
        string profile = Regex.Replace($"vp_{Application.dataPath.GetHashCode():X8}", "[^a-zA-Z0-9_-]", "_");
        return profile.Length > 30 ? profile.Substring(profile.Length - 30) : profile;
    }

    public static bool HasTag(string tag)
    {
#if UNITY_EDITOR
        return Array.IndexOf(Unity.Multiplayer.PlayMode.CurrentPlayer.ReadOnlyTags(), tag) >= 0;
#else
        return false;
#endif
    }
}
