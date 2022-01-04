using UnityEngine;

public class ItemActionEntryPinRecipes : BaseItemActionEntry
{

    // Recipe attach to this action
    private static Recipe recipe;

    public ItemActionEntryPinRecipes(XUiController controller, Recipe _recipe) :
        base(controller, "lblContextActionPinRecipe", "ui_game_symbol_pin", GamepadShortCut.DPadLeft)
    {
        recipe = _recipe;
    }

    public override void OnActivated()
    {
        PinRecipesManager.Instance.PinRecipe(recipe);
    }

}
