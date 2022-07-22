// This Shared Data Object does all the caching for us
// It is passed around the various other components
// Most of its values are read-only by nature
// Only a few parameters are allowed to change

using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;

public class PinnedRecipeSDO
{

    public int Count = 1; // adjustable
    public readonly Recipe Recipe = null;
    public readonly string Title = null;
    public readonly string IconImg = null;
    public readonly string IconTint = null;
    public bool CorrectArea = false;
    public bool IsCraftable = false;
    public bool IsLocked = false;

    // Tier may change if player stats change
    // E.g. when upgrading stats after leveling
    public float Tier = -1;

    // Same goes for needed ingredients
    public readonly List<PinnedIngredientSDO>
        Ingredients = new List<PinnedIngredientSDO>();

    private static readonly CachedStringFormatterXuiRgbaColor
        colorFormatter = new CachedStringFormatterXuiRgbaColor();

    // Call this to get an index safely (will check bounds)
    public PinnedIngredientSDO GetIngredient(int index)
    {
        if (index >= Ingredients.Count) return null;
        else return Ingredients[index];
    }

    // Update the multiplier
    public void SetCount(int count)
    {
        count = Math.Max(count, 1);
        count = Math.Min(count, 9999);
        if (Count == count) return;
        Count = count; // Update
        UpdateIsCraftable();
    }

    // Updates `CorrectArea` from the given window reference
    public void UpdateCraftArea(XUiC_CraftingWindowGroup window)
    {
        CorrectArea = false;
        if (window == null) return;
        if (!PinRecipesManager.HasInstance) return;
        if (!CraftingRequirementsValid(window)) return;
        if (!IsCorrectCraftingArea(window)) return;
        CorrectArea = true;
    }

    // Check if recipe is unlocked and has enough material
    // You must check if you are at the correct craft area too
    public void UpdateIsCraftable()
    {
        IsCraftable = false;
        if (Recipe == null) return;
        if (!PinRecipesManager.HasInstance) return;
        IsCraftable = !IsLocked && HasEnoughCraftingMaterials();
    }

    // Call this when user stats have changed
    public void OnUserStatsChanged()
    {
        if (!PinRecipesManager.HasInstance) return;
        // ToDo: is there a more direct way to get this?
        var player = PinRecipesManager.Instance.Player;
        // Bail out early if no user is attached yet
        if (player == null) return;
        // Tier seems only dependent on recipe
        Tier = EffectManager.GetValue(
            PassiveEffects.CraftingTier,
            _originalValue: 1f,
            _entity: player,
            _recipe: Recipe,
            tags: Recipe.tags);
        // Also cache a few things from ingredients
        for (int i = 0; i < Ingredients.Count; i++)
            Ingredients[i].RecalcNeeded(player);
        // Check if recipe is locked for user
        IsLocked = !Recipe.IsUnlocked(player);
        // Update if we can craft it
        UpdateIsCraftable();
    }

    // Call this when inventory changed
    public void OnInventoryChanged()
    {
        // ToDo: is there a more direct way to get this?
        var xui = PinRecipesManager.Instance?.XUI;
        // Also cache a few things from ingredients
        for (int i = 0; i < Ingredients.Count; i++)
            Ingredients[i].RecalcAvailable(xui);
        // Update if we can craft it
        UpdateIsCraftable();
    }

    public PinnedRecipeSDO(Recipe recipe, int count,
        XUiC_CraftingWindowGroup area)
    {
        Count = count;
        Recipe = recipe;
        Ingredients.Clear();
        if (Recipe != null)
        {
            Title = Localization.Get(Recipe.GetName());
            ItemValue itemValue = new ItemValue(Recipe.itemValueType);
            IconImg = itemValue.GetPropertyOverride("CustomIcon",
                itemValue.ItemClass.GetIconName());
            IconTint = colorFormatter.Format(itemValue
                .ItemClass.GetIconTint(itemValue));
            // Also cache a few things from ingredients
            for (int i = 0; i < Recipe.ingredients.Count; i++)
                Ingredients.Add(new PinnedIngredientSDO(this, i));
        }
        // Some safety checks
        Count = Math.Max(Count, 1);
        Count = Math.Min(Count, 9999);
        // Update `CorrectArea`
        UpdateCraftArea(area);
        // Update user dependent state
        OnUserStatsChanged();
        // Update Inventory state
        OnInventoryChanged();
    }

    private bool IsCorrectCraftingArea(XUiC_CraftingWindowGroup win)
    {
        if (win == null || win.Workstation == null) return false;
        // Copied from XUiC_ItemActionList::SetCraftingActionList
        Block block = Block.GetBlockByName(win.Workstation);
        if (block != null && block.Properties.Values
            .ContainsKey("Workstation.CraftingAreaRecipes"))
        {
            string str = block.Properties.Values[
                "Workstation.CraftingAreaRecipes"];
            string[] areas = str.Split(new[] { ',', ' ' },
                    StringSplitOptions.RemoveEmptyEntries);
            foreach (var area in areas)
            {
                if (area.EqualsCaseInsensitive(Recipe.craftingArea))
                {
                    return true;
                }
                else if (area.EqualsCaseInsensitive("player"))
                {
                    if (Recipe.craftingArea == null) return true;
                    if (Recipe.craftingArea == string.Empty) return true;
                }
            }
        }
        else
        {
            return win.Workstation.EqualsCaseInsensitive(Recipe.craftingArea);
        }
        return false;
    }

    static readonly FieldInfo FieldToolWindow = AccessTools.Field(typeof(XUiC_WorkstationWindowGroup), "toolWindow");
    static readonly FieldInfo FieldInputWindow = AccessTools.Field(typeof(XUiC_WorkstationWindowGroup), "inputWindow");
    static readonly FieldInfo FieldOutputWindow = AccessTools.Field(typeof(XUiC_WorkstationWindowGroup), "outputWindow");
    static readonly FieldInfo FieldFuelWindow = AccessTools.Field(typeof(XUiC_WorkstationWindowGroup), "fuelWindow");

    public bool CraftingRequirementsValid(XUiC_WorkstationWindowGroup win, bool includeFuel = false)
    {
        if (Recipe == null) return false;
        if (FieldToolWindow.GetValue(win) is XUiC_WorkstationToolGrid tools)
            if (tools != null && !tools.HasRequirement(Recipe)) return false;
        if (FieldInputWindow.GetValue(win) is XUiC_WorkstationInputGrid input)
            if (input != null && !input.HasRequirement(Recipe)) return false;
        if (FieldOutputWindow.GetValue(win) is XUiC_WorkstationOutputGrid output)
            if (output != null && !output.HasRequirement(Recipe)) return false;
        if (includeFuel && FieldFuelWindow.GetValue(win) is XUiC_WorkstationFuelGrid fuel)
            if (fuel != null && !fuel.HasRequirement(Recipe)) return false;
        return true;
    }

    public bool CraftingRequirementsValid(XUiC_CraftingWindowGroup win, bool includeFuel = false)
    {
        if (win is XUiC_WorkstationWindowGroup workstation)
        {
            return CraftingRequirementsValid(workstation, includeFuel);
        }
        return true;
    }

    private bool HasEnoughCraftingMaterials()
    {
        if (Recipe == null) return false;
        foreach (PinnedIngredientSDO ido in Ingredients)
            if (ido.Available < ido.Needed) return false;
        return true;
    }

    // We can't use Equals since `Seed` is different!?
    private bool IsValidItemValue(ItemValue a, ItemValue b)
    {
        if (a == null)
        {
            return b == null;
        }
        else if (b == null)
        {
            return false;
        }
        else if (a.HasQuality)
        {
            return b.HasQuality
                && a.type == b.type
                && a.Quality == b.Quality;
        }
        else
        {
            return a.type == b.type;
        }
    }

    private bool IsValidItemValue(ItemStack a, ItemStack b)
    {
        return IsValidItemValue(a?.itemValue, b?.itemValue);
    }

    private int GetAvailableItems(ItemStack wanted, ItemStack[] bag)
    {
        int available = 0;
        foreach (ItemStack item in bag)
        {
            if (item == null || item.IsEmpty()) continue;
            if (!IsValidItemValue(item, wanted)) continue;
            available += item.count;
            if (available >= wanted.count) break;
        }
        return Utils.FastMin(available, wanted.count);
    }

    // Remove items from container (decreasing count in `taken`)
    private bool RemoveFromContainer(ItemStack[] container, ItemStack taken)
    {
        bool changed = false;
        for (int i = 0; i < container.Length; i += 1)
        {
            // Check if container has the specific item in this slot
            if (container[i] == null || container[i].IsEmpty()) continue;
            if (!IsValidItemValue(container[i], taken)) continue;
            // Check if we have less than we want
            // Full stack will be taken, left empty
            if (container[i].count <= taken.count)
            {
                taken.count -= container[i].count;
                container[i].Clear();
                changed = true;
            }
            // Otherwise stack has all we need
            else if (container[i].count > 0)
            {
                container[i].count -= taken.count;
                taken.count = 0;
                changed = true;
                break;
            }
        }
        return changed;
    }


    private Dictionary<int, int> ingredients = new Dictionary<int, int>();

    public bool GrabRequiredItems(XUiM_PlayerInventory inventory, TileEntityLootContainer loot)
    {
        bool changed = false;
        // Re-use container
        ingredients.Clear();
        // Cant do one by one, as it will not sum up the
        // wanted ingredients (would only add the maximum)
        foreach (PinnedIngredientSDO ingredient in Ingredients)
        {
            int type = ingredient.ItemValue.type;
            var wanted = ingredient.Needed - ingredient.Available;
            if (wanted <= 0) continue; // Already enough?
            if (ingredients.ContainsKey(type))
                ingredients[type] += wanted;
            else ingredients.Add(type, wanted);
        }
        // Try to grab items for all ingredients
        foreach (var ingredient in ingredients)
        {
            var iv = new ItemValue(ingredient.Key);
            var stack = new ItemStack(iv, ingredient.Value);
            int take = stack.count = GetAvailableItems(stack, loot.items);
            // Abort if nothing to be taken
            if (stack.count == 0) continue;
            // Ignore return value, since it will only
            // return true if all items have been added
            // It will still take items partially though
            inventory.AddItem(stack);
            // Check if any items are taken at all
            if (stack.count == take) continue;
            // Calculate items to remove from container
            stack.count = take - stack.count;
            changed |= RemoveFromContainer(loot.items, stack);
        }
        // Re-use container
        ingredients.Clear();
        return changed;
    }

}
