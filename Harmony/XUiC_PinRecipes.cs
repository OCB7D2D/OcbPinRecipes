using System.Collections.Generic;
using UnityEngine;

public class XUiC_PinRecipes : XUiController
{
    // private XUiC_OcbPowerSourceStats PowerSourceStats;

    public static string ID = "";

    private readonly CachedStringFormatterXuiRgbaColor colorFormatter = new CachedStringFormatterXuiRgbaColor();

    // private List<XUiController> ingredientEntries = new List<XUiController>();

    public override void Init()
    {
        base.Init();
        ID = WindowGroup.ID;
        if (GetChildById("Unpin0") is XUiController unpin0)
            unpin0.OnPress += new XUiEvent_OnPressEventHandler(OnUnpin0);
        if (GetChildById("Unpin1") is XUiController unpin1)
            unpin1.OnPress += new XUiEvent_OnPressEventHandler(OnUnpin1);
        if (GetChildById("Unpin2") is XUiController unpin2)
            unpin2.OnPress += new XUiEvent_OnPressEventHandler(OnUnpin2);
        if (GetChildById("Unpin3") is XUiController unpin3)
            unpin3.OnPress += new XUiEvent_OnPressEventHandler(OnUnpin3);
        if (GetChildById("Unpin4") is XUiController unpin4)
            unpin4.OnPress += new XUiEvent_OnPressEventHandler(OnUnpin4);
        if (GetChildById("Unpin5") is XUiController unpin5)
            unpin5.OnPress += new XUiEvent_OnPressEventHandler(OnUnpin5);
        if (GetChildById("Unpin6") is XUiController unpin6)
            unpin6.OnPress += new XUiEvent_OnPressEventHandler(OnUnpin6);
        if (GetChildById("Unpin7") is XUiController unpin7)
            unpin7.OnPress += new XUiEvent_OnPressEventHandler(OnUnpin7);
        if (GetChildById("Unpin8") is XUiController unpin8)
            unpin8.OnPress += new XUiEvent_OnPressEventHandler(OnUnpin8);
        if (GetChildById("Unpin9") is XUiController unpin9)
            unpin9.OnPress += new XUiEvent_OnPressEventHandler(OnUnpin9);
        IsDirty = true;
    }

    private void OnUnpin0(XUiController _sender, int _mouseButton)
    {
        if (PinRecipes.Recipes.Count < 1) return;
        PinRecipes.Recipes.RemoveAt(0);
        PinRecipes.IsDirty = true;
    }
    private void OnUnpin1(XUiController _sender, int _mouseButton)
    {
        if (PinRecipes.Recipes.Count < 2) return;
        PinRecipes.Recipes.RemoveAt(1);
        PinRecipes.IsDirty = true;
    }
    private void OnUnpin2(XUiController _sender, int _mouseButton)
    {
        if (PinRecipes.Recipes.Count < 3) return;
        PinRecipes.Recipes.RemoveAt(2);
        PinRecipes.IsDirty = true;
    }
    private void OnUnpin3(XUiController _sender, int _mouseButton)
    {
        if (PinRecipes.Recipes.Count < 4) return;
        PinRecipes.Recipes.RemoveAt(3);
        PinRecipes.IsDirty = true;
    }
    private void OnUnpin4(XUiController _sender, int _mouseButton)
    {
        if (PinRecipes.Recipes.Count < 5) return;
        PinRecipes.Recipes.RemoveAt(4);
        PinRecipes.IsDirty = true;
    }
    private void OnUnpin5(XUiController _sender, int _mouseButton)
    {
        if (PinRecipes.Recipes.Count < 6) return;
        PinRecipes.Recipes.RemoveAt(5);
        PinRecipes.IsDirty = true;
    }
    private void OnUnpin6(XUiController _sender, int _mouseButton)
    {
        if (PinRecipes.Recipes.Count < 7) return;
        PinRecipes.Recipes.RemoveAt(6);
        PinRecipes.IsDirty = true;
    }
    private void OnUnpin7(XUiController _sender, int _mouseButton)
    {
        if (PinRecipes.Recipes.Count < 8) return;
        PinRecipes.Recipes.RemoveAt(7);
        PinRecipes.IsDirty = true;
    }
    private void OnUnpin8(XUiController _sender, int _mouseButton)
    {
        if (PinRecipes.Recipes.Count < 9) return;
        PinRecipes.Recipes.RemoveAt(8);
        PinRecipes.IsDirty = true;
    }
    private void OnUnpin9(XUiController _sender, int _mouseButton)
    {
        if (PinRecipes.Recipes.Count < 10) return;
        PinRecipes.Recipes.RemoveAt(9);
        PinRecipes.IsDirty = true;
    }

    public override void OnOpen()
    {
        base.OnOpen();
        xui.PlayerInventory.OnBackpackItemsChanged += new XUiEvent_BackpackItemsChanged(this.PlayerInventory_OnBackpackItemsChanged);
        xui.PlayerInventory.OnToolbeltItemsChanged += new XUiEvent_ToolbeltItemsChanged(this.PlayerInventory_OnToolbeltItemsChanged);
    }

    public override void OnClose()
    {
        base.OnClose();
        xui.PlayerInventory.OnBackpackItemsChanged -= new XUiEvent_BackpackItemsChanged(this.PlayerInventory_OnBackpackItemsChanged);
        xui.PlayerInventory.OnToolbeltItemsChanged -= new XUiEvent_ToolbeltItemsChanged(this.PlayerInventory_OnToolbeltItemsChanged);
    }

    private void PlayerInventory_OnToolbeltItemsChanged() => IsDirty = true;

    private void PlayerInventory_OnBackpackItemsChanged() => IsDirty = true;

    public override void Update(float _dt)
    {
        IsDirty = IsDirty || PinRecipes.IsDirty;
        if (IsDirty == false) return;
        RefreshBindings();
        base.Update(_dt);
        PinRecipes.IsDirty = false;
        IsDirty = PinRecipes.IsDirty;
    }
    private float GetMaterialAvailable(int queue, int idx)
    {
        if (PinRecipes.Recipes == null) return -999; 
        if (PinRecipes.Recipes.Count <= queue) return -99;
        if (PinRecipes.Recipes[queue].ingredients.Count <= idx) return -9;
        return xui.PlayerInventory.GetItemCount(
            PinRecipes.Recipes[queue].ingredients[idx].itemValue);
    }

    private float GetMaterialNeeded(int queue, int idx)
    {
        if (PinRecipes.Recipes == null) return -999;
        if (PinRecipes.Recipes.Count <= queue) return -99;
        if (PinRecipes.Recipes[queue].ingredients.Count <= idx) return -9;
        // I hope I copied the following code correctly
        // Should take tier into account for what's needed
        // Then reach out to player inventory for what we have
        float tier = EffectManager.GetValue(
            PassiveEffects.CraftingTier,
            _originalValue: 1f,
            _entity: xui.playerUI.entityPlayer,
            _recipe: PinRecipes.Recipes[queue],
            tags: PinRecipes.Recipes[queue].tags);
        var itemStack = PinRecipes.Recipes[queue].ingredients[idx];
        return EffectManager.GetValue(
            PassiveEffects.CraftingIngredientCount,
            _originalValue: itemStack.count,
            _entity: xui.playerUI.entityPlayer,
            _recipe: PinRecipes.Recipes[queue],
            tags: FastTags.Parse(itemStack.itemValue.ItemClass.GetItemName()),
            craftingTier: (int)tier);
    }

    private string GetMaterialIcon(int queue, int idx)
    {
        if (PinRecipes.Recipes == null) return "";
        if (PinRecipes.Recipes.Count <= queue) return "";
        if (PinRecipes.Recipes[queue].ingredients.Count <= idx) return "";
        ItemValue itemValue = PinRecipes.Recipes[queue].ingredients[idx].itemValue;
        return itemValue.GetPropertyOverride("CustomIcon", itemValue.ItemClass.GetIconName());
    }

    private string GetMaterialIconTint(int queue, int idx)
    {
        Color32 tint = Color.white;
        if (PinRecipes.Recipes == null) return colorFormatter.Format(Color.white);
        if (PinRecipes.Recipes.Count <= queue) return colorFormatter.Format(Color.white);
        if (PinRecipes.Recipes[queue].ingredients.Count <= idx) return colorFormatter.Format(Color.white);
        ItemValue itemValue = PinRecipes.Recipes[queue].ingredients[idx].itemValue;
        return colorFormatter.Format(itemValue.ItemClass.GetIconTint(itemValue));
    }

    private string GetMaterialVisible(int queue, int idx)
    {
        if (PinRecipes.Recipes == null) return "false";
        if (PinRecipes.Recipes.Count <= queue) return "false";
        if (PinRecipes.Recipes[queue].ingredients.Count <= idx) return "false";
        return "true";
    }


    private bool GetMaterialHasAll(int queue, int idx)
    {
        if (PinRecipes.Recipes == null) return false;
        if (PinRecipes.Recipes.Count <= queue) return false;
        if (PinRecipes.Recipes[queue].ingredients.Count <= idx) return false;
        float available = GetMaterialAvailable(queue, idx);
        float needed = GetMaterialNeeded(queue, idx);
        return available >= needed;

    }

    private string GetMaterialCount(int queue, int idx)
    {
        if (PinRecipes.Recipes == null) return "nr";
        if (PinRecipes.Recipes.Count <= queue) return "nq";
        if (PinRecipes.Recipes[queue].ingredients.Count <= idx) return "na";
        return (GetMaterialNeeded(queue, idx) -
                GetMaterialAvailable(queue, idx)
            ).ToString();
    }

    private string GetMaterialTxtCol(int queue, int idx)
    {
        if (PinRecipes.Recipes == null) return "[red]";
        if (PinRecipes.Recipes.Count <= queue) return "[red]";
        if (PinRecipes.Recipes[queue].ingredients.Count <= idx) return "[red]";
        float available = GetMaterialAvailable(queue, idx);
        float needed = GetMaterialNeeded(queue, idx);
        Color color = available < needed ? Color.red : Color.green;
        return colorFormatter.Format(color);
    }

    private string GetItemIcon(int queue)
    {
        if (PinRecipes.Recipes == null) return "";
        if (PinRecipes.Recipes.Count <= queue) return "";
        ItemValue itemValue = new ItemValue(PinRecipes.Recipes[queue].itemValueType);
        return itemValue.GetPropertyOverride("CustomIcon", itemValue.ItemClass.GetIconName());
    }

    private string GetItemTitle(int queue)
    {
        if (PinRecipes.Recipes == null) return "";
        if (PinRecipes.Recipes.Count <= queue) return "";
        if (PinRecipes.Recipes[queue].materialBasedRecipe)
        {
            // str1 = !Localization.Exists("lbl" + PinRecipes.Recipes[queue].Material) ? XUi.UppercaseFirst(this.material) : Localization.Get("lbl" + this.material);
            return "Mat";
        }
        else
        {
            ItemValue itemValue = new ItemValue(PinRecipes.Recipes[queue].itemValueType);
            return itemValue.ItemClass.GetLocalizedItemName();
        }
    }

    private string GetItemIconTint(int queue)
    {
        if (PinRecipes.Recipes == null) return colorFormatter.Format(Color.white);
        if (PinRecipes.Recipes.Count <= queue) return colorFormatter.Format(Color.white);
        ItemValue itemValue = new ItemValue(PinRecipes.Recipes[queue].itemValueType);
        return colorFormatter.Format(itemValue.ItemClass.GetIconTint(itemValue));
    }

    private string GetItemVisible(int queue)
    {
        if (PinRecipes.Recipes == null) return "false";
        if (PinRecipes.Recipes.Count <= queue) return "false";
        return "true";
    }

    public override bool GetBindingValue(ref string value, string bindingName)
    {

        if (bindingName.StartsWith("item"))
        {
            int i = int.Parse(bindingName.Substring(4, 1));
            switch (bindingName.Substring(5))
            {
                case "title":
                    value = GetItemTitle(i);
                    return true;
                case "icon":
                    value = GetItemIcon(i);
                    return true;
                case "icontint":
                    value = GetItemIconTint(i);
                    return true;
                case "visible":
                    value = GetItemVisible(i);
                    return true;
            }
        }
        else if (bindingName.StartsWith("material"))
        {
            int i = int.Parse(bindingName.Substring(8, 1));
            int n = int.Parse(bindingName.Substring(9, 1));
            switch (bindingName.Substring(10))
            {
                case "icon":
                    value = GetMaterialIcon(i, n - 1);
                    return true;
                case "tint":
                    value = GetMaterialIconTint(i, n - 1);
                    return true;
                case "visible":
                    value = GetMaterialVisible(i, n - 1);
                    return true;
                case "count":
                    value = GetMaterialCount(i, n - 1);
                    return true;
                case "needmore":
                    value = GetMaterialHasAll(i, n - 1) ? "false" : "true";
                    return true;
                case "fulfill":
                    value = GetMaterialHasAll(i, n - 1) ? "true" : "false";
                    return true;
                case "txtcol":
                    value = GetMaterialTxtCol(i, n - 1);
                    return true;
            }
        }
        Log.Warning("Counldn't get " + bindingName);
        value = "";
        return false;
    }

}
