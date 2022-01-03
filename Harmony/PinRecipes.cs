using HarmonyLib;
using UnityEngine;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;

public class PinRecipes : IModApi
{

    // Abuse static fields as poor man's manager
    public static List<Recipe> Recipes = new List<Recipe>();
    public static bool IsDirty = true;

    public void InitMod(Mod mod)
    {
        Log.Out(" Loading Patch: " + this.GetType().ToString());
        var harmony = new HarmonyLib.Harmony(GetType().ToString());
        harmony.PatchAll(Assembly.GetExecutingAssembly());
    }

    [HarmonyPatch(typeof(EntityPlayerLocal))]
    [HarmonyPatch("OnHUD")]
    public class EntityPlayerLocal_OnHUD
    {
        static void Postfix(GUIWindowManager ___windowManager)
        {
            ___windowManager.OpenIfNotOpen(XUiC_PinRecipes.ID, false);
            IsDirty = true;
        }
    }

    [HarmonyPatch(typeof(XUiC_ItemActionList))]
    [HarmonyPatch("SetCraftingActionList")]
    public class XUiC_ItemActionList_SetCraftingActionList
    {
        static void Postfix(XUiC_ItemActionList __instance, Recipe ___recipe,
            XUiC_ItemActionList.ItemActionListTypes _actionListType,
            XUiController itemController)
        {
            if (___recipe == null) return;
            if (___recipe.materialBasedRecipe) return;
            if (_actionListType != XUiC_ItemActionList.ItemActionListTypes.Crafting) return;
            XUiC_RecipeEntry xuiCRecipeEntry = (XUiC_RecipeEntry)itemController;
            if (___recipe != xuiCRecipeEntry.Recipe) return;
            __instance.AddActionListEntry(new ItemActionEntryPinRecipes(itemController, ___recipe));
        }
    }

}
