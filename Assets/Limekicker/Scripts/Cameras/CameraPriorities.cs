/// <summary>
/// Cinemachine priorities for the match cameras. The highest priority among live cameras wins, so
/// each match view sits on its own tier and no two views tie.
/// </summary>
public static class CameraPriorities
{
    public const int Inactive = 0;
    public const int PlayerCar = 10;
    public const int IntroDolly = 15;
    public const int RaceChase = 20;
    public const int VictoryDolly = 25;
}
