using System.Collections.Generic;

public class PinRecipesManager
{

    public XUi XUI { get; private set; }

    public EntityPlayerLocal Player { get; private set; }

    private static PinRecipesManager instance = null;

    public List<PinnedRecipeSDO> Recipes = new List<PinnedRecipeSDO>();

    public List<XUiC_PinnedRecipe> Slots = new List<XUiC_PinnedRecipe>();

    public List<XUiC_PinRecipes> Windows = new List<XUiC_PinRecipes>();

    public static byte FileVersion = 1;

    public XUiC_CraftingWindowGroup CraftArea = null;

    public int MenusOpen = 0;

    public byte CurrentFileVersion { get; set; }

    public static PinRecipesManager Instance
    {
        get
        {
            if (instance != null) return instance;
            return new PinRecipesManager();
        }
    }

    public static bool HasInstance => instance != null;

    private PinRecipesManager()
    {
        instance = this;
    }

    public void AttachPlayerAndInventory(XUi xui)
    {
        XUI = xui; Player = xui?.playerUI?.entityPlayer;
        if (xui?.PlayerInventory != null) xui.PlayerInventory.OnBackpackItemsChanged += OnInventoryChanged;
        if (xui?.PlayerInventory != null) xui.PlayerInventory.OnToolbeltItemsChanged += OnInventoryChanged;
        if (QuestEventManager.Current != null) QuestEventManager.Current.SkillPointSpent += OnSkillPointSpent;
        OnInventoryChanged(); OnSkillsChanged(); // Update the stats when attached
    }

    public void DetachPlayerAndInventory()
    {
        if (XUI?.PlayerInventory != null) XUI.PlayerInventory.OnBackpackItemsChanged -= OnInventoryChanged;
        if (XUI?.PlayerInventory != null) XUI.PlayerInventory.OnToolbeltItemsChanged -= OnInventoryChanged;
        if (QuestEventManager.Current != null) QuestEventManager.Current.SkillPointSpent -= OnSkillPointSpent;
        XUI = null; Player = null;
    }

    private void OnInventoryChanged()
    {
        foreach (var recipe in Recipes)
            recipe.OnInventoryChanged();
        SetWidgetsDirty();
    }

    public void SetCraftArea(XUiC_CraftingWindowGroup area)
    {
        if (CraftArea == area) return;
        foreach (var recipe in Recipes)
            recipe.UpdateCraftArea(area);
        CraftArea = area;
        SetWidgetsDirty();
    }

    private void OnSkillPointSpent(string skillName)
    {
        OnSkillsChanged();
    }

    public void OnSkillsChanged()
    {
        foreach (var recipe in Recipes)
            recipe.OnUserStatsChanged();
        SetWidgetsDirty();
    }

    public static XUiC_CraftingWindowGroup GetOpenCraftingWindow(XUi xui)
    {
        if (instance == null) return null;
        return instance.CraftArea;
    }

    public void SetWidgetsDirty()
    {
        // Update main and all children
        foreach (var ui in Windows)
            ui.SetAllChildrenDirty();
    }

    // We only really support one window currently
    public void RegisterWindow(XUiC_PinRecipes widget)
    {
        Windows.Add(widget);
    }

    // Do not pass any updates when unregistered
    public void UnregisterWindow(XUiC_PinRecipes widget)
    {
        Windows.Remove(widget);
    }

    // Make sure to update when slot changes
    private void HandleSlotUpdate(int slot)
    {
        if (slot >= Slots.Count) return;
        if (slot >= Recipes.Count) Slots[slot].SetRecipe(null);
        else Slots[slot].SetRecipe(Recipes[slot]);
    }

    // Register widget controller for one pinned recipe
    public void RegisterSlot(XUiC_PinnedRecipe widget)
    {
        Slots.Add(widget);
        HandleSlotUpdate(Slots.Count - 1);
    }

    // Unregister widget controller for one pinned recipe
    public void UnregisterSlot(XUiC_PinnedRecipe widget)
    {
        Slots.Remove(widget);
        // Not properly implemented
        // We don't really need it!
    }


    // Add a recipe and amount to the queued pins
    // Note: only a few may be shown on the screen
    public void PinRecipe(Recipe recipe, int amount)
    {
        Recipes.Add(new PinnedRecipeSDO(recipe, amount, CraftArea));
        HandleSlotUpdate(Recipes.Count - 1);
    }

    // Remove a pin from the queue (for whatever reason)
    // Make sure to show any more queued recipes instead
    public bool UnpinRecipe(PinnedRecipeSDO rdo)
    {
        int slot = Recipes.IndexOf(rdo);
        if (slot == -1) return false;
        Recipes.RemoveAt(slot);
        for (int i = slot; i < Slots.Count; i++)
            HandleSlotUpdate(i);
        return true;
    }
 
    // There is a weird edge case, as you can run the game twice from
    // the same steam account. When you connect one to the other, you
    // will end up with the same "EOS ID", thus only one user profile.
    // Without our "fix", the recipes would then get somewhat synced.
    // I believe under the hood this may also trigger other edge cases.
    // Btw. we store into `Saves/{World}/{SaveGame}/Player/EOS_XYZ.ttp`
    public void ReadPlayerData(PooledBinaryReader br, int entityId)
    {
        // Check if we are reading for the same entityID
        // Otherwise we do not update our pinned recipes
        // But we must still fully consume the packet
        bool isSameUser = (Player == null || Player.entityId == entityId);
        if (isSameUser == true) Recipes.Clear();

        // Check if we have additional data to be read
        // This way we should be able to upgrade the stream if needed
        if (br.BaseStream.Position >= br.BaseStream.Length)
        {
            Log.Warning("OcbPinRecipes: Vanilla game detected, user data will be upgraded");
            return;
        }

        CurrentFileVersion = br.ReadByte();
        int count = br.ReadInt32();

        for (int index = 0; index < count; ++index)
        {
            int amount = br.ReadInt32();
            string name = br.ReadString();
            if (isSameUser == false) continue;
            if (CraftingManager.GetRecipe(name) is Recipe recipe)
                Recipes.Add(new PinnedRecipeSDO(recipe, amount, CraftArea));
        }
        // Make sure to update all slots
        for (int i = 0; i < Slots.Count; i++)
            HandleSlotUpdate(i);
    }

    // Append pinned recipes to user data
    public void WritePlayerData(PooledBinaryWriter bw)
    {
        bw.Write(FileVersion);
        bw.Write(Recipes.Count);
        foreach (PinnedRecipeSDO recipe in Recipes)
        {
            bw.Write(recipe.Count);
            bw.Write(recipe.Recipe.GetName());
        }
    }

}
