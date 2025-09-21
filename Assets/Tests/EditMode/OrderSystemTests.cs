#if UNITY_INCLUDE_TESTS
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using TavernSim.Domain;
using TavernSim.Simulation.Systems;
using UnityEngine;

public class OrderSystemTests
{
    [Test]
    public void OrdersAdvancePerPrepArea()
    {
        var orders = new OrderSystem();
        orders.SetKitchenStations(1);
        orders.SetBarStations(1);

        var kitchenRecipe = CreateRecipe("stew", "Ensopado", 0.1f);
        var barRecipe = CreateRecipe("beer", "Cerveja", 0.1f);

        orders.EnqueueOrder(1, kitchenRecipe);
        orders.EnqueueOrder(2, barRecipe);

        orders.Tick(0.05f);
        var snapshot = orders.GetOrders();
        Assert.AreEqual(2, snapshot.Count);
        Assert.IsTrue(snapshot.Any(o => o.TableId == 1 && o.Area == PrepArea.Kitchen));
        Assert.IsTrue(snapshot.Any(o => o.TableId == 2 && o.Area == PrepArea.Bar));

        orders.Tick(0.1f);

        Assert.IsTrue(orders.TryConsumeReadyOrder(1, out var kitchenReady, out var kitchenArea));
        Assert.AreSame(kitchenRecipe, kitchenReady);
        Assert.AreEqual(PrepArea.Kitchen, kitchenArea);

        Assert.IsTrue(orders.TryConsumeReadyOrder(2, out var barReady, out var barArea));
        Assert.AreSame(barRecipe, barReady);
        Assert.AreEqual(PrepArea.Bar, barArea);
    }

    [Test]
    public void RespectsStationLimits()
    {
        var orders = new OrderSystem();
        orders.SetKitchenStations(1);
        orders.SetBarStations(1);

        var first = CreateRecipe("stewA", "Ensopado A", 0.2f);
        var second = CreateRecipe("stewB", "Ensopado B", 0.2f);

        orders.EnqueueOrder(1, first);
        orders.EnqueueOrder(2, second);
        orders.Tick(0f);

        var snapshot = orders.GetOrders();
        var active = snapshot.First(o => o.TableId == 1);
        var queued = snapshot.First(o => o.TableId == 2);
        Assert.AreEqual(OrderState.InPreparation, active.State);
        Assert.AreEqual(OrderState.Queued, queued.State);

        orders.Tick(0.25f);
        Assert.IsTrue(orders.TryConsumeReadyOrder(1, out _, out _));

        orders.Tick(0f);
        snapshot = orders.GetOrders();
        var promoted = snapshot.First(o => o.TableId == 2);
        Assert.AreEqual(OrderState.InPreparation, promoted.State);
    }

    [Test]
    public void TryConsumeReadyOrderMatchesTable()
    {
        var orders = new OrderSystem();
        orders.SetKitchenStations(1);
        var soup = CreateRecipe("soup", "Sopa", 0.05f);

        orders.EnqueueOrder(1, soup);
        orders.Tick(0.1f);

        Assert.IsFalse(orders.TryConsumeReadyOrder(2, out _, out _));
        Assert.IsTrue(orders.TryConsumeReadyOrder(1, out var recipe, out var area));
        Assert.AreSame(soup, recipe);
        Assert.AreEqual(PrepArea.Kitchen, area);
    }

    private static RecipeSO CreateRecipe(string id, string name, float prepTime)
    {
        var recipe = ScriptableObject.CreateInstance<RecipeSO>();
        typeof(RecipeSO).GetField("id", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(recipe, id);
        typeof(RecipeSO).GetField("displayName", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(recipe, name);
        typeof(RecipeSO).GetField("prepTime", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(recipe, prepTime);
        return recipe;
    }
}
#endif
