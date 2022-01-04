using System.Collections.Generic;
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

    private void OnUnpin(XUiController _sender, int _mouseButton)
    {
        PinRecipesManager.Instance.UnpinRecipe(Slot);
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
        IsDirty = true;
    }

    public override void Update(float _dt)
    {
        if (IsDirty == false) return;
        if (!XUi.IsGameRunning()) return;
        ViewComponent.IsVisible =
            (GetRecipe() != null);
        RefreshBindings();
        base.Update(_dt);
        IsDirty = false;
    }

    private Recipe GetRecipe()
    {
        return PinManager.GetRecipe(Slot);
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
            case "isVisible":
                value = IsVisible().ToString();
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
