using sysfetch.Helpers;

namespace sysfetch_tests;

public class ProbeTests
{
    [Test]
    public void Result_ReturnsValueWhenProbeCompletes()
    {
        var task = Probe.Start(() => "ok");
        Probe.WaitAll(TimeSpan.FromSeconds(5), task);

        Assert.That(Probe.Result(task, "fallback"), Is.EqualTo("ok"));
    }

    [Test]
    public void Result_FallsBackWhenProbeThrows()
    {
        var task = Probe.Start<string>(() => throw new IOException("boom"));
        Probe.WaitAll(TimeSpan.FromSeconds(5), task);

        Assert.That(Probe.Result(task, "fallback"), Is.EqualTo("fallback"));
    }

    [Test]
    public void WaitAll_DoesNotBlockPastTimeout()
    {
        using var release = new ManualResetEventSlim();
        var slow = Probe.Start(() => { release.Wait(); return "late"; });
        var fast = Probe.Start(() => "fast");

        var started = DateTime.UtcNow;
        Probe.WaitAll(TimeSpan.FromMilliseconds(100), slow, fast);
        var elapsed = DateTime.UtcNow - started;

        Assert.That(elapsed, Is.LessThan(TimeSpan.FromSeconds(2)));
        Assert.That(Probe.Result(fast, "fallback"), Is.EqualTo("fast"));
        Assert.That(Probe.Result(slow, "fallback"), Is.EqualTo("fallback"));
        release.Set();
    }
}
