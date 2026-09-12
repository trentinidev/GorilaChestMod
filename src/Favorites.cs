using UnityEngine;

namespace GorilaChestMod
{
    /// <summary>
    /// Marks a stack as one that automatic moving must leave alone.
    ///
    /// The mark lives in the item's own custom data, the dictionary the game
    /// serialises alongside durability and quality. That buys three things for
    /// free: it survives saving and loading, it is copied by ItemData.Clone so a
    /// split half stays marked, and it travels with the stack if you move it by
    /// hand. Nothing extra is written to disk and nothing is keyed by slot, so a
    /// reorganised inventory keeps its marks.
    /// </summary>
    internal static class Favorites
    {
        /// <summary>Namespaced, since custom data is shared with the game and other mods.</summary>
        private const string Key = "GorilaChestMod.favorite";

        private const string Yes = "1";

        internal static bool IsFavorite(ItemDrop.ItemData item)
        {
            return ModConfig.FavoritesEnabled.Value &&
                   item?.m_customData != null &&
                   item.m_customData.TryGetValue(Key, out string value) &&
                   value == Yes;
        }

        /// <summary>Returns the state the stack ended up in.</summary>
        internal static bool Toggle(ItemDrop.ItemData item)
        {
            return Set(item, !IsFavorite(item));
        }

        internal static bool Set(ItemDrop.ItemData item, bool favorite)
        {
            if (item == null)
            {
                return false;
            }

            if (item.m_customData == null)
            {
                item.m_customData = new System.Collections.Generic.Dictionary<string, string>();
            }

            if (favorite)
            {
                item.m_customData[Key] = Yes;
            }
            else
            {
                item.m_customData.Remove(Key);
            }

            return favorite;
        }

        /// <summary>
        /// Merging pours one stack into another and keeps the destination object,
        /// which would quietly drop the mark. A merge where either side was marked
        /// stays marked.
        /// </summary>
        internal static void CarryOverOnMerge(ItemDrop.ItemData destination, ItemDrop.ItemData source)
        {
            if (destination != null && source != null && IsFavorite(source))
            {
                Set(destination, favorite: true);
            }
        }

        internal static void Announce(ItemDrop.ItemData item, bool favorite)
        {
            if (!ModConfig.FavoritesAnnounce.Value || Player.m_localPlayer == null || item?.m_shared == null)
            {
                return;
            }

            string name = Localization.instance.Localize(item.m_shared.m_name);
            Player.m_localPlayer.Message(
                MessageHud.MessageType.TopLeft,
                favorite ? name + ": favorite" : name + ": favorite removed");
        }

        /// <summary>True while the player is holding the key that turns a click into a mark.</summary>
        internal static bool ModifierHeld()
        {
            KeyCode key = ModConfig.FavoriteModifier.Value;
            if (key == KeyCode.None)
            {
                return false;
            }

            return Input.GetKey(key) || Input.GetKey(PairedSide(key));
        }

        /// <summary>Alt, control and shift come in pairs, and players use whichever is nearer.</summary>
        private static KeyCode PairedSide(KeyCode key)
        {
            switch (key)
            {
                case KeyCode.LeftAlt: return KeyCode.RightAlt;
                case KeyCode.RightAlt: return KeyCode.LeftAlt;
                case KeyCode.LeftControl: return KeyCode.RightControl;
                case KeyCode.RightControl: return KeyCode.LeftControl;
                case KeyCode.LeftShift: return KeyCode.RightShift;
                case KeyCode.RightShift: return KeyCode.LeftShift;
                default: return key;
            }
        }
    }
}
