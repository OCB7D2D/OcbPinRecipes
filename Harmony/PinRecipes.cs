using HarmonyLib;
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
            XUiController itemController)
        {
            if (itemController is XUiC_RecipeEntry xuiCRecipeEntry)
            {
                if (xuiCRecipeEntry.Recipe == null || xuiCRecipeEntry.Recipe.materialBasedRecipe) return;
                __instance.AddActionListEntry(new ItemActionEntryPinRecipes(itemController, xuiCRecipeEntry.Recipe));
            }
        }
    }

}
