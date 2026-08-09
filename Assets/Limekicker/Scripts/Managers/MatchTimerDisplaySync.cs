using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Syncs countdown and round-timer IntVariables from server-authoritative match timing.
/// </summary>
public class MatchTimerDisplaySync
{
    private readonly RaceContext context;
    private readonly IntVariable countdownValue;
    private readonly IntVariable gameTimerValue;
    private readonly NetworkVariable<RaceContext.MatchPhase> matchPhase;
    private readonly NetworkVariable<double> phaseStartServerTime;
    private readonly NetworkVariable<double> roundEndServerTime;

    private int lastCountdownDisplayValue = -1;

    public MatchTimerDisplaySync(
        RaceContext context,
        IntVariable countdownValue,
        IntVariable gameTimerValue,
        NetworkVariable<RaceContext.MatchPhase> matchPhase,
        NetworkVariable<double> phaseStartServerTime,
        NetworkVariable<double> roundEndServerTime)
    {
        this.context = context;
        this.countdownValue = countdownValue;
        this.gameTimerValue = gameTimerValue;
        this.matchPhase = matchPhase;
        this.phaseStartServerTime = phaseStartServerTime;
        this.roundEndServerTime = roundEndServerTime;
    }

    public void Tick()
    {
        SyncCountdownDisplayFromServerTime();
        SyncRoundTimerFromServerTime();
    }

    private void SyncCountdownDisplayFromServerTime()
    {
        if (matchPhase.Value != RaceContext.MatchPhase.Countdown || NetworkManager.Singleton == null)
            return;

        int displayValue = GetCountdownDisplayValue(NetworkManager.Singleton.ServerTime.Time - phaseStartServerTime.Value);
        if (displayValue == lastCountdownDisplayValue)
            return;

        lastCountdownDisplayValue = displayValue;
        countdownValue.Value = displayValue;
    }

    private int GetCountdownDisplayValue(double elapsedSeconds)
    {
        if (elapsedSeconds < 0d)
            return -1;

        float interval = context.countdownIntervalSeconds;
        if (elapsedSeconds < interval)
            return 3;
        if (elapsedSeconds < interval * 2f)
            return 2;
        if (elapsedSeconds < interval * 3f)
            return 1;
        if (elapsedSeconds < interval * 3f + context.countdownGoDelaySeconds)
            return 0;

        return -1;
    }

    private void SyncRoundTimerFromServerTime()
    {
        if (matchPhase.Value != RaceContext.MatchPhase.Playing || NetworkManager.Singleton == null)
            return;

        int remaining = Mathf.CeilToInt((float)(roundEndServerTime.Value - NetworkManager.Singleton.ServerTime.Time));
        remaining = Mathf.Max(0, remaining);

        if (gameTimerValue.Value != remaining)
            gameTimerValue.Value = remaining;
    }
}
