using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;

public class PinRecipesManager
{

    private static PinRecipesManager instance;

    public bool IsDirty = true;

    public List<PinnedRecipeDTO> Recipes = new List<PinnedRecipeDTO>();

    public List<XUiController> widgets = new List<XUiController>();

    public static byte FileVersion = 1;

    public byte CurrentFileVersion { get; set; }

    public static PinRecipesManager Instance
    {
        get
        {
            if (instance != null) return instance;
            return new PinRecipesManager();
        }
    }

    public static bool HasInstance => instance != null;

    private PinRecipesManager()
    {
        instance = this;
    }

    public static XUiC_CraftingWindowGroup GetOpenCraftingWindow(XUi xui)
    {
        if (instance == null) return null;
        foreach (var window in xui.GetChildrenByType<XUiC_CraftingWindowGroup>())
        {
            if (window == null) continue;
            if (window.WindowGroup == null) continue;
            if (window.WindowGroup.isShowing) return window;
        }
        return null;
    }

    static readonly FieldInfo FieldToolWindow = AccessTools.Field(typeof(XUiC_WorkstationWindowGroup), "toolWindow");
    static readonly FieldInfo FieldInputWindow = AccessTools.Field(typeof(XUiC_WorkstationWindowGroup), "inputWindow");
    static readonly FieldInfo FieldOutputWindow = AccessTools.Field(typeof(XUiC_WorkstationWindowGroup), "outputWindow");
    static readonly FieldInfo FieldFuelWindow = AccessTools.Field(typeof(XUiC_WorkstationWindowGroup), "fuelWindow");

    public static bool CraftingRequirementsValid(XUiC_WorkstationWindowGroup win, Recipe recipe, bool includeFuel = false)
    {
        if (FieldToolWindow.GetValue(win) is XUiC_WorkstationToolGrid tools)
            if (tools != null && !tools.HasRequirement(recipe)) return false;
        if (FieldInputWindow.GetValue(win) is XUiC_WorkstationInputGrid input)
            if (input != null && !input.HasRequirement(recipe)) return false;
        if (FieldOutputWindow.GetValue(win) is XUiC_WorkstationOutputGrid output)
            if (output != null && !output.HasRequirement(recipe)) return false;
        if (includeFuel && FieldFuelWindow.GetValue(win) is XUiC_WorkstationFuelGrid fuel)
            if (fuel != null && !fuel.HasRequirement(recipe)) return false;
        return true;
    }

    public static bool CraftingRequirementsValid(XUiC_CraftingWindowGroup win, Recipe recipe, bool includeFuel = false)
    {
        if (win is XUiC_WorkstationWindowGroup workstation)
        {
            return CraftingRequirementsValid(workstation, recipe, includeFuel);
        }
        return true;
    }

    public void SetWidgetsDirty()
    {
        foreach (var widget in widgets)
            widget.IsDirty = true;
        IsDirty = true;
    }

    public void RegisterWidget(XUiController widget)
    {
        widgets.Add(widget);
        SetWidgetsDirty();
    }

    public void UnregisterWidget(XUiController widget)
    {
        widgets.Remove(widget);
        SetWidgetsDirty();
    }

    public void PinRecipe(Recipe recipe, int count = 1)
    {
        Recipes.Add(new PinnedRecipeDTO(recipe, count));
        SetWidgetsDirty();
    }

    public void UnpinRecipe(int slot)
    {
        if (Recipes.Count <= slot) return;
        Recipes.RemoveAt(slot);
        SetWidgetsDirty();
    }

    public void IncrementCount(int slot)
    {
        if (Recipes.Count <= slot) return;
        Recipes[slot].Count += 1;
        SetWidgetsDirty();
    }
    public void DecrementCount(int slot)
    {
        if (Recipes.Count <= slot) return;
        if (Recipes[slot].Count < 2) return;
        Recipes[slot].Count -= 1;
        SetWidgetsDirty();
    }

    public Recipe GetRecipe(int slot)
    {
        return Recipes.Count <= slot ?
            null : Recipes[slot].Recipe;
    }

    public int GetRecipeCount(int slot)
    {
        return Recipes.Count <= slot ?
            -1 : Recipes[slot].Count;
    }

    public int GetAvailableIngredient(int slot, int idx, XUi xui)
    {
        ItemStack ingredient = GetRecipeIngredient(slot, idx);
        if (ingredient == null) return -1;
        return xui.PlayerInventory.GetItemCount(
            ingredient.itemValue);
    }

    public int GetNeededIngredient(int slot, int idx, XUi xui)
    {
        Recipe recipe = GetRecipe(slot);
        if (recipe == null) return -1;
        ItemStack ingredient = GetRecipeIngredient(slot, idx);
        if (ingredient == null) return 999999;
        // I hope I copied the following code correctly
        // Should take tier into account for what's needed
        // Then reach out to player inventory for what we have
        float tier = EffectManager.GetValue(
            PassiveEffects.CraftingTier,
            _originalValue: 1f,
            _entity: xui.playerUI.entityPlayer,
            _recipe: recipe,
            tags: recipe.tags);
        int needed = (int)EffectManager.GetValue(
            PassiveEffects.CraftingIngredientCount,
            _originalValue: ingredient.count,
            _entity: xui.playerUI.entityPlayer,
            _recipe: recipe,
            tags: FastTags.Parse(ingredient.itemValue.ItemClass.GetItemName()),
            craftingTier: (int)tier);
        return needed * GetRecipeCount(slot);
    }

    public ItemStack GetRecipeIngredient(int slot, int index)
    {
        Recipe recipe = GetRecipe(slot);
        if (recipe == null) return null;
        if (recipe.ingredients == null) return null;
        if (recipe.ingredients.Count <= index) return null;
        return recipe.ingredients[index];
    }

    public void WritePlayerData(PooledBinaryWriter bw)
    {
        bw.Write(FileVersion);
        bw.Write(Recipes.Count);
        foreach (PinnedRecipeDTO recipe in Recipes)
        {
            bw.Write(recipe.Count);
            bw.Write(recipe.Recipe.GetName());
        }
    }

    public void ReadPlayerData(PooledBinaryReader br)
    {
        // Check if we have additional data to be read
        // This way we should be able to upgrade the stream if needed
        if (br.BaseStream.Position >= br.BaseStream.Length)
        {
            Log.Warning("Vanilla game detected, user data will be upgraded");
            return;
        }
        Recipes.Clear();
        CurrentFileVersion = br.ReadByte();
        int count = br.ReadInt32();
        for (int index = 0; index < count; ++index)
        {
            int multiply = br.ReadInt32();
            string name = br.ReadString();
            if (CraftingManager.GetRecipe(name) is Recipe recipe)
            {
                Recipes.Add(new PinnedRecipeDTO(recipe, multiply));
            }
        }
    }

}
