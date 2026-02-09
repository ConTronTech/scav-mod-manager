// ModManager.cs v3 — Canvas-based mod menu for Scav (Casualties Unknown)
// Uses Unity Canvas + TextMeshPro. Toggle with M key.
// Fixed: console commands, unicode chars, XP per skill, spawn count, teleport toggle

using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System.Reflection;

public class ModManager : MonoBehaviour
{
    public static ModManager Instance;
    private static GameObject modManagerGO;

    // === State ===
    private bool menuOpen = false;
    private int currentTab = 0;
    private string[] tabNames = { "Player", "Spawner", "World", "Cheats", "Info" };

    // === Cheat Toggles ===
    public static bool godMode = false;
    public static bool infiniteStamina = false;
    public static bool noHunger = false;
    public static bool noThirst = false;
    public static bool noFallDamage = false;
    public static bool speedHack = false;
    public static float speedMultiplier = 2f;
    public static bool teleportMode = false;
    public static bool noclipMode = false;

    // === Spawner ===
    private List<string> allItemIds = new List<string>();
    private List<string> filteredItems = new List<string>();
    private int spawnCount = 1;
    private int spawnPageStart = 0;
    private const int ITEMS_PER_PAGE = 12;

    // === Radar ===
    private float radarRadius = 30f;
    private Transform radarListContainer;
    private TextMeshProUGUI radarCountText;
    private float lastRadarUpdate = 0f;
    private float lastUIUpdate = 0f;
    private const float RADAR_INTERVAL = 0.5f;
    private Image[] radiusBtnImages;
    private float[] radiusValues = { 10f, 30f, 50f, 100f };

    // === UI References ===
    private Canvas canvas;
    private GameObject mainPanel;
    private GameObject[] tabPanels;
    private GameObject[] tabButtons;
    private TMP_FontAsset tmpFont;
    private TMP_InputField searchField;
    private Transform spawnListContainer;
    private TextMeshProUGUI infoText;

    // === Log System ===
    private static List<string> logEntries = new List<string>();
    private const int MAX_LOG_ENTRIES = 100;
    private TextMeshProUGUI logText;
    private ScrollRect logScrollRect;
    private static bool logHookRegistered = false;
    private int lastLogCount = 0;
    private TextMeshProUGUI pageLabel;
    private TextMeshProUGUI positionText;
    private TextMeshProUGUI speedLabel;

    // Count button refs for visual feedback
    private Image[] countBtnImages;
    private int[] countValues = { 1, 5, 10, 25 };

    // Toggle refs
    private Dictionary<string, Image> toggleBGs = new Dictionary<string, Image>();
    private Dictionary<string, TextMeshProUGUI> toggleTexts = new Dictionary<string, TextMeshProUGUI>();

    // === Colors ===
    private Color bgColor = new Color(0.08f, 0.08f, 0.12f, 0.95f);
    private Color darkColor = new Color(0.05f, 0.05f, 0.08f, 0.98f);
    private Color accentColor = new Color(0.6f, 0.2f, 0.8f, 0.95f);
    private Color greenColor = new Color(0.15f, 0.55f, 0.25f, 0.95f);
    private Color redColor = new Color(0.55f, 0.12f, 0.12f, 0.95f);
    private Color tabInactive = new Color(0.15f, 0.15f, 0.22f, 0.95f);
    private Color btnColor = new Color(0.18f, 0.18f, 0.25f, 0.95f);
    private Color selectedColor = new Color(0.4f, 0.15f, 0.6f, 0.95f);

    public static void InitLogHook()
    {
        if (!logHookRegistered)
        {
            Application.logMessageReceived += OnLogMessage;
            logHookRegistered = true;
        }
    }

    public static void Init()
    {
        InitLogHook();
        if (Instance != null) return;
        modManagerGO = new GameObject("__ModManager__");
        Instance = modManagerGO.AddComponent<ModManager>();
        DontDestroyOnLoad(modManagerGO);
        Debug.Log("[ModManager] Initialized! Press M to open menu.");
    }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        var allFonts = Resources.FindObjectsOfTypeAll<TMP_FontAsset>();
        if (allFonts.Length > 0)
        {
            tmpFont = allFonts.FirstOrDefault(f => f.name.ToLower().Contains("liberation"))
                   ?? allFonts.FirstOrDefault(f => f.name.ToLower().Contains("arial"))
                   ?? allFonts[0];
        }

        if (FindObjectOfType<EventSystem>() == null)
        {
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();
            DontDestroyOnLoad(es);
        }

        BuildUI();
        mainPanel.SetActive(false);

        // Delay item list load slightly so GlobalItems is fully populated
        Invoke("DelayedInit", 0.5f);
    }

    void DelayedInit()
    {
        RefreshItemList();
        filteredItems = new List<string>(allItemIds);
        Debug.Log("[ModManager] Loaded " + allItemIds.Count + " items");
    }

    void RefreshItemList()
    {
        allItemIds.Clear();
        try
        {
            var globalItems = typeof(Item).GetField("GlobalItems", BindingFlags.Public | BindingFlags.Static);
            if (globalItems != null)
            {
                var dict = globalItems.GetValue(null) as IDictionary<string, ItemInfo>;
                if (dict != null)
                    allItemIds = dict.Keys.OrderBy(k => k).ToList();
            }
        }
        catch (Exception) { }
        filteredItems = new List<string>(allItemIds);
    }

    // ===========================
    // BUILD UI
    // ===========================
    void BuildUI()
    {
        var canvasGO = new GameObject("ModManagerCanvas");
        canvasGO.transform.SetParent(transform);
        canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 32767;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        canvasGO.AddComponent<GraphicRaycaster>();

        mainPanel = CreatePanel(canvasGO.transform, "MainPanel", bgColor, new Vector2(520, 580));
        var mainRT = mainPanel.GetComponent<RectTransform>();
        mainRT.anchorMin = new Vector2(0, 1);
        mainRT.anchorMax = new Vector2(0, 1);
        mainRT.pivot = new Vector2(0, 1);
        mainRT.anchoredPosition = new Vector2(50, -50);
        mainPanel.AddComponent<DragHandler>();

        // Title (no unicode!)
        CreateText(mainPanel.transform, "Title", "-- SCAV MOD MANAGER --", 18, accentColor,
            new Vector2(0, 1), new Vector2(1, 1), new Vector2(0.5f, 1), new Vector2(0, -5), new Vector2(0, 30),
            alignment: TextAlignmentOptions.Center);

        // Tab bar
        var tabBar = CreatePanel(mainPanel.transform, "TabBar", new Color(0, 0, 0, 0), Vector2.zero);
        var tabBarRT = tabBar.GetComponent<RectTransform>();
        tabBarRT.anchorMin = new Vector2(0, 1);
        tabBarRT.anchorMax = new Vector2(1, 1);
        tabBarRT.pivot = new Vector2(0.5f, 1);
        tabBarRT.anchoredPosition = new Vector2(0, -40);
        tabBarRT.sizeDelta = new Vector2(-20, 35);
        var tabHLG = tabBar.AddComponent<HorizontalLayoutGroup>();
        tabHLG.spacing = 4;
        tabHLG.childForceExpandWidth = true;

        tabButtons = new GameObject[tabNames.Length];
        for (int i = 0; i < tabNames.Length; i++)
        {
            int idx = i;
            tabButtons[i] = CreateButton(tabBar.transform, "Tab_" + tabNames[i], tabNames[i], 14,
                i == 0 ? accentColor : tabInactive, Color.white, () => SwitchTab(idx));
        }

        tabPanels = new GameObject[tabNames.Length];
        for (int i = 0; i < tabNames.Length; i++)
        {
            tabPanels[i] = CreatePanel(mainPanel.transform, "TabContent_" + i, new Color(0, 0, 0, 0), Vector2.zero);
            var rt = tabPanels[i].GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, 1);
            rt.offsetMin = new Vector2(10, 40);
            rt.offsetMax = new Vector2(-10, -80);
            tabPanels[i].SetActive(i == 0);
        }

        BuildPlayerTab(tabPanels[0].transform);
        BuildSpawnerTab(tabPanels[1].transform);
        BuildWorldTab(tabPanels[2].transform);
        BuildCheatsTab(tabPanels[3].transform);
        BuildInfoTab(tabPanels[4].transform);

        CreateText(mainPanel.transform, "Footer", "Press M to close | Made by Jeffery & Contolis", 12,
            new Color(0.6f, 0.4f, 0.8f), new Vector2(0, 0), new Vector2(1, 0), new Vector2(0.5f, 0),
            new Vector2(0, 8), new Vector2(0, 25), alignment: TextAlignmentOptions.Center);
    }

    // ===========================
    // PLAYER TAB
    // ===========================
    void BuildPlayerTab(Transform parent)
    {
        AddVerticalLayout(parent.gameObject, 5);

        CreateHeader(parent, "PLAYER CHEATS");
        CreateToggleRow(parent, "godMode", "God Mode", godMode, v => godMode = v);
        CreateToggleRow(parent, "infStam", "Infinite Stamina", infiniteStamina, v => infiniteStamina = v);
        CreateToggleRow(parent, "noHunger", "No Hunger", noHunger, v => noHunger = v);
        CreateToggleRow(parent, "noThirst", "No Thirst", noThirst, v => noThirst = v);
        CreateToggleRow(parent, "noFall", "No Fall Damage", noFallDamage, v => noFallDamage = v);
        CreateToggleRow(parent, "speed", "Speed Hack", speedHack, v => speedHack = v);

        var speedRow = CreateRow(parent, "SpeedRow");
        var sl = CreateText(speedRow.transform, "SpeedLabel", "  Speed: 2.0x", 14, Color.white, width: 160);
        speedLabel = sl.GetComponent<TextMeshProUGUI>();
        CreateButton(speedRow.transform, "SpeedDown", "-", 14, btnColor, Color.white, () => {
            speedMultiplier = Mathf.Max(1f, speedMultiplier - 0.5f);
            speedLabel.text = "  Speed: " + speedMultiplier.ToString("F1") + "x";
        }, 35);
        CreateButton(speedRow.transform, "SpeedUp", "+", 14, btnColor, Color.white, () => {
            speedMultiplier = Mathf.Min(20f, speedMultiplier + 0.5f);
            speedLabel.text = "  Speed: " + speedMultiplier.ToString("F1") + "x";
        }, 35);

        CreateSpacer(parent, 8);
        CreateHeader(parent, "XP CONTROLS");

        // STR row
        var strRow = CreateRow(parent, "STRRow");
        CreateText(strRow.transform, "L", "STR", 14, new Color(1f, 0.4f, 0.4f), width: 40);
        CreateButton(strRow.transform, "s0", "Lv-1", 11, redColor, Color.white, () => AddLevel("str", -1), 42);
        CreateButton(strRow.transform, "s1", "-500", 11, redColor, Color.white, () => AddXP("str", -500), 45);
        CreateButton(strRow.transform, "s3", "+500", 11, greenColor, Color.white, () => AddXP("str", 500), 45);
        CreateButton(strRow.transform, "s4", "+5K", 11, greenColor, Color.white, () => AddXP("str", 5000), 40);
        CreateButton(strRow.transform, "s5", "Lv+1", 11, greenColor, Color.white, () => AddLevel("str", 1), 42);
        CreateButton(strRow.transform, "s6", "MAX", 11, accentColor, Color.white, () => SetLevel("str", 30), 40);

        // RES row
        var resRow = CreateRow(parent, "RESRow");
        CreateText(resRow.transform, "L", "RES", 14, new Color(0.4f, 0.7f, 1f), width: 40);
        CreateButton(resRow.transform, "r0", "Lv-1", 11, redColor, Color.white, () => AddLevel("res", -1), 42);
        CreateButton(resRow.transform, "r1", "-500", 11, redColor, Color.white, () => AddXP("res", -500), 45);
        CreateButton(resRow.transform, "r3", "+500", 11, greenColor, Color.white, () => AddXP("res", 500), 45);
        CreateButton(resRow.transform, "r4", "+5K", 11, greenColor, Color.white, () => AddXP("res", 5000), 40);
        CreateButton(resRow.transform, "r5", "Lv+1", 11, greenColor, Color.white, () => AddLevel("res", 1), 42);
        CreateButton(resRow.transform, "r6", "MAX", 11, accentColor, Color.white, () => SetLevel("res", 30), 40);

        // INT row
        var intRow = CreateRow(parent, "INTRow");
        CreateText(intRow.transform, "L", "INT", 14, new Color(0.6f, 1f, 0.4f), width: 40);
        CreateButton(intRow.transform, "i0", "Lv-1", 11, redColor, Color.white, () => AddLevel("int", -1), 42);
        CreateButton(intRow.transform, "i1", "-500", 11, redColor, Color.white, () => AddXP("int", -500), 45);
        CreateButton(intRow.transform, "i3", "+500", 11, greenColor, Color.white, () => AddXP("int", 500), 45);
        CreateButton(intRow.transform, "i4", "+5K", 11, greenColor, Color.white, () => AddXP("int", 5000), 40);
        CreateButton(intRow.transform, "i5", "Lv+1", 11, greenColor, Color.white, () => AddLevel("int", 1), 42);
        CreateButton(intRow.transform, "i6", "MAX", 11, accentColor, Color.white, () => SetLevel("int", 30), 40);
    }

    // ===========================
    // SPAWNER TAB
    // ===========================
    void BuildSpawnerTab(Transform parent)
    {
        AddVerticalLayout(parent.gameObject, 4);

        CreateHeader(parent, "ITEM SPAWNER");

        var searchRow = CreateRow(parent, "SearchRow");
        CreateText(searchRow.transform, "SearchLabel", "Search:", 14, Color.white, width: 70);
        searchField = CreateInputField(searchRow.transform, "SearchInput", "type to filter...");

        // Count row with visual selection
        var countRow = CreateRow(parent, "CountRow");
        CreateText(countRow.transform, "CountLabel", "Count:", 14, Color.white, width: 60);
        countBtnImages = new Image[countValues.Length];
        for (int i = 0; i < countValues.Length; i++)
        {
            int val = countValues[i];
            int idx = i;
            var btn = CreateButton(countRow.transform, "C" + val, val.ToString(), 13,
                val == 1 ? selectedColor : btnColor, Color.white, () => {
                    spawnCount = val;
                    UpdateCountButtons();
                }, 40);
            countBtnImages[i] = btn.GetComponent<Image>();
        }

        CreateSpacer(countRow.transform, 10, horizontal: true);

        // Pagination
        CreateButton(countRow.transform, "PgUp", "<", 14, btnColor, Color.white, () => {
            spawnPageStart = Mathf.Max(0, spawnPageStart - ITEMS_PER_PAGE); RefreshSpawnList();
        }, 30);
        var pl = CreateText(countRow.transform, "PageLabel", "0-0/0", 11, Color.white, width: 80);
        pageLabel = pl.GetComponent<TextMeshProUGUI>();
        pageLabel.alignment = TextAlignmentOptions.Center;
        CreateButton(countRow.transform, "PgDn", ">", 14, btnColor, Color.white, () => {
            if (spawnPageStart + ITEMS_PER_PAGE < filteredItems.Count) { spawnPageStart += ITEMS_PER_PAGE; RefreshSpawnList(); }
        }, 30);

        spawnListContainer = CreatePanel(parent, "SpawnList", darkColor, Vector2.zero).transform;
        var slRT = spawnListContainer.GetComponent<RectTransform>();
        slRT.sizeDelta = new Vector2(0, ITEMS_PER_PAGE * 28 + 8);
        var slLayout = AddVerticalLayout(spawnListContainer.gameObject, 2);
        slLayout.padding = new RectOffset(4, 4, 4, 4);

        RefreshSpawnList();
    }

    void UpdateRadiusButtons()
    {
        for (int i = 0; i < radiusValues.Length; i++)
        {
            if (radiusBtnImages[i] != null)
                radiusBtnImages[i].color = (radiusValues[i] == radarRadius) ? selectedColor : btnColor;
        }
    }

    void UpdateCountButtons()
    {
        for (int i = 0; i < countValues.Length; i++)
        {
            if (countBtnImages[i] != null)
                countBtnImages[i].color = (countValues[i] == spawnCount) ? selectedColor : btnColor;
        }
    }

    // ===========================
    // WORLD TAB
    // ===========================
    void BuildWorldTab(Transform parent)
    {
        AddVerticalLayout(parent.gameObject, 4);

        CreateHeader(parent, "PLAYER POSITION");
        var posGO = CreateText(parent, "PosText", "X: 0.0  Y: 0.0", 15, Color.white);
        positionText = posGO.GetComponent<TextMeshProUGUI>();

        // Movement row
        var mvRow = CreateRow(parent, "Mvmt");
        CreateToggleRow(mvRow.transform, "teleport", "Mid-Click TP", teleportMode, v => teleportMode = v);

        CreateToggleRow(parent, "noclip", "Noclip", noclipMode, v => {
            noclipMode = v;
            RunConsoleCommand("noclip");
        });

        CreateSpacer(parent, 6);
        CreateHeader(parent, "NEARBY RADAR");

        // Radar controls
        var radarCtrl = CreateRow(parent, "RadarCtrl");
        CreateText(radarCtrl.transform, "RL", "Radius:", 13, Color.white, width: 60);
        radiusBtnImages = new Image[radiusValues.Length];
        for (int i = 0; i < radiusValues.Length; i++)
        {
            float val = radiusValues[i];
            int idx = i;
            var rbtn = CreateButton(radarCtrl.transform, "R" + val, val.ToString("F0"), 12,
                val == radarRadius ? selectedColor : btnColor, Color.white, () => {
                    radarRadius = val;
                    UpdateRadiusButtons();
                }, val >= 100 ? 40 : 35);
            radiusBtnImages[i] = rbtn.GetComponent<Image>();
        }
        CreateSpacer(radarCtrl.transform, 10, horizontal: true);
        var rc = CreateText(radarCtrl.transform, "Count", "0 nearby", 12, new Color(0.7f, 0.7f, 0.7f), width: 90);
        radarCountText = rc.GetComponent<TextMeshProUGUI>();

        // Radar list
        radarListContainer = CreatePanel(parent, "RadarList", darkColor, Vector2.zero).transform;
        var rlRT = radarListContainer.GetComponent<RectTransform>();
        rlRT.sizeDelta = new Vector2(0, 240);
        var rlLayout = AddVerticalLayout(radarListContainer.gameObject, 1);
        rlLayout.padding = new RectOffset(4, 4, 4, 4);
    }

    // ===========================
    // CHEATS TAB
    // ===========================
    void BuildCheatsTab(Transform parent)
    {
        AddVerticalLayout(parent.gameObject, 6);

        CreateHeader(parent, "SPAWN PRESETS");
        CreateButton(parent, "Fork", "[GOD] Plastic Fork", 15, accentColor, Color.white,
            () => SpawnItem("plasticfork", 1));

        var row1 = CreateRow(parent, "P1");
        CreateButton(row1.transform, "Armor", "Best Armor Set", 13, btnColor, Color.white, () => {
            foreach (var a in new[] { "bikehelmet", "bellyarmor", "kneepads", "sneakers", "carapace" })
                SpawnItem(a, 1);
        });
        CreateButton(row1.transform, "Meds", "Medical Kit", 13, btnColor, Color.white, () => {
            SpawnItem("sterilizedbandage", 5); SpawnItem("antibiotics", 3); SpawnItem("painkillers", 3);
        });

        var row2 = CreateRow(parent, "P2");
        CreateButton(row2.transform, "Food", "Food & Water", 13, btnColor, Color.white, () => {
            SpawnItem("nutrientbar", 10); SpawnItem("waterbottle", 5);
        });
        CreateButton(row2.transform, "Bag", "Duffel Bag", 13, btnColor, Color.white, () => SpawnItem("duffelbag", 1));

        CreateSpacer(parent, 8);
        CreateHeader(parent, "QUICK COMMANDS");
        var row3 = CreateRow(parent, "C1");
        CreateButton(row3.transform, "CmdHeal", "Heal", 13, greenColor, Color.white, () => RunConsoleCommand("heal"));
        CreateButton(row3.transform, "CmdSkip", "Skip Layer", 13, btnColor, Color.white, () => RunConsoleCommand("skiplayer"));
    }

    // ===========================
    // INFO TAB
    // ===========================
    float GetBodyFloat(Body body, string field)
    {
        var f = typeof(Body).GetField(field, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
        if (f != null) return (float)f.GetValue(body);
        return 0f;
    }

    float GetFloat(object obj, string field)
    {
        var f = obj.GetType().GetField(field, BindingFlags.Public | BindingFlags.Instance);
        if (f != null) return (float)f.GetValue(obj);
        return 0f;
    }

    // === LOG SYSTEM ===
    static void OnLogMessage(string message, string stackTrace, LogType type)
    {
        string prefix = "";
        string color = "CCCCCC";
        switch (type)
        {
            case LogType.Error:
            case LogType.Exception:
                prefix = "[ERR] ";
                color = "FF4444";
                break;
            case LogType.Warning:
                prefix = "[WRN] ";
                color = "FFAA00";
                break;
            default:
                color = "88FF88";
                break;
        }

        // Capture ALL logs — no filtering. Game console shows everything, so should we.
        string timestamp = System.DateTime.Now.ToString("HH:mm:ss");
        string entry = "<color=#888888>[" + timestamp + "]</color> <color=#" + color + ">" + prefix + message + "</color>";

        logEntries.Add(entry);
        if (logEntries.Count > MAX_LOG_ENTRIES)
            logEntries.RemoveAt(0);
    }

    void BuildInfoTab(Transform parent)
    {
        AddVerticalLayout(parent.gameObject, 3);

        // Compact header
        CreateText(parent, "V", "v3.5 | Jeffery & Contolis | F5=Reload", 11, new Color(0.5f, 0.5f, 0.7f));

        // Compact live status (3 rows max)
        var info = CreateText(parent, "InfoText", "Loading...", 12, new Color(0.8f, 0.9f, 0.8f));
        infoText = info.GetComponent<TextMeshProUGUI>();
        infoText.enableWordWrapping = true;

        CreateSpacer(parent, 4);

        // Log header + controls on same row
        var logHeader = CreateRow(parent, "LogHeader");

        CreateText(logHeader.transform, "LogLabel", "LOG", 12, new Color(0.6f, 0.4f, 0.8f));
        CreateButton(logHeader.transform, "ClearLog", "Clear", 11, new Color(0.6f, 0.2f, 0.2f), Color.white,
            () => { logEntries.Clear(); }, 45);
        CreateButton(logHeader.transform, "CopyLog", "Copy All", 11, new Color(0.2f, 0.4f, 0.6f), Color.white,
            () => {
                string allLogs = "";
                foreach (var e in logEntries)
                {
                    string clean = Regex.Replace(e, "<[^>]+>", "");
                    allLogs += clean + "\n";
                }
                GUIUtility.systemCopyBuffer = allLogs;
                Debug.Log("[ModManager] Logs copied to clipboard (" + logEntries.Count + " entries)");
            }, 70);

        // Scrollable log area
        var logPanel = CreatePanel(parent, "LogPanel", new Color(0.08f, 0.08f, 0.12f, 0.95f), Vector2.zero);
        var logPanelRT = logPanel.GetComponent<RectTransform>();
        // Force size via LayoutElement since parent has VerticalLayout
        var logLayout = logPanel.AddComponent<LayoutElement>();
        logLayout.minHeight = 350;
        logLayout.preferredHeight = 350;
        logLayout.flexibleWidth = 1;

        // Add scroll rect
        logScrollRect = logPanel.AddComponent<ScrollRect>();
        logScrollRect.horizontal = false;
        logScrollRect.movementType = ScrollRect.MovementType.Elastic;
        logScrollRect.scrollSensitivity = 30f;

        // Mask for clipping
        logPanel.AddComponent<RectMask2D>();

        // Viewport (needed for proper scrollbar alignment)
        var viewport = new GameObject("Viewport", typeof(RectTransform));
        viewport.transform.SetParent(logPanel.transform, false);
        var vpRT = viewport.GetComponent<RectTransform>();
        vpRT.anchorMin = Vector2.zero;
        vpRT.anchorMax = Vector2.one;
        vpRT.offsetMin = new Vector2(0, 0);
        vpRT.offsetMax = new Vector2(-12, 0); // Leave room for scrollbar
        vpRT.pivot = new Vector2(0, 1);
        logScrollRect.viewport = vpRT;

        // Content container
        var content = new GameObject("LogContent", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        var contentRT = content.GetComponent<RectTransform>();
        contentRT.anchorMin = new Vector2(0, 1);
        contentRT.anchorMax = new Vector2(1, 1);
        contentRT.pivot = new Vector2(0, 1);
        contentRT.sizeDelta = new Vector2(0, 0);

        var contentFitter = content.AddComponent<ContentSizeFitter>();
        contentFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        logScrollRect.content = contentRT;

        // Vertical scrollbar
        var scrollbarGO = new GameObject("Scrollbar", typeof(RectTransform));
        scrollbarGO.transform.SetParent(logPanel.transform, false);
        var sbRT = scrollbarGO.GetComponent<RectTransform>();
        sbRT.anchorMin = new Vector2(1, 0);
        sbRT.anchorMax = new Vector2(1, 1);
        sbRT.pivot = new Vector2(1, 0.5f);
        sbRT.sizeDelta = new Vector2(10, 0);
        sbRT.anchoredPosition = Vector2.zero;
        var sbImage = scrollbarGO.AddComponent<Image>();
        sbImage.color = new Color(0.15f, 0.15f, 0.2f, 0.8f);

        var scrollbar = scrollbarGO.AddComponent<Scrollbar>();
        scrollbar.direction = Scrollbar.Direction.BottomToTop;

        // Scrollbar handle
        var slideArea = new GameObject("SlidingArea", typeof(RectTransform));
        slideArea.transform.SetParent(scrollbarGO.transform, false);
        var saRT = slideArea.GetComponent<RectTransform>();
        saRT.anchorMin = Vector2.zero;
        saRT.anchorMax = Vector2.one;
        saRT.offsetMin = Vector2.zero;
        saRT.offsetMax = Vector2.zero;

        var handle = new GameObject("Handle", typeof(RectTransform));
        handle.transform.SetParent(slideArea.transform, false);
        var handleRT = handle.GetComponent<RectTransform>();
        handleRT.anchorMin = Vector2.zero;
        handleRT.anchorMax = Vector2.one;
        handleRT.offsetMin = Vector2.zero;
        handleRT.offsetMax = Vector2.zero;
        var handleImg = handle.AddComponent<Image>();
        handleImg.color = new Color(0.5f, 0.4f, 0.7f, 0.9f);

        scrollbar.handleRect = handleRT;
        scrollbar.targetGraphic = handleImg;

        logScrollRect.verticalScrollbar = scrollbar;
        logScrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;

        // Log text
        var logGO = new GameObject("LogText", typeof(RectTransform));
        logGO.transform.SetParent(content.transform, false);
        var logRT = logGO.GetComponent<RectTransform>();
        logRT.anchorMin = Vector2.zero;
        logRT.anchorMax = new Vector2(1, 1);
        logRT.offsetMin = new Vector2(5, 2);
        logRT.offsetMax = new Vector2(-5, -2);

        logText = logGO.AddComponent<TextMeshProUGUI>();
        logText.fontSize = 11;
        logText.color = new Color(0.8f, 0.8f, 0.8f);
        logText.enableWordWrapping = true;
        logText.richText = true;
        logText.overflowMode = TextOverflowModes.Overflow;
        if (tmpFont != null) logText.font = tmpFont;
        logText.text = "<color=#888888>Log system active. Capturing mod messages and errors.</color>";
    }

    // ===========================
    // UI HELPER METHODS
    // ===========================
    GameObject CreatePanel(Transform parent, string name, Color color, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        if (size != Vector2.zero)
            go.GetComponent<RectTransform>().sizeDelta = size;
        return go;
    }

    GameObject CreateText(Transform parent, string name, string text, int fontSize, Color color,
        Vector2? anchorMin = null, Vector2? anchorMax = null, Vector2? pivot = null,
        Vector2? anchoredPos = null, Vector2? sizeDelta = null, float width = 0,
        TextAlignmentOptions alignment = TextAlignmentOptions.Left)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.enableWordWrapping = false;
        tmp.overflowMode = TextOverflowModes.Ellipsis;
        if (tmpFont != null) tmp.font = tmpFont;

        var rt = go.GetComponent<RectTransform>();
        if (anchorMin.HasValue) rt.anchorMin = anchorMin.Value;
        if (anchorMax.HasValue) rt.anchorMax = anchorMax.Value;
        if (pivot.HasValue) rt.pivot = pivot.Value;
        if (anchoredPos.HasValue) rt.anchoredPosition = anchoredPos.Value;
        if (sizeDelta.HasValue) rt.sizeDelta = sizeDelta.Value;

        if (width > 0)
        {
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.minWidth = width;
        }
        return go;
    }

    GameObject CreateButton(Transform parent, string name, string text, int fontSize,
        Color bgCol, Color textCol, Action onClick, float width = 0)
    {
        var go = CreatePanel(parent, name, bgCol, Vector2.zero);
        var btn = go.AddComponent<Button>();
        btn.onClick.AddListener(() => {
            onClick();
            // Clear selection so spacebar doesn't re-trigger
            if (EventSystem.current != null)
                EventSystem.current.SetSelectedGameObject(null);
        });
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.3f, 1.3f, 1.3f);
        colors.pressedColor = new Color(0.7f, 0.7f, 0.7f);
        btn.colors = colors;

        var txt = CreateText(go.transform, "Text", text, fontSize, textCol,
            alignment: TextAlignmentOptions.Center);
        var txtRT = txt.GetComponent<RectTransform>();
        txtRT.anchorMin = Vector2.zero;
        txtRT.anchorMax = Vector2.one;
        txtRT.sizeDelta = Vector2.zero;

        if (width > 0)
        {
            var le = go.AddComponent<LayoutElement>();
            le.preferredWidth = width;
            le.minWidth = width;
        }
        var le2 = go.GetComponent<LayoutElement>() ?? go.AddComponent<LayoutElement>();
        le2.preferredHeight = 30;
        le2.minHeight = 28;
        return go;
    }

    TMP_InputField CreateInputField(Transform parent, string name, string placeholder)
    {
        var go = CreatePanel(parent, name, new Color(0.15f, 0.15f, 0.22f), Vector2.zero);
        var le = go.AddComponent<LayoutElement>();
        le.flexibleWidth = 1;
        le.preferredHeight = 30;

        var textArea = new GameObject("TextArea", typeof(RectTransform));
        textArea.transform.SetParent(go.transform, false);
        var taRT = textArea.GetComponent<RectTransform>();
        taRT.anchorMin = Vector2.zero;
        taRT.anchorMax = Vector2.one;
        taRT.offsetMin = new Vector2(8, 2);
        taRT.offsetMax = new Vector2(-8, -2);
        textArea.AddComponent<RectMask2D>();

        var ph = CreateText(textArea.transform, "Placeholder", placeholder, 13, new Color(0.5f, 0.5f, 0.5f));
        var phRT = ph.GetComponent<RectTransform>();
        phRT.anchorMin = Vector2.zero; phRT.anchorMax = Vector2.one; phRT.sizeDelta = Vector2.zero;

        var inputText = CreateText(textArea.transform, "Text", "", 13, Color.white);
        var itRT = inputText.GetComponent<RectTransform>();
        itRT.anchorMin = Vector2.zero; itRT.anchorMax = Vector2.one; itRT.sizeDelta = Vector2.zero;

        var input = go.AddComponent<TMP_InputField>();
        input.textViewport = taRT;
        input.textComponent = inputText.GetComponent<TextMeshProUGUI>();
        input.placeholder = ph.GetComponent<TextMeshProUGUI>();
        if (tmpFont != null) input.fontAsset = tmpFont;
        input.pointSize = 13;
        input.onValueChanged.AddListener(OnSearchChanged);
        return input;
    }

    void CreateToggleRow(Transform parent, string key, string label, bool initial, Action<bool> onToggle)
    {
        var row = CreateRow(parent, "Toggle_" + key);
        CreateText(row.transform, "Label", label, 14, Color.white, width: 220);
        var btnGO = CreateButton(row.transform, "Btn", initial ? "ON" : "OFF", 14,
            initial ? greenColor : redColor, Color.white, () => {
                bool newVal = !GetToggleState(key);
                onToggle(newVal);
                UpdateToggleVisual(key, newVal);
            }, 65);
        toggleBGs[key] = btnGO.GetComponent<Image>();
        toggleTexts[key] = btnGO.GetComponentInChildren<TextMeshProUGUI>();
    }

    bool GetToggleState(string key)
    {
        switch (key) {
            case "godMode": return godMode;
            case "infStam": return infiniteStamina;
            case "noHunger": return noHunger;
            case "noThirst": return noThirst;
            case "noFall": return noFallDamage;
            case "speed": return speedHack;
            case "teleport": return teleportMode;
            case "noclip": return noclipMode;
            default: return false;
        }
    }

    void UpdateToggleVisual(string key, bool val)
    {
        if (toggleBGs.ContainsKey(key))
            toggleBGs[key].color = val ? greenColor : redColor;
        if (toggleTexts.ContainsKey(key))
            toggleTexts[key].text = val ? "ON" : "OFF";
    }

    GameObject CreateRow(Transform parent, string name)
    {
        var row = new GameObject(name, typeof(RectTransform));
        row.transform.SetParent(parent, false);
        var hlg = row.AddComponent<HorizontalLayoutGroup>();
        hlg.spacing = 6;
        hlg.childForceExpandWidth = true;
        hlg.childForceExpandHeight = false;
        hlg.childAlignment = TextAnchor.MiddleLeft;
        var le = row.AddComponent<LayoutElement>();
        le.preferredHeight = 32;
        return row;
    }

    void CreateHeader(Transform parent, string text)
    {
        var go = CreateText(parent, "Header_" + text, "=== " + text + " ===", 15, accentColor,
            alignment: TextAlignmentOptions.Center);
        var le = go.AddComponent<LayoutElement>();
        le.preferredHeight = 25;
    }

    void CreateSpacer(Transform parent, float size, bool horizontal = false)
    {
        var go = new GameObject("Spacer", typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var le = go.AddComponent<LayoutElement>();
        if (horizontal) le.preferredWidth = size;
        else le.preferredHeight = size;
    }

    VerticalLayoutGroup AddVerticalLayout(GameObject go, float spacing)
    {
        var vlg = go.AddComponent<VerticalLayoutGroup>();
        vlg.spacing = spacing;
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        vlg.childAlignment = TextAnchor.UpperLeft;
        return vlg;
    }

    // ===========================
    // SPAWNER LOGIC
    // ===========================
    void OnSearchChanged(string search)
    {
        spawnPageStart = 0;
        filteredItems = string.IsNullOrEmpty(search)
            ? new List<string>(allItemIds)
            : allItemIds.Where(id => id.ToLower().Contains(search.ToLower())).ToList();
        RefreshSpawnList();
    }

    void RefreshSpawnList()
    {
        if (spawnListContainer == null) return;
        for (int i = spawnListContainer.childCount - 1; i >= 0; i--)
            Destroy(spawnListContainer.GetChild(i).gameObject);

        int end = Mathf.Min(spawnPageStart + ITEMS_PER_PAGE, filteredItems.Count);
        for (int i = spawnPageStart; i < end; i++)
        {
            string itemId = filteredItems[i];
            var row = CreateRow(spawnListContainer, "Item_" + i);
            row.GetComponent<LayoutElement>().preferredHeight = 26;
            CreateText(row.transform, "Name", itemId, 13, Color.white, width: 280);
            CreateButton(row.transform, "Spawn", "Spawn", 12, accentColor, Color.white,
                () => SpawnItem(itemId, spawnCount), 70);
        }

        if (pageLabel != null)
            pageLabel.text = filteredItems.Count > 0
                ? (spawnPageStart + 1) + "-" + end + "/" + filteredItems.Count
                : "0/0";
    }

    // ===========================
    // UPDATE LOOP
    // ===========================
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            menuOpen = !menuOpen;
            mainPanel.SetActive(menuOpen);
        }

        // F5 = Hot reload mods from mods/items/ folder
        if (Input.GetKeyDown(KeyCode.F5))
        {
            try
            {
                int oldCount = allItemIds.Count;
                int count = ItemLoader.HotReload();
                RefreshItemList();
                int newCount = allItemIds.Count;
                Debug.Log("[ModManager] Hot reload: " + count + " custom items. Total items: " + oldCount + " -> " + newCount);
                if (menuOpen) { RefreshSpawnList(); }
            }
            catch (System.Exception e)
            {
                Debug.LogError("[ModManager] Hot reload failed: " + e.Message);
            }
        }

        // Middle-click teleport (works even with menu closed)
        if (teleportMode && Input.GetMouseButtonDown(2))
        {
            TeleportToMouse();
        }

        ApplyCheats();

        if (menuOpen)
        {
            float now = Time.time;
            bool uiTick = now - lastUIUpdate > 0.25f;

            // Position always updates when menu open (it's on Player tab header area)
            if (uiTick) UpdatePositionDisplay();

            // Tab-specific updates
            if (currentTab == 4 && uiTick) UpdateInfoTab();

            // Log display — always check for new entries (lightweight, only writes on change)
            UpdateLogDisplay();

            // Radar update (throttled)
            if (currentTab == 2 && now - lastRadarUpdate > RADAR_INTERVAL)
            {
                lastRadarUpdate = now;
                UpdateRadar();
            }

            if (uiTick) lastUIUpdate = now;
        }
    }

    void UpdatePositionDisplay()
    {
        if (positionText == null) return;
        try
        {
            var body = FindObjectOfType<Body>();
            if (body != null)
            {
                Vector3 pos = body.transform.position;
                positionText.text = "X: " + pos.x.ToString("F1") + "  Y: " + pos.y.ToString("F1");
            }
        }
        catch (Exception) { }
    }

    void UpdateRadar()
    {
        if (radarListContainer == null) return;

        // Clear old entries
        for (int i = radarListContainer.childCount - 1; i >= 0; i--)
            Destroy(radarListContainer.GetChild(i).gameObject);

        try
        {
            var playerBody = FindObjectOfType<Body>();
            if (playerBody == null) return;
            Vector2 playerPos = playerBody.transform.position;

            var entries = new List<RadarEntry>();

            // Find enemies — SpiderHandler components (actual enemies)
            var spiders = FindObjectsOfType<SpiderHandler>();
            foreach (var spider in spiders)
            {
                float dist = Vector2.Distance(playerPos, spider.transform.position);
                if (dist <= radarRadius)
                {
                    string name = spider.gameObject.name;
                    // Clean up Unity clone naming
                    name = name.Replace("(Clone)", "").Trim();

                    // Get HP from BuildingEntity component
                    float hp = -1;
                    var bld = spider.GetComponent<BuildingEntity>();
                    if (bld != null)
                    {
                        var hpField = typeof(BuildingEntity).GetField("health", BindingFlags.Public | BindingFlags.Instance);
                        if (hpField != null) hp = (float)hpField.GetValue(bld);
                    }

                    Vector2 dir = (Vector2)spider.transform.position - playerPos;
                    string direction = GetDirection(dir);

                    entries.Add(new RadarEntry {
                        name = name,
                        distance = dist,
                        direction = direction,
                        type = "enemy",
                        extra = hp >= 0 ? "HP:" + hp.ToString("F0") : ""
                    });
                }
            }

            // Find traders/survivors
            var traders = FindObjectsOfType<TraderScript>();
            foreach (var trader in traders)
            {
                float dist = Vector2.Distance(playerPos, trader.transform.position);
                if (dist <= radarRadius)
                {
                    string name = trader.gameObject.name.Replace("(Clone)", "").Trim();

                    float hp = -1;
                    var bld = trader.GetComponent<BuildingEntity>();
                    if (bld != null)
                    {
                        var hpField = typeof(BuildingEntity).GetField("health", BindingFlags.Public | BindingFlags.Instance);
                        if (hpField != null) hp = (float)hpField.GetValue(bld);
                    }

                    // Check hostility
                    float hostility = 0;
                    var hostField = typeof(TraderScript).GetField("hostility", BindingFlags.Public | BindingFlags.Instance);
                    if (hostField != null) hostility = (float)hostField.GetValue(trader);
                    bool isHostile = hostility >= 100f;

                    // Check reputation
                    float rep = 0;
                    var repField = typeof(TraderScript).GetField("reputation", BindingFlags.Public | BindingFlags.Instance);
                    if (repField != null) rep = (float)repField.GetValue(trader);

                    Vector2 dir = (Vector2)trader.transform.position - playerPos;
                    string direction = GetDirection(dir);

                    string extra = "";
                    if (hp >= 0) extra += "HP:" + hp.ToString("F0") + " ";
                    extra += isHostile ? "HOSTILE" : "Rep:" + rep.ToString("F0");

                    entries.Add(new RadarEntry {
                        name = name,
                        distance = dist,
                        direction = direction,
                        type = isHostile ? "hostile_trader" : "trader",
                        extra = extra.Trim()
                    });
                }
            }

            // Find other destructible entities (not spiders, not traders)
            var buildings = FindObjectsOfType<BuildingEntity>();
            foreach (var bld in buildings)
            {
                // Skip if already counted (spider or trader)
                if (bld.GetComponent<SpiderHandler>() != null) continue;
                if (bld.GetComponent<TraderScript>() != null) continue;

                float dist = Vector2.Distance(playerPos, bld.transform.position);
                if (dist <= radarRadius)
                {
                    string name = bld.gameObject.name.Replace("(Clone)", "").Trim();
                    float hp = 0;
                    var hpField = typeof(BuildingEntity).GetField("health", BindingFlags.Public | BindingFlags.Instance);
                    if (hpField != null) hp = (float)hpField.GetValue(bld);

                    Vector2 dir = (Vector2)bld.transform.position - playerPos;
                    string direction = GetDirection(dir);

                    entries.Add(new RadarEntry {
                        name = name,
                        distance = dist,
                        direction = direction,
                        type = "structure",
                        extra = "HP:" + hp.ToString("F0")
                    });
                }
            }

            // Find items on the ground
            var items = FindObjectsOfType<Item>();
            foreach (var item in items)
            {
                // Skip items in containers/inventory (parented)
                if (item.transform.parent != null) continue;

                float dist = Vector2.Distance(playerPos, item.transform.position);
                if (dist <= radarRadius)
                {
                    string itemName = item.gameObject.name;
                    var idField = typeof(Item).GetField("id", BindingFlags.Public | BindingFlags.Instance);
                    if (idField != null)
                    {
                        string id = idField.GetValue(item) as string;
                        if (!string.IsNullOrEmpty(id)) itemName = id;
                    }

                    Vector2 dir = (Vector2)item.transform.position - playerPos;
                    string direction = GetDirection(dir);

                    entries.Add(new RadarEntry {
                        name = itemName,
                        distance = dist,
                        direction = direction,
                        type = "item",
                        extra = ""
                    });
                }
            }

            // Sort by distance
            entries.Sort((a, b) => a.distance.CompareTo(b.distance));

            // Count
            int enemyCount = entries.Count(e => e.type == "enemy");
            int traderCount = entries.Count(e => e.type == "trader");
            int itemCount = entries.Count(e => e.type == "item");
            int structCount = entries.Count(e => e.type == "structure");
            if (radarCountText != null)
                radarCountText.text = enemyCount + "E/" + traderCount + "T/" + itemCount + "I";

            // Display (max ~15 entries to prevent lag)
            int shown = 0;
            foreach (var entry in entries)
            {
                if (shown >= 15) break;

                Color entryColor;
                switch (entry.type)
                {
                    case "enemy": entryColor = new Color(1f, 0.4f, 0.4f); break;
                    case "hostile_trader": entryColor = new Color(1f, 0.3f, 0.1f); break;
                    case "trader": entryColor = new Color(0.3f, 1f, 0.4f); break;
                    case "structure": entryColor = new Color(1f, 0.8f, 0.3f); break;
                    default: entryColor = new Color(0.4f, 0.8f, 1f); break;
                }

                string label = entry.direction + " " + entry.distance.ToString("F0") + "m  ";
                label += entry.name;
                if (!string.IsNullOrEmpty(entry.extra)) label += " [" + entry.extra + "]";

                var textGO = CreateText(radarListContainer, "R_" + shown, label, 12, entryColor);
                var le = textGO.AddComponent<LayoutElement>();
                le.preferredHeight = 16;
                shown++;
            }

            if (shown == 0)
            {
                CreateText(radarListContainer, "Empty", "(nothing nearby)", 12, new Color(0.5f, 0.5f, 0.5f));
            }
        }
        catch (Exception e)
        {
            Debug.Log("[ModManager] Radar error: " + e.Message);
        }
    }

    struct RadarEntry
    {
        public string name;
        public float distance;
        public string direction;
        public string type;
        public string extra;
    }

    string GetDirection(Vector2 dir)
    {
        if (dir.magnitude < 0.5f) return "[HERE]";

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        if (angle < 0) angle += 360f;

        if (angle >= 337.5f || angle < 22.5f) return "[E ]";
        if (angle >= 22.5f && angle < 67.5f) return "[NE]";
        if (angle >= 67.5f && angle < 112.5f) return "[N ]";
        if (angle >= 112.5f && angle < 157.5f) return "[NW]";
        if (angle >= 157.5f && angle < 202.5f) return "[W ]";
        if (angle >= 202.5f && angle < 247.5f) return "[SW]";
        if (angle >= 247.5f && angle < 292.5f) return "[S ]";
        return "[SE]";
    }

    void UpdateInfoTab()
    {
        if (infoText == null) return;
        try
        {
            // Compact single-line format
            string info = "";
            var body = FindObjectOfType<Body>();
            if (body != null)
            {
                // Row 1: Vitals
                float hp = GetBodyFloat(body, "health");
                float stam = GetBodyFloat(body, "stamina");
                float hunger = GetBodyFloat(body, "hunger");
                float thirst = GetBodyFloat(body, "thirst");
                float bleed = GetBodyFloat(body, "bleedRate");
                info += "HP:" + hp.ToString("F0") + "  STA:" + stam.ToString("F0") + 
                        "  HUN:" + hunger.ToString("F0") + "  THR:" + thirst.ToString("F0");
                if (bleed > 0) info += "  BLD:" + bleed.ToString("F1");

                // Row 2: XP
                var skillsField = typeof(Body).GetField("skills", BindingFlags.Public | BindingFlags.Instance);
                if (skillsField != null)
                {
                    var skills = skillsField.GetValue(body);
                    if (skills != null)
                    {
                        float str = GetFloat(skills, "expSTR");
                        float res = GetFloat(skills, "expRES");
                        float intl = GetFloat(skills, "expINT");
                        info += "\nSTR:" + str.ToString("F0") + "  RES:" + res.ToString("F0") + "  INT:" + intl.ToString("F0");
                    }
                }
            }

            // Row 3: Cheats (compact)
            var cheats = new List<string>();
            if (godMode) cheats.Add("God");
            if (infiniteStamina) cheats.Add("Stam");
            if (noHunger) cheats.Add("NoHng");
            if (noThirst) cheats.Add("NoThr");
            if (noFallDamage) cheats.Add("NoFall");
            if (speedHack) cheats.Add("Spd" + speedMultiplier + "x");
            if (teleportMode) cheats.Add("TP");
            info += "\nItems:" + allItemIds.Count + "  Cheats:" + (cheats.Count > 0 ? string.Join(",", cheats.ToArray()) : "none");

            infoText.text = info;
        }
        catch (Exception) { }

    }

    void UpdateLogDisplay()
    {
        // ONLY update when entries change to avoid resetting scroll
        if (logText != null && logEntries.Count != lastLogCount)
        {
            lastLogCount = logEntries.Count;

            if (logEntries.Count > 0)
            {
                // Reverse order — newest at top so default scroll position shows latest
                var reversed = new List<string>(logEntries);
                reversed.Reverse();
                logText.text = string.Join("\n", reversed.ToArray());
            }
            else
            {
                logText.text = "<color=#666666>No log entries yet.</color>";
            }
        }
    }

    // ===========================
    // CHEAT APPLICATION
    // ===========================
    void ApplyCheats()
    {
        try
        {
            var body = FindObjectOfType<Body>();
            if (body == null) return;

            if (godMode)
            {
                SetField(body, "health", 100f);
                SetField(body, "bleedRate", 0f);
            }
            if (infiniteStamina)
                SetField(body, "stamina", 100f);
            if (noHunger)
                SetField(body, "hunger", 100f);
            if (noThirst)
                SetField(body, "thirst", 100f);
            if (noFallDamage)
            {
                // Zero out the rigidbody Y velocity when falling
                var rb = body.GetComponent<Rigidbody2D>();
                if (rb != null && rb.velocity.y < -8f)
                    rb.velocity = new Vector2(rb.velocity.x, -8f);
            }
            if (speedHack)
                SetField(body, "maxSpeed", 5f * speedMultiplier);
        }
        catch (Exception) { }
    }

    void SetField(object obj, string fieldName, float value)
    {
        var f = obj.GetType().GetField(fieldName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
        if (f != null) f.SetValue(obj, value);
    }

    // ===========================
    // CONSOLE COMMAND EXECUTION
    // ===========================
    void RunConsoleCommand(string fullCmd)
    {
        try
        {
            var console = FindObjectOfType<ConsoleScript>();
            if (console == null) { Debug.Log("[ModManager] ConsoleScript not found"); return; }

            // Split into args array
            string[] args = fullCmd.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

            // Call TryExecuteCommand(string[] args, bool addToLog)
            var method = typeof(ConsoleScript).GetMethod("TryExecuteCommand",
                BindingFlags.Public | BindingFlags.Instance);
            if (method != null)
            {
                method.Invoke(console, new object[] { args, false });
                Debug.Log("[ModManager] Executed: " + fullCmd);
            }
            else
            {
                Debug.Log("[ModManager] TryExecuteCommand method not found");
            }
        }
        catch (Exception e) { Debug.Log("[ModManager] Console error: " + e.Message); }
    }

    // ===========================
    // XP CONTROL
    // ===========================
    void AddXP(string type, float amount)
    {
        try
        {
            var body = FindObjectOfType<Body>();
            if (body == null) return;

            var skillsField = typeof(Body).GetField("skills", BindingFlags.Public | BindingFlags.Instance);
            if (skillsField == null) return;
            var skills = skillsField.GetValue(body);
            if (skills == null) return;

            if (amount > 0)
            {
                // Positive: use normal AddExp (handles level ups)
                int typeIdx = type == "str" ? 0 : type == "res" ? 1 : 2;
                var addExp = skills.GetType().GetMethod("AddExp", BindingFlags.Public | BindingFlags.Instance);
                if (addExp != null)
                    addExp.Invoke(skills, new object[] { typeIdx, amount });
            }
            else
            {
                // Negative: directly set XP and recalculate level
                string expField = type == "str" ? "expSTR" : type == "res" ? "expRES" : "expINT";
                string levelField = type == "str" ? "STR" : type == "res" ? "RES" : "INT";

                var expF = skills.GetType().GetField(expField, BindingFlags.Public | BindingFlags.Instance);
                var levelF = skills.GetType().GetField(levelField, BindingFlags.Public | BindingFlags.Instance);
                if (expF == null || levelF == null) return;

                float currentXP = (float)expF.GetValue(skills);
                float newXP = Mathf.Max(0f, currentXP + amount); // amount is negative
                expF.SetValue(skills, newXP);

                // Recalculate level from XP using GetExperienceForLevel
                var getExpForLevel = skills.GetType().GetMethod("GetExperienceForLevel",
                    BindingFlags.Public | BindingFlags.Static);
                if (getExpForLevel != null)
                {
                    int newLevel = 1;
                    for (int lv = 1; lv < 50; lv++)
                    {
                        int xpNeeded = (int)getExpForLevel.Invoke(null, new object[] { lv });
                        if (newXP >= xpNeeded)
                            newLevel = lv;
                        else
                            break;
                    }
                    levelF.SetValue(skills, newLevel);
                }

                // Update boundaries
                var updateBounds = skills.GetType().GetMethod("UpdateExpBoundaries",
                    BindingFlags.Public | BindingFlags.Instance);
                if (updateBounds != null)
                    updateBounds.Invoke(skills, null);
            }

            Debug.Log("[ModManager] XP " + type + ": " + (amount > 0 ? "+" : "") + amount);
        }
        catch (Exception e) { Debug.Log("[ModManager] XP error: " + e.Message); }
    }

    void AddLevel(string type, int delta)
    {
        try
        {
            var body = FindObjectOfType<Body>();
            if (body == null) return;
            var skillsField = typeof(Body).GetField("skills", BindingFlags.Public | BindingFlags.Instance);
            if (skillsField == null) return;
            var skills = skillsField.GetValue(body);
            if (skills == null) return;

            string levelField = type == "str" ? "STR" : type == "res" ? "RES" : "INT";
            var levelF = skills.GetType().GetField(levelField, BindingFlags.Public | BindingFlags.Instance);
            if (levelF == null) return;

            int current = (int)levelF.GetValue(skills);
            int newLevel = Mathf.Clamp(current + delta, 1, 50);
            SetLevel(type, newLevel);
        }
        catch (Exception e) { Debug.Log("[ModManager] AddLevel error: " + e.Message); }
    }

    void SetLevel(string type, int level)
    {
        try
        {
            var body = FindObjectOfType<Body>();
            if (body == null) return;
            var skillsField = typeof(Body).GetField("skills", BindingFlags.Public | BindingFlags.Instance);
            if (skillsField == null) return;
            var skills = skillsField.GetValue(body);
            if (skills == null) return;

            string levelField = type == "str" ? "STR" : type == "res" ? "RES" : "INT";
            string expField = type == "str" ? "expSTR" : type == "res" ? "expRES" : "expINT";

            var levelF = skills.GetType().GetField(levelField, BindingFlags.Public | BindingFlags.Instance);
            var expF = skills.GetType().GetField(expField, BindingFlags.Public | BindingFlags.Instance);
            if (levelF == null || expF == null) return;

            levelF.SetValue(skills, level);

            // Set XP to the minimum for that level
            var getExpForLevel = skills.GetType().GetMethod("GetExperienceForLevel",
                BindingFlags.Public | BindingFlags.Static);
            if (getExpForLevel != null)
            {
                int xpForLevel = (int)getExpForLevel.Invoke(null, new object[] { level });
                expF.SetValue(skills, (float)xpForLevel);
            }

            var updateBounds = skills.GetType().GetMethod("UpdateExpBoundaries",
                BindingFlags.Public | BindingFlags.Instance);
            if (updateBounds != null)
                updateBounds.Invoke(skills, null);

            Debug.Log("[ModManager] Set " + type + " to level " + level);
        }
        catch (Exception e) { Debug.Log("[ModManager] SetLevel error: " + e.Message); }
    }

    // ===========================
    // ACTIONS
    // ===========================
    void SwitchTab(int idx)
    {
        currentTab = idx;
        for (int i = 0; i < tabPanels.Length; i++)
        {
            tabPanels[i].SetActive(i == idx);
            tabButtons[i].GetComponent<Image>().color = (i == idx) ? accentColor : tabInactive;
        }
        // Refresh spawn list when switching to spawner tab
        if (idx == 1)
        {
            if (allItemIds.Count == 0) RefreshItemList();
            RefreshSpawnList();
        }
    }

    void SpawnItem(string itemId, int count)
    {
        try
        {
            var body = FindObjectOfType<Body>();
            if (body == null) return;
            for (int i = 0; i < count; i++)
            {
                Vector2 pos = body.transform.position;
                pos.x += UnityEngine.Random.Range(-1.5f, 1.5f);
                pos.y += UnityEngine.Random.Range(0.5f, 2.5f);
                Ext.Create(itemId, pos, 0f);
            }
        }
        catch (Exception e) { Debug.Log("[ModManager] Spawn error: " + e.Message); }
    }

    void TeleportToMouse()
    {
        try
        {
            var body = FindObjectOfType<Body>();
            if (body == null) return;
            var cam = Camera.main;
            if (cam == null) return;
            Vector3 mp = cam.ScreenToWorldPoint(Input.mousePosition);
            mp.z = body.transform.position.z;
            body.transform.position = mp;
        }
        catch (Exception) { }
    }
}

// ===========================
// DRAG HANDLER
// ===========================
public class DragHandler : MonoBehaviour, IDragHandler, IBeginDragHandler
{
    private Vector2 offset;
    private RectTransform rt;
    void Awake() { rt = GetComponent<RectTransform>(); }
    public void OnBeginDrag(PointerEventData e)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rt.parent as RectTransform, e.position, e.pressEventCamera, out offset);
        offset = rt.anchoredPosition - offset;
    }
    public void OnDrag(PointerEventData e)
    {
        Vector2 pos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            rt.parent as RectTransform, e.position, e.pressEventCamera, out pos);
        rt.anchoredPosition = pos + offset;
    }
}
