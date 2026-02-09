// ItemRegistry.cs — Compiled custom items (always loaded)
// For hot-loadable items, use mods/items/<id>/item.json instead
// Items here are loaded FIRST, then JSON items override/add to them

using System.Collections.Generic;

public static class ItemRegistry
{
    public static List<CustomItem> Items = new List<CustomItem>
    {
        // Compiled items go here if you want items baked into the DLL
        // For most items, use the mods/items/ JSON folder instead
    };
}
