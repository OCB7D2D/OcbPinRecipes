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
