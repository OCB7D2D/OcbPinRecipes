using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class XUiC_PinnedRecipe : XUiController
{

    private int Slot = 0;

    public static string ID = "";

    private readonly List<XUiC_PinnedRecipeIngredient> uiIngredients
        = new List<XUiC_PinnedRecipeIngredient>();
    private static readonly CachedStringFormatterXuiRgbaColor colorFormatter
        = new CachedStringFormatterXuiRgbaColor();
    private static readonly PinRecipesManager PinManager = PinRecipesManager.Instance;

    private void OnIncrement(XUiController _sender, int _mouseButton)
    {
        PinRecipesManager.Instance.IncrementCount(Slot);
    }

    private void OnDecrement(XUiController _sender, int _mouseButton)
    {
        PinRecipesManager.Instance.DecrementCount(Slot);
    }

    private void OnUnpin(XUiController _sender, int _mouseButton)
    {
        PinRecipesManager.Instance.UnpinRecipe(Slot);
    }

    static readonly FieldInfo FieldFuelWindow = AccessTools.Field(typeof(XUiC_WorkstationWindowGroup), "fuelWindow");

    private void OnCraft(XUiController _sender, int _mouseButton)
    {
        Recipe recipe = GetRecipe();

        // Try to get the crafting window (only have one!?)
        XUiC_CraftingWindowGroup craftWin = GetOpenCraftingWindow();
        if (craftWin == null) return;

        // Check if the recipe is actually unlocked
        if (!XUiM_Recipes.GetRecipeIsUnlocked(xui, recipe)) return;
        // EntityPlayerLocal entityPlayer = xui.playerUI.entityPlayer;
        if (!craftWin.CraftingRequirementsValid(recipe)) return;
        // ItemClass klass = ItemClass.GetForId(recipe.itemValueType);

        // Get the crafting tier to apply effects
        int tier = (int)EffectManager.GetValue(
            PassiveEffects.CraftingTier,
            _originalValue: 1f,
            _entity: xui.playerUI.entityPlayer,
            _recipe: recipe,
            tags: recipe.tags);

        // Create adjusted recipe
        Recipe _recipe = new Recipe()
        {
            itemValueType = recipe.itemValueType,
            // Apply craft output count effect (what to actually produce)
            count = XUiM_Recipes.GetRecipeCraftOutputCount(xui, recipe),
            craftingArea = recipe.craftingArea,
            craftExpGain = recipe.craftExpGain,
            craftingTime = XUiM_Recipes.GetRecipeCraftTime(xui, recipe),
            craftingToolType = recipe.craftingToolType,
            tags = recipe.tags
        };

        List<ItemStack> allItemStacks = xui.PlayerInventory.GetAllItemStacks();
        // Process all ingredients and adjust counts
        foreach (var ingredient in recipe.ingredients)
        {
            int count = ingredient.count;
            if (recipe.UseIngredientModifier)
                count = (int)EffectManager.GetValue(
                    PassiveEffects.CraftingIngredientCount,
                    _originalValue: ingredient.count,
                    _entity: xui.playerUI.entityPlayer,
                    _recipe: recipe,
                    tags: FastTags.Parse(ingredient.itemValue.ItemClass.GetItemName()),
                    craftingTier: tier);
            // Count was adjusted by effect?
            if (count != ingredient.count)
                _recipe.scrapable = true;
            if (!ingredient.itemValue.HasQuality)
            {
                _recipe.AddIngredient(ingredient.itemValue, count);
            }
            else
            {
                Log.Warning("Ingredient without Quality found, use-case is untested");
                Log.Warning("Please report detailed reproduction case to:");
                Log.Error("https://github.com/OCB7D2D/OcbPinRecipes/issues");
                // Code below is copied to my best knowledge (untested)
                int len = count == 0 ? 1 : count;
                for (int i = 0; i < len; ++i)
                {
                    foreach (var itemStack in allItemStacks)
                    {
                        if (itemStack.itemValue.type != ingredient.itemValue.type) continue;
                        _recipe.AddIngredient(itemStack.itemValue.Clone(), 1);
                        break;
                    }
                }
            }
        }

        int muliplier = GetRecipeCount();
        if (!xui.PlayerInventory.HasItems(_recipe.ingredients, muliplier)) return;
        // XUiC_WorkstationInputGrid childByType = this.craftCountControl.WindowGroup.Controller.GetChildByType<XUiC_WorkstationInputGrid>();
        // if (childByType != null) flag2 |= childByType.HasItems(recipe.ingredients, count2);
        if (craftWin.AddItemToQueue(_recipe, muliplier))
        {
            if (craftWin is XUiC_WorkstationWindowGroup)
            {
                if (FieldFuelWindow.GetValue(craftWin) is XUiC_WorkstationFuelGrid grid)
                {
                    grid.TurnOn();
                }
            }
            // Should trigger to turn it on
            // craftWin.AddItemToQueue(null);
            // if (childByType != null) childByType.
            //   RemoveItems(_recipe.ingredients, multiplier);
            // else
            xui.PlayerInventory.RemoveItems(_recipe.ingredients, muliplier);
            PinRecipesManager.Instance.UnpinRecipe(Slot);
        }
        // else WarnQueueFull();
        craftWin.WindowGroup.Controller.SetAllChildrenDirty();
        SetAllChildrenDirty();
    }

    public override void Init()
    {
        base.Init();
        ID = WindowGroup.ID;
        // Collect all UI placeholders for pinned recipes
        foreach (var ui in GetChildrenByType<XUiC_PinnedRecipeIngredient>())
        {
            if (ui == null) continue;
            uiIngredients.Add(ui);
            ui.Slot = Slot;
        }
        if (GetChildById("Unpin") is XUiController unpin)
            unpin.OnPress += new XUiEvent_OnPressEventHandler(OnUnpin);
        if (GetChildById("Decrement") is XUiController decrement)
            decrement.OnPress += new XUiEvent_OnPressEventHandler(OnDecrement);
        if (GetChildById("Increment") is XUiController increment)
            increment.OnPress += new XUiEvent_OnPressEventHandler(OnIncrement);
        if (GetChildById("Craft") is XUiController craft)
            craft.OnPress += new XUiEvent_OnPressEventHandler(OnCraft);
        IsDirty = true;
    }

    public override void Update(float _dt)
    {
        base.Update(_dt);
        if (IsDirty == false) return;
        if (!XUi.IsGameRunning()) return;
        ViewComponent.IsVisible =
            (GetRecipe() != null);
        RefreshBindings();
        IsDirty = false;
    }

    private Recipe GetRecipe()
    {
        return PinManager.GetRecipe(Slot);
    }
    private int GetRecipeCount()
    {
        return PinManager.GetRecipeCount(Slot);
    }

    private string GetIcon()
    {
        Recipe recipe = GetRecipe();
        if (recipe == null) return string.Empty;
        ItemValue itemValue = new ItemValue(recipe.itemValueType);
        return itemValue.GetPropertyOverride("CustomIcon",
            itemValue.ItemClass.GetIconName());
    }

    private string GetTitle()
    {
        Recipe recipe = GetRecipe();
        if (recipe == null) return string.Empty;
        return Localization.Get(recipe.GetName());
    }

    private string GetIconTint()
    {
        Recipe recipe = GetRecipe();
        if (recipe == null) return string.Empty;
        ItemValue itemValue = new ItemValue(recipe.itemValueType);
        return colorFormatter.Format(itemValue.ItemClass.GetIconTint(itemValue));
    }

    private bool IsVisible()
    {
        return GetRecipe() != null;
    }

    private XUiC_CraftingWindowGroup GetOpenCraftingWindow()
    {
        foreach (var window in xui.GetChildrenByType<XUiC_CraftingWindowGroup>())
        {
            if (window.WindowGroup == null) continue;
            if (!window.WindowGroup.isShowing) continue;
            return window;
        }
        return null;
    }

    private bool HasEnoughCraftingMaterials(XUiC_CraftingWindowGroup win, Recipe recipe)
    {
        if (recipe == null) return false;
        for (int idx = 0; idx < recipe.ingredients.Count; idx += 1)
        {
            int needed = PinRecipesManager.Instance.GetNeededIngredient(Slot, idx, xui);
            int available = PinRecipesManager.Instance.GetAvailableIngredient(Slot, idx, xui);
            if (available < needed) return false;
        }
        return true;
    }

    private bool IsCorrectCraftingArea(XUiC_CraftingWindowGroup win, Recipe recipe)
    {
        if (win == null || win.Workstation == null) return false;
        // Copied from XUiC_ItemActionList::SetCraftingActionList
        Block block = Block.GetBlockByName(win.Workstation);
        if (block != null && block.Properties.Values
            .ContainsKey("Workstation.CraftingAreaRecipes"))
        {
            string str = block.Properties.Values[
                "Workstation.CraftingAreaRecipes"];
            string[] areas = new string[1] { str };
            if (str.Contains(","))
            {
                areas = str
                    .Replace(", ", ",")
                    .Replace(" ,", ",")
                    .Replace(" , ", ",")
                    .Split(',');
            }
            foreach (var area in areas)
            {
                if (area.EqualsCaseInsensitive(recipe.craftingArea))
                {
                    return true;
                }
                else if (area.EqualsCaseInsensitive("player"))
                {
                    if (recipe.craftingArea == null) return true;
                    if (recipe.craftingArea == string.Empty) return true;
                }
            }
        }
        else
        {
            return win.Workstation.EqualsCaseInsensitive(recipe.craftingArea);
        }
        return false;
    }

    private bool CanCraft()
    {
        Recipe recipe = GetRecipe();
        if (recipe == null) return false;
        var craftWin = GetOpenCraftingWindow();
        if (craftWin == null) return false;
        if (!XUiM_Recipes.GetRecipeIsUnlocked(xui, recipe)) return false;
        if (!craftWin.CraftingRequirementsValid(recipe)) return false;
        if (!IsCorrectCraftingArea(craftWin, recipe)) return false;
        if (!HasEnoughCraftingMaterials(craftWin, recipe)) return false;
        return true;
    }

    public override bool GetBindingValue(ref string value, string bindingName)
    {
        switch (bindingName)
        {
            case "title":
                value = GetTitle();
                return true;
            case "icon":
                value = GetIcon();
                return true;
            case "iconTint":
                value = GetIconTint();
                return true;
            case "amount":
                value = GetRecipeCount().ToString();
                return true;
            case "isVisible":
                value = IsVisible().ToString();
                return true;
            case "canCraft":
                value = CanCraft().ToString();
                return true;
            case "showDecrement":
                value = (GetRecipeCount() > 1 &&
                    GetOpenCraftingWindow() != null).ToString();
                return true;
            case "hasCraftArea":
                value = (GetOpenCraftingWindow() != null).ToString();
                return true;
        }
        value = "";
        return false;
    }

    public override bool ParseAttribute(string name, string value, XUiController _parent)
    {
        switch (name)
        {
            case "slot":
                Slot = int.Parse(value);
                return true;
            default:
                return base.ParseAttribute(
                    name, value, _parent);
        }
    }

}
