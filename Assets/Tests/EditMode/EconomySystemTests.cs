#if UNITY_EDITOR
using NUnit.Framework;
using TavernSim.Simulation.Systems;

public class EconomySystemTests
{
    [Test]
    public void SpendAndRevenue_WorksAndNotifies()
    {
        var sys = new EconomySystem(initialCash: 100f, overheadPerMinute: 0f);
        float lastCash = -1f;
        sys.CashChanged += v => lastCash = v;

        Assert.IsTrue(sys.TrySpend(25f));
        Assert.AreEqual(75f, sys.Cash, 1e-3);

        sys.AddRevenue(10f);
        Assert.AreEqual(85f, lastCash, 1e-3);
    }
}
#endif
