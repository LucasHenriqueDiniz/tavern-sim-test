#if UNITY_EDITOR
using System.Reflection;
using NUnit.Framework;
using TavernSim.Domain;
using TavernSim.Simulation.Systems;
using UnityEngine;

public class OrderSystemTests
{
    [Test]
    public void Enqueue_Progress_ConsumeReady()
    {
        var orders = new OrderSystem();
        var recipe = ScriptableObject.CreateInstance<RecipeSO>();

        // Configure the dummy recipe's prep time for a quick turnaround in tests.
        var prepField = typeof(RecipeSO).GetField("prepTime", BindingFlags.Instance | BindingFlags.NonPublic);
        prepField?.SetValue(recipe, 0.01f);

        orders.EnqueueOrder(tableId: 1, recipe: recipe);
        orders.Tick(0.02f);

        Assert.IsTrue(orders.TryConsumeReadyOrder(1, out var delivered));
        Assert.NotNull(delivered);
    }
}
#endif
