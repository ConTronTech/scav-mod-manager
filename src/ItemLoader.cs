// ItemLoader.cs — Handles runtime registration and spawning of custom items
// Loads items from:
//   1. Compiled ItemRegistry.cs (built-in items)
//   2. JSON files from CasualtiesUnknown_Data/mods/items/<id>/item.json (hot-loadable)
// Called by generic hooks in Assembly-CSharp.dll

using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

public static class ItemLoader
{
    public static Dictionary<string, CustomItem> customItems = new Dictionary<string, CustomItem>();
    private static Dictionary<string, Sprite> spriteCache = new Dictionary<string, Sprite>();
    private static string modsPath;

    // === CALLED FROM PATCHED SetupItems() ===
    public static void RegisterAllCustomItems()
    {
        // Register log hook before anything else so all messages are captured
        try
        {
            var hookField = typeof(ModManager).GetField("logHookRegistered", BindingFlags.NonPublic | BindingFlags.Static);
            if (hookField != null && !(bool)hookField.GetValue(null))
            {
                var method = typeof(ModManager).GetMethod("InitLogHook", BindingFlags.Public | BindingFlags.Static);
                if (method != null) method.Invoke(null, null);
            }
        }
        catch (Exception) { }

        modsPath = Path.Combine(Application.dataPath, "mods");
        Debug.Log("[ItemLoader] Mods path: " + modsPath);

        // Phase 1: Load compiled registry items
        int compiled = 0;
        foreach (var item in ItemRegistry.Items)
        {
            try
            {
                RegisterItem(item);
                customItems[item.id] = item;
                compiled++;
                Debug.Log("[ItemLoader] Registered (compiled): " + item.id);
            }
            catch (Exception e)
            {
                Debug.LogError("[ItemLoader] Failed to register " + item.id + ": " + e.Message + "\n" + e.StackTrace);
            }
        }

        // Phase 2: Scan mods/items/ for JSON definitions
        int loaded = 0;
        string itemsDir = Path.Combine(modsPath, "items");
        if (Directory.Exists(itemsDir))
        {
            foreach (var dir in Directory.GetDirectories(itemsDir))
            {
                string jsonPath = Path.Combine(dir, "item.json");
                if (!File.Exists(jsonPath)) continue;

                try
                {
                    var item = LoadItemFromJson(jsonPath, dir);
                    if (item != null && !string.IsNullOrEmpty(item.id))
                    {
                        RegisterItem(item);
                        customItems[item.id] = item;
                        loaded++;
                        Debug.Log("[ItemLoader] Registered (JSON): " + item.id + " from " + dir);
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("[ItemLoader] Failed to load " + jsonPath + ": " + e.Message);
                }
            }
        }
        else
        {
            // Create the folder structure for the user
            try
            {
                Directory.CreateDirectory(itemsDir);
                Directory.CreateDirectory(Path.Combine(modsPath, "sprites"));
                Debug.Log("[ItemLoader] Created mods directory structure at " + modsPath);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[ItemLoader] Could not create mods directory: " + e.Message);
            }
        }

        Debug.Log("[ItemLoader] Registration complete. " + compiled + " compiled + " + loaded + " JSON = " + customItems.Count + " total items.");

        // NOTE: Do NOT re-init loot pool — CorpseScript uses Resources.Load directly
        // which returns null for custom items (no real prefabs). Custom items are
        // only available via spawn command / console, not natural loot drops.
        // To fix natural loot, CorpseScript.Start() would need patching too.
    }

    // === JSON PARSER (no external dependencies — manual parsing) ===
    static CustomItem LoadItemFromJson(string path, string dir)
    {
        string json = File.ReadAllText(path);
        var item = new CustomItem();

        // Simple JSON parser — handles our flat structure
        item.id = GetJsonString(json, "id");
        item.name = GetJsonString(json, "name");
        item.description = GetJsonString(json, "description");
        item.basePrefab = GetJsonString(json, "cloneFrom");
        if (string.IsNullOrEmpty(item.basePrefab))
            item.basePrefab = GetJsonString(json, "basePrefab"); // legacy compat
        if (string.IsNullOrEmpty(item.basePrefab)) item.basePrefab = "machete";

        // General properties
        item.category = GetJsonString(json, "category");
        if (string.IsNullOrEmpty(item.category)) item.category = "custom";
        item.weight = GetJsonFloat(json, "weight", 1f);
        item.value = GetJsonInt(json, "value", 0);
        item.tags = GetJsonString(json, "tags");
        item.slotRotation = GetJsonFloat(json, "slotRotation", 0f);
        item.rotSpeed = GetJsonFloat(json, "rotSpeed", 0f);

        // Usability flags
        item.usable = GetJsonBool(json, "usable", false);
        item.usableWithLMB = GetJsonBool(json, "usableWithLMB", false);
        item.usableOnLimb = GetJsonBool(json, "usableOnLimb", false);
        item.autoAttack = GetJsonBool(json, "autoAttack", false);
        item.onlyHoldInHands = GetJsonBool(json, "onlyHoldInHands", false);

        // Combat / Weapon
        item.isWeapon = GetJsonBool(json, "isWeapon", false);
        item.damage = GetJsonFloat(json, "damage", 0f);
        item.structuralDamage = GetJsonFloat(json, "structuralDamage", 0f);
        item.knockback = GetJsonFloat(json, "knockBack", 0f);
        if (item.knockback == 0f) item.knockback = GetJsonFloat(json, "knockback", 0f); // alt casing
        item.cooldown = GetJsonFloat(json, "cooldown", 0f);
        item.attackCooldownMult = GetJsonFloat(json, "attackCooldownMult", 1f);
        item.distance = GetJsonFloat(json, "distance", 1f);
        item.staminaUse = GetJsonFloat(json, "staminaUse", 0f);
        item.piercing = GetJsonBool(json, "piercing", false);
        item.rotateAmount = GetJsonFloat(json, "rotateAmount", 0f);
        item.volume = GetJsonFloat(json, "volume", 0f);
        item.physicalSwing = GetJsonBool(json, "physicalSwing", true);
        item.doAttackAnim = GetJsonBool(json, "doAttackAnim", true);
        item.metalMoreDamage = GetJsonBool(json, "metalMoreDamage", false);
        item.attackAnim = GetJsonString(json, "attackAnim");
        if (string.IsNullOrEmpty(item.attackAnim)) item.attackAnim = "SwingAnim";
        item.swingSounds = GetJsonString(json, "swingSounds");
        if (string.IsNullOrEmpty(item.swingSounds)) item.swingSounds = "BSSwing1,BSSwing2,BSSwing3,BSSwing4";

        // Food / Consumable
        item.isFood = GetJsonBool(json, "isFood", false);
        item.foodValue = GetJsonFloat(json, "foodValue", 0f);
        item.waterValue = GetJsonFloat(json, "waterValue", 0f);

        // Wearable / Armor
        item.wearable = GetJsonBool(json, "wearable", false);
        item.desiredWearLimb = GetJsonString(json, "desiredWearLimb");
        item.wearSlotId = GetJsonString(json, "wearSlotId");
        item.wearableArmor = GetJsonFloat(json, "wearableArmor", 0f);
        item.wearableIsolation = GetJsonFloat(json, "wearableIsolation", 0f);
        item.wearableHitDurabilityLossMultiplier = GetJsonFloat(json, "wearableHitDurabilityLossMultiplier", 0f);
        item.wearableVisualOffset = GetJsonInt(json, "wearableVisualOffset", 0);
        item.jumpHeightMultChange = GetJsonFloat(json, "jumpHeightMultChange", 0f);

        // Decay & Condition
        item.destroyAtZeroCondition = GetJsonBool(json, "destroyAtZeroCondition", false);
        item.scaleWeightWithCondition = GetJsonBool(json, "scaleWeightWithCondition", false);
        item.decayInfo = GetJsonInt(json, "decayInfo", 0);
        item.decayMinutes = GetJsonFloat(json, "decayMinutes", 0f);

        // Crafting & Economy
        item.combineable = GetJsonBool(json, "combineable", false);
        item.ignoreDepression = GetJsonBool(json, "ignoreDepression", false);
        item.intRequirement = GetJsonInt(json, "intRequirement", 0);
        item.qualities = GetJsonString(json, "qualities");

        // Sprite — check for sprite file in the item folder
        string spriteFile = GetJsonString(json, "sprite");
        if (!string.IsNullOrEmpty(spriteFile))
        {
            string spritePath = Path.Combine(dir, spriteFile);
            if (File.Exists(spritePath))
            {
                // Load sprite bytes and store as base64 for our existing pipeline
                byte[] spriteBytes = File.ReadAllBytes(spritePath);
                item.spriteBase64 = Convert.ToBase64String(spriteBytes);
                Debug.Log("[ItemLoader] Loaded sprite: " + spritePath);
            }
            else
            {
                // Also check mods/sprites/ folder
                string altPath = Path.Combine(modsPath, "sprites", spriteFile);
                if (File.Exists(altPath))
                {
                    byte[] spriteBytes = File.ReadAllBytes(altPath);
                    item.spriteBase64 = Convert.ToBase64String(spriteBytes);
                    Debug.Log("[ItemLoader] Loaded sprite from sprites/: " + altPath);
                }
            }
        }

        // Also check for sprite.png directly in folder
        if (string.IsNullOrEmpty(item.spriteBase64))
        {
            string defaultSprite = Path.Combine(dir, "sprite.png");
            if (File.Exists(defaultSprite))
            {
                byte[] spriteBytes = File.ReadAllBytes(defaultSprite);
                item.spriteBase64 = Convert.ToBase64String(spriteBytes);
                Debug.Log("[ItemLoader] Loaded default sprite.png from " + dir);
            }
        }

        item.spriteWidth = GetJsonInt(json, "spriteWidth", 0);
        item.spriteHeight = GetJsonInt(json, "spriteHeight", 0);
        item.texWidth = GetJsonInt(json, "texWidth", 16);
        item.texHeight = GetJsonInt(json, "texHeight", 16);

        // Light properties
        item.lightIntensity = GetJsonFloat(json, "lightIntensity", -1f);
        item.lightRadius = GetJsonFloat(json, "lightRadius", -1f);
        item.lightColorR = GetJsonFloat(json, "lightColorR", -1f);
        item.lightColorG = GetJsonFloat(json, "lightColorG", -1f);
        item.lightColorB = GetJsonFloat(json, "lightColorB", -1f);

        return item;
    }

    // === MINI JSON HELPERS (no Unity JsonUtility for plain classes) ===
    static string GetJsonString(string json, string key)
    {
        string pattern = "\"" + key + "\"";
        int idx = json.IndexOf(pattern);
        if (idx < 0) return "";
        int colonIdx = json.IndexOf(':', idx + pattern.Length);
        if (colonIdx < 0) return "";
        // Find the opening quote
        int startQuote = json.IndexOf('"', colonIdx + 1);
        if (startQuote < 0) return "";
        int endQuote = json.IndexOf('"', startQuote + 1);
        if (endQuote < 0) return "";
        return json.Substring(startQuote + 1, endQuote - startQuote - 1);
    }

    static float GetJsonFloat(string json, string key, float defaultVal)
    {
        string pattern = "\"" + key + "\"";
        int idx = json.IndexOf(pattern);
        if (idx < 0) return defaultVal;
        int colonIdx = json.IndexOf(':', idx + pattern.Length);
        if (colonIdx < 0) return defaultVal;
        // Read number after colon
        int start = colonIdx + 1;
        while (start < json.Length && (json[start] == ' ' || json[start] == '\t')) start++;
        int end = start;
        while (end < json.Length && (char.IsDigit(json[end]) || json[end] == '.' || json[end] == '-')) end++;
        string numStr = json.Substring(start, end - start).Trim();
        float result;
        if (float.TryParse(numStr, System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out result))
            return result;
        return defaultVal;
    }

    static int GetJsonInt(string json, string key, int defaultVal)
    {
        return (int)GetJsonFloat(json, key, defaultVal);
    }

    static bool GetJsonBool(string json, string key, bool defaultVal)
    {
        string pattern = "\"" + key + "\"";
        int idx = json.IndexOf(pattern);
        if (idx < 0) return defaultVal;
        int colonIdx = json.IndexOf(':', idx + pattern.Length);
        if (colonIdx < 0) return defaultVal;
        string rest = json.Substring(colonIdx + 1, Math.Min(10, json.Length - colonIdx - 1)).Trim().ToLower();
        if (rest.StartsWith("true")) return true;
        if (rest.StartsWith("false")) return false;
        return defaultVal;
    }

    // === ITEM REGISTRATION ===
    static void RegisterItem(CustomItem item)
    {
        var globalItemsField = typeof(Item).GetField("GlobalItems", BindingFlags.Public | BindingFlags.Static);
        if (globalItemsField == null) { Debug.LogError("[ItemLoader] GlobalItems not found"); return; }
        var globalItems = globalItemsField.GetValue(null) as IDictionary<string, ItemInfo>;
        if (globalItems == null) { Debug.LogError("[ItemLoader] GlobalItems is null"); return; }

        var info = new ItemInfo();
        info.fullName = item.name;
        info.description = item.description;
        info.category = item.category;
        info.weight = item.weight;
        info.value = item.value;
        info.tags = item.tags;
        info.slotRotation = item.slotRotation;
        info.rotSpeed = item.rotSpeed;
        info.rec = new Recognition(item.intRequirement);

        // Usability flags
        info.usable = item.usable;
        info.usableWithLMB = item.usableWithLMB;
        info.usableOnLimb = item.usableOnLimb;
        info.autoAttack = item.autoAttack;
        info.onlyHoldInHands = item.onlyHoldInHands;

        // Wearable
        info.wearable = item.wearable;
        if (!string.IsNullOrEmpty(item.desiredWearLimb)) info.desiredWearLimb = item.desiredWearLimb;
        if (!string.IsNullOrEmpty(item.wearSlotId)) info.wearSlotId = item.wearSlotId;
        info.wearableArmor = item.wearableArmor;
        info.wearableIsolation = item.wearableIsolation;
        info.wearableHitDurabilityLossMultiplier = item.wearableHitDurabilityLossMultiplier;
        info.wearableVisualOffset = item.wearableVisualOffset;
        info.jumpHeightMultChange = item.jumpHeightMultChange;

        // Decay & Condition
        info.destroyAtZeroCondition = item.destroyAtZeroCondition;
        info.scaleWeightWithCondition = item.scaleWeightWithCondition;
        info.decayInfo = (byte)item.decayInfo;
        info.decayMinutes = item.decayMinutes;

        // Crafting
        info.combineable = item.combineable;
        info.ignoreDepression = item.ignoreDepression;

        // Qualities
        if (!string.IsNullOrEmpty(item.qualities))
        {
            info.qualities = new List<CraftingQuality>();
            foreach (var q in item.qualities.Split(','))
            {
                string trimmed = q.Trim();
                if (trimmed.Length > 0) info.qualities.Add(new CraftingQuality(trimmed));
            }
        }

        // Use action — weapons get attack handler, food gets food handler
        if (item.isWeapon && item.usable)
        {
            try
            {
                var useActionField = typeof(ItemInfo).GetField("useAction", BindingFlags.Public | BindingFlags.Instance);
                if (useActionField != null)
                {
                    var delegateType = useActionField.FieldType;
                    var attackMethod = typeof(ItemLoader).GetMethod("GenericAttackHandler",
                        BindingFlags.Public | BindingFlags.Static);
                    if (attackMethod != null)
                    {
                        var del = Delegate.CreateDelegate(delegateType, attackMethod);
                        useActionField.SetValue(info, del);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[ItemLoader] Failed to set weapon useAction for " + item.id + ": " + e.Message);
            }
        }
        else if (item.isFood && item.usable)
        {
            try
            {
                var useActionField = typeof(ItemInfo).GetField("useAction", BindingFlags.Public | BindingFlags.Instance);
                if (useActionField != null)
                {
                    var delegateType = useActionField.FieldType;
                    var foodMethod = typeof(ItemLoader).GetMethod("GenericFoodHandler",
                        BindingFlags.Public | BindingFlags.Static);
                    if (foodMethod != null)
                    {
                        var del = Delegate.CreateDelegate(delegateType, foodMethod);
                        useActionField.SetValue(info, del);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.LogError("[ItemLoader] Failed to set food useAction for " + item.id + ": " + e.Message);
            }
        }

        globalItems[item.id] = info;
    }

    // === ATTACK HANDLER ===
    // Signature must match ItemInfo.Use delegate: void(Body, Item)
    public static void GenericAttackHandler(Body body, Item weaponItem)
    {
        try
        {
            string itemId = weaponItem.id;
            if (!customItems.ContainsKey(itemId))
            {
                Debug.LogError("[ItemLoader] Attack: unknown custom item " + itemId);
                return;
            }
            var def = customItems[itemId];

            var atkInfo = new AttackInfo();
            atkInfo.damage = def.damage;
            atkInfo.structuralDamage = def.structuralDamage;
            atkInfo.knockBack = def.knockback;
            atkInfo.cooldown = def.cooldown;
            atkInfo.attackCooldownMult = def.attackCooldownMult;
            atkInfo.distance = def.distance;
            atkInfo.staminaUse = def.staminaUse;
            atkInfo.piercing = def.piercing;
            atkInfo.volume = def.volume;
            atkInfo.rotateAmount = def.rotateAmount;
            atkInfo.physicalSwing = def.physicalSwing;
            atkInfo.doAttackAnim = def.doAttackAnim;
            atkInfo.metalMoreDamage = def.metalMoreDamage;

            // Load attack animation
            atkInfo.attackAnim = Resources.Load<GameObject>(def.attackAnim);

            // Set swing sounds from comma-separated string
            if (!string.IsNullOrEmpty(def.swingSounds))
                atkInfo.swingSounds = def.swingSounds.Split(',');
            else
                atkInfo.swingSounds = new string[] { "BSSwing1", "BSSwing2", "BSSwing3", "BSSwing4" };

            // Body.Attack(AttackInfo, int) — second arg is hand index (0)
            bool hit = body.Attack(atkInfo, 0);

            // Degrade condition on hit
            if (hit && def.rotSpeed > 0)
            {
                weaponItem.condition -= 0.005f;
            }
        }
        catch (Exception e)
        {
            Debug.LogError("[ItemLoader] Attack error: " + e.Message + "\n" + e.StackTrace);
        }
    }

    // === FOOD HANDLER ===
    // Signature must match ItemInfo.Use delegate: void(Body, Item)
    public static void GenericFoodHandler(Body body, Item foodItem)
    {
        try
        {
            string itemId = foodItem.id;
            if (!customItems.ContainsKey(itemId)) return;
            var def = customItems[itemId];

            // Apply food/water values via Body fields
            if (def.foodValue != 0f)
            {
                var foodField = typeof(Body).GetField("food", BindingFlags.Public | BindingFlags.Instance);
                if (foodField != null)
                {
                    float current = (float)foodField.GetValue(body);
                    foodField.SetValue(body, Mathf.Clamp(current + def.foodValue, 0f, 100f));
                }
            }
            if (def.waterValue != 0f)
            {
                var waterField = typeof(Body).GetField("water", BindingFlags.Public | BindingFlags.Instance);
                if (waterField != null)
                {
                    float current = (float)waterField.GetValue(body);
                    waterField.SetValue(body, Mathf.Clamp(current + def.waterValue, 0f, 100f));
                }
            }

            // Destroy the item after use (consume it)
            if (def.destroyAtZeroCondition)
            {
                foodItem.condition = 0f;
            }
            else
            {
                UnityEngine.Object.Destroy(foodItem.gameObject);
            }

            Debug.Log("[ItemLoader] Used food: " + def.name + " (food:" + def.foodValue + " water:" + def.waterValue + ")");
        }
        catch (Exception e)
        {
            Debug.LogError("[ItemLoader] Food error: " + e.Message + "\n" + e.StackTrace);
        }
    }

    // === CALLED FROM PATCHED Ext.Create() ===
    public static GameObject TryCreateCustom(string id, Vector2 position, float rotation)
    {
        if (!customItems.ContainsKey(id)) return null;

        var def = customItems[id];
        try
        {
            var prefab = Resources.Load<GameObject>(def.basePrefab);
            if (prefab == null)
            {
                Debug.LogError("[ItemLoader] Base prefab not found: " + def.basePrefab);
                return null;
            }

            var go = UnityEngine.Object.Instantiate(prefab, position,
                Quaternion.Euler(0, 0, rotation));
            go.name = def.id;

            var itemComp = go.GetComponent<Item>();
            if (itemComp != null)
            {
                var idField = typeof(Item).GetField("id", BindingFlags.Public | BindingFlags.Instance);
                if (idField != null) idField.SetValue(itemComp, def.id);
            }

            ApplyCustomSprite(go, def);
            ApplyLightProperties(go, def);
            Debug.Log("[ItemLoader] Created custom item: " + def.id);
            return go;
        }
        catch (Exception e)
        {
            Debug.LogError("[ItemLoader] Create error for " + id + ": " + e.Message);
            return null;
        }
    }

    // === LIGHT PROPERTIES ===
    static void ApplyLightProperties(GameObject go, CustomItem def)
    {
        // Only modify if any light property is set
        if (def.lightIntensity < 0 && def.lightRadius < 0 && def.lightColorR < 0) return;

        try
        {
            // Find Light2D component (URP)
            // Use reflection since we may not have the assembly reference
            var light2dType = System.Type.GetType("UnityEngine.Rendering.Universal.Light2D, Unity.RenderPipelines.Universal.Runtime");
            if (light2dType == null)
            {
                Debug.LogWarning("[ItemLoader] Light2D type not found — can't modify light properties");
                return;
            }

            var lightComp = go.GetComponent(light2dType);
            // Also check children
            if (lightComp == null)
                lightComp = go.GetComponentInChildren(light2dType);

            if (lightComp == null)
            {
                Debug.LogWarning("[ItemLoader] No Light2D on " + def.id + " — base prefab may not have one");
                return;
            }

            if (def.lightIntensity >= 0)
            {
                var intensityProp = light2dType.GetProperty("intensity");
                if (intensityProp != null) intensityProp.SetValue(lightComp, def.lightIntensity);
            }

            if (def.lightRadius >= 0)
            {
                // pointLightOuterRadius for point lights
                var radiusProp = light2dType.GetProperty("pointLightOuterRadius");
                if (radiusProp != null) radiusProp.SetValue(lightComp, def.lightRadius);
            }

            if (def.lightColorR >= 0)
            {
                var colorProp = light2dType.GetProperty("color");
                if (colorProp != null)
                {
                    float r = def.lightColorR >= 0 ? def.lightColorR : 1f;
                    float g = def.lightColorG >= 0 ? def.lightColorG : 1f;
                    float b = def.lightColorB >= 0 ? def.lightColorB : 1f;
                    colorProp.SetValue(lightComp, new Color(r, g, b, 1f));
                }
            }

            Debug.Log("[ItemLoader] Applied light properties to " + def.id);
        }
        catch (Exception e)
        {
            Debug.LogError("[ItemLoader] Light properties error: " + e.Message);
        }
    }

    // === SPRITE HANDLING ===
    static void ApplyCustomSprite(GameObject go, CustomItem def)
    {
        if (string.IsNullOrEmpty(def.sprite) && string.IsNullOrEmpty(def.spriteBase64))
            return;

        Sprite sprite = GetOrLoadSprite(def);
        if (sprite == null) return;

        var sr = go.GetComponent<SpriteRenderer>();
        if (sr != null) sr.sprite = sprite;
    }

    static Sprite GetOrLoadSprite(CustomItem def)
    {
        if (spriteCache.ContainsKey(def.id)) return spriteCache[def.id];

        byte[] pngData = null;

        if (!string.IsNullOrEmpty(def.sprite))
        {
            // Check item folder first, then mods/sprites/
            string spritePath = Path.Combine(modsPath, "sprites", def.sprite);
            if (File.Exists(spritePath))
            {
                pngData = File.ReadAllBytes(spritePath);
            }
        }

        if (pngData == null && !string.IsNullOrEmpty(def.spriteBase64))
        {
            pngData = Convert.FromBase64String(def.spriteBase64);
        }

        if (pngData == null) return null;

        int tw = def.texWidth > 0 ? def.texWidth : 16;
        int th = def.texHeight > 0 ? def.texHeight : 16;
        var tex = new Texture2D(tw, th, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.LoadImage(pngData);

        int sw = def.spriteWidth > 0 ? def.spriteWidth : tw;
        int sh = def.spriteHeight > 0 ? def.spriteHeight : th;
        var sprite = Sprite.Create(tex, new Rect(0, 0, sw, sh), new Vector2(0.5f, 0.5f), 16f);

        spriteCache[def.id] = sprite;
        return sprite;
    }

    // === HOT RELOAD — re-scan mods folder and re-register all items ===
    public static int HotReload()
    {
        int beforeCount = customItems.Count;
        
        // Get GlobalItems
        var globalItemsField = typeof(Item).GetField("GlobalItems", BindingFlags.Public | BindingFlags.Static);
        if (globalItemsField == null) { Debug.LogError("[ItemLoader] HotReload: GlobalItems not found"); return 0; }
        var globalItems = globalItemsField.GetValue(null) as IDictionary<string, ItemInfo>;
        if (globalItems == null) { Debug.LogError("[ItemLoader] HotReload: GlobalItems is null"); return 0; }

        // Clear existing custom items from GlobalItems
        foreach (var id in customItems.Keys)
        {
            if (globalItems.ContainsKey(id))
                globalItems.Remove(id);
        }
        customItems.Clear();
        spriteCache.Clear();

        // Re-register everything
        RegisterAllCustomItems();

        int newItems = customItems.Count - beforeCount;
        Debug.Log("[ItemLoader] Hot reload complete. " + customItems.Count + " items total. " + 
            (newItems > 0 ? newItems + " new items." : "No new items."));

        // NOTE: Loot pool re-init disabled — see RegisterAllCustomItems() comment

        return customItems.Count;
    }

    public static List<string> GetCustomItemIds()
    {
        return new List<string>(customItems.Keys);
    }

    static void SetField(object obj, string name, object value)
    {
        var field = obj.GetType().GetField(name,
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
        if (field != null)
            field.SetValue(obj, value);
    }
}
