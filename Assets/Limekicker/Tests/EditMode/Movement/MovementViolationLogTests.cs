using NUnit.Framework;

public class MovementViolationLogTests
{
    [Test]
    public void Register_BelowLimit_DoesNotTrigger()
    {
        var log = new MovementViolationLog(3, 10f);

        Assert.IsFalse(log.Register(1f));
        Assert.IsFalse(log.Register(2f));
    }

    [Test]
    public void Register_ReachingLimitInsideWindow_Triggers()
    {
        var log = new MovementViolationLog(3, 10f);

        log.Register(1f);
        log.Register(2f);

        Assert.IsTrue(log.Register(3f));
    }

    [Test]
    public void Register_SpreadBeyondWindow_DoesNotTrigger()
    {
        var log = new MovementViolationLog(3, 10f);

        log.Register(0f);
        log.Register(6f);

        Assert.IsFalse(log.Register(12f));
    }

    [Test]
    public void Register_ClockRestart_DropsOldEntries()
    {
        var log = new MovementViolationLog(2, 10f);

        log.Register(100f);

        Assert.IsFalse(log.Register(1f));
    }
}
