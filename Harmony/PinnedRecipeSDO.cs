// This Shared Data Object does all the caching for us
// It is passed around the various other components
// Most of its values are read-only by nature
// Only a few parameters are allowed to change

using System;
using System.Collections.Generic;

public class PinnedRecipeSDO
{

    public int Count = 1;
    public int CraftingTier = -1;
    public Recipe Recipe = null;
    public string Title = null;
    public string IconImg = null;
    public string IconTint = null;
    public bool CorrectArea = false;
    public bool IsCraftable = false;
    public bool IsLocked = false;

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

    public bool HasQuality()
    {
        return Recipe != null && !PinRecipes.NoQuality &&
            ItemClass.GetForId(Recipe.itemValueType).HasQuality;
    }

    public int MaxQuality()
    {
        return QualityInfo.qualityColors.Length - 1;
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
        // Also cache a few things from ingredients
        for (int i = 0; i < Ingredients.Count; i++)
            Ingredients[i].RecalcNeeded(player);
        // Check if recipe is locked for user
        IsLocked = !CanCraftQuality() ||
            !Recipe.IsUnlocked(player);
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
        XUiC_CraftingWindowGroup area, int tier = -1)
    {
        Count = count;
        if (tier > 0) CraftingTier =
            recipe.craftingTier = tier;
        UpdateRecipe(recipe);
        // Update `CorrectArea`
        UpdateCraftArea(area);
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

    public bool CraftingRequirementsValid(XUiC_WorkstationWindowGroup win, bool includeFuel = false)
    {
        if (Recipe == null) return false;
        if (win.toolWindow is XUiC_WorkstationToolGrid tools)
            if (tools != null && !tools.HasRequirement(Recipe)) return false;
        if (win.inputWindow is XUiC_WorkstationInputGrid input)
            if (input != null && !input.HasRequirement(Recipe)) return false;
        if (win.outputWindow is XUiC_WorkstationOutputGrid output)
            if (output != null && !output.HasRequirement(Recipe)) return false;
        if (win.fuelWindow is XUiC_WorkstationFuelGrid fuel)
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

    private bool CanCraftQuality()
    {
        if (Recipe == null) return false;
        var player = PinRecipesManager.Instance.Player;
        int craftingTier = (int)EffectManager.GetValue(
            PassiveEffects.CraftingTier,
            _originalValue: 1f,
            _entity: player,
            _recipe: Recipe,
            tags: Recipe.tags);
        return Recipe.craftingTier <= craftingTier;
    }

    private bool HasEnoughCraftingMaterials()
    {
        if (Recipe == null) return false;
        foreach (PinnedIngredientSDO ido in Ingredients)
            if (ido.Available < ido.Needed) return false;
        return true;
    }

    public void UpdateRecipe(Recipe recipe, bool force = false)
    {
        if (Recipe == recipe)
            if (!force) return;
        Recipe = recipe;
        Ingredients.Clear();
        if (Recipe != null)
        {
            if (Recipe.craftingTier > 0)
                CraftingTier = Recipe.craftingTier;
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
        // Update craft area to check if it can be built       
        UpdateCraftArea(PinRecipesManager.OptInstance?.CraftArea);
        // Update user dependent state
        OnUserStatsChanged();
        // Update Inventory state
        OnInventoryChanged();
    }
}
