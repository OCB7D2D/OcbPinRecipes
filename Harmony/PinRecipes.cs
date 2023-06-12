using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;

public class PinRecipes : IModApi
{

    public void InitMod(Mod mod)
    {
        Log.Out("OCB Harmony Patch: " + GetType().ToString());
        Harmony harmony = new Harmony(GetType().ToString());
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }

    // Patch to add pin option into action list
    [HarmonyPatch(typeof(XUiC_ItemActionList))]
    [HarmonyPatch("SetCraftingActionList")]
    public class XUiC_ItemActionList_SetCraftingActionList
    {
        static void Postfix(XUiC_ItemActionList __instance,
            List<BaseItemActionEntry> ___itemActionEntries,
            XUiC_RecipeCraftCount ___craftCountControl,
            XUiController itemController)
        {
            if (itemController is XUiC_RecipeEntry xuiCRecipeEntry)
            {
                if (xuiCRecipeEntry.Recipe == null || xuiCRecipeEntry.Recipe.materialBasedRecipe) return;
                ___itemActionEntries.RemoveAll(x => x.ActionName == "Track");
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
            XUi xui = LocalPlayerUI.GetUIForPrimaryPlayer()?.xui;
            // Force instance; player wouldn't be known otherwise
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
            if (XUiC_PinRecipes.ID == string.Empty) return;
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
            PinRecipesManager.Instance.SetCraftArea(null);
        }
    }

    // Update state when work-station is opened
    [HarmonyPatch(typeof(XUiC_WorkstationWindowGroup))]
    [HarmonyPatch("OnOpen")]
    public class XUiC_WorkstationWindowGroup_OnOpen
    {
        static void Postfix(XUiC_WorkstationWindowGroup __instance)
        {
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
            PinRecipesManager.Instance.SetCraftArea(null);
        }
    }

    // Update state when tools are changed in workstation
    [HarmonyPatch(typeof(XUiC_WorkstationToolGrid))]
    [HarmonyPatch("UpdateBackend")]
    public class XUiC_WorkstationToolGrid_UpdateBackend
    {
        static void Postfix(XUiC_WorkstationToolGrid __instance)
        {
            
            if (__instance?.WindowGroup?.Controller is XUiC_WorkstationWindowGroup window)
                PinRecipesManager.Instance.SetCraftArea(window, true);
        }
    }

    // Hook into UI startup when user is attached
    [HarmonyPatch(typeof(LocalPlayerUI))]
    [HarmonyPatch("DispatchNewPlayerForUI")]
    public class LocalPlayerUI_DispatchNewPlayerForUI
    {
        static void Postfix()
        {
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

    // Patch world unload to cleanup on exit
    // This ensures that recipes are not carried
    // over when you create a new world from scratch
    [HarmonyPatch(typeof(World))]
    [HarmonyPatch("UnloadWorld")]
    public class World_UnloadWorld
    {
        static void Postfix()
        {
            PinRecipesManager.Clear();
        }
    }

    // Add new field to loot containers
    [HarmonyPatch(typeof(XUiC_LootWindow))]
    [HarmonyPatch("GetBindingValue")]
    public class XUiC_LootWindow_GetBindingValue
    {
        static bool Prefix(
            ref string _value,
            string _bindingName,
            ref bool __result)
        {
            if (_bindingName == "hasPinnedRecipes")
            {
                _value = PinRecipesManager.HasRecipes.ToString();
                __result = true;
                return false;
            }
            return true;
        }
    }

    // Add new field to vehicle containers
    [HarmonyPatch(typeof(XUiC_VehicleContainer))]
    [HarmonyPatch("GetBindingValue")]
    public class XUiC_VehicleContainer_GetBindingValue
    {
        static bool Prefix(
            ref string _value,
            string _bindingName,
            ref bool __result)
        {
            if (_bindingName == "hasPinnedRecipes")
            {
                _value = PinRecipesManager.HasRecipes.ToString();
                __result = true;
                return false;
            }
            return true;
        }
    }

    // ****************************************************
    // Implementation for grabbing ingredients from loot:
    // ****************************************************

    [HarmonyPatch(typeof(XUiC_ContainerStandardControls))]
    [HarmonyPatch("Init")]
    public class XUiC_ContainerStandardControls_Init
    {
        static void Postfix(XUiC_ContainerStandardControls __instance)
        {
            XUiController grab = __instance.GetChildById("btnPinGrab");
            if (grab != null) grab.OnPress += Grab_OnPress;
        }
        private static void Grab_OnPress(XUiController _sender, int _mouseButton)
        {
            if (!PinRecipesManager.HasInstance) return;
            PinRecipesManager.Instance.GrabIngredients();
        }
    }

    // ****************************************************
    // Implementation to postpone our xml patching.
    // Allows us to run after our dependencies :-)
    // ****************************************************

    // Return in our load order
    [HarmonyPatch(typeof(ModManager))]
    [HarmonyPatch("GetLoadedMods")]
    public class ModManager_GetLoadedMods
    {
        static void Postfix(ref List<Mod> __result)
        {
            int myPos = -1, depPos = -1;
            if (__result == null) return;
            // Find position of mods we depend on
            for (int i = 0; i < __result.Count; i += 1)
            {
                switch (__result[i].Name)
                {
                    case "SMXui":
                        depPos = i + 1;
                        break;
                    case "OcbPinRecipes":
                        myPos = i;
                        break;
                }
            }
            // Didn't detect ourself?
            if (myPos == -1)
            {
                Log.Error("Did not detect our own Mod?");
                return;
            }
            // Detected no dependencies?
            if (depPos == -1) return;
            // Move our mod after deps
            var item = __result[myPos];
            __result.RemoveAt(myPos);
            if (depPos > myPos) depPos--;
            __result.Insert(depPos, item);
        }
    }

}
