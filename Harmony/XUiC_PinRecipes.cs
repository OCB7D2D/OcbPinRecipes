using GameEvent.SequenceActions;
using GUI_2;
using InControl;
using UnityEngine;
using XMLData.Parsers;

public class XUiC_PinRecipes : XUiController
{

    public static string ID = string.Empty;

    public KeyCode KeyBinding = KeyCode.None;

    public string CtrlBinding = null;

    public static PlayerAction CtrlAction = null;

    public static UIUtils.ButtonIcon CtrlBtn =
        UIUtils.ButtonIcon.DPadDown;

    public override void Init()
    {
        base.Init();
        KeyBinding = KeyCode.None;
        if (GetChildById("btnGrab") is XUiController grab) grab.OnPress += OnGrab;
        // Must fetch from other node, since window group has no custom attributes
        var attributes = xui?.GetWindow("windowPinRecipes")?.Controller?.CustomAttributes;
        if (attributes.TryGetValue("grab_key_binding", out string value))
            KeyBinding = EnumParser.Parse<KeyCode>(value);
        if (attributes.TryGetValue("grab_ctrl_icon", out string icon))
            CtrlBtn = EnumParser.Parse<UIUtils.ButtonIcon>(icon);
        attributes.TryGetValue("grab_ctrl_binding", out CtrlBinding);
        ID = WindowGroup.ID;
        IsDirty = true;
    }

    public override void Cleanup()
    {
        base.Cleanup();
        if (GetChildById("btnGrab") is XUiController grab) grab.OnPress -= OnGrab;
    }

    public PlayerAction GetDpadAction(string name)
    {
        switch (name)
        {
            case "Left": return xui.playerUI.playerInput.GUIActions.Left;
            case "Right": return xui.playerUI.playerInput.GUIActions.Right;
            case "Up": return xui.playerUI.playerInput.GUIActions.Up;
            case "Down": return xui.playerUI.playerInput.GUIActions.Down;
            case "DPad_Left": return xui.playerUI.playerInput.GUIActions.DPad_Left;
            case "DPad_Right": return xui.playerUI.playerInput.GUIActions.DPad_Right;
            case "DPad_Up": return xui.playerUI.playerInput.GUIActions.DPad_Up;
            case "DPad_Down": return xui.playerUI.playerInput.GUIActions.DPad_Down;
            case "CameraLeft": return xui.playerUI.playerInput.GUIActions.CameraLeft;
            case "CameraRight": return xui.playerUI.playerInput.GUIActions.CameraRight;
            case "CameraUp": return xui.playerUI.playerInput.GUIActions.CameraUp;
            case "CameraDown": return xui.playerUI.playerInput.GUIActions.CameraDown;
            case "Submit": return xui.playerUI.playerInput.GUIActions.Submit;
            case "Cancel": return xui.playerUI.playerInput.GUIActions.Cancel;
            case "HalfStack": return xui.playerUI.playerInput.GUIActions.HalfStack;
            case "Inspect": return xui.playerUI.playerInput.GUIActions.Inspect;
            case "WindowPagingLeft": return xui.playerUI.playerInput.GUIActions.WindowPagingLeft;
            case "WindowPagingRight": return xui.playerUI.playerInput.GUIActions.WindowPagingRight;
            case "PageUp": return xui.playerUI.playerInput.GUIActions.PageUp;
            case "PageDown": return xui.playerUI.playerInput.GUIActions.PageDown;
            case "RightStick": return xui.playerUI.playerInput.GUIActions.RightStick;
            case "LeftStick": return xui.playerUI.playerInput.GUIActions.LeftStick;
            case "None": return null;
            default: throw new System.Exception("Unknown Control Name");
        }
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
        if (CtrlBinding != null) CtrlAction
            = GetDpadAction(CtrlBinding);
    }

    public override void OnClose()
    {
        base.OnClose();
        PinRecipesManager.Instance
            .UnregisterWindow(this);
        CtrlAction = null;
    }

    public override void Update(float _dt)
    {
        base.Update(_dt);
        if (!XUi.IsGameRunning()) return;
        if (PinRecipesManager.HasInstance && PinRecipesManager.IsMenuOpen())
        {
            if (KeyBinding != KeyCode.None && Input.GetKeyDown(KeyBinding))
                PinRecipesManager.Instance.GrabIngredients();
            else if (CtrlAction != null && CtrlAction.WasPressed)
                PinRecipesManager.Instance.GrabIngredients();
        }
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
                    if (PinRecipesManager.IsMenuOpen() && PinRecipesManager.HasRecipes)
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
