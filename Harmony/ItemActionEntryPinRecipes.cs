﻿public class ItemActionEntryPinRecipes : BaseItemActionEntry
{

    // Recipe attached to this action
    private readonly Recipe Recipe;
    // Amount of items to be built in total
    private readonly XUiC_RecipeCraftCount Counter;

    public ItemActionEntryPinRecipes(XUiController controller, Recipe recipe, XUiC_RecipeCraftCount counter) :
        base(controller, "lblContextActionPinRecipe", "ui_game_symbol_pin", GamepadShortCut.DPadLeft)
    {
        Recipe = recipe;
        Counter = counter;
    }

    public override void OnActivated()
    {
        PinRecipesManager.Instance.PinRecipe(
            Recipe, Counter.Count);
    }

}