using System;

[Serializable]
public class CustomItem
{
    public string id;
    public string name;
    public string description;
    public string basePrefab;

    public string category = "custom";
    public float weight = 1f;
    public int value = 0;
    public string tags = "";
    public float slotRotation = 0f;
    public float rotSpeed = 0f;

    public bool usable = false;
    public bool usableWithLMB = false;
    public bool usableOnLimb = false;
    public bool autoAttack = false;
    public bool onlyHoldInHands = false;

    public bool isWeapon = false;
    public float damage = 0f;
    public float structuralDamage = 0f;
    public float knockback = 0f;
    public float cooldown = 0f;
    public float attackCooldownMult = 1f;
    public float distance = 1f;
    public float staminaUse = 0f;
    public bool piercing = false;
    public float rotateAmount = 0f;
    public float volume = 0f;
    public bool physicalSwing = true;
    public bool doAttackAnim = true;
    public bool metalMoreDamage = false;
    public string attackAnim = "SwingAnim";
    public string swingSounds = "BSSwing1,BSSwing2,BSSwing3,BSSwing4";

    // === Food / Consumable ===
    public bool isFood = false;
    public float foodValue = 0f;
    public float waterValue = 0f;

    // === Light ===
    public float lightIntensity = -1f;
    public float lightRadius = -1f;
    public float lightColorR = -1f;
    public float lightColorG = -1f;
    public float lightColorB = -1f;

    // === Wearable / Armor ===
    public bool wearable = false;
    public string desiredWearLimb = "";
    public string wearSlotId = "";
    public float wearableArmor = 0f;
    public float wearableIsolation = 0f;
    public float wearableHitDurabilityLossMultiplier = 0f;
    public int wearableVisualOffset = 0;
    public float jumpHeightMultChange = 0f;

    // === Decay & Condition ===
    public bool destroyAtZeroCondition = false;
    public bool scaleWeightWithCondition = false;
    public int decayInfo = 0;
    public float decayMinutes = 0f;

    // === Crafting & Economy ===
    public bool combineable = false;
    public bool ignoreDepression = false;
    public int intRequirement = 0;
    public string qualities = "";

    // === Sprite ===
    public string sprite = "";
    public string spriteBase64 = "";
    public int spriteWidth = 0;
    public int spriteHeight = 0;
    public int texWidth = 16;
    public int texHeight = 16;
}
