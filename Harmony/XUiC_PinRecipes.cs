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
        xui.PlayerInventory.OnBackpackItemsChanged += new XUiEvent_BackpackItemsChanged(PlayerInventory_OnBackpackItemsChanged);
        xui.PlayerInventory.OnToolbeltItemsChanged += new XUiEvent_ToolbeltItemsChanged(PlayerInventory_OnToolbeltItemsChanged);
        QuestEventManager.Current.SkillPointSpent += new QuestEvent_SkillPointSpent(QuestEvent_OnSkillPointSpent);
        PinRecipesManager.Instance.RegisterWidget(this);
    }

    public override void OnClose()
    {
        base.OnClose();
        xui.PlayerInventory.OnBackpackItemsChanged -= new XUiEvent_BackpackItemsChanged(PlayerInventory_OnBackpackItemsChanged);
        xui.PlayerInventory.OnToolbeltItemsChanged -= new XUiEvent_ToolbeltItemsChanged(PlayerInventory_OnToolbeltItemsChanged);
        QuestEventManager.Current.SkillPointSpent -= new QuestEvent_SkillPointSpent(QuestEvent_OnSkillPointSpent);
        PinRecipesManager.Instance.UnregisterWidget(this);
    }

    private void QuestEvent_OnSkillPointSpent(string skillName) => SetAllChildrenDirty(true);

    private void PlayerInventory_OnToolbeltItemsChanged() => SetAllChildrenDirty(true);

    private void PlayerInventory_OnBackpackItemsChanged() => SetAllChildrenDirty(true);

    public override void Update(float _dt)
    {
        if (!XUi.IsGameRunning()) return;
        // if (IsDirty == false) return;
        RefreshBindings();
        base.Update(_dt);
        IsDirty = false;
    }

    // public override bool GetBindingValue(ref string value, string bindingName)
    // {
    //     value = "";
    //     return false;
    // }

}
