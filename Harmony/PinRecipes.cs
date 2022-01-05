using HarmonyLib;
using System;
using System.Reflection;

public class PinRecipes : IModApi
{

    public void InitMod(Mod mod)
    {
        Log.Out(" Loading Patch: " + GetType().ToString());
        Harmony harmony = new Harmony(GetType().ToString());
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }

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

    // This patch is somewhat ambivalent, since it seems
    // to be called too often, but only if the UI is shown.
    // So still acceptable as regular CPU load is zero.
    // It does seem to be to "one hook to solve it all"
    [HarmonyPatch(typeof(GUIWindowManager))]
    [HarmonyPatch("OnGUI")]
    public class XUiC_Backpack_Open
    {
        static void Postfix()
        {
            if (!PinRecipesManager.HasInstance) return;
            PinRecipesManager.Instance.SetWidgetsDirty();
        }
    }

}
