// CustomItem.cs — Item definition struct for the mod system

using System;

[Serializable]
public class CustomItem
{
    // === Identity ===
    public string id;              // Unique internal ID (e.g. "plasticfork")
    public string name;            // Display name (e.g. "Plastic Fork")
    public string description;     // Item description
    public string basePrefab;      // Which game prefab to clone (e.g. "machete", "pistol")

    // === Stats ===
    public float weight = 1f;
    public int value = 10;
    public string category = "tool";  // tool, food, medical, drug, container, utility, water, custom
    public string tags = "";          // Comma-separated tags
    public int intRequirement = 0;    // INT level needed to identify

    // === Combat ===
    public float damage = 50f;
    public float structuralDamage = 50f;
    public float knockback = 100f;
    public float cooldown = 0.3f;
    public float cooldownMult = 0.5f;
    public float distance = 4f;
    public float staminaUse = 0.3f;
    public bool piercing = false;
    public float volume = 0.3f;
    public float rotation = 15.5f;

    // === Durability ===
    public float maxDurability = 100f;
    public bool unbreakable = false;
    public float cuttingQuality = 0f;   // For cutting items

    // === Sprite ===
    public string sprite = "";          // Filename in mods/sprites/ (e.g. "plasticfork.png")
    public string spriteBase64 = "";    // OR embed as base64 PNG
    public int spriteWidth = 0;         // Original sprite width (for rect)
    public int spriteHeight = 0;        // Original sprite height
    public int texWidth = 16;           // Power-of-2 texture width
    public int texHeight = 16;          // Power-of-2 texture height

    // === Light (for items cloned from light prefabs like roselight, flashlight) ===
    public float lightIntensity = -1f;  // -1 = keep prefab default
    public float lightRadius = -1f;     // -1 = keep prefab default
    public float lightColorR = -1f;     // -1 = keep prefab default
    public float lightColorG = -1f;
    public float lightColorB = -1f;

    // === Flags ===
    public bool isWeapon = true;
    public bool hasUseAction = true;    // Register an attack action
    public bool autoAttack = true;      // Hold LMB to auto-swing
}
