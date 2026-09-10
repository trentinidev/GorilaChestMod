namespace CraftFromChests
{
    /// <summary>
    /// Tracks whether the game is currently inside a building-related code path
    /// (build hud requirement display, piece placement) so that the
    /// <see cref="ModConfig.UseForBuilding"/> option can be honoured. The patched
    /// methods can nest, hence a depth counter instead of a plain flag.
    /// </summary>
    internal static class BuildContext
    {
        private static int _depth;

        internal static bool Active => _depth > 0;

        internal static void Enter() => _depth++;

        internal static void Exit()
        {
            if (_depth > 0)
            {
                _depth--;
            }
        }
    }
}
