using System.Collections.Generic;

public class XUiC_PinRecipes : XUiController
{

    public static string ID = "";

    private readonly List<XUiC_PinnedRecipe> uiRecpies
        = new List<XUiC_PinnedRecipe>();

    public override void Init()
    {
        base.Init();
        ID = WindowGroup.ID;
        // Collect all UI placeholders for pinned recipes
        foreach (var ui in GetChildrenByType<XUiC_PinnedRecipe>())
        {
            if (ui == null) continue;
            uiRecpies.Add(ui);
        }
    }

    public override void OnOpen()
    {
        base.OnOpen();
        xui.PlayerInventory.OnBackpackItemsChanged += new XUiEvent_BackpackItemsChanged(OnInventoryEvent);
        xui.PlayerInventory.OnToolbeltItemsChanged += new XUiEvent_ToolbeltItemsChanged(OnInventoryEvent);
        QuestEventManager.Current.SkillPointSpent += new QuestEvent_SkillPointSpent(OnQuestEvent);
        QuestEventManager.Current.WindowOpened += new QuestEvent_WindowOpened(OnQuestEvent);
        PinRecipesManager.Instance.RegisterWidget(this);
    }

    public override void OnClose()
    {
        base.OnClose();
        xui.PlayerInventory.OnBackpackItemsChanged -= new XUiEvent_BackpackItemsChanged(OnInventoryEvent);
        xui.PlayerInventory.OnToolbeltItemsChanged -= new XUiEvent_ToolbeltItemsChanged(OnInventoryEvent);
        QuestEventManager.Current.SkillPointSpent -= new QuestEvent_SkillPointSpent(OnQuestEvent);
        QuestEventManager.Current.WindowOpened -= new QuestEvent_WindowOpened(OnQuestEvent);
        PinRecipesManager.Instance.UnregisterWidget(this);
    }

    private void OnQuestEvent(string window)
    {
        if (window == "compass")
        {
            if (PinRecipesManager.HasInstance)
                PinRecipesManager.Instance.SetWidgetsDirty();
            IsDirty = true;
        }
    }

    private void OnInventoryEvent()
    {
        if (PinRecipesManager.HasInstance)
            PinRecipesManager.Instance.SetWidgetsDirty();
        IsDirty = true;
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
                    (PinRecipesManager.Instance.Recipes.Count > 0).ToString();
                else
                    value = "false";
                return true;
            case "pinCount":
                if (PinRecipesManager.HasInstance)
                    PinRecipesManager.Instance.Recipes.Count.ToString();
                else
                    value = "0";
                return true;
        }
        value = "";
        return false;
    }

}
