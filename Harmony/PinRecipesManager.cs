using System;
using System.Collections.Generic;

public class PinRecipesManager
{

    public XUi XUI { get; private set; }

    public EntityPlayerLocal Player { get; private set; }

    private static PinRecipesManager instance = null;

    public List<PinnedRecipeSDO> Recipes = new List<PinnedRecipeSDO>();

    public List<XUiC_PinnedRecipe> Slots = new List<XUiC_PinnedRecipe>();

    public List<XUiC_PinRecipes> Windows = new List<XUiC_PinRecipes>();

    public static byte FileVersion = 2;

    public XUiC_CraftingWindowGroup CraftArea = null;

    public static bool IsMenuOpen()
    {
        if (HasInstance == false) return false;
        return Instance.MenusOpen > 0;
    }

    public int MenusOpen = 0;

    public byte CurrentFileVersion { get; set; }

    public static PinRecipesManager OptInstance => instance;

    public static PinRecipesManager Instance
    {
        get
        {
            if (instance != null) return instance;
            return new PinRecipesManager();
        }
    }

    public static bool HasInstance => instance != null;

    public static bool HasRecipes => HasInstance && instance.Recipes.Count > 0;

    private PinRecipesManager()
    {
        instance = this;
    }

    // Unload and reset singleton
    public static void Clear()
    {
        if (instance == null) return;
        instance.Recipes.Clear();
        instance.Slots.Clear();
        instance.Windows.Clear();
        instance.DetachPlayerAndInventory();
        instance.CraftArea = null;
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

    public void SetCraftArea(XUiC_CraftingWindowGroup area, bool force = false)
    {
        if (CraftArea == area && !force) return;
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

    public static XUiC_CraftingWindowGroup GetOpenCraftingWindow()
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

    // Refresh loot containers on "Unpin"
    // Currently only refreshes "grab" icon
    public void RefreshContainers()
    {
        var vehicle = XUI?.FindWindowGroupByName(XUiC_VehicleStorageWindowGroup.ID);
        if (vehicle != null) vehicle.RefreshBindingsSelfAndChildren();
        var loot = XUI?.FindWindowGroupByName(XUiC_LootWindowGroup.ID);
        if (loot != null) loot.RefreshBindingsSelfAndChildren();
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
        SetWidgetsDirty();
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
        // Update "grab" button
        RefreshContainers();
        SetWidgetsDirty();
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
            if (CurrentFileVersion == 1)
            {
                string name = br.ReadString();
                if (isSameUser == false) continue;
                if (CraftingManager.GetRecipe(name) is Recipe recipe)
                    Recipes.Add(new PinnedRecipeSDO(recipe, amount, CraftArea));
            }
            else
            {
                int hash = br.ReadInt32();
                if (isSameUser == false) continue;
                if (CraftingManager.GetRecipe(hash) is Recipe recipe)
                    Recipes.Add(new PinnedRecipeSDO(recipe, amount, CraftArea));
            }
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
            bw.Write(recipe.Recipe.GetHashCode());
        }
    }

    // We can't use Equals since `Seed` is different!?
    private bool IsValidItemValue(ItemValue a, ItemValue b)
    {
        if (a == null)
        {
            return b == null;
        }
        else if (b == null)
        {
            return false;
        }
        else if (a.HasQuality)
        {
            return b.HasQuality
                && a.type == b.type
                && a.Quality == b.Quality;
        }
        else
        {
            return a.type == b.type;
        }
    }

    private bool IsValidItemValue(ItemStack a, ItemStack b)
    {
        return IsValidItemValue(a?.itemValue, b?.itemValue);
    }

    private int GetAvailableItems(ItemStack wanted, ItemStack[] bag)
    {
        int available = 0;
        foreach (ItemStack item in bag)
        {
            if (item == null || item.IsEmpty()) continue;
            if (!IsValidItemValue(item, wanted)) continue;
            available += item.count;
            if (available >= wanted.count) break;
        }
        return Utils.FastMin(available, wanted.count);
    }

    // Remove items from container (decreasing count in `taken`)
    private bool RemoveFromContainer(ItemStack[] container, ItemStack taken)
    {
        bool changed = false;
        for (int i = 0; i < container.Length; i += 1)
        {
            // Check if container has the specific item in this slot
            if (container[i] == null || container[i].IsEmpty()) continue;
            if (!IsValidItemValue(container[i], taken)) continue;
            // Check if we have less than we want
            // Full stack will be taken, left empty
            if (container[i].count <= taken.count)
            {
                taken.count -= container[i].count;
                container[i].Clear();
                changed = true;
            }
            // Otherwise stack has all we need
            else if (container[i].count > 0)
            {
                container[i].count -= taken.count;
                taken.count = 0;
                changed = true;
                break;
            }
        }
        return changed;
    }


    private Dictionary<int, int> ingredients = new Dictionary<int, int>();

    public bool GrabRequiredItems(XUiM_PlayerInventory inventory,
        ref ItemStack[] items, List<PinnedRecipeSDO> recipes)
    {
        bool changed = false;
        // Re-use container
        ingredients.Clear();
        // Cant do one by one, as it will not sum up the
        // wanted ingredients (would only add the maximum)
        foreach (PinnedRecipeSDO recipe in recipes)
        {
            foreach (PinnedIngredientSDO ingredient in recipe.Ingredients)
            {
                int type = ingredient.ItemValue.type;
                var wanted = ingredient.Needed - ingredient.Available;
                if (wanted <= 0) continue; // Already enough?
                if (ingredients.ContainsKey(type))
                    ingredients[type] += wanted;
                else ingredients.Add(type, wanted);
            }
        }
        // Try to grab items for all ingredients
        foreach (var ingredient in ingredients)
        {
            var iv = new ItemValue(ingredient.Key);
            var stack = new ItemStack(iv, ingredient.Value);
            int take = stack.count = GetAvailableItems(stack, items);
            // Abort if nothing to be taken
            if (stack.count == 0) continue;
            // Ignore return value, since it will only
            // return true if all items have been added
            // It will still take items partially though
            inventory.AddItem(stack);
            // Check if any items are taken at all
            if (stack.count == take) continue;
            // Calculate items to remove from container
            stack.count = take - stack.count;
            changed |= RemoveFromContainer(items, stack);
        }
        // Re-use container
        ingredients.Clear();
        return changed;
    }

    private void GrabIngredients(TileEntityLootContainer container)
    {
        if (XUI == null) return;
        if (container == null) return;
        var inventory = XUI.PlayerInventory;
        if (inventory == null) return;
        // Fetch ingredients for all pinned recipes
        ItemStack[] slots = container.GetItems();
        if (GrabRequiredItems(inventory, ref slots, Recipes))
        {
            // Re-implement `TileEntityChanged`, since it is private
            // E.g. see how `TileEntityLootContainer.UpdateSlot` works
            for (int index = 0; index < container.listeners.Count; ++index)
                container.listeners[index].OnTileEntityChanged(container, 0);
            // See `SetEmpty()`
            container.bTouched = true;
            container.SetModified();
        }
    }

    private void GrabIngredients(Bag container)
    {
        if (XUI == null) return;
        if (container == null) return;
        var inventory = XUI.PlayerInventory;
        if (inventory == null) return;
        // Fetch ingredients for all pinned recipes
        ItemStack[] slots = container.GetSlots();
        if (GrabRequiredItems(inventory, ref slots, Recipes))
            container.SetSlots(slots);
    }

    public void GrabIngredients()
    {
        if (XUI == null) return;
        if (XUI.lootContainer != null) GrabIngredients(XUI.lootContainer);
        else if (XUI.vehicle?.bag != null) GrabIngredients(XUI.vehicle.bag);
    }

}
