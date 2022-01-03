using UnityEngine;

public class ItemActionEntryPinRecipes : BaseItemActionEntry
{

    // Recipe attach to this action
    private static Recipe recipe;

    public ItemActionEntryPinRecipes(XUiController controller, Recipe _recipe) :
        base(controller, "lblContextActionPinRecipe", "ui_game_symbol_pen", GamepadShortCut.DPadLeft)
    {
        recipe = _recipe;
    }

    public override void OnActivated()
    {
        // Update via static "proxy"
        PinRecipes.Recipes.Add(recipe);
        PinRecipes.IsDirty = true;
    }

}
