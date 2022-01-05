using System.Collections.Generic;
using System.IO;
using UnityEngine;

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

    public void SetWidgetsDirty()
    {
        foreach (var widget in widgets)
            widget.SetAllChildrenDirty(true);
    }

    public void RegisterWidget(XUiController widget)
    {
        widgets.Add(widget);
        widget.SetAllChildrenDirty(true);
    }

    public void UnregisterWidget(XUiController widget)
    {
        widgets.Remove(widget);
        widget.SetAllChildrenDirty(true);
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
