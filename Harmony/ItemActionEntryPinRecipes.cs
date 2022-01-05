using UnityEngine;

public class ItemActionEntryPinRecipes : BaseItemActionEntry
{

    // Recipe attach to this action
    private Recipe Recipe;
    private XUiC_RecipeCraftCount Counter;

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
