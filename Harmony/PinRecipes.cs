﻿using HarmonyLib;
using System.Reflection;

public class PinRecipes : IModApi
{

    public void InitMod(Mod mod)
    {
        Log.Out(" Loading Patch: " + GetType().ToString());
        Harmony harmony = new Harmony(GetType().ToString());
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }

    // Patch to add pin option into action list
    [HarmonyPatch(typeof(XUiC_ItemActionList))]
    [HarmonyPatch("SetCraftingActionList")]
    public class XUiC_ItemActionList_SetCraftingActionList
    {
        static void Postfix(XUiC_ItemActionList __instance,
            XUiC_RecipeCraftCount ___craftCountControl,
            XUiController itemController)
        {
            if (itemController is XUiC_RecipeEntry xuiCRecipeEntry)
            {
                if (xuiCRecipeEntry.Recipe == null || xuiCRecipeEntry.Recipe.materialBasedRecipe) return;
                __instance.AddActionListEntry(new ItemActionEntryPinRecipes(itemController,
                    xuiCRecipeEntry.Recipe, ___craftCountControl));
            }
        }
    }

    // Register event handlers when game starts
    [HarmonyPatch(typeof(GameStateManager))]
    [HarmonyPatch("StartGame")]
    public class GameStateManager_StartGame
    {
        static void Postfix()
        {
            if (!PinRecipesManager.HasInstance) return;
            XUi xui = LocalPlayerUI.GetUIForPrimaryPlayer()?.xui;
            PinRecipesManager.Instance.AttachPlayerAndInventory(xui);
        }
    }

    // Unregister event handlers when game ends
    [HarmonyPatch(typeof(GameStateManager))]
    [HarmonyPatch("EndGame")]
    public class GameStateManager_EndGame
    {
        static void Postfix()
        {
            if (!PinRecipesManager.HasInstance) return;
            PinRecipesManager.Instance.DetachPlayerAndInventory();
        }
    }

    // Patch into where regular windows are also opened
    // Note: It seems to me this is called pretty often!
    // Note: But all other windows do exactly the same!
    [HarmonyPatch(typeof(EntityPlayerLocal))]
    [HarmonyPatch("guiDrawCrosshair")]
    public class EntityPlayerLocal_OnHUD
    {
        static void Postfix(GUIWindowManager ___windowManager)
        {
            if (GameManager.Instance.IsPaused()) return;
            ___windowManager.OpenIfNotOpen(XUiC_PinRecipes.ID, false);
        }
    }

    // Hide the pins when the main menu is shown
    [HarmonyPatch(typeof(XUiC_InGameMenuWindow))]
    [HarmonyPatch("OnOpen")]
    public class XUiC_InGameMenuWindow_OnOpen
    {
        static void Postfix(XUiC_InGameMenuWindow __instance)
        {
            if (!GameManager.Instance.IsPaused()) return;
            __instance.xui.playerUI.windowManager
                .CloseIfOpen(XUiC_PinRecipes.ID);
        }
    }

    // Update state when cursor is showing
    [HarmonyPatch(typeof(GUIWindowManager))]
    [HarmonyPatch("EnableWindowActionSet")]
    public class XUiWindowGroup_OnOpen
    {
        static void Postfix(XUiC_CraftingWindowGroup __instance)
        {
            if (!PinRecipesManager.HasInstance) return;
            PinRecipesManager manager = PinRecipesManager.Instance;
            manager.MenusOpen += 1;
            if (manager.MenusOpen == 1)
                manager.SetWidgetsDirty();
        }
    }

    // Update state when cursor is hidden
    [HarmonyPatch(typeof(GUIWindowManager))]
    [HarmonyPatch("DisableWindowActionSet")]
    public class XUiWindowGroup_onClose
    {
        static void Postfix()
        {
            if (!PinRecipesManager.HasInstance) return;
            PinRecipesManager manager = PinRecipesManager.Instance;
            manager.MenusOpen -= 1;
            if (manager.MenusOpen == 0)
                manager.SetWidgetsDirty();
        }
    }

    // Update state when craft-station is opened
    [HarmonyPatch(typeof(XUiC_CraftingWindowGroup))]
    [HarmonyPatch("OnOpen")]
    public class XUiC_CraftingWindowGroup_OnOpen
    {
        static void Postfix(XUiC_CraftingWindowGroup __instance)
        {
            if (!PinRecipesManager.HasInstance) return;
            PinRecipesManager.Instance.SetCraftArea(__instance);
        }
    }

    // Update state when craft-station is closed
    [HarmonyPatch(typeof(XUiC_CraftingWindowGroup))]
    [HarmonyPatch("OnClose")]
    public class XUiC_CraftingInfoWindow_onClose
    {
        static void Postfix()
        {
            if (!PinRecipesManager.HasInstance) return;
            PinRecipesManager.Instance.SetCraftArea(null);
        }
    }

    // Update state when work-station is opened
    [HarmonyPatch(typeof(XUiC_WorkstationWindowGroup))]
    [HarmonyPatch("OnOpen")]
    public class XUiC_WorkstationWindowGroup_OnOpen
    {
        static void Postfix(XUiC_CraftingWindowGroup __instance)
        {
            if (!PinRecipesManager.HasInstance) return;
            PinRecipesManager.Instance.SetCraftArea(__instance);
        }
    }

    // Update state when work-station is closed
    [HarmonyPatch(typeof(XUiC_WorkstationWindowGroup))]
    [HarmonyPatch("OnClose")]
    public class XUiC_WorkstationWindowGroup_onClose
    {
        static void Postfix()
        {
            if (!PinRecipesManager.HasInstance) return;
            PinRecipesManager.Instance.SetCraftArea(null);
        }
    }

    // Hook into UI startup when user is attached
    [HarmonyPatch(typeof(LocalPlayerUI))]
    [HarmonyPatch("DispatchNewPlayerForUI")]
    public class LocalPlayerUI_DispatchNewPlayerForUI
    {
        static void Postfix()
        {
            if (!PinRecipesManager.HasInstance) return;
            PinRecipesManager.Instance.OnSkillsChanged();
        }
    }

    // Hook into Grandpa's Forgetting Elixir
    [HarmonyPatch(typeof(Progression))]
    [HarmonyPatch("ResetProgression")]
    public class Progression_ResetProgression
    {
        static void Postfix()
        {
            if (!PinRecipesManager.HasInstance) return;
            PinRecipesManager.Instance.OnSkillsChanged();
        }
    }

    // Patch player data write to append our data
    [HarmonyPatch(typeof(PlayerDataFile))]
    [HarmonyPatch("Write")]
    public class PlayerDataFile_Write
    {
        static void Postfix(
            PlayerDataFile __instance,
            PooledBinaryWriter _bw)
        {
            PinRecipesManager.Instance.WritePlayerData(_bw);
        }
    }

    // Patch player data read to ingest our data
    [HarmonyPatch(typeof(PlayerDataFile))]
    [HarmonyPatch("Read")]
    public class PlayerDataFile_Read
    {
        static void Postfix(
            PlayerDataFile __instance,
            PooledBinaryReader _br)
        {
            PinRecipesManager.Instance.ReadPlayerData(_br, __instance.id);
        }
    }

}
