using UnityEngine;
using XMLData.Parsers;

public class XUiC_PinRecipes : XUiController
{

    public static string ID = string.Empty;

    public KeyCode KeyBinding = KeyCode.None;

    public override void Init()
    {
        base.Init();
        KeyBinding = KeyCode.None;
        if (GetChildById("btnGrab") is XUiController grab) grab.OnPress += OnGrab;
        // Must fetch from other node, since window group has no custom attributes
        var attributes = xui?.GetWindow("windowPinRecipes")?.Controller?.CustomAttributes;
        if (attributes.TryGetValue("grab_key_binding", out string value))
            KeyBinding = EnumParser.Parse<KeyCode>(value);
        ID = WindowGroup.ID;
        IsDirty = true;
    }

    public override void Cleanup()
    {
        base.Cleanup();
        if (GetChildById("btnGrab") is XUiController grab) grab.OnPress -= OnGrab;
    }

    private void OnGrab(XUiController sender, int mouseButton)
    {
        PinRecipesManager.OptInstance?.GrabIngredients();
    }

    public override void OnOpen()
    {
        base.OnOpen();
        IsDirty = true;
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
        if (KeyBinding != KeyCode.None && Input.GetKeyDown(KeyBinding))
            PinRecipesManager.OptInstance?.GrabIngredients();
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
            case "hasContainer":
                value = false.ToString();
                // Checking xui.lootContainer seems to early for here
                if (xui?.playerUI?.windowManager is GUIWindowManager mgr)
                    if (PinRecipesManager.IsMenuOpen())
                        value = (mgr.IsWindowOpen("looting") ||
                            xui?.vehicle?.bag != null).ToString();
                return true;
            case "isMenuOpen":
            case "hasCraftArea": // deprecated
                value = PinRecipesManager.
                    IsMenuOpen().ToString();
                return true;
        }
        value = string.Empty;
        return false;
    }

}
