using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace GorilaChestMod
{
    /// <summary>
    /// The private parts of InventoryGui the tab needs, resolved once at startup.
    ///
    /// The recipe list is a List of a private struct, RecipeDataPair, so it cannot
    /// be named from here: it is handled as an IList and its entries are read
    /// through their properties. If anything is missing after a game update the
    /// plugin logs it and stays off instead of throwing every frame.
    /// </summary>
    internal static class GuiAccess
    {
        private static FieldInfo _availableRecipes;
        private static FieldInfo _selectedRecipe;
        private static FieldInfo _craftTimer;
        private static FieldInfo _craftRecipe;
        private static FieldInfo _multiCrafting;
        private static FieldInfo _recipeListBaseSize;

        private static PropertyInfo _pairRecipe;
        private static PropertyInfo _pairItem;
        private static PropertyInfo _pairElement;

        private static MethodInfo _addRecipeToList;
        private static MethodInfo _updateCraftingPanel;
        private static MethodInfo _setActiveGroup;

        /// <summary>Index of the crafting panel in InventoryGui.m_uiGroups, the one the vanilla tabs focus.</summary>
        internal const int CraftingGroup = 3;

        /// <summary>
        /// False until everything below has been found. The rest of the mod keeps
        /// working either way: only the Deconstruct tab stays away.
        /// </summary>
        internal static bool Ready { get; private set; }

        internal static bool Resolve()
        {
            Type gui = typeof(InventoryGui);
            Type pair = AccessTools.Inner(gui, "RecipeDataPair");

            _availableRecipes = AccessTools.Field(gui, "m_availableRecipes");
            _selectedRecipe = AccessTools.Field(gui, "m_selectedRecipe");
            _craftTimer = AccessTools.Field(gui, "m_craftTimer");
            _craftRecipe = AccessTools.Field(gui, "m_craftRecipe");
            _multiCrafting = AccessTools.Field(gui, "m_multiCrafting");
            _recipeListBaseSize = AccessTools.Field(gui, "m_recipeListBaseSize");

            if (pair != null)
            {
                _pairRecipe = AccessTools.Property(pair, "Recipe");
                _pairItem = AccessTools.Property(pair, "ItemData");
                _pairElement = AccessTools.Property(pair, "InterfaceElement");
            }

            _addRecipeToList = AccessTools.Method(gui, "AddRecipeToList",
                new[] { typeof(Player), typeof(Recipe), typeof(ItemDrop.ItemData), typeof(bool) });
            _updateCraftingPanel = AccessTools.Method(gui, "UpdateCraftingPanel", new[] { typeof(bool) });
            _setActiveGroup = AccessTools.Method(gui, "SetActiveGroup", new[] { typeof(int), typeof(bool) });

            var members = new (string Name, object Member)[]
            {
                ("InventoryGui.RecipeDataPair", pair),
                ("InventoryGui.m_availableRecipes", _availableRecipes),
                ("InventoryGui.m_selectedRecipe", _selectedRecipe),
                ("InventoryGui.m_craftTimer", _craftTimer),
                ("InventoryGui.m_craftRecipe", _craftRecipe),
                ("InventoryGui.m_multiCrafting", _multiCrafting),
                ("InventoryGui.m_recipeListBaseSize", _recipeListBaseSize),
                ("RecipeDataPair.Recipe", _pairRecipe),
                ("RecipeDataPair.ItemData", _pairItem),
                ("RecipeDataPair.InterfaceElement", _pairElement),
                ("InventoryGui.AddRecipeToList", _addRecipeToList),
                ("InventoryGui.UpdateCraftingPanel", _updateCraftingPanel),
                ("InventoryGui.SetActiveGroup(int, bool)", _setActiveGroup),
            };

            bool ok = true;
            foreach (var (name, member) in members)
            {
                if (member == null)
                {
                    GorilaChestModPlugin.Log.LogError($"Missing game member {name}.");
                    ok = false;
                }
            }

            Ready = ok;
            return ok;
        }

        internal static IList AvailableRecipes(InventoryGui gui) => (IList)_availableRecipes.GetValue(gui);

        internal static GameObject ElementOf(object pair) => (GameObject)_pairElement.GetValue(pair);

        /// <summary>The recipe and item behind the entry currently selected in the list, both null when nothing is.</summary>
        internal static void Selected(InventoryGui gui, out Recipe recipe, out ItemDrop.ItemData item)
        {
            object pair = _selectedRecipe.GetValue(gui);
            recipe = (Recipe)_pairRecipe.GetValue(pair);
            item = (ItemDrop.ItemData)_pairItem.GetValue(pair);
        }

        internal static float CraftTimer(InventoryGui gui) => (float)_craftTimer.GetValue(gui);

        internal static float RecipeListBaseSize(InventoryGui gui) => (float)_recipeListBaseSize.GetValue(gui);

        /// <summary>
        /// Starts the vanilla progress bar. UpdateRecipe counts it up with the same
        /// duration as a single craft and calls DoCrafting at the end, which is
        /// where the deconstruction takes over.
        /// </summary>
        internal static void StartTimer(InventoryGui gui, Recipe recipe)
        {
            _craftRecipe.SetValue(gui, recipe);
            _multiCrafting.SetValue(gui, false);
            _craftTimer.SetValue(gui, 0f);
        }

        internal static void AddRecipeToList(InventoryGui gui, Player player, Recipe recipe, ItemDrop.ItemData item, bool canCraft)
        {
            _addRecipeToList.Invoke(gui, new object[] { player, recipe, item, canCraft });
        }

        internal static void UpdateCraftingPanel(InventoryGui gui)
        {
            _updateCraftingPanel.Invoke(gui, new object[] { false });
        }

        internal static void FocusCraftingPanel(InventoryGui gui)
        {
            if (gui.m_uiGroups != null && gui.m_uiGroups.Length > CraftingGroup)
            {
                _setActiveGroup.Invoke(gui, new object[] { CraftingGroup, true });
            }
        }
    }
}
