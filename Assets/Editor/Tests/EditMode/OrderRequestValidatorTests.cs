#if UNITY_INCLUDE_TESTS
using System.Reflection;
using NUnit.Framework;
using TavernSim.Domain;
using TavernSim.Simulation.Systems;
using TavernSim.UI;
using UnityEngine;

public class OrderRequestValidatorTests
{
    [Test]
    public void MenuPolicyBlocksOrder()
    {
        var recipe = CreateRecipe("drink", "Bebida", 1f);
        var policy = new StubMenuPolicy(false);
        var inventory = new StubInventory(true, true);
        var result = OrderRequestValidator.IsAllowed(policy, inventory, recipe);
        Assert.IsFalse(result);
    }

    [Test]
    public void InventoryAllowsOrder()
    {
        var recipe = CreateRecipe("soup", "Sopa", 1f);
        var policy = new StubMenuPolicy(true);
        var inventory = new StubInventory(true, true);
        var result = OrderRequestValidator.IsAllowed(policy, inventory, recipe);
        Assert.IsTrue(result);
    }

    private static RecipeSO CreateRecipe(string id, string name, float prepTime)
    {
        var recipe = ScriptableObject.CreateInstance<RecipeSO>();
        typeof(RecipeSO).GetField("id", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(recipe, id);
        typeof(RecipeSO).GetField("displayName", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(recipe, name);
        typeof(RecipeSO).GetField("prepTime", BindingFlags.Instance | BindingFlags.NonPublic)?.SetValue(recipe, prepTime);
        return recipe;
    }

    private sealed class StubMenuPolicy : IMenuPolicy
    {
        private readonly bool _allowed;

        public StubMenuPolicy(bool allowed)
        {
            _allowed = allowed;
        }

        public bool IsAllowed(RecipeSO recipe) => _allowed;
    }

    private sealed class StubInventory : IInventoryService
    {
        private readonly bool _canCraft;
        private readonly bool _consume;

        public StubInventory(bool canCraft, bool consume)
        {
            _canCraft = canCraft;
            _consume = consume;
        }

        public bool CanCraft(RecipeSO recipe) => _canCraft;

        public bool TryConsume(RecipeSO recipe) => _consume;
    }
}
#endif
