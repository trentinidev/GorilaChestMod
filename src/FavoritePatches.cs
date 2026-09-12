using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace GorilaChestMod
{
    /// <summary>
    /// The star drawn on a marked slot. There is no star in the game's own UI
    /// atlas to borrow, so one is painted into a small texture the first time it
    /// is needed and reused for every slot after that.
    /// </summary>
    internal static class FavoriteMarker
    {
        private const string ChildName = "GorilaChestMod_Favorite";
        private const int TextureSize = 64;

        private static Sprite _star;

        internal static Sprite Star
        {
            get
            {
                if (_star == null)
                {
                    _star = Paint();
                }

                return _star;
            }
        }

        /// <summary>Shows or hides the star on one slot, creating it on first use.</summary>
        internal static void Apply(InventoryElement element, bool favorite)
        {
            if (element == null || element.m_icon == null)
            {
                return;
            }

            Transform slot = element.m_icon.transform.parent;
            if (slot == null)
            {
                return;
            }

            Transform existing = slot.Find(ChildName);

            if (!favorite || !ModConfig.FavoritesShowMarker.Value)
            {
                if (existing != null)
                {
                    existing.gameObject.SetActive(false);
                }

                return;
            }

            if (existing == null)
            {
                GameObject go = new GameObject(ChildName, typeof(RectTransform), typeof(Image));
                RectTransform rect = go.GetComponent<RectTransform>();
                rect.SetParent(slot, worldPositionStays: false);

                // Top left corner of the slot, clear of the amount and quality labels.
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(2f, -2f);
                rect.sizeDelta = new Vector2(22f, 22f);

                Image image = go.GetComponent<Image>();
                image.sprite = Star;
                image.raycastTarget = false;

                existing = rect;
            }

            existing.SetAsLastSibling();
            existing.gameObject.SetActive(true);
        }

        private static Sprite Paint()
        {
            Texture2D texture = new Texture2D(TextureSize, TextureSize, TextureFormat.RGBA32, mipChain: false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            Vector2 centre = new Vector2(TextureSize / 2f, TextureSize / 2f);
            Vector2[] points = new Vector2[10];
            for (int i = 0; i < points.Length; i++)
            {
                // Alternating long and short spokes make the five points.
                float radius = (i % 2 == 0) ? TextureSize * 0.46f : TextureSize * 0.19f;
                float angle = -Mathf.PI / 2f + i * Mathf.PI / 5f;
                points[i] = centre + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            }

            Color gold = new Color(1f, 0.78f, 0.22f, 1f);
            Color edge = new Color(0.16f, 0.11f, 0.02f, 1f);
            Color clear = new Color(0f, 0f, 0f, 0f);

            for (int y = 0; y < TextureSize; y++)
            {
                for (int x = 0; x < TextureSize; x++)
                {
                    Vector2 p = new Vector2(x + 0.5f, y + 0.5f);
                    if (!Inside(points, p))
                    {
                        texture.SetPixel(x, y, clear);
                        continue;
                    }

                    // A dark rim where the shape is about to end, so the star stays
                    // readable over a bright item icon.
                    bool rim = !Inside(points, p + Vector2.right * 2f) || !Inside(points, p + Vector2.left * 2f) ||
                               !Inside(points, p + Vector2.up * 2f) || !Inside(points, p + Vector2.down * 2f);
                    texture.SetPixel(x, y, rim ? edge : gold);
                }
            }

            texture.Apply();
            return Sprite.Create(texture, new Rect(0f, 0f, TextureSize, TextureSize), new Vector2(0.5f, 0.5f));
        }

        /// <summary>Even odd ray casting, enough for a small convex-ish polygon.</summary>
        private static bool Inside(Vector2[] polygon, Vector2 point)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Length - 1; i < polygon.Length; j = i++)
            {
                if (polygon[i].y > point.y != polygon[j].y > point.y &&
                    point.x < (polygon[j].x - polygon[i].x) * (point.y - polygon[i].y) / (polygon[j].y - polygon[i].y) + polygon[i].x)
                {
                    inside = !inside;
                }
            }

            return inside;
        }
    }

    /// <summary>Holding the modifier turns a left click into "mark this stack".</summary>
    [HarmonyPatch(typeof(InventoryGui), "OnSelectedItem")]
    internal static class InventoryGuiSelectFavoritePatch
    {
        private static readonly FieldInfo DragGo = AccessTools.Field(typeof(InventoryGui), "m_dragGo");

        private static bool Prefix(InventoryGui __instance, ItemDrop.ItemData item, InventoryGrid.Modifier mod)
        {
            if (!ModConfig.FavoritesEnabled.Value || item == null || mod != InventoryGrid.Modifier.Select)
            {
                return true;
            }

            if (!Favorites.ModifierHeld())
            {
                return true;
            }

            // Mid drag the click has to finish the drag, marking would strand the item.
            if (DragGo != null && DragGo.GetValue(__instance) is GameObject drag && drag != null)
            {
                return true;
            }

            bool favorite = Favorites.Toggle(item);
            Favorites.Announce(item, favorite);

            if (ModConfig.Verbose.Value)
            {
                GorilaChestModPlugin.Log.LogInfo(
                    $"{item.m_shared.m_name} is {(favorite ? "now a favorite" : "no longer a favorite")}.");
            }

            // The click was spent on the mark, so the vanilla pick up is skipped.
            return false;
        }
    }

    /// <summary>Paints the star on the slots whose stack is marked.</summary>
    [HarmonyPatch(typeof(InventoryGrid), "UpdateGui")]
    internal static class InventoryGridFavoriteMarkerPatch
    {
        private static readonly FieldInfo Elements = AccessTools.Field(typeof(InventoryGrid), "m_elements");

        private static void Postfix(InventoryGrid __instance)
        {
            if (!ModConfig.FavoritesEnabled.Value || Elements == null)
            {
                return;
            }

            if (!(Elements.GetValue(__instance) is List<InventoryElement> elements) || elements.Count == 0)
            {
                return;
            }

            Inventory inventory = __instance.GetInventory();
            if (inventory == null)
            {
                return;
            }

            foreach (InventoryElement element in elements)
            {
                FavoriteMarker.Apply(element, favorite: false);
            }

            int width = inventory.GetWidth();
            foreach (ItemDrop.ItemData item in inventory.GetAllItems())
            {
                if (!Favorites.IsFavorite(item))
                {
                    continue;
                }

                // Same index the grid itself uses to find a slot's element.
                int index = item.m_gridPos.y * width + item.m_gridPos.x;
                if (index >= 0 && index < elements.Count)
                {
                    FavoriteMarker.Apply(elements[index], favorite: true);
                }
            }
        }
    }

    /// <summary>
    /// The chest's own stack button skips whatever you have equipped. A marked
    /// stack now counts as equally untouchable, which is the whole point of the
    /// mark.
    /// </summary>
    [HarmonyPatch(typeof(Inventory), nameof(Inventory.StackAll))]
    internal static class InventoryStackAllFavoritePatch
    {
        internal static bool IsProtected(Humanoid character, ItemDrop.ItemData item)
        {
            return (character != null && character.IsItemEquiped(item)) || Favorites.IsFavorite(item);
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            return CallRedirector.Apply(
                instructions,
                "Inventory.StackAll",
                new CallSwap(
                    AccessTools.Method(typeof(Humanoid), nameof(Humanoid.IsItemEquiped)),
                    AccessTools.Method(typeof(InventoryStackAllFavoritePatch), nameof(IsProtected))));
        }
    }

    /// <summary>
    /// Pouring one stack into another keeps the destination object, so a mark on
    /// the incoming stack would be lost. Either side being marked keeps the mark.
    /// </summary>
    [HarmonyPatch(typeof(Inventory), "AddItem",
        new[] { typeof(ItemDrop.ItemData), typeof(int), typeof(int), typeof(int), typeof(bool) })]
    internal static class InventoryAddItemFavoriteMergePatch
    {
        private static void Prefix(Inventory __instance, int x, int y, out ItemDrop.ItemData __state)
        {
            __state = __instance.GetItemAt(x, y);
        }

        private static void Postfix(Inventory __instance, ItemDrop.ItemData item, int x, int y, ItemDrop.ItemData __state)
        {
            if (!ModConfig.FavoritesEnabled.Value)
            {
                return;
            }

            ItemDrop.ItemData destination = __instance.GetItemAt(x, y);
            if (destination == null || destination == item)
            {
                return;
            }

            // Only a merge, where the slot already held a stack of the same kind.
            if (__state != null && ReferenceEquals(__state, destination))
            {
                Favorites.CarryOverOnMerge(destination, item);
            }
        }
    }
}
