public class PinnedRecipeDTO
{

    public Recipe Recipe;
    public int Count = 1;

    public PinnedRecipeDTO(Recipe recipe, int count)
    {
        Count = count;
        Recipe = recipe;
    }

}
