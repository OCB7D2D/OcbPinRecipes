using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PinRecipesManager
{

    private static PinRecipesManager instance;

    public bool IsDirty = true;

    public List<Recipe> Recipes = new List<Recipe>();

    public List<XUiController> widgets = new List<XUiController>();

    public static byte FileVersion = 1;

    public byte CurrentFileVersion { get; set; }

    private ThreadManager.ThreadInfo dataSaveThreadInfo;

    public static PinRecipesManager Instance
    {
        get
        {
            if (instance != null) return instance;
            instance = new PinRecipesManager();
            instance.LoadPinnedRecipesManager();
            return instance;
        }
    }

    public static bool HasInstance => instance != null;

    private PinRecipesManager()
    {
        instance = this;
    }

    private void UpdateAllWidgets()
    {
        foreach (var widget in widgets)
            widget.SetAllChildrenDirty(true);
        SavePinnedRecipesManager();
    }

    public void RegisterWidget(XUiController widget)
    {
        widgets.Add(widget);
        widget.SetAllChildrenDirty(true);
    }

    public void UnregisterWidget(XUiController widget)
    {
        widgets.Remove(widget);
        widget.SetAllChildrenDirty(true);
    }

    public void PinRecipe(Recipe recipe)
    {
        Recipes.Add(recipe);
        UpdateAllWidgets();
    }

    public void UnpinRecipe(int slot)
    {
        if (Recipes.Count <= slot) return;
        Recipes.RemoveAt(slot);
        UpdateAllWidgets();
    }

    public Recipe GetRecipe(int slot)
    {
        return Recipes.Count <= slot ?
            null : Recipes[slot];
    }

    public ItemStack GetRecipeIngredient(int slot, int index)
    {
        Recipe recipe = GetRecipe(slot);
        if (recipe == null) return null;
        if (recipe.ingredients == null) return null;
        if (recipe.ingredients.Count <= index) return null;
        return recipe.ingredients[index];
    }


    public void LoadPinnedRecipesManager()
    {
        string path = string.Format("{0}/{1}", GameIO.GetSaveGameDir(), "pinned_recipes.dat");
        if (!File.Exists(path)) return;
        try
        {
            using (FileStream fileStream = File.OpenRead(path))
            {
                using (PooledBinaryReader pooledBinaryReader = MemoryPools.poolBinaryReader.AllocSync(false))
                {
                    pooledBinaryReader.SetBaseStream(fileStream);
                    Read(pooledBinaryReader);
                }
            }
        }
        catch (System.Exception)
        {
            string backup = path + ".bak";
            if (!File.Exists(backup)) return;
            using (FileStream fileStream = File.OpenRead(backup))
            {
                using (PooledBinaryReader pooledBinaryReader = MemoryPools.poolBinaryReader.AllocSync(false))
                {
                    pooledBinaryReader.SetBaseStream(fileStream);
                    Read(pooledBinaryReader);
                }
            }
        }
    }

    private int SavePinnedRecipesDataThreaded(ThreadManager.ThreadInfo _threadInfo)
    {
        PooledExpandableMemoryStream parameter = (PooledExpandableMemoryStream)_threadInfo.parameter;
        string str = string.Format("{0}/{1}", GameIO.GetSaveGameDir(), "pinned_recipes.dat");
        if (File.Exists(str)) File.Copy(str, str + ".bak", true);
        parameter.Position = 0L;
        StreamUtils.WriteStreamToFile((Stream)parameter, str);
        MemoryPools.poolMemoryStream.FreeSync(parameter);
        return -1;
    }

    public void SavePinnedRecipesManager()
    {
        // Only create the save manager thread once is enough, bail out early if it already exists
        if (dataSaveThreadInfo != null && ThreadManager.ActiveThreads.ContainsKey("silent_pinnedRecipesDataSave")) return;
        // ToDo: check why we allocate a in-memory stream here (though we are going to write on wire or disk)
        PooledExpandableMemoryStream expandableMemoryStream = MemoryPools.poolMemoryStream.AllocSync(true);
        using (PooledBinaryWriter pooledBinaryWriter = MemoryPools.poolBinaryWriter.AllocSync(false))
        {
            pooledBinaryWriter.SetBaseStream(expandableMemoryStream);
            Write(pooledBinaryWriter);
        }
        // Create a background thread to do the actual data saving and writing to disk
        dataSaveThreadInfo = ThreadManager.StartThread("silent_pinnedRecipesDataSave", null,
            new ThreadManager.ThreadFunctionLoopDelegate(SavePinnedRecipesDataThreaded),
            null, _parameter: expandableMemoryStream);
    }

    // Write all data to the storage
    public void Write(BinaryWriter bw)
    {
        bw.Write(FileVersion);
        bw.Write(Recipes.Count);
        foreach (Recipe recipe in Recipes)
        {
            bw.Write(recipe.GetName());
        }
    }

    // Read all data back from storage
    public void Read(BinaryReader br)
    {
        CurrentFileVersion = br.ReadByte();
        int count = br.ReadInt32();
        for (int index = 0; index < count; ++index)
        {
            string name = br.ReadString();
            if (CraftingManager.GetRecipe(name) is Recipe recipe) {
                Recipes.Add(recipe);
            }
        }
    }

}
