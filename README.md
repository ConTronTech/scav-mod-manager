# Scav Mod Manager

A mod manager and custom item framework for **Scav Prototype: Casualties Unknown** (`ScavDemoV5PreTesting5`).

Drop in two DLLs, add items with JSON files. No coding required.

---

## 🚀 Installation (2 minutes)

### Step 1: Download
Download the latest release from the [Releases](../../releases) page, or grab the files from `prebuilt/`.

### Step 2: Install DLLs
Copy these two files into your game's `Managed` folder:

```
CasualtiesUnknownDemo/
└── CasualtiesUnknown_Data/
    └── Managed/
        ├── Assembly-CSharp.dll    ← REPLACE this one
        └── ModManager.dll         ← ADD this one
```

> ⚠️ **Back up your original `Assembly-CSharp.dll` first!** Just rename it to `Assembly-CSharp.dll.bak`

### Step 3: Install Mod Items
Copy the `mods` folder into `CasualtiesUnknown_Data/`:

```
CasualtiesUnknownDemo/
└── CasualtiesUnknown_Data/
    └── mods/
        └── items/
            ├── _TEMPLATE/
            ├── plasticfork/
            ├── tastelesscube/
            └── railwaylight/
```

### Step 4: Play
Launch the game. Press **M** to open the mod menu.

---

## 🎮 Controls

| Key | Action |
|-----|--------|
| **M** | Open/close mod menu |
| **F5** | Hot reload all mods (no restart needed) |

---

## 📦 Mod Menu Features

- **Player Tab** — Health, hunger, thirst, stamina sliders. XP control. God mode. Speed hack.
- **Spawner Tab** — Spawn any item (vanilla + custom) with search filter.
- **World Tab** — Enemy radar, trader/survivor detection.
- **Cheats Tab** — Toggle god mode, infinite stamina, etc.
- **Info Tab** — Live player stats + full game log viewer (color-coded errors/warnings).

---

## 🔧 Creating Custom Items

### The Easy Way

1. Copy the `_TEMPLATE` folder inside `mods/items/`
2. Rename it to your item's ID (lowercase, no spaces — e.g. `mysword`)
3. Edit `item.json` with your stats
4. Replace `sprite.png` with your item's icon (any size, will be auto-scaled)
5. Press **F5** in-game to load it — no restart needed!

### item.json Reference

```json
{
  "id": "mysword",
  "name": "My Custom Sword",
  "description": "A very cool sword.",
  "cloneFrom": "yourknife",
  "category": "Melee",
  "weight": 1.5,
  "value": 100,

  "isWeapon": true,
  "damage": 50,
  "knockBack": 10,
  "cooldown": 0.4,
  "attackCooldownMult": 1.0,
  "distance": 1.5,
  "staminaUse": 5,
  "piercing": false,
  "rotateAmount": 45,

  "isFood": false,
  "foodValue": 0,
  "waterValue": 0,

  "lightIntensity": 0,
  "lightRadius": 0,
  "lightColorR": 1.0,
  "lightColorG": 1.0,
  "lightColorB": 1.0
}
```

### Key Fields

| Field | Description |
|-------|-------------|
| `id` | Unique item ID (must match folder name) |
| `name` | Display name in-game |
| `description` | Item tooltip text |
| `cloneFrom` | Base game item to clone the prefab from (required — see list below) |
| `category` | Item category: `Melee`, `Ranged`, `Food`, `Medical`, `Utility`, `Misc` |
| `isWeapon` | Set `true` for melee weapons |
| `damage` | Attack damage |
| `cooldown` | Seconds between attacks |
| `distance` | Attack reach |
| `isFood` | Set `true` for consumables |
| `foodValue` / `waterValue` | How much hunger/thirst it restores |
| `lightIntensity` | Light brightness (0 = no light). Clone from `roselight` for 360° lights |
| `lightRadius` | Light range |
| `lightColorR/G/B` | Light color (0.0-1.0 each) |

### Good Base Items to Clone From

| Clone From | Use For |
|------------|---------|
| `yourknife` | Melee weapons |
| `youraxe` | Heavy melee weapons |
| `bread` | Food items |
| `roselight` | 360° light sources |
| `flashlight` | Directional lights |
| `bag` | Containers |

> 💡 `cloneFrom` determines the prefab (3D model, animations, physics). Your custom sprite replaces the icon.

---

## 📁 Example Items Included

| Item | Type | Description |
|------|------|-------------|
| `plasticfork` | Weapon | Absurdly powerful fork (999999 damage) |
| `tastelesscube` | Food | Mysterious food cube |
| `railwaylight` | Light | Red 360° railway signal light |

---

## 🛠️ Building From Source

If you want to modify the mod manager itself or recompile:

### Requirements
- .NET SDK (any recent version with `csc` compiler)
- Mono (for `monodis`) — optional, for IL inspection
- The game's original `Assembly-CSharp.dll`
- NuGet package: `Mono.Cecil` (auto-restored by build script)

### Build
```bash
cd src/
# Edit build.sh paths to point to your game installation
bash build.sh
```

The build script:
1. Restores the original `Assembly-CSharp.dll` from backup
2. Compiles `ModManager.dll` from source
3. Compiles and runs `GenericPatcher.cs` to inject 4 hooks into the game DLL
4. Outputs patched `Assembly-CSharp.dll` + `ModManager.dll`

### Architecture

The mod system uses 4 IL hooks injected into `Assembly-CSharp.dll`:

1. **SetupItems Hook** → Calls `ItemLoader.RegisterAllCustomItems()` to load JSON items into the game's item registry
2. **Ext.Create Hook** → Intercepts item spawning to build custom items from JSON definitions
3. **Spawn Command Hook** → Adds custom item IDs to the console's autocomplete
4. **Init Hook** → Boots `ModManager.Init()` for the mod menu UI

Items are defined as pure data (JSON + sprite) and loaded at runtime — no recompilation needed to add new items.

---

## ⚠️ Compatibility

- **Game Version:** `ScavDemoV5PreTesting5` (Casualties Unknown Demo)
- **Platform:** Windows (also works on Linux via Wine/Lutris/Proton)
- **Engine:** Unity Mono (not IL2CPP)

> This mod directly patches the game's DLL. It will **not** work with other game versions without rebuilding from source.

---

## 📜 License

MIT — do whatever you want with it.

---

## Credits

Built by Contolis & Jeffery.
