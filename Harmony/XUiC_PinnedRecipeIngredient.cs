using UnityEngine;

public class XUiC_PinnedRecipeIngredient : XUiController
{

    public int Slot = 0;

    public int Index = 0;

    public static string ID = "";

    public int Needed = 9999;
    public int Available = -1;

    private static readonly CachedStringFormatterXuiRgbaColor colorFormatter
        = new CachedStringFormatterXuiRgbaColor();
    private static readonly PinRecipesManager PinManager = PinRecipesManager.Instance;

    public override void Init()
    {
        base.Init();
        ID = WindowGroup.ID;
        IsDirty = true;
    }

    public override void Update(float _dt)
    {
        base.Update(_dt);
        if (IsDirty == false) return;
        if (!XUi.IsGameRunning()) return;
        ViewComponent.IsVisible = IsVisible();
        ViewComponent.ToolTip = GetTitle();
        ViewComponent.IsDirty = true;
        Available = GetAvailable();
        Needed = GetNeeded();
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

    private ItemStack GetIngredient()
    {
        return PinManager.GetRecipeIngredient(Slot, Index);
    }

    private string GetName()
    {
        ItemStack ingredient = GetIngredient();
        if (ingredient == null) return string.Empty;
        return ingredient.itemValue.ItemClass.GetItemName();
    }

    private string GetTitle()
    {
        ItemStack ingredient = GetIngredient();
        if (ingredient == null) return string.Empty;
        return ingredient.itemValue.ItemClass.GetLocalizedItemName();
    }

    private string GetIcon()
    {
        ItemStack ingredient = GetIngredient();
        if (ingredient == null) return string.Empty;
        return ingredient.itemValue.GetPropertyOverride("CustomIcon",
            ingredient.itemValue.ItemClass.GetIconName());
    }

    private string GetIconTint()
    {
        ItemStack ingredient = GetIngredient();
        if (ingredient == null) return colorFormatter.Format(Color.white);
        return colorFormatter.Format(ingredient.itemValue.
            ItemClass.GetIconTint(ingredient.itemValue));
    }

    private bool IsVisible()
    {
        return GetIngredient() != null;
    }

    private int GetAvailable()
    {
        return PinManager.GetAvailableIngredient(Slot, Index, xui);
    }

    private int GetNeeded()
    {
        return PinManager.GetNeededIngredient(Slot, Index, xui);
    }

    public override bool GetBindingValue(ref string value, string bindingName)
    {
        switch (bindingName)
        {
            case "title":
                value = GetTitle();
                return true;
            case "needed":
                value = Needed.ToString();
                return true;
            case "available":
                value = Available.ToString();
                return true;
            case "delta":
                value = (Needed - Available).ToString();
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
            case "needsMore":
                value = (Available < Needed).ToString();
                return true;
            case "hasEnough":
                value = (Available >= Needed).ToString();
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

    public override bool ParseAttribute(string name, string value, XUiController _parent)
    {
        switch (name)
        {
            case "index":
                Index = int.Parse(value);
                return true;
            default:
                return base.ParseAttribute(
                    name, value, _parent);
        }
    }

}
