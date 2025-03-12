public interface ITimeManager : IUpdateableService
{
    public float GetPassedTime { get; }
    public float GetNormalTimeScale { get; }
    void UpdateNormalTimeScale(float newTimeScale);
}
