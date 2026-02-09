// CustomItem.cs — Item definition struct for the mod system
// Covers ALL ItemInfo, AttackInfo, and component fields from the game

using System;

[Serializable]
public class CustomItem
{
    // === Identity ===
    public string id;              // Unique internal ID (must match folder name)
    public string name;            // Display name
    public string description;     // Item tooltip/description
    public string basePrefab;      // "cloneFrom" — which game prefab to clone

    // === General Properties ===
    public string category = "custom";  // tool, utility, food, water, medical, drug, container, trash, custom, unobtainable
    public float weight = 1f;
    public int value = 0;
    public string tags = "";           // Comma-separated tags: tool, sharp, dressing, cangetwet, gun, ammo, backflip
    public float slotRotation = 0f;    // Rotation in inventory slot (degrees)
    public float rotSpeed = 0f;        // Decay/condition loss speed multiplier (0 = no decay)

    // === Usability Flags ===
    public bool usable = false;         // Can be "used" (right-click/use key)
    public bool usableWithLMB = false;  // Can be used with left mouse button
    public bool usableOnLimb = false;   // Can be applied to a body limb (bandages, splints)
    public bool autoAttack = false;     // Auto-attack when holding LMB
    public bool onlyHoldInHands = false;// Can only be held, not stored

    // === Combat / Weapon ===
    public bool isWeapon = false;       // Enable attack behavior
    public float damage = 0f;
    public float structuralDamage = 0f;
    public float knockback = 0f;
    public float cooldown = 0f;         // Base cooldown seconds
    public float attackCooldownMult = 1f;// Cooldown multiplier
    public float distance = 1f;         // Attack reach/range
    public float staminaUse = 0f;
    public bool piercing = false;
    public float rotateAmount = 0f;     // Visual swing arc rotation (degrees)
    public float volume = 0f;           // Attack sound volume (attracts enemies)
    public bool physicalSwing = true;
    public bool doAttackAnim = true;
    public bool metalMoreDamage = false;
    public string attackAnim = "SwingAnim";  // SwingAnim, ClawAnim, LaserAnim
    public string swingSounds = "BSSwing1,BSSwing2,BSSwing3,BSSwing4";

    // === Food / Consumable ===
    public bool isFood = false;
    public float foodValue = 0f;        // Hunger restored
    public float waterValue = 0f;       // Thirst restored

    // === Light ===
    public float lightIntensity = -1f;  // -1 = keep prefab default. 0+ = override
    public float lightRadius = -1f;
    public float lightColorR = -1f;
    public float lightColorG = -1f;
    public float lightColorB = -1f;

    // === Wearable / Armor ===
    public bool wearable = false;
    public string desiredWearLimb = ""; // Head, UpTorso, ThighF, ThighB, HandF, DownArmF
    public string wearSlotId = "";      // hat, eyes, mouth, blindfold, balaclava, neck, back, torso, outertorso, torsofront, bandolier, belt, thigh, thighback, hands, arms, wraps, knees, feet
    public float wearableArmor = 0f;    // Damage reduction (0.0-1.0)
    public float wearableIsolation = 0f;// Cold protection
    public float wearableHitDurabilityLossMultiplier = 0f;
    public int wearableVisualOffset = 0;// Visual layer priority
    public float jumpHeightMultChange = 0f;// Jump height modifier when worn

    // === Decay & Condition ===
    public bool destroyAtZeroCondition = false;
    public bool scaleWeightWithCondition = false;
    public int decayInfo = 0;           // Bitfield: 1=no decay empty container, 2=no decay in container, 4=no decay stationary, 16=battery powered
    public float decayMinutes = 0f;     // Time to fully decay (alternative to rotSpeed)

    // === Crafting & Economy ===
    public bool combineable = false;    // Can stack with identical items
    public bool ignoreDepression = false;// Ignore depression debuff on use
    public int intRequirement = 0;      // Min INT to recognize (0 = always visible)
    public string qualities = "";       // Comma-separated crafting quality IDs: dressing, fabric, metal, wood, sharp, fire

    // === Sprite ===
    public string sprite = "";          // Sprite filename (optional, auto-detects sprite.png)
    public string spriteBase64 = "";    // Loaded sprite as base64
    public int spriteWidth = 0;
    public int spriteHeight = 0;
    public int texWidth = 16;
    public int texHeight = 16;
}
