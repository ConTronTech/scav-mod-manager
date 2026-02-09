<div align="center">

# Scav Mod Manager

### A mod manager & custom item framework for Scav Prototype: Casualties Unknown

[![Release](https://img.shields.io/github/v/release/ConTronTech/scav-mod-manager?style=flat-square&color=blue)](../../releases)
[![License](https://img.shields.io/github/license/ConTronTech/scav-mod-manager?style=flat-square)](LICENSE)
[![Game](https://img.shields.io/badge/game-ScavDemoV5PreTesting5-orange?style=flat-square)](https://orsonik.itch.io/scav-prototype)

**Drop in two DLLs. Add items with JSON. No coding required.**

[Download](#-quick-start) · [Create Items](#-creating-custom-items) · [Full Reference](item_json_reference.txt) · [Build From Source](#%EF%B8%8F-building-from-source)

</div>

---

## Features

-  **In-game mod menu** — Press `M` to open
-  **Item spawner** — Spawn any of the 265 vanilla items or your custom ones
-  **Player controls** — Health, hunger, thirst, stamina, XP
-  **World radar** — Enemy, trader, and survivor detection
-  **Cheats** — God mode, infinite stamina, speed hack
-  **Live log viewer** — Color-coded game logs with search
-  **JSON item system** — Add items with just a JSON file + sprite
-  **Hot reload** — Press `F5` to load new items without restarting

---

## Quick Start

### 1. Download

Grab the latest zip from [**Releases**](../../releases/latest).

### 2. Install

```
CasualtiesUnknownDemo/
└── CasualtiesUnknown_Data/
    ├── Managed/
    │   ├── Assembly-CSharp.dll   ← replace (back up original first!)
    │   └── ModManager.dll        ← add this
    └── mods/
        └── items/                ← add this folder
            ├── _TEMPLATE/
            ├── plasticfork/
            ├── tastelesscube/
            └── railwaylight/
```

### 3. Play

Launch the game. Press **M** for the mod menu. Press **F5** to hot reload mods.

> **Always back up your original `Assembly-CSharp.dll` before replacing it!**

---

## Creating Custom Items

### Step by step

1. Copy `mods/items/_TEMPLATE/` → rename to your item ID (e.g. `mysword`)
2. Edit `item.json` with your stats
3. Replace `sprite.png` with your item's icon
4. Press **F5** in-game — done!

### Example: Melee Weapon

```json
{
  "id": "mysword",
  "name": "My Custom Sword",
  "description": "A very cool sword.",
  "cloneFrom": "yourknife",
  "category": "tool",
  "weight": 1.5,
  "value": 100,
  "usable": true,
  "usableWithLMB": true,
  "autoAttack": true,
  "isWeapon": true,
  "damage": 50,
  "knockBack": 200,
  "cooldown": 0.4,
  "distance": 4.0,
  "staminaUse": 1.5,
  "rotateAmount": 15
}
```

### Example: Food Item

```json
{
  "id": "mysteryjerky",
  "name": "Mystery Jerky",
  "description": "Don't ask what animal this came from.",
  "cloneFrom": "bread",
  "category": "food",
  "weight": 0.3,
  "value": 15,
  "usable": true,
  "isFood": true,
  "foodValue": 30,
  "waterValue": -5,
  "destroyAtZeroCondition": true
}
```

### Example: Light Source

```json
{
  "id": "redlantern",
  "name": "Red Lantern",
  "description": "Casts an eerie red glow.",
  "cloneFrom": "roselight",
  "category": "utility",
  "weight": 1.0,
  "value": 40,
  "lightIntensity": 3.0,
  "lightRadius": 10.0,
  "lightColorR": 1.0,
  "lightColorG": 0.1,
  "lightColorB": 0.1
}
```

### Clone Sources

| Clone From | Best For |
|:-----------|:---------|
| `yourknife` | Small melee weapons |
| `youraxe` | Heavy melee weapons |
| `bread` | Food items |
| `roselight` | 360° light sources |
| `flashlight` | Directional lights |
| `bag` | Containers |
| `waterbottle` | Liquid containers |
| `pistol` | Firearms |

> See [`item_json_reference.txt`](item_json_reference.txt) for the **complete list** of every supported field — weapons, armor, wearables, food, lights, decay, crafting, and more.

---

## Included Example Items

| Item | Type | Description |
|:-----|:-----|:------------|
| `plasticfork` | Weapon | Absurdly powerful fork (999,999 damage) |
| `tastelesscube` | Food | Mysterious food cube |
| `railwaylight` | Light | Red 360° railway signal light |

---

## How It Works

The mod system injects **4 hooks** into `Assembly-CSharp.dll` using [Mono.Cecil](https://github.com/jbevain/cecil):

| Hook | What It Does |
|:-----|:-------------|
| **SetupItems** | Loads all JSON items into the game's item registry |
| **Ext.Create** | Intercepts item spawning to build custom items from JSON |
| **Spawn Command** | Adds custom item IDs to console autocomplete |
| **Init** | Boots the mod menu UI |

All items are **pure data** (JSON + sprite) loaded at runtime. No recompilation needed to add new content.

---

## Building From Source

<details>
<summary>Click to expand</summary>

### Requirements

- Mono / .NET SDK (`mcs` compiler)
- NuGet package: `Mono.Cecil` (auto-restored by build script)
- The game's original `Assembly-CSharp.dll`

### Build

```bash
cd src/
# Edit build.sh paths to point to your game installation
bash build.sh
```

### What the build does

1. Restores original `Assembly-CSharp.dll` from backup
2. Compiles `ModManager.dll` from source
3. Compiles and runs `GenericPatcher.cs` to inject hooks
4. Outputs patched DLLs ready to install

### Source Files

| File | Purpose |
|:-----|:--------|
| `ModManager.cs` | In-game UI (tabs, spawner, cheats, log viewer) |
| `ItemLoader.cs` | JSON item loading, prefab interception, attack/food handlers |
| `CustomItem.cs` | Item definition class with all stat fields |
| `ItemRegistry.cs` | Compiled item registry (empty — items are JSON now) |
| `GenericPatcher.cs` | Mono.Cecil patcher that injects the 4 hooks |
| `build.sh` | Full build pipeline |

</details>

---

## Compatibility

| | |
|:--|:--|
| **Game Version** | `ScavDemoV5PreTesting5` (Casualties Unknown Demo) |
| **Platform** | Windows · Linux (Wine/Lutris/Proton) |
| **Engine** | Unity Mono (not IL2CPP) |

> This mod patches the game's DLL directly. Other game versions will need a rebuild from source.

---

## License

[MIT](LICENSE) — do whatever you want with it.

---

<div align="center">

Built by **Contolis** & **Jeffery**

</div>
