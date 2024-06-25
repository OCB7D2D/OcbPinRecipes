// This Shared Data Object does all the caching for us
// It is passed around the various other components
// Most of its values are read-only by nature
// Only a few parameters are allowed to change

public class PinnedIngredientSDO
{

    public int Need = 1; // adjustable
    public int Available = 1; // adjustable
    public readonly int Index = -1;
    public readonly string Title = null;
    public readonly string IconImg = null;
    public readonly string IconTint = null;
    public readonly PinnedRecipeSDO RDO = null;

    private static readonly CachedStringFormatterXuiRgbaColor
        colorFormatter = new CachedStringFormatterXuiRgbaColor();

    public Recipe Recipe => RDO?.Recipe;

    public ItemValue ItemValue => Ingredient?.itemValue;

    public ItemStack Ingredient => Recipe?.ingredients[Index];

    public int Needed => (int)(Need * RDO?.Count);

    public PinnedIngredientSDO(PinnedRecipeSDO recipe, int index)
    {
        RDO = recipe;
        Index = index;
        if (RDO != null)
        {
            ItemValue itemValue = Ingredient.itemValue;
            Title = itemValue.ItemClass.GetLocalizedItemName();
            IconImg = itemValue.GetPropertyOverride("CustomIcon",
                itemValue.ItemClass.GetIconName());
            IconTint = colorFormatter.Format(itemValue
                .ItemClass.GetIconTint(itemValue));
        }
    }

    // Call this when user stats have changed
    public void RecalcNeeded(EntityAlive player)
    {
        if (Recipe.UseIngredientModifier)
        {
            Need = (int)EffectManager.GetValue(
                 PassiveEffects.CraftingIngredientCount,
                 _originalValue: Ingredient.count,
                 _entity: player, _recipe: RDO.Recipe,
                 tags: FastTags<TagGroup.Global>.Parse(Ingredient.
                    itemValue.ItemClass.GetItemName()),
                craftingTier: RDO.Recipe.craftingTier);
        }
        else
        {
            Need = Ingredient.count;
        }
    }

    // Call this when inventory has changed
    public void RecalcAvailable(XUi xui)
    {
        if (xui?.PlayerInventory == null) return;
        Available = xui.PlayerInventory.
            GetItemCount(Ingredient.itemValue);
    }

}