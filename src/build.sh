#!/bin/bash
# v2 Build Pipeline — Generic mod system with JSON item loading
# 1. Restore DLL from backup
# 2. Compile ModManager.dll (with ItemLoader, ItemRegistry, CustomItem)
# 3. Generate item ID list for patcher (from JSON + compiled)
# 4. Run GenericPatcher on Assembly-CSharp.dll
# 5. Copy + zip (including mods template)

set -e

MANAGED="/root/clawd/projects/scav/game/CasualtiesUnknownDemo/CasualtiesUnknown_Data/Managed"
V2DIR="/root/clawd/projects/scav/modmanager/v2"
V1DIR="/root/clawd/projects/scav/modmanager"
CECIL="/usr/lib/mono/gac/Mono.Cecil/0.11.0.0__0738eb9f132ed756/Mono.Cecil.dll"

echo "=== V2 MOD SYSTEM BUILD ==="
echo ""

echo "=== Step 1: Restore from backup ==="
cp "$MANAGED/Assembly-CSharp.dll.bak" "$MANAGED/Assembly-CSharp.dll"

echo "=== Step 2: Compile ModManager.dll ==="
cd "$V2DIR"
mcs -target:library \
  -reference:"$MANAGED/Assembly-CSharp.dll" \
  -reference:"$MANAGED/UnityEngine.dll" \
  -reference:"$MANAGED/UnityEngine.CoreModule.dll" \
  -reference:"$MANAGED/UnityEngine.IMGUIModule.dll" \
  -reference:"$MANAGED/UnityEngine.InputLegacyModule.dll" \
  -reference:"$MANAGED/UnityEngine.Physics2DModule.dll" \
  -reference:"$MANAGED/UnityEngine.TextRenderingModule.dll" \
  -reference:"$MANAGED/UnityEngine.ImageConversionModule.dll" \
  -reference:"$MANAGED/UnityEngine.UIModule.dll" \
  -reference:"$MANAGED/UnityEngine.UI.dll" \
  -reference:"$MANAGED/Unity.TextMeshPro.dll" \
  -reference:"$MANAGED/netstandard.dll" \
  "$V1DIR/ModManager.cs" \
  "$V2DIR/CustomItem.cs" \
  "$V2DIR/ItemRegistry.cs" \
  "$V2DIR/ItemLoader.cs" \
  -out:ModManager.dll
echo "  ModManager.dll compiled"

echo "=== Step 3: Generate item ID list ==="
# Get IDs from compiled registry
grep 'id = "' "$V2DIR/ItemRegistry.cs" 2>/dev/null | grep -v '//' | sed 's/.*id = "\([^"]*\)".*/\1/' > "$V2DIR/custom_item_ids.txt"
# Get IDs from JSON mods
for f in "$V2DIR/mods_template/items"/*/item.json; do
  [ -f "$f" ] || continue
  dirname=$(basename $(dirname "$f"))
  [ "$dirname" = "_TEMPLATE" ] && continue
  grep '"id"' "$f" | sed 's/.*"id"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/' >> "$V2DIR/custom_item_ids.txt"
done
sort -u "$V2DIR/custom_item_ids.txt" -o "$V2DIR/custom_item_ids.txt"
echo "  IDs: $(cat "$V2DIR/custom_item_ids.txt" | tr '\n' ' ')"

echo "=== Step 4: Compile GenericPatcher ==="
mcs -reference:"$CECIL" GenericPatcher.cs -out:GenericPatcher.exe

echo "=== Step 5: Patch Assembly-CSharp.dll ==="
mono GenericPatcher.exe "$MANAGED/Assembly-CSharp.dll" "$V2DIR/ModManager.dll"

echo ""
echo "=== Step 6: Copy ModManager.dll ==="
cp "$V2DIR/ModManager.dll" "$MANAGED/ModManager.dll"

echo "=== Step 7: Create distribution zip ==="
ZIPFILE="/root/clawd/temp/scav_modmanager_v2.zip"
rm -f "$ZIPFILE"

# Add DLLs
cd "$MANAGED"
zip -j "$ZIPFILE" Assembly-CSharp.dll ModManager.dll

# Add mods folder template
cd "$V2DIR/mods_template"
zip -r "$ZIPFILE" . -x "*/._*"

echo ""
echo "=== V2 BUILD COMPLETE ==="
echo "Zip: $ZIPFILE"
echo ""
echo "Install:"
echo "  1. Drop Assembly-CSharp.dll + ModManager.dll into Managed/"
echo "  2. Copy 'items/' and 'README.txt' into CasualtiesUnknown_Data/mods/"
echo "  3. Add new items: copy _TEMPLATE, edit item.json, add sprite.png"
echo "  4. Press M in-game for mod menu"
