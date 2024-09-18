using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public class XUiC_PinnedRecipe : XUiController
{

    private PinnedRecipeSDO RDO;
    public Recipe Recipe => RDO?.Recipe;
    public int Amount => RDO == null ? -1 : RDO.Count;

    // Add count into title (if more than one)
    public string Title =>
        (RDO?.Recipe?.count ?? 1) == 1 ? RDO?.Title
        : string.Format("{0} [ff6033](x{1})[-]",
            RDO?.Title, RDO?.Recipe?.count ?? 1);

    public string IconImg => RDO?.IconImg;
    public string IconTint => RDO?.IconTint;

    public bool IsLocked => RDO != null && RDO.IsLocked;
    public bool IsCraftable => RDO != null && RDO.IsCraftable;
    public bool IsCorrectArea => RDO != null && RDO.CorrectArea;

    private XUiC_PinnedRecipeIngredient[] uiIngredients;

    public void SetRecipe(PinnedRecipeSDO dto)
    {
        RDO = dto;
        ReAssign();
    }

    public void ReAssign()
    {
        if (RDO == null) return;
        // Reset existing ingredients on the UI
        for (int n = 0; n < uiIngredients.Length; n++)
        {
            uiIngredients[n].SetIngredient(null);
        }
        // Add ingredients for the current recipe
        for (int i = 0, n = 0; i < uiIngredients.Length; i++)
        {
            var ingredient = RDO.GetIngredient(i);
            if (ingredient != null && ingredient.Need > 0)
            {
                uiIngredients[n++].SetIngredient(ingredient);
            }
        }
        IsDirty = true;
    }

    public override void OnOpen()
    {
        base.OnOpen();
        PinRecipesManager.Instance
            .RegisterSlot(this);
    }

    public override void OnClose()
    {
        base.OnClose();
        PinRecipesManager.Instance
            .UnregisterSlot(this);
    }

    public override void Init()
    {
        base.Init();
        // Collect all UI placeholders for pinned recipes
        uiIngredients = GetChildrenByType<XUiC_PinnedRecipeIngredient>();
        // Connect our event handlers if elements are found
        if (GetChildById("Unpin") is XUiController unpin) unpin.OnPress += OnUnpin;
        if (GetChildById("Decrement") is XUiController decrement) decrement.OnPress += OnDecrement;
        if (GetChildById("Increment") is XUiController increment) increment.OnPress += OnIncrement;
        if (GetChildById("Craft") is XUiController craft) craft.OnPress += OnCraft;
        OnScroll += HandleScroll;
        IsDirty = true;
    }

    public override void Cleanup()
    {
        base.Cleanup();
        uiIngredients = null;
        if (GetChildById("Unpin") is XUiController unpin) unpin.OnPress -= OnUnpin;
        if (GetChildById("Decrement") is XUiController decrement) decrement.OnPress -= OnDecrement;
        if (GetChildById("Increment") is XUiController increment) increment.OnPress -= OnIncrement;
        if (GetChildById("Craft") is XUiController craft) craft.OnPress -= OnCraft;
        OnScroll -= HandleScroll;
    }

    public override void Update(float _dt)
    {
        base.Update(_dt);
        if (IsDirty == false) return;
        if (!XUi.IsGameRunning()) return;
        ViewComponent.IsVisible = (Recipe != null);
        RefreshBindings();
        IsDirty = false;
    }

    public override bool GetBindingValue(ref string value, string bindingName)
    {
        switch (bindingName)
        {
            case "title":
                value = Title;
                return true;
            case "icon":
                value = IconImg;
                return true;
            case "iconTint":
                value = IconTint;
                return true;
            case "amount":
                value = Amount.ToString();
                return true;
            case "quality":
                value = RDO == null ? "[NA]" :
                    RDO.CraftingTier.ToString();
                return true;
            case "hasQuality":
                value = RDO == null ? "False" :
                    RDO.HasQuality().ToString();
                return true;
            case "isVisible":
                value = (Recipe != null).ToString();
                return true;
            case "canCraft":
                value = (IsCraftable && IsCorrectArea).ToString();
                return true;
            case "isCorrectArea":
                value = (IsCorrectArea).ToString();
                return true;
            case "isRecipeLocked":
                value = (IsLocked).ToString();
                return true;
            case "showDecrement":
                value = (Amount > 1 && PinRecipesManager.
                    Instance.MenusOpen > 0).ToString();
                return true;
            case "showIncrement":
                value = (Amount != -1 && PinRecipesManager.
                    Instance.MenusOpen > 0).ToString();
                return true;
        }
        value = "";
        return false;
    }

    private void OnUnpin(XUiController sender, int mouseButton)
    {
        PinRecipesManager.Instance.UnpinRecipe(RDO);
    }

    private void OnIncrement(XUiController sender, int mouseButton)
    {
        if (RDO == null) return;
        int factor = IsShiftPressed ? 10 : 1;
        RDO.SetCount(RDO.Count + factor);
        SetAllChildrenDirty();
    }

    private void OnDecrement(XUiController sender, int mouseButton)
    {
        if (RDO == null) return;
        if (RDO.Count < 2) return;
        int factor = IsShiftPressed ? 10 : 1;
        int count = RDO.Count - factor;
        if (count < 1) count = 1;
        RDO.SetCount(count);
        SetAllChildrenDirty();
    }

    // Scroll through different recipes for same item
    private void HandleAltScroll(float delta)
    {
        if (RDO == null) return;
        string name = RDO.Recipe.GetName();
        List<Recipe> recipes = CraftingManager.GetRecipes(name);
        recipes.RemoveAll(x => x.ingredients.Count == 0);
        var mgr = PinRecipesManager.OptInstance;
        if (recipes == null || mgr == null) return;
        var idx = recipes.IndexOf(RDO.Recipe);
        if (idx == -1) return;
        idx += (int)(delta * 10f);
        if (idx < 0) idx = recipes.Count - 1;
        if (idx >= recipes.Count) idx = 0;
        recipes[idx].craftingTier = RDO.CraftingTier;
        RDO.UpdateRecipe(recipes[idx]);
        SetRecipe(RDO); // Update Myself
    }

    // Scroll through different qualities for recipe
    private void HandleQualityScroll(float delta)
    {
        if (RDO == null) return;
        // Hardcoded to 6 in vanilla
        var maxq = RDO.MaxQuality();
        if (maxq <= 0) return;
        var tier = RDO.CraftingTier + (int)(delta * 10);
        if (tier < 1) tier = maxq;
        else if (tier > maxq) tier = 1;
        RDO.Recipe.craftingTier =
            RDO.CraftingTier = tier;
        RDO.UpdateRecipe(RDO.Recipe, true);
        SetRecipe(RDO); // Update Myself
    }

    static bool IsAltPressed => Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
    static bool IsShiftPressed => Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

    private void HandleScroll(XUiController sender, float delta)
    {
        if (RDO == null) return;
        var mgr = PinRecipesManager.OptInstance;
        if (mgr == null) return;
        // Handle alternative recipe scroll
        if (IsAltPressed)
        {
            if (IsShiftPressed)
            {
                // Handle quality alternation
                HandleQualityScroll(delta);
            }
            else
            {
                // Handle recipe alternation
                HandleAltScroll(delta);
            }
        }
        else
        {
            // Handle increased amount (shift) scroll
            float factor = IsShiftPressed ? 100f : 10f;
            RDO.SetCount(RDO.Count + (int)(delta * factor));
        }
        SetAllChildrenDirty();
    }

    // Most complex functionality I had to copy in order to support
    // Let's hope I got all the math correct to not allow any cheating ;)
    // Note: maybe we should enforce an update of the cached values?
    private void OnCraft(XUiController _sender, int _mouseButton)
    {
        if (!PinRecipesManager.HasInstance) return;

        // Get the crafting area if one is open
        XUiC_CraftingWindowGroup craftArea =
            PinRecipesManager.Instance.CraftArea;
        // Return if we can't craft anywhere
        if (craftArea == null) return;

        // Check if the recipe is actually unlocked
        if (!XUiM_Recipes.GetRecipeIsUnlocked(xui, Recipe)) return;
        if (!RDO.CraftingRequirementsValid(craftArea)) return;
        // ItemClass klass = ItemClass.GetForId(recipe.itemValueType);

        // Create adjusted recipe
        Recipe _recipe = new Recipe()
        {
            itemValueType = Recipe.itemValueType,
            // Apply craft output count effect (what to actually produce)
            count = XUiM_Recipes.GetRecipeCraftOutputCount(xui, Recipe),
            craftingArea = Recipe.craftingArea,
            craftExpGain = Recipe.craftExpGain,
            craftingTime = XUiM_Recipes.GetRecipeCraftTime(xui, Recipe),
            craftingToolType = Recipe.craftingToolType,
            craftingTier = RDO.CraftingTier,
            tags = Recipe.tags
        };

        EntityPlayerLocal player = xui.playerUI.entityPlayer;
        List<ItemStack> allItemStacks = xui.PlayerInventory.GetAllItemStacks();
        // Process all ingredients and adjust counts
        foreach (var ingredient in RDO.Ingredients)
        {
            ingredient.RecalcNeeded(player);
            var itemValue = ingredient.Ingredient.itemValue;
            if (itemValue.HasQuality)
            {
                // This branch is called for e.g. car batteries
                // Otherwise we don't give correct items back on cancel
                List<ItemValue> available = new List<ItemValue>();
                foreach (var itemStack in allItemStacks)
                {
                    if (itemStack.itemValue.type == itemValue.type)
                        available.Add(itemStack.itemValue.Clone());
                }
                available.Sort((a, b) => a.Quality - b.Quality);
                int len = ingredient.Need == 0 ? 1 : ingredient.Need;
                foreach (var item in available)
                {
                    if (item.type != itemValue.type) continue;
                    _recipe.AddIngredient(item, 1);
                    if (--len == 0) break;
                }
                if (len != 0) return;
            }
            else
            {
                _recipe.AddIngredient(
                    itemValue, ingredient.Need);
            }
            // Weird case, but needed for dedicated server support
            // Seems TFP has "abused" this property a little in
            // order to return the correct stuff back on cancel!?
            // Only required for dynamic `CraftingIngredientCount`
            _recipe.scrapable |= ingredient.Need != ingredient.Ingredient.count;
        }
        // Check if we have the required materials in the inventory
        if (!xui.PlayerInventory.HasItems(_recipe.ingredients, Amount)) return;
        // Enqueue items to crafted after requirements are checked
        if (craftArea.AddItemToQueue(_recipe, Amount))
        {
            if (craftArea is XUiC_WorkstationWindowGroup workstation)
            {
                if (workstation.fuelWindow is XUiC_WorkstationFuelGrid grid)
                {
                    if (RDO.CraftingRequirementsValid(workstation, true))
                    {
                        grid.TurnOn();
                    }
                }
                // Vanilla now gets this info directly?
                // FieldHasQueueChanged.SetValue(workstation, true);
            }
            // Consume the items once we scheduled the crafting
            xui.PlayerInventory.RemoveItems(_recipe.ingredients, Amount);
            // Unpin the recipe, it fulfilled its purpose
            PinRecipesManager.Instance.UnpinRecipe(RDO);
        }
    }

}
