/// <summary>
/// Outcome of checking one owner-reported position against the server's movement limits.
/// </summary>
public enum MovementVerdict
{
    Accepted,
    TooFast,
    OutOfBounds
}
