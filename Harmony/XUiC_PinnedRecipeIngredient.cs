using System;
using System.Collections.Generic;
using UnityEngine;

public class XUiC_PinnedRecipeIngredient : XUiController
{

    private PinnedIngredientSDO IDO;

    public Recipe Recipe => IDO?.Recipe;
    public ItemStack Ingredient => IDO?.Ingredient;

    private static readonly CachedStringFormatterXuiRgbaColor colorFormatter
        = new CachedStringFormatterXuiRgbaColor();

    public void SetIngredient(PinnedIngredientSDO ido)
    {
        IDO = ido;
        IsDirty = true;
    }

    public override void Init()
    {
        base.Init();
        if (GetChildById("Ingredient") is XUiController ingredient)
            ingredient.OnDoubleClick += OnIngredientPin;
    }

    private void OnIngredientPin(XUiController sender, int mouseButton)
    {
        // Play safe and check first
        if (IDO?.Ingredient == null) return;
        ItemStack ingredient = IDO.Ingredient;

        // Get the required delta amount
        int amount = IDO.Needed - IDO.Available;

        // Make sure to always pin one
        if (amount < 1) amount = 1;

        string name = ingredient.itemValue.ItemClass.GetItemName();
        List<Recipe> recipes = CraftingManager.GetRecipes(name);

        // Make sure we don't have any recipes without ingredients
        recipes.RemoveAll(recipe => recipe.ingredients.Count == 0);

        // Make sure we don't have any material based recipes
        recipes.RemoveAll(recipe => recipe.materialBasedRecipe);

        // Check if we have no recipes at all (abort)
        if (recipes.Count == 0)
        {
            GameManager.ShowTooltip(
                XUiM_Player.GetPlayer() as EntityPlayerLocal,
                Localization.Get("ttNoRecipesForItem"));
            return;
        }

        // Check if we have ambiguous recipes
        // ToDo: maybe we can optimize a little?
        // Like checking for same craft area etc
        if (recipes.Count > 1)
        {
            GameManager.ShowTooltip(
                XUiM_Player.GetPlayer() as EntityPlayerLocal,
                Localization.Get("ttManyRecipesForItem"));
        }

        // For now just pin the first recipe we found
        PinRecipesManager.Instance.PinRecipe(recipes[0], amount);
    }

    public override void Update(float _dt)
    {
        base.Update(_dt);
        if (IsDirty == false) return;
        if (!XUi.IsGameRunning()) return;
        ViewComponent.IsVisible = (Ingredient != null);
        ViewComponent.ToolTip = IDO?.Title;
        RefreshBindings(true);
        IsDirty = false;
    }

    public override bool GetBindingValue(ref string value, string bindingName)
    {
        switch (bindingName)
        {
            case "title":
                value = IDO?.Title;
                return true;
            case "needed":
                value = IDO?.Needed.ToString();
                return true;
            case "available":
                value = IDO?.Available.ToString();
                return true;
            case "delta":
                value = (IDO?.Needed - IDO?.Available).ToString();
                return true;
            case "excess":
                value = (IDO?.Available - IDO?.Needed).ToString();
                return true;
            case "icon":
                value = IDO?.IconImg;
                return true;
            case "iconTint":
                value = IDO?.IconTint;
                return true;
            case "isVisible":
                value = (Ingredient != null).ToString();
                return true;
            case "needsMore":
                value = (IDO?.Available < IDO?.Needed).ToString();
                return true;
            case "hasEnough":
                value = (IDO?.Available >= IDO?.Needed).ToString();
                return true;
            case "hasExcess":
                value = (IDO?.Available > IDO?.Needed).ToString();
                return true;
            case "textColor":
                // int delta = Needed - Available;
                var color = new Color32(255, 80, 80, 255);
                value = colorFormatter.Format(color);
                return true;
        }
        value = "";
        return false;
    }

}
