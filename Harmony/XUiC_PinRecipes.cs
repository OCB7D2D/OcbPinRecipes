public class XUiC_PinRecipes : XUiController
{

    public static string ID = "";

    public override void Init()
    {
        base.Init();
        ID = WindowGroup.ID;
        IsDirty = true;
    }

    public override void OnOpen()
    {
        base.OnOpen();
        PinRecipesManager.Instance
            .RegisterWindow(this);
    }

    public override void OnClose()
    {
        base.OnClose();
        PinRecipesManager.Instance
            .UnregisterWindow(this);
    }

    public override void Update(float _dt)
    {
        base.Update(_dt);
        if (!XUi.IsGameRunning()) return;
        if (IsDirty == false) return;
        RefreshBindings();
        IsDirty = false;
    }

    public override bool GetBindingValue(ref string value, string bindingName)
    {
        switch (bindingName)
        {
            case "hasPinnedRecipe":
                if (PinRecipesManager.HasInstance)
                    value = (PinRecipesManager.Instance
                        .Recipes.Count > 0).ToString();
                else
                    value = "false";
                return true;
            case "pinCount":
                if (PinRecipesManager.HasInstance)
                    value = PinRecipesManager.Instance
                        .Recipes.Count.ToString();
                else
                    value = "0";
                return true;
            case "isMenuOpen":
            case "hasCraftArea": // deprecated
                if (PinRecipesManager.HasInstance)
                    value = (PinRecipesManager.Instance
                        .MenusOpen > 0).ToString();
                else
                    value = "false";
                return true;
        }
        value = string.Empty;
        return false;
    }

}
