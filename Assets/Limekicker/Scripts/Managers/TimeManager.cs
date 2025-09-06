using UnityEngine;

public class TimeManager : ITimeManager
{
    private float passedTime;
    private float currentNormalTimeScale = 1f;
    public float GetPassedTime => passedTime;

    public float GetNormalTimeScale => currentNormalTimeScale;

    public TimeManager(IGameStateHandler gameStateHandler)
    {
        gameStateHandler.OnGameStateChanged += CheckToAlterTimescale;
    }

    public void Update()
    {
        passedTime += Time.deltaTime;

        if(Input.GetKeyDown(KeyCode.Escape))
        {
            Debug.Break();
        }
    }

    private void CheckToAlterTimescale(GameState gameState)
    {
        if (gameState != GameState.Normal)
            Time.timeScale = 0.0000001f;
        else UpdateNormalTimeScale(currentNormalTimeScale);
    }

    public void UpdateNormalTimeScale(float newTimeScale)
    {
        currentNormalTimeScale = newTimeScale;
        Time.timeScale = currentNormalTimeScale;
    }
}
