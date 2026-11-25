using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

// Spawns the 3D board into the game's scenes, so the scene files and game logic stay untouched.
public static class WarTableBootstrap
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Initialize()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
        Spawn();
    }

    static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Spawn();
    }

    static void Spawn()
    {
        if (Object.FindFirstObjectByType<WarTable>() != null)
            return;

        var country = Object.FindFirstObjectByType<CountryManager>(FindObjectsInactive.Include);
        bool isMenu = country == null && Object.FindFirstObjectByType<MainMenuManager>(FindObjectsInactive.Include) != null;
        if (country == null && !isMenu)
            return;

        var art = Resources.Load<BoardArt>(BoardArt.ResourcePath);
        if (art == null)
        {
            Debug.LogError("[WarTable] BoardArt is missing. Run Cold War > Rebuild 3D Board.");
            return;
        }

        new GameObject("WarTable").AddComponent<WarTable>().Begin(art, country);
    }
}

// Reads game state and shows it on the board. It never writes back to the game.
public partial class WarTable : MonoBehaviour
{
    // Tanks travel and park in an arrowhead: one in front, the others in pairs behind it to the left and right.
    static Vector3 ArrowSpot(int i)
    {
        int row = (i + 1) / 2;
        float sideways = i == 0 ? 0f : (i % 2 == 1 ? -1f : 1f) * Tuning.tankSpread * row;
        return new Vector3(sideways, 0f, -Tuning.tankGap * row);
    }

    // Every unit is shown on the board. A cap can be set in Game Tuning (0 = no cap).
    static bool AtCap(int count, int cap) => cap > 0 && count >= cap;
    const float TileWidth = 0.86f;
    const int UnrestBelow = 25;
    // Missiles shown in the pool at 100% danger. They are split between the halves by each side's share of the power,
    // so each half has room for all of them.
    const int PoolCapacity = 24;
    const float PoolRadius = 3.3f;
    static readonly Vector2 PoolSpot = new Vector2(0.42f, 0.72f);

    class ActionVisual
    {
        public float totalMonths;
        public Action<float> onProgress;
        public Action onDone;
        public Action onCancel;
    }

    class Orbiter
    {
        public Transform transform;
        public Vector3 center;
        public float radius;
        public float speed;
        public float angle;
        public float bob;
        // Satellites: which side's capitol they circle and their place in the order, so their circle follows the
        // Game Tuning values live.
        public Side side;
        public int index = -1;
    }

    class Side
    {
        public bool isRed;
        public Color color;
        public int capitalTile;
        public Vector3 capital;
        public Transform capitalRoot;
        public Transform capitol;
        public float capitalTop;
        public GameObject[] flags;
        public readonly List<Transform> missiles = new List<Transform>();
        public readonly List<Vector3> missileHomes = new List<Vector3>();
        public readonly List<Vector3> missileScales = new List<Vector3>();
        public readonly List<ParticleSystem> fires = new List<ParticleSystem>();
        public readonly Dictionary<ActionFunction, ActionVisual> actions = new Dictionary<ActionFunction, ActionVisual>();
        public President president;
        public int factories;
        public int wars;
        public int spies;
        public int satellites;
        public int homeBuilds;
        public Transform infoCenter;
        public Transform infoSign;
        public MeshFilter infoBuilding;
        // The hidden level the side's units are drawn at, counted from 0 (level 1). -1 until first set.
        public int mark = -1;
        public readonly List<Transform> tankColumns = new List<Transform>();
        public readonly List<Transform> fleets = new List<Transform>();
        public readonly List<Transform> satellites3D = new List<Transform>();
        public readonly List<FactorySpot> factorySpots = new List<FactorySpot>();
        // Factories and tank columns grow a little with every power tier.
        public readonly List<Transform> upgradable = new List<Transform>();
        public readonly List<Vector3> upgradableScales = new List<Vector3>();
        public float shownGrowth = 1f;
    }

    class FactorySpot
    {
        public GameObject factory;
        public int tile;
    }

    static GameTuning Tuning => GameTuning.Current;

    BoardArt art;
    HexBoard board;
    BoardFx fx;
    CountryManager country;
    Camera mainCamera;
    Side blue;
    Side red;
    readonly HashSet<int> claimed = new HashSet<int>();
    readonly List<Orbiter> orbiters = new List<Orbiter>();
    readonly List<Vector3> warSites = new List<Vector3>();
    readonly HashSet<ActionFunction> seen = new HashSet<ActionFunction>();

    Vector3 cameraHome;
    Quaternion cameraRotation;
    float cameraAspect;
    float shake;
    float nextWarBlast;
    bool endOfWorld;

    Vignette vignette;
    ColorAdjustments grading;


    bool IsMenu => country == null;
    public static bool IsRunning { get; private set; }

    // The board animates on real time, so it keeps moving through the tutorial's slow-motion steps. Only the
    // player's pause button freezes it: these stop while paused and carry on from the same point afterwards.
    public static bool Frozen => CountryManager.PlayerPaused && Time.timeScale == 0f;
    public static float WorldDelta => Frozen ? 0f : Time.unscaledDeltaTime;
    public static float WorldTime { get; private set; }
    bool particlesFrozen;

    // Particles run on real time too, so they are paused and resumed with the board.
    void FreezeParticles(bool freeze)
    {
        if (freeze == particlesFrozen)
            return;
        particlesFrozen = freeze;
        foreach (ParticleSystem system in GetComponentsInChildren<ParticleSystem>())
        {
            if (freeze)
                system.Pause(false);
            else if (system.isPaused)
                system.Play(false);
        }
    }

    public Transform CapitalRoot(bool isRed) => (isRed ? red : blue).capitalRoot;
    public Transform InfoCenter(bool isRed) => (isRed ? red : blue).infoCenter;

    // Tutorial step 10 shows what unrest looks like: Red's capital is drawn as if its people were very unhappy.
    const int DemoHappiness = 8;
    bool ShowingUnrestDemo(Side side) => side.isRed && country != null && country.IsTutorial && Tutorial.Level == 9;
    int ShownHappiness(Side side, Party party) => ShowingUnrestDemo(side) ? DemoHappiness : party.PeoplSatsfaction;

    // Just south of Red's capital, where the demo fires burn.
    public Vector3 DemoFireSpot => red.capital + new Vector3(0f, 0.2f, -2.3f);

    // The top of the tallest flag still standing over a capital.
    public Vector3 FlagsTop(bool isRed)
    {
        Side side = isRed ? red : blue;
        Bounds? all = null;
        foreach (GameObject flag in side.flags)
        {
            if (!flag.activeSelf)
                continue;
            Bounds bounds = WorldBounds(flag);
            if (all.HasValue)
            {
                Bounds grown = all.Value;
                grown.Encapsulate(bounds);
                all = grown;
            }
            else
                all = bounds;
        }

        return all.HasValue ? new Vector3(all.Value.center.x, all.Value.max.y, all.Value.center.z) : CapitalTop(isRed);
    }

    // The capitol building alone, without its flags.
    public Bounds CapitolBounds(bool isRed)
    {
        Transform capitol = (isRed ? red : blue).capitol;
        var renderer = capitol.GetComponent<Renderer>();
        return renderer != null ? renderer.bounds : new Bounds(CapitalTop(isRed), Vector3.one);
    }

    public Vector3 CapitalTop(bool isRed)
    {
        Side side = isRed ? red : blue;
        return new Vector3(side.capital.x, side.capitalTop, side.capital.z);
    }

    World3DUi ui;
    bool peaceStarted;
    float nextFirework;
    float nextFocusPulse;

    public void Begin(BoardArt boardArt, CountryManager countryManager)
    {
        art = boardArt;
        country = countryManager;
        fx = new BoardFx(art, transform);

        SetupLightingAndCamera();
        SetupPostProcessing();

        var boardObject = new GameObject("HexBoard");
        boardObject.transform.SetParent(transform, false);
        board = boardObject.AddComponent<HexBoard>();
        board.Build(WithCapitalGround(art.data), art.boardMaterial);
        board.SnapTo(IsMenu ? 0.5f : GameManager.Com);


        blue = BuildSide(false, art.data.blueCapital, art.data.blueSilos, art.blueCity);
        red = BuildSide(true, art.data.redCapital, art.data.redSilos, art.redCity);
        tallestCapital = Mathf.Max(blue.capitalTop, red.capitalTop);
        BuildNukePool();

        HideFlatMap();
        FrameCamera();

        if (!IsMenu)
        {
            IsRunning = true;
            ui = gameObject.AddComponent<World3DUi>();
            ui.enabled = false;
            CountryManager.Refused += OnRefused;
            CountryManager.ActionCompletedInstantly += OnInstantAction;
            CountryManager.ChaosStruck += OnChaos;
        }
    }

    void Start()
    {
        // Waits for CountryManager.Start, which sets up the parties and hides the tutorial HUD.
        if (ui != null)
        {
            ui.Begin(this, country, art, mainCamera);
            ui.enabled = true;
        }
    }

    void OnDestroy()
    {
        IsRunning = false;
        CountryManager.Refused -= OnRefused;
        CountryManager.ActionCompletedInstantly -= OnInstantAction;
        CountryManager.ChaosStruck -= OnChaos;
    }

    // A president's agenda finishes actions immediately, so their visuals jump straight to the result.
    void OnInstantAction(Party party, ActionFunction action)
    {
        Side side = party == country.Communism ? red : blue;
        ActionVisual visual = CreateActionVisual(side, action);
        visual.onDone?.Invoke();
    }

    void OnChaos(Party party, ChaosKind kind, string english, string arabic)
    {
        Color warning = new Color(1f, 0.55f, 0.15f);
        if (kind == ChaosKind.MissileCrisis)
        {
            StartCoroutine(ExpandingRing(poolCenter + Vector3.up * 0.6f, new Color(1f, 0.25f, 0.15f), PoolRadius + 2.5f));
        }
        else
        {
            foreach (Side side in party == null ? new[] { blue, red } : new[] { party == country.Communism ? red : blue })
            {
                StartCoroutine(ExpandingRing(side.capital + Vector3.up * 0.2f, warning, 4f));
                fx.Burst(side.capital + Vector3.up * 1.5f, warning, 24, 0.3f, 2.2f, 0.8f);
            }
        }
        shake = Mathf.Max(shake, 0.12f);
    }

    void OnRefused(Party party, Refusal reason)
    {
        Side side = party == country.Communism ? red : blue;
        Color warning = new Color(1f, 0.2f, 0.12f);
        switch (reason)
        {
            default: StartCoroutine(ExpandingRing(side.capital + Vector3.up * 0.2f, warning, 2.6f)); break;
        }

        shake = Mathf.Max(shake, 0.07f);
    }

    // ----- World setup -----

    void SetupLightingAndCamera()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
            mainCamera = cameraObject.AddComponent<Camera>();
        }

        mainCamera.orthographic = false;
        mainCamera.fieldOfView = 26f;
        mainCamera.nearClipPlane = 1f;
        mainCamera.farClipPlane = 300f;
        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        mainCamera.backgroundColor = new Color(0.012f, 0.025f, 0.05f);
        mainCamera.cullingMask = ~(1 << 5);

        var cameraData = mainCamera.GetComponent<UniversalAdditionalCameraData>();
        if (cameraData == null)
            cameraData = mainCamera.gameObject.AddComponent<UniversalAdditionalCameraData>();
        cameraData.renderPostProcessing = true;

        Light sun = null;
        foreach (Light light in Object.FindObjectsByType<Light>(FindObjectsSortMode.None))
        {
            if (light.type == LightType.Directional)
                sun = light;
        }

        if (sun == null)
            sun = new GameObject("Sun").AddComponent<Light>();

        sun.type = LightType.Directional;
        sun.transform.rotation = Quaternion.Euler(50f, -32f, 0f);
        sun.color = new Color(1f, 0.93f, 0.82f);
        sun.intensity = 0.82f;
        sun.shadows = LightShadows.Soft;
        sun.shadowStrength = 0.85f;

        var ambient = new Color(0.26f, 0.28f, 0.36f);
        Shader.SetGlobalColor("_BoardAmbient", ambient);
        var probe = new SphericalHarmonicsL2();
        probe.AddAmbientLight(ambient);
        RenderSettings.ambientMode = AmbientMode.Custom;
        RenderSettings.ambientProbe = probe;
    }

    void SetupPostProcessing()
    {
        var volume = gameObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 10f;
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        volume.profile = profile;

        var bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(1.05f);
        bloom.intensity.Override(0.45f);
        bloom.scatter.Override(0.6f);

        var tonemap = profile.Add<Tonemapping>(true);
        tonemap.mode.Override(TonemappingMode.Neutral);

        grading = profile.Add<ColorAdjustments>(true);
        grading.contrast.Override(8f);
        grading.saturation.Override(0f);
        grading.colorFilter.Override(Color.white);

        vignette = profile.Add<Vignette>(true);
        vignette.intensity.Override(0.28f);
        vignette.smoothness.Override(0.5f);
        vignette.color.Override(Color.black);
    }

    void FrameCamera()
    {
        cameraAspect = mainCamera.aspect;
        const float pitch = 60f;
        cameraRotation = Quaternion.Euler(pitch, 0f, 0f);

        Vector2 min = Vector2.positiveInfinity;
        Vector2 max = Vector2.negativeInfinity;
        foreach (BoardTile tile in art.data.land)
        {
            min = Vector2.Min(min, tile.position);
            max = Vector2.Max(max, tile.position);
        }

        Vector2 landSize = max - min;
        Vector2 landCenter = (min + max) * 0.5f;
        float halfFov = mainCamera.fieldOfView * 0.5f * Mathf.Deg2Rad;
        float fitWidth = landSize.x * 0.53f / (Mathf.Tan(halfFov) * cameraAspect);
        float fitHeight = landSize.y * 0.5f / Mathf.Tan(halfFov);
        float distance = Mathf.Max(fitWidth, fitHeight);
        // Aim slightly south of the land's center so it clears the buttons along the bottom.
        var target = new Vector3(landCenter.x, 0f, landCenter.y - landSize.y * 0.06f);
        cameraHome = target - cameraRotation * Vector3.forward * distance;
        mainCamera.transform.SetPositionAndRotation(cameraHome, cameraRotation);
    }

    void HideFlatMap()
    {
        Transform map = null;
        foreach (Canvas canvas in Object.FindObjectsByType<Canvas>(FindObjectsSortMode.None))
        {
            map = canvas.transform.Find("Map");
            if (map != null)
                break;
        }

        if (map == null)
            return;

        foreach (string layer in new[] { "BackGround", "Com", "Cap", "CapSouth", "Polar" })
        {
            Transform child = map.Find(layer);
            if (child == null)
                continue;

            foreach (Image image in child.GetComponentsInChildren<Image>(true))
            {
                if (IsUnderCenterButton(image.transform, child))
                    continue;

                // Clear rather than disabled, so taps on the map still reach these buttons.
                Color color = image.color;
                color.a = 0f;
                image.color = color;
            }
        }
    }

    static bool IsUnderCenterButton(Transform item, Transform stopAt)
    {
        for (Transform t = item; t != null && t != stopAt; t = t.parent)
        {
            if (t.name.Contains("Center"))
                return true;
        }

        return false;
    }

    Side BuildSide(bool isRed, Vector2 capitalPoint, Vector2 siloPoint, GameObject[] city)
    {
        var side = new Side { isRed = isRed, color = isRed ? HexBoard.RedTop : HexBoard.BlueTop };
        // The capital is one building, standing exactly where it was laid out by hand. It never changes;
        // power levels show in the side's factories, tanks and satellites instead.
        CapitolLayout layout = isRed ? RedCapitol : BlueCapitol;
        capitalPoint = new Vector2(layout.position.x, layout.position.z);
        side.capitalTile = board.NearestTile(capitalPoint);
        side.capital = new Vector3(layout.position.x, board.TileTop(side.capitalTile).y, layout.position.z);
        claimed.Add(side.capitalTile);

        side.capitalRoot = new GameObject(isRed ? "RedCapital" : "BlueCapital").transform;
        side.capitalRoot.SetParent(transform, false);
        side.capitalRoot.position = side.capital;

        GameObject capitol = InstantiateClean(city[0], layout.position);
        capitol.transform.rotation = Quaternion.Euler(0f, layout.rotationY, 0f);
        capitol.transform.localScale = Vector3.one * layout.scale;
        capitol.name = "City";
        capitol.transform.SetParent(side.capitalRoot, true);
        side.capitol = capitol.transform;
        Bounds capitolBounds = WorldBounds(capitol);
        side.capitalTop = capitolBounds.max.y;
        AddFootprint(capitolBounds);
        // Nothing else may be built on the tiles under the capitol.
        for (int i = 0; i < board.TileCount; i++)
        {
            Vector3 top = board.TileTop(i);
            if (top.x > capitolBounds.min.x && top.x < capitolBounds.max.x && top.z > capitolBounds.min.z && top.z < capitolBounds.max.z)
                claimed.Add(i);
        }

        BuildInfoCenter(side, capitalPoint);
        BuildFlags(side);
        return side;
    }

    // Ground footprints of everything standing near the capitals, so no building is ever placed inside another.
    readonly List<Rect> footprints = new List<Rect>();
    const float FootprintGap = 0.22f;

    void AddFootprint(Bounds bounds)
    {
        footprints.Add(Rect.MinMaxRect(bounds.min.x, bounds.min.z, bounds.max.x, bounds.max.z));
    }

    void AddFootprint(Vector3 center, Vector2 half)
    {
        footprints.Add(new Rect(center.x - half.x, center.z - half.y, half.x * 2f, half.y * 2f));
    }

    bool IsClear(Vector3 center, Vector2 half)
    {
        var rect = new Rect(center.x - half.x - FootprintGap, center.z - half.y - FootprintGap, (half.x + FootprintGap) * 2f, (half.y + FootprintGap) * 2f);
        foreach (Rect other in footprints)
        {
            if (rect.Overlaps(other))
                return false;
        }

        return true;
    }

    // True when the point is on (or right next to) the ground of a capital building, factory or info center.
    // Tanks, drones, riots and new sites never go there.
    bool OnBuildingGround(Vector3 point, float margin = 0.45f)
    {
        foreach (Rect rect in footprints)
        {
            if (point.x > rect.xMin - margin && point.x < rect.xMax + margin && point.z > rect.yMin - margin && point.z < rect.yMax + margin)
                return true;
        }

        return false;
    }

    // Moves a placed object to the free tile nearest the point where it doesn't stand inside anything else.
    int MoveToClearTile(GameObject item, Vector2 near, Func<int, bool> filter = null)
    {
        Bounds bounds = WorldBounds(item);
        var half = new Vector2(bounds.extents.x, bounds.extents.z);
        Vector3 pivotOffset = item.transform.position - bounds.center;
        int tile = ClaimNear(near, i => (filter == null || filter(i)) && IsClear(board.TileTop(i), half));
        Vector3 top = board.TileTop(tile);
        item.transform.position = new Vector3(top.x + pivotOffset.x, top.y + (item.transform.position.y - bounds.min.y), top.z + pivotOffset.z);
        AddFootprint(top, half);
        return tile;
    }

    // Holds both sides at one hidden level (1 and up), for the trailer and the Test Panel. Null in normal play.
    public static int? ForcedLevel;

    // The hidden level a side is drawn at: 1 and up, one more for every step of zeros in its power (see GameTuning).
    public static int LevelOf(Party party) => ForcedLevel ?? Tuning.LevelFor(party.Power);

    void UpdateTiers(Side side, Party party)
    {
        int level = LevelOf(party);
        float dt = WorldDelta;
        // Levels are never shown as numbers. They show in how the side's units look: factories, the info center,
        // tanks, warships and satellites each change model as the level rises.
        int mark = Mathf.Max(0, level - 1);
        if (side.mark != mark)
        {
            bool upgrade = side.mark >= 0;
            side.mark = mark;
            if (upgrade)
                UpgradeUnits(side);
        }

        float growth = Mathf.Min(Tuning.maxGrowth, 1f + Tuning.growthPerLevel * mark);
        side.shownGrowth = Mathf.MoveTowards(side.shownGrowth, growth, dt * 0.5f);
        for (int i = side.upgradable.Count - 1; i >= 0; i--)
        {
            if (side.upgradable[i] == null)
            {
                side.upgradable.RemoveAt(i);
                side.upgradableScales.RemoveAt(i);
                continue;
            }
            side.upgradable[i].localScale = side.upgradableScales[i] * side.shownGrowth;
        }
    }

    Mesh TankMesh(Side side, int mark)
    {
        int key = (side.isRed ? 4 : 0) + Mathf.Clamp(mark, 0, 3);
        if (!tankMeshes.TryGetValue(key, out Mesh mesh))
        {
            mesh = ProcMesh.Tank(side.color, mark);
            tankMeshes[key] = mesh;
        }
        return mesh;
    }

    readonly Dictionary<int, Mesh> tankMeshes = new Dictionary<int, Mesh>();

    Mesh ShipMesh(Side side, int mark)
    {
        int key = 100 + (side.isRed ? 4 : 0) + Mathf.Clamp(mark, 0, 3);
        if (!tankMeshes.TryGetValue(key, out Mesh mesh))
        {
            mesh = ProcMesh.Ship(side.color, mark);
            tankMeshes[key] = mesh;
        }
        return mesh;
    }


    // A unit that keeps its place while its model changes with the side's level.
    Transform UnitContainer(string kind, Side side, Transform parent)
    {
        var unit = new GameObject(kind).transform;
        unit.SetParent(parent, false);
        FillUnit(unit, kind, side);
        return unit;
    }

    // Puts the model for the side's current level inside the unit: the asset pack's model when one is set,
    // sized to the same footprint as the built-in shape and tinted to the side's color; otherwise the built-in shape.
    void FillUnit(Transform unit, string kind, Side side)
    {
        for (int i = unit.childCount - 1; i >= 0; i--)
            Destroy(unit.GetChild(i).gameObject);

        int mark = Mathf.Max(side.mark, 0);
        GameObject[] models = kind == "Tank" ? art.tankModels : kind == "Ship" ? art.shipModels : art.satelliteModels;
        GameObject prefab = models != null && models.Length > 0 ? models[Mathf.Min(mark, models.Length - 1)] : null;
        if (prefab == null)
        {
            Mesh mesh = kind == "Tank" ? TankMesh(side, mark) : kind == "Ship" ? ShipMesh(side, mark) : ProcMesh.Satellite(side.color, Mathf.Min(mark, 2));
            ProcMesh.Spawn(kind + "Shape", mesh, art.boardMaterial, unit);
            return;
        }

        GameObject model = Instantiate(prefab, unit, false);
        CleanModel(model);
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.identity;

        // Same length as the built-in shape, standing on the unit's origin.
        float target = kind == "Tank" ? Tuning.tankSize : kind == "Ship" ? Tuning.shipSize : Tuning.satelliteSize;
        // Models whose length runs along x are turned so they drive and sail nose first.
        Bounds raw = WorldBounds(model);
        if (kind != "Satellite" && raw.size.x > raw.size.z * 1.2f)
            model.transform.localRotation = Quaternion.Euler(0f, ModelYaw(prefab), 0f);
        Bounds bounds = WorldBounds(model);
        float size = Mathf.Max(bounds.size.x, bounds.size.z, 0.001f) / Mathf.Max(unit.lossyScale.x, 0.001f);
        model.transform.localScale *= target / size;
        bounds = WorldBounds(model);
        Vector3 lift = unit.position - new Vector3(bounds.center.x, kind == "Satellite" ? bounds.center.y : bounds.min.y, bounds.center.z);
        model.transform.position += lift;
        TintTeam(model, side.color, kind);
    }

    // Strips a pack model down to what the board shows: no physics, scripts, animation, lights or particle effects,
    // and none of the hidden helper meshes some packs ship with (collision shapes, spare wheels, landing gear).
    static readonly string[] HelperParts = { "_Collider", "IdlerWheel", "RoadWheel", "Sprocket", "SupportWheel", "_Sus_", "Idler_Arm", "TrackPiece", "Chassis", "Wheels" };

    static void CleanModel(GameObject model)
    {
        foreach (Collider collider in model.GetComponentsInChildren<Collider>(true))
            Destroy(collider);
        foreach (Rigidbody body in model.GetComponentsInChildren<Rigidbody>(true))
            Destroy(body);
        foreach (MonoBehaviour script in model.GetComponentsInChildren<MonoBehaviour>(true))
            Destroy(script);
        foreach (Animator animator in model.GetComponentsInChildren<Animator>(true))
            Destroy(animator);
        foreach (Light light in model.GetComponentsInChildren<Light>(true))
            Destroy(light);
        foreach (ParticleSystem particles in model.GetComponentsInChildren<ParticleSystem>(true))
            particles.gameObject.SetActive(false);
        foreach (Renderer part in model.GetComponentsInChildren<Renderer>(true))
        {
            foreach (string helper in HelperParts)
            {
                if (part.name.Contains(helper))
                {
                    part.enabled = false;
                    break;
                }
            }
        }
    }

    // The turn that points a sideways model's nose along the unit's forward direction.
    static float ModelYaw(GameObject prefab) => prefab.name.StartsWith("STR-MGS") ? -90f : 90f;

    // A light wash of the side's color over a model, keeping its own textures.
    // A plain team-colored material per side, for the parts of a unit that carry the side's color.
    static readonly Dictionary<Color, Material> teamPaint = new Dictionary<Color, Material>();

    static Material TeamPaint(Color color)
    {
        if (!teamPaint.TryGetValue(color, out Material paint))
        {
            paint = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            paint.SetColor("_BaseColor", Color.Lerp(color, Color.white, 0.12f));
            paint.SetFloat("_Smoothness", 0.35f);
            teamPaint[color] = paint;
        }
        return paint;
    }

    // Units keep their pack's own colors; only a part that is easy to see from above is painted in the side's color:
    // a tank's turret and gun, a warship's gun turrets, a satellite's dish.
    static void TintTeam(GameObject model, Color color, string kind)
    {
        Material paint = TeamPaint(color);
        foreach (Renderer part in model.GetComponentsInChildren<Renderer>())
        {
            Material[] materials = part.sharedMaterials;
            bool whole = kind == "Tank" && (part.name.StartsWith("Ring") || part.name.StartsWith("MainGun")
                                            || part.name.Contains("Turret") || part.name.Contains("Cannon") || part.name.Contains("Barrel"));
            for (int i = 0; i < materials.Length; i++)
            {
                if (materials[i] == null)
                    continue;
                string name = materials[i].name;
                if (whole || kind == "Ship" && name.StartsWith("Light Grey") || kind == "Satellite" && name.StartsWith("dish")
                    || kind == "Rocket" && name.StartsWith("Shuttle_Wings_Spoiler"))
                    materials[i] = paint;
            }
            part.sharedMaterials = materials;
        }
    }

    // Two warships sail, one behind the other, from the side's own coast to a spot off the enemy's coast and
    // stay there. Only over water. False when there's no enemy coast left to reach.
    bool BuildNavalVisual(Side side, ActionVisual visual)
    {
        EnsureSeaDistance();
        int destination = PickOffshore(side);
        if (destination < 0)
            return false;
        List<Vector3> route = OpenWaterRoute(destination, side.isRed ? 1 : 0, -1);
        ReserveSea(destination, true);

        var fleet = new GameObject("Fleet").transform;
        fleet.SetParent(transform, false);
        var ships = new Transform[Mathf.Max(1, Tuning.shipsPerFleet)];
        for (int i = 0; i < ships.Length; i++)
        {
            ships[i] = UnitContainer("Ship", side, fleet);
            ships[i].localScale = Vector3.one * 1.7f;
        }
        side.fleets.Add(fleet);
        var war = new Deployment { side = side, root = fleet, units = ships, naval = true, sea = destination, site = SeaPoint(destination) };
        deployments.Add(war);

        var along = new float[route.Count];
        for (int i = 1; i < route.Count; i++)
            along[i] = along[i - 1] + (Flat(route[i]) - Flat(route[i - 1])).magnitude;
        float length = Mathf.Max(0.001f, along[route.Count - 1]);
        float Gap = Tuning.shipGap;

        (Vector3, Vector3) OnRoute(float d)
        {
            d = Mathf.Clamp(d, 0f, length);
            int i = 1;
            while (i < route.Count - 1 && along[i] < d)
                i++;
            float span = Mathf.Max(0.001f, along[i] - along[i - 1]);
            Vector3 heading = route[i] - route[i - 1];
            heading.y = 0f;
            return (Vector3.Lerp(route[i - 1], route[i], Mathf.Clamp01((d - along[i - 1]) / span)), heading);
        }

        void Place(float t, bool snap)
        {
            float lead = Mathf.Clamp01(t) * (length + Gap * (ships.Length - 1));
            fleet.position = OnRoute(lead).Item1;
            for (int i = 0; i < ships.Length; i++)
            {
                (Vector3 position, Vector3 heading) = OnRoute(lead - Gap * i);
                // A gentle roll and bob on the water.
                position.y = 0.01f + Mathf.Sin(WorldTime * 1.6f + i) * 0.015f;
                ships[i].position = position;
                if (heading.sqrMagnitude > 0.0001f)
                {
                    Quaternion facing = Quaternion.LookRotation(heading.normalized) * Quaternion.Euler(0f, 0f, Mathf.Sin(WorldTime * 1.3f + i) * 3f);
                    ships[i].rotation = snap ? facing : Quaternion.Slerp(ships[i].rotation, facing, WorldDelta * 4f);
                }
            }
        }

        Place(0f, true);
        fx.Burst(route[0] + Vector3.up * 0.1f, new Color(0.8f, 0.9f, 1f), 14, 0.18f, 1.2f, 0.6f, false);
        float shown = 0f;
        visual.onProgress = progress =>
        {
            shown = Mathf.MoveTowards(shown, progress, WorldDelta * 0.8f);
            Place(shown, false);
        };
        visual.onDone = () =>
        {
            Place(1f, true);
            // Parked along the coast, not pointing into it.
            ships[0].rotation = AlongCoast(destination);
            war.ready = true;
            Vector3 target = route[route.Count - 1];
            warSites.Add(target);
            fx.Explosion(target + Vector3.up * 0.2f, 1f);
            StartCoroutine(ExpandingRing(target + Vector3.up * 0.05f, side.color, 2.4f));
            shake = Mathf.Max(shake, 0.1f);
        };
        visual.onCancel = () =>
        {
            ReserveSea(destination, false);
            deployments.Remove(war);
            side.fleets.Remove(fleet);
            Destroy(fleet.gameObject);
        };
        return true;
    }

    // ----- Sea routes for ships -----
    Dictionary<Vector2Int, int> seaAt;

    Vector3 SeaPoint(int index) => new Vector3(art.data.sea[index].x, 0f, art.data.sea[index].y);

    void EnsureSeaGrid()
    {
        if (seaAt != null)
            return;
        foreach (int _ in LandNeighbours(0)) { }   // builds the land grid and the hex steps
        seaAt = new Dictionary<Vector2Int, int>();
        for (int i = 0; i < art.data.sea.Length; i++)
        {
            Vector3 point = SeaPoint(i);
            Vector2Int key = HexKey(point);
            // The nuke meter sits in open sea: its water is closed to ships, so fleets sail round it.
            Vector3 fromPool = point - poolCenter;
            fromPool.y = 0f;
            if (fromPool.magnitude < PoolRadius + 0.35f + Tuning.nukeMeterClearance)
                continue;
            if (!tileAt.ContainsKey(key))
                seaAt[key] = i;
        }
    }

    IEnumerable<Vector2Int> AroundKey(Vector3 p)
    {
        foreach (var step in new[] { new Vector2(hexStepX, 0f), new Vector2(-hexStepX, 0f), new Vector2(hexStepX * 0.5f, hexStepZ), new Vector2(-hexStepX * 0.5f, hexStepZ), new Vector2(hexStepX * 0.5f, -hexStepZ), new Vector2(-hexStepX * 0.5f, -hexStepZ) })
            yield return HexKey(p + new Vector3(step.x, 0f, step.y));
    }

    // Which side holds the land next to this sea tile: 1 = red, 0 = blue, -1 = no land beside it.
    int CoastOwner(int sea)
    {
        foreach (Vector2Int key in AroundKey(SeaPoint(sea)))
        {
            if (tileAt.TryGetValue(key, out int land))
                return board.ThresholdOf(land) < board.Com ? 1 : 0;
        }
        return -1;
    }

    // Factory models from small to large, two per mark.
    static readonly int[][] FactoryByMark = { new[] { 5, 6 }, new[] { 4, 3 }, new[] { 2, 2 }, new[] { 0, 1 } };
    static readonly float[] FactoryFootprint = { 1.5f, 1.75f, 2.0f, 2.3f };
    static readonly float[] FactoryHeight = { 1.3f, 1.55f, 1.8f, 2.1f };

    GameObject NewFactory(Side side, int tile)
    {
        int mark = Mathf.Clamp(side.mark, 0, 3);
        int[] options = FactoryByMark[mark];
        int index = Mathf.Min(options[UnityEngine.Random.Range(0, options.Length)], art.factories.Length - 1);
        return PlacePrefab(art.factories[index], tile, FactoryFootprint[mark], FactoryHeight[mark]);
    }

    // The side reached a new mark: its standing factories are rebuilt, and its tanks and satellites refitted, in place.
    void UpgradeUnits(Side side)
    {
        Color flash = side.color + Color.white * 0.35f;
        foreach (Transform column in side.tankColumns)
        {
            if (column == null)
                continue;
            foreach (Transform tank in column)
                FillUnit(tank, "Tank", side);
            fx.Burst(column.position + Vector3.up * 0.3f, flash, 10, 0.16f, 1.2f, 0.5f);
        }

        foreach (Transform fleet in side.fleets)
        {
            if (fleet == null)
                continue;
            foreach (Transform ship in fleet)
                FillUnit(ship, "Ship", side);
            fx.Burst(fleet.position + Vector3.up * 0.3f, flash, 10, 0.16f, 1.2f, 0.5f);
        }

        if (side.infoBuilding != null)
        {
            side.infoBuilding.sharedMesh = InfoCenterMesh(side, side.mark);
            side.infoSign.localPosition = Vector3.up * InfoSignHeight(side.mark);
            fx.Burst(side.infoCenter.position + Vector3.up * 0.6f, flash, 12, 0.2f, 1.6f, 0.6f);
        }

        foreach (Transform satellite in side.satellites3D)
        {
            if (satellite != null)
                FillUnit(satellite, "Satellite", side);
        }

        foreach (FactorySpot spot in side.factorySpots)
        {
            if (spot.factory == null)
                continue;
            int at = side.upgradable.IndexOf(spot.factory.transform);
            Destroy(spot.factory);
            spot.factory = NewFactory(side, spot.tile);
            if (at >= 0)
            {
                side.upgradable[at] = spot.factory.transform;
                side.upgradableScales[at] = spot.factory.transform.localScale;
            }
            Vector3 top = board.TileTop(spot.tile);
            fx.Burst(top + Vector3.up * 0.4f, new Color(0.75f, 0.72f, 0.66f), 18, 0.24f, 1.6f, 0.8f, false);
            fx.Burst(top + Vector3.up * 0.6f, flash, 12, 0.2f, 1.6f, 0.6f);
        }
    }

    // A tile deep inside the enemy's half of the map, so each side's tanks stand out against the other's color.
    // A small radio station beside the capital, with a floating "i" sign. Tapping it opens the country report.
    void BuildInfoCenter(Side side, Vector2 capitalPoint)
    {
        // A few tiles toward the camera and away from the coast, so the capital never hides it.
        Vector2 spot = capitalPoint + new Vector2(side.isRed ? 2.4f : -2.4f, -2.6f);
        var half = new Vector2(0.72f, 0.62f);
        int tile = ClaimNear(spot, i => IsClear(board.TileTop(i), half));
        var root = new GameObject("InfoCenter").transform;
        root.SetParent(transform, false);
        root.position = board.TileTop(tile);
        AddFootprint(root.position, half);

        Color gold = new Color(0.95f, 0.72f, 0.22f);
        side.infoBuilding = ProcMesh.Spawn("Building", InfoCenterMesh(side, 0), art.boardMaterial, root).GetComponent<MeshFilter>();

        var sign = new GameObject("InfoSign").transform;
        sign.SetParent(root, false);
        sign.localPosition = Vector3.up * InfoSignHeight(0);
        GameObject disc = ProcMesh.Spawn("Disc", ProcMesh.Token(gold, new Color(0.13f, 0.17f, 0.29f)), art.boardMaterial, sign);
        disc.transform.localScale = Vector3.one * 0.85f;

        var letter = new GameObject("Letter").AddComponent<TextMeshPro>();
        letter.transform.SetParent(sign, false);
        letter.transform.localPosition = new Vector3(0f, 0.16f, 0f);
        letter.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        letter.font = TMP_Settings.defaultFontAsset;
        letter.text = "i";
        letter.fontSize = 13f;
        letter.fontStyle = FontStyles.Bold;
        letter.color = gold;
        letter.alignment = TextAlignmentOptions.Center;
        letter.rectTransform.sizeDelta = new Vector2(1.2f, 1.2f);

        side.infoCenter = root;
        side.infoSign = sign;
    }

    // The "i" sign floats well clear above the mast's light at every level.
    static float InfoSignHeight(int mark) => 0.95f + (mark >= 1 ? 0.6f : 0f) + (mark >= 3 ? 2.0f : 1.35f) + 1.05f;

    // The info center grows with the side's hidden level: a second floor, then a radar dish, then a taller mast.
    Mesh InfoCenterMesh(Side side, int mark)
    {
        Color wall = Color.Lerp(side.color, Color.white, 0.6f);
        Color roof = side.color * 0.55f;
        roof.a = 1f;
        Color steel = new Color(0.3f, 0.32f, 0.36f);
        Color white = new Color(0.88f, 0.89f, 0.92f);
        float lift = mark >= 1 ? 0.6f : 0f;
        var m = new ProcMesh()
            .Box(new Vector3(0f, 0.42f, 0f), new Vector3(1.25f, 0.84f, 1.05f), wall);
        if (mark >= 1)
            m.Box(new Vector3(-0.1f, 1.14f, 0f), new Vector3(1.0f, 0.6f, 0.9f), wall)
             .Box(new Vector3(-0.1f, 1.08f, 0.46f), new Vector3(0.8f, 0.12f, 0.02f), ProcMesh.Glow(side.color, 0.7f));
        m.Box(new Vector3(0f, 0.9f + lift, 0f), new Vector3(1.38f, 0.12f, 1.18f), roof)
         .Frustum(new Vector3(-0.28f, 0.96f + lift, 0f), 0.34f, 0.1f, 0.34f, 10, wall);
        float mast = mark >= 3 ? 2.0f : 1.35f;
        m.Box(new Vector3(0.5f, 0.95f + lift + mast * 0.5f, 0.38f), new Vector3(0.08f, mast, 0.08f), steel)
         .Box(new Vector3(0.5f, 0.95f + lift + mast, 0.38f), new Vector3(0.16f, 0.16f, 0.16f), ProcMesh.Glow(new Color(1f, 0.3f, 0.2f), 0.9f));
        if (mark >= 2)
            m.Frustum(new Vector3(-0.28f, 1.3f + lift, 0f), 0.04f, 0.32f, 0.16f, 12, white);
        return m.ToMesh("InfoCenter");
    }

    // One flag for every 20 points of happiness, in a "Flags" folder under the capitol. The spots were placed by hand
    // on each capitol and are kept in its own space; the first one is the first to fly and the last to go.
    void BuildFlags(Side side)
    {
        FlagSpot[] spots = side.isRed ? RedFlags : BlueFlags;
        Mesh mesh = side.isRed ? ProcMesh.Banner(side.color) : ProcMesh.Flag(side.color);
        var folder = new GameObject("Flags").transform;
        folder.SetParent(side.capitol, false);
        side.flags = new GameObject[spots.Length];
        for (int i = 0; i < spots.Length; i++)
        {
            GameObject flag = ProcMesh.Spawn("Flag " + (i + 1), mesh, art.boardMaterial, folder);
            flag.transform.localPosition = spots[i].position;
            flag.transform.localRotation = Quaternion.Euler(spots[i].rotation);
            flag.transform.localScale = Vector3.one * spots[i].scale;
            side.flags[i] = flag;
        }
    }

    struct FlagSpot
    {
        public Vector3 position;
        public Vector3 rotation;
        public float scale;

        public FlagSpot(Vector3 position, Vector3 rotation, float scale)
        {
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
        }
    }

    struct CapitolLayout
    {
        public Vector3 position;
        public float rotationY;
        public float scale;

        public CapitolLayout(Vector3 position, float rotationY, float scale)
        {
            this.position = position;
            this.rotationY = rotationY;
            this.scale = scale;
        }
    }

    // Placed by hand in the editor (Nov 2026): where each capitol stands, its size, and its five flags in the order
    // they appear. Flag spots are in the capitol's own space.
    static readonly CapitolLayout BlueCapitol = new CapitolLayout(new Vector3(-12.93338f, 0.77462f, 3.18889f), 120f, 0.10069f);
    static readonly CapitolLayout RedCapitol = new CapitolLayout(new Vector3(6.58f, 0.2033f, 4.41f), 235.961f, 0.1064f);

    static readonly FlagSpot[] BlueFlags =
    {
        new FlagSpot(new Vector3(-6.00729f, 55.92009f, 4.82325f), new Vector3(0f, 270f, 329.9745f), 23.05452f),
        new FlagSpot(new Vector3(5.82351f, 55.92009f, -6.4317f), new Vector3(0f, 45f, 329.9745f), 23.05453f),
        new FlagSpot(new Vector3(-5.12283f, 71.27467f, 0.96448f), new Vector3(0f, 255f, 329.9745f), 23.05452f),
        new FlagSpot(new Vector3(3.69722f, 74.29396f, -3.10641f), new Vector3(0f, 45f, 329.9745f), 23.05453f),
        new FlagSpot(new Vector3(-0.01143f, 90.08553f, -0.4768f), new Vector3(7.43546f, 46.93568f, 359.1219f), 23.05453f),
    };

    static readonly FlagSpot[] RedFlags =
    {
        new FlagSpot(new Vector3(2.60902f, 60.21107f, 7.16128f), new Vector3(0f, 0f, 0f), 28.42092f),
        new FlagSpot(new Vector3(-6.95462f, 60.21107f, -0.18797f), new Vector3(0f, 270f, 0f), 28.42092f),
        new FlagSpot(new Vector3(-4.7206f, 60.21107f, 7.16128f), new Vector3(0f, 0f, 0f), 28.42092f),
        new FlagSpot(new Vector3(-0.56389f, 82.88879f, 6.19337f), new Vector3(0f, 0f, 0f), 21.38703f),
        new FlagSpot(new Vector3(-5.98662f, 83.28352f, -0.17857f), new Vector3(0f, 268.6226f, 0f), 21.38703f),
    };

    // The map has a gap in the land where the red capitol stands. A copy of the board data gets land tiles added
    // in any gap under either capitol, so neither stands over the sea.
    static BoardData WithCapitalGround(BoardData data)
    {
        var land = new List<BoardTile>(data.land);
        float dx = data.hexRadius * Mathf.Sqrt(3f);
        float dz = data.hexRadius * 1.5f;
        foreach (CapitolLayout layout in new[] { BlueCapitol, RedCapitol })
        {
            var center = new Vector2(layout.position.x, layout.position.z);
            int nearest = 0;
            for (int i = 1; i < land.Count; i++)
            {
                if ((land[i].position - center).sqrMagnitude < (land[nearest].position - center).sqrMagnitude)
                    nearest = i;
            }

            BoardTile anchor = land[nearest];
            // Every spot on the hex grid within reach of the capitol's base.
            for (int row = -3; row <= 3; row++)
            {
                for (int col = -3; col <= 3; col++)
                {
                    var spot = anchor.position + new Vector2(col * dx + ((row & 1) != 0 ? dx * 0.5f : 0f), row * dz);
                    if ((spot - center).magnitude > 1.45f)
                        continue;
                    bool taken = false;
                    foreach (BoardTile tile in land)
                    {
                        if ((tile.position - spot).sqrMagnitude < dx * dx * 0.2f)
                        {
                            taken = true;
                            break;
                        }
                    }

                    if (!taken)
                        land.Add(new BoardTile { position = spot, redThreshold = anchor.redThreshold, elevation = anchor.elevation });
                }
            }
        }

        // Also close every single-tile hole in the land: a free spot with land on at least five of its six sides.
        var occupied = new HashSet<Vector2Int>();
        Vector2Int Key(Vector2 point) => new Vector2Int(Mathf.RoundToInt(point.x / (dx * 0.5f)), Mathf.RoundToInt(point.y / dz));
        foreach (BoardTile tile in land)
            occupied.Add(Key(tile.position));
        var around = new[] { new Vector2(dx, 0f), new Vector2(-dx, 0f), new Vector2(dx * 0.5f, dz), new Vector2(-dx * 0.5f, dz), new Vector2(dx * 0.5f, -dz), new Vector2(-dx * 0.5f, -dz) };
        int existing = land.Count;
        for (int i = 0; i < existing; i++)
        {
            foreach (Vector2 step in around)
            {
                Vector2 spot = land[i].position + step;
                if (occupied.Contains(Key(spot)))
                    continue;
                int neighbours = 0;
                foreach (Vector2 next in around)
                {
                    if (occupied.Contains(Key(spot + next)))
                        neighbours++;
                }
                if (neighbours < 5)
                    continue;
                occupied.Add(Key(spot));
                land.Add(new BoardTile { position = spot, redThreshold = land[i].redThreshold, elevation = land[i].elevation });
            }
        }

        if (land.Count == data.land.Length)
            return data;
        BoardData copy = Object.Instantiate(data);
        copy.land = land.ToArray();
        return copy;
    }

    TextMeshPro poolLabel;
    Vector3 poolCenter;
    public Vector3 PoolTop => poolCenter + Vector3.up * 0.9f;

    // A round pool in the South Atlantic, split down the middle. Each side's half fills with nukes as danger rises.
    void BuildNukePool()
    {
        Vector2 size = art.data.boardSize;
        poolCenter = new Vector3((PoolSpot.x - 0.5f) * size.x, 0f, (0.5f - PoolSpot.y) * size.y);

        var pool = new GameObject("NukePool").transform;
        pool.SetParent(transform, false);
        pool.position = poolCenter;

        Color steel = new Color(0.2f, 0.22f, 0.26f);
        Color darkSteel = new Color(0.1f, 0.11f, 0.13f);
        var mesh = new ProcMesh()
            .Frustum(Vector3.zero, PoolRadius + 0.35f, PoolRadius + 0.25f, 0.55f, 24, steel, false)
            .Frustum(new Vector3(0f, 0.55f, 0f), PoolRadius + 0.25f, PoolRadius + 0.25f, 0.001f, 24, darkSteel, false)
            .Box(new Vector3(0f, 0.32f, 0f), new Vector3(0.22f, 0.5f, PoolRadius * 2f), steel);
        ProcMesh.Spawn("Rim", mesh.ToMesh("PoolRim"), art.boardMaterial, pool);
        AddPoolHalf(pool, false);
        AddPoolHalf(pool, true);

        PlaceNukes(blue, -1f);
        PlaceNukes(red, 1f);

        poolLabel = new GameObject("DangerLabel").AddComponent<TextMeshPro>();
        poolLabel.transform.SetParent(pool, false);
        poolLabel.transform.position = poolCenter + new Vector3(0f, 0.7f, -PoolRadius - 0.9f);
        poolLabel.transform.rotation = cameraRotation;
        poolLabel.font = TMP_Settings.defaultFontAsset;
        poolLabel.fontSize = 9f;
        poolLabel.fontStyle = FontStyles.Bold;
        poolLabel.alignment = TextAlignmentOptions.Center;
        poolLabel.rectTransform.sizeDelta = new Vector2(6f, 1.6f);
        poolLabel.outlineWidth = 0.2f;
        poolLabel.outlineColor = new Color32(0, 0, 0, 220);
    }

    void AddPoolHalf(Transform pool, bool isRed)
    {
        Color water = (isRed ? HexBoard.RedTop : HexBoard.BlueTop) * 0.35f;
        water.a = 1f;
        var half = new ProcMesh();
        const int segments = 14;
        for (int i = 0; i < segments; i++)
        {
            float a0 = Mathf.PI * (0.5f + (isRed ? -1f : 1f) * i / segments);
            float a1 = Mathf.PI * (0.5f + (isRed ? -1f : 1f) * (i + 1) / segments);
            Vector3 p0 = new Vector3(Mathf.Cos(a0), 0f, Mathf.Sin(a0)) * PoolRadius + Vector3.up * 0.3f;
            Vector3 p1 = new Vector3(Mathf.Cos(a1), 0f, Mathf.Sin(a1)) * PoolRadius + Vector3.up * 0.3f;
            Vector3 middle = Vector3.up * 0.3f;
            if (isRed)
                half.Tri(middle, p0, p1, ProcMesh.Glow(water, 0.2f));
            else
                half.Tri(middle, p1, p0, ProcMesh.Glow(water, 0.2f));
        }

        ProcMesh.Spawn(isRed ? "RedWater" : "BlueWater", half.ToMesh("PoolWater"), art.boardMaterial, pool);
    }

    // Fills the half nearest the middle line first, so the two sides read as a matching pair.
    void PlaceNukes(Side side, float direction)
    {
        var spots = new List<Vector3>();
        const float spacing = 0.66f;
        for (float x = 0.5f; x < PoolRadius - 0.3f; x += spacing)
        {
            for (float z = -PoolRadius; z <= PoolRadius; z += spacing)
            {
                if (new Vector2(x, z).magnitude < PoolRadius - 0.45f)
                    spots.Add(new Vector3(x * direction, 0.3f, z));
            }
        }

        spots.Sort((a, b) => (Mathf.Abs(a.x) + Mathf.Abs(a.z) * 0.6f).CompareTo(Mathf.Abs(b.x) + Mathf.Abs(b.z) * 0.6f));
        for (int i = 0; i < PoolCapacity && i < spots.Count; i++)
        {
            Vector3 home = poolCenter + spots[i];
            Transform missile = NukeModel("Nuke", side.color);
            missile.position = home;
            missile.localScale = Vector3.one * 1.55f;
            side.missiles.Add(missile);
            side.missileHomes.Add(missile.position);
            side.missileScales.Add(missile.localScale);
            missile.localScale = Vector3.zero;
        }
    }


    // A nuclear missile from the missile pack, standing nose up on the transform's origin, the same size as the old
    // built-in shape (so the pool, launches and strikes keep their scales). Falls back to that shape.
    static float nukeHeight = -1f, nukeBase;

    Transform NukeModel(string name, Color color)
    {
        if (nukeHeight < 0f)
        {
            Bounds old = ProcMesh.Rocket(color).bounds;
            nukeHeight = old.size.y;
            nukeBase = old.min.y;
        }
        if (art.nukeMissile == null)
            return ProcMesh.Spawn(name, ProcMesh.Rocket(color), art.boardMaterial, transform).transform;

        var root = new GameObject(name).transform;
        root.SetParent(transform, false);
        GameObject model = Instantiate(art.nukeMissile, root, false);
        CleanModel(model);
        model.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);   // the missiles in the pack lie along +z
        Bounds bounds = WorldBounds(model);
        model.transform.localScale *= nukeHeight / Mathf.Max(0.001f, bounds.size.y);
        bounds = WorldBounds(model);
        model.transform.position += root.position + Vector3.up * nukeBase - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        return root;
    }

    GameObject InstantiateClean(GameObject prefab, Vector3 position)
    {
        GameObject instance = Instantiate(prefab, position, Quaternion.identity, transform);
        foreach (Collider collider in instance.GetComponentsInChildren<Collider>(true))
            Destroy(collider);
        return instance;
    }

    // Scales a prefab to a target height and rotates its longest axis to point up, with its pivot at the base.
    void StandUpright(GameObject go, float targetHeight)
    {
        Bounds bounds = WorldBounds(go);
        Vector3 size = bounds.size;
        if (size.x > size.y && size.x >= size.z)
            go.transform.rotation = Quaternion.Euler(0f, 0f, 90f) * go.transform.rotation;
        else if (size.z > size.y && size.z >= size.x)
            go.transform.rotation = Quaternion.Euler(-90f, 0f, 0f) * go.transform.rotation;

        bounds = WorldBounds(go);
        if (bounds.size.y > 0.001f)
            go.transform.localScale *= targetHeight / bounds.size.y;

        bounds = WorldBounds(go);
        go.transform.position += new Vector3(go.transform.position.x - bounds.center.x, go.transform.position.y - bounds.min.y, go.transform.position.z - bounds.center.z);
    }

    static Bounds WorldBounds(GameObject go)
    {
        // Only what is drawn counts: hidden helper meshes and particle effects are left out.
        bool any = false;
        Bounds bounds = new Bounds(go.transform.position, Vector3.zero);
        foreach (Renderer item in go.GetComponentsInChildren<Renderer>())
        {
            if (!item.enabled || item is ParticleSystemRenderer)
                continue;
            if (!any) { bounds = item.bounds; any = true; }
            else bounds.Encapsulate(item.bounds);
        }
        return bounds;
    }

    GameObject PlacePrefab(GameObject prefab, int tile, float footprintScale, float maxHeight, float? rotationY = null)
    {
        Vector3 top = board.TileTop(tile);
        GameObject instance = InstantiateClean(prefab, top);
        instance.transform.rotation = Quaternion.Euler(0f, rotationY ?? 60f * UnityEngine.Random.Range(0, 6), 0f);

        Bounds bounds = WorldBounds(instance);
        float footprint = Mathf.Max(bounds.size.x, bounds.size.z);
        if (footprint > 0.001f)
        {
            float scale = TileWidth * footprintScale / footprint;
            if (bounds.size.y * scale > maxHeight)
                scale = maxHeight / bounds.size.y;
            instance.transform.localScale *= scale;
        }

        bounds = WorldBounds(instance);
        instance.transform.position += new Vector3(top.x - bounds.center.x, top.y - bounds.min.y, top.z - bounds.center.z);
        return instance;
    }

    int ClaimNear(Vector2 point, Func<int, bool> filter)
    {
        int best = -1;
        float bestDistance = float.MaxValue;
        for (int i = 0; i < board.TileCount; i++)
        {
            if (claimed.Contains(i) || !filter(i))
                continue;

            Vector3 top = board.TileTop(i);
            if (OnBuildingGround(top, 0f))
                continue;
            float distance = (new Vector2(top.x, top.z) - point).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = i;
            }
        }

        if (best < 0)
        {
            // Nothing fits the wish: the nearest free tile anywhere (the map has room for everything).
            for (int i = 0; i < board.TileCount; i++)
            {
                if (claimed.Contains(i) || OnBuildingGround(board.TileTop(i), 0f))
                    continue;
                float distance = (Flat(board.TileTop(i)) - point).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }
        }
        if (best < 0)
            best = board.NearestTile(point);
        claimed.Add(best);
        return best;
    }

    int ClaimFront()
    {
        int best = -1;
        float bestScore = float.MaxValue;
        for (int i = 0; i < board.TileCount; i++)
        {
            if (claimed.Contains(i) || OnBuildingGround(board.TileTop(i), 1.1f))
                continue;

            float score = Mathf.Abs(board.ThresholdOf(i) - board.Com) + UnityEngine.Random.Range(0f, 0.03f);
            if (score < bestScore)
            {
                bestScore = score;
                best = i;
            }
        }

        if (best < 0)
            best = 0;
        claimed.Add(best);
        return best;
    }

    // ----- Land routes for tanks -----
    Dictionary<Vector2Int, int> tileAt;
    float hexStepX, hexStepZ;

    Vector2Int HexKey(Vector3 point) => new Vector2Int(Mathf.RoundToInt(point.x / (hexStepX * 0.5f)), Mathf.RoundToInt(point.z / hexStepZ));

    IEnumerable<int> LandNeighbours(int tile)
    {
        if (tileAt == null)
        {
            hexStepX = art.data.hexRadius * Mathf.Sqrt(3f);
            hexStepZ = art.data.hexRadius * 1.5f;
            tileAt = new Dictionary<Vector2Int, int>();
            for (int i = 0; i < board.TileCount; i++)
                tileAt[HexKey(board.TileTop(i))] = i;
        }

        Vector3 p = board.TileTop(tile);
        foreach (var step in new[] { new Vector2(hexStepX, 0f), new Vector2(-hexStepX, 0f), new Vector2(hexStepX * 0.5f, hexStepZ), new Vector2(-hexStepX * 0.5f, hexStepZ), new Vector2(hexStepX * 0.5f, -hexStepZ), new Vector2(-hexStepX * 0.5f, -hexStepZ) })
        {
            if (tileAt.TryGetValue(HexKey(p + new Vector3(step.x, 0f, step.y)), out int next))
                yield return next;
        }
    }

    // The tiles a column drives over to reach the target: from the nearest tile its own side holds on the same
    // landmass, or, when the whole landmass is the enemy's, from a staging point a few tiles back. Never over water,
    // never through a city.
    List<Vector3> LandRoute(Side side, int target)
    {
        var cameFrom = new Dictionary<int, int> { { target, -1 } };
        var queue = new Queue<int>();
        queue.Enqueue(target);
        int start = -1, staging = target, stagingSteps = 0;
        var steps = new Dictionary<int, int> { { target, 0 } };
        while (queue.Count > 0 && start < 0)
        {
            int current = queue.Dequeue();
            foreach (int next in LandNeighbours(current))
            {
                if (cameFrom.ContainsKey(next) || OnBuildingGround(board.TileTop(next), 0.3f))
                    continue;
                cameFrom[next] = current;
                steps[next] = steps[current] + 1;
                bool ours = side.isRed ? board.ThresholdOf(next) < board.Com : board.ThresholdOf(next) >= board.Com;
                if (ours)
                {
                    start = next;
                    break;
                }
                if (steps[next] <= 7 && steps[next] > stagingSteps)
                {
                    staging = next;
                    stagingSteps = steps[next];
                }
                queue.Enqueue(next);
            }
        }

        if (start < 0)
            start = staging;
        var route = new List<Vector3>();
        for (int t = start; t >= 0; t = cameFrom[t])
            route.Add(board.TileTop(t));
        if (route.Count == 1)
            route.Add(board.TileTop(target));
        return route;
    }

    static Vector2 Flat(Vector3 point)
    {
        return new Vector2(point.x, point.z);
    }

    // ----- Live state -----

    void Update()
    {
        WorldTime += WorldDelta;
        FreezeParticles(Frozen);
        if (Mathf.Abs(mainCamera.aspect - cameraAspect) > 0.001f)
            FrameCamera();

        fx.Tick();
        UpdateOrbiters();
        UpdateRoamers();
        UpdateUnits();
        UpdateCamera();
        BobInfoSign(blue);
        BobInfoSign(red);

        if (IsMenu)
        {
            board.SetCom(0.5f + 0.2f * Mathf.Sin(WorldTime * 0.12f));
            return;
        }

        board.SetCom(GameManager.Com);
        UpdateSide(blue, country.Capitalism);
        UpdateSide(red, country.Communism);
        UpdateDanger();
        UpdateWarSites();
        UpdatePeace();
        if (country.IsTutorial)
            UpdateTutorialFocus();
    }

    void BobInfoSign(Side side)
    {
        if (side.infoSign == null)
            return;
        side.infoSign.localPosition = Vector3.up * (2.9f + Mathf.Sin(WorldTime * 2.2f + (side.isRed ? 1f : 0f)) * 0.18f);
        // The sign's face turns toward the camera so it reads as a badge, not a flat disc.
        side.infoSign.rotation = cameraRotation * Quaternion.Euler(-90f, 0f, 0f);
    }

    // Surviving to 1991: the silos close and fireworks rise over both capitals.
    void UpdatePeace()
    {
        if (GameManager.Singleton == null || !GameManager.Singleton.HasWon)
            return;

        peaceStarted = true;
        if (WorldTime < nextFirework)
            return;

        nextFirework = WorldTime + UnityEngine.Random.Range(0.25f, 0.6f);
        Side side = UnityEngine.Random.value < 0.5f ? blue : red;
        Vector3 spot = side.capital + new Vector3(UnityEngine.Random.Range(-5f, 5f), UnityEngine.Random.Range(3f, 7f), UnityEngine.Random.Range(-3f, 4f));
        Color color = Color.HSVToRGB(UnityEngine.Random.value, 0.65f, 1f);
        fx.Burst(spot, color, 30, 0.3f, 3.4f, 1.1f);
    }

    // Pulses a ring on whatever the current tutorial caption is talking about.
    void UpdateTutorialFocus()
    {
        if (WorldTime < nextFocusPulse)
            return;

        nextFocusPulse = WorldTime + 1.1f;
        Color color = new Color(1f, 0.92f, 0.35f);
        switch (Tutorial.Level)
        {
            case 5:
                StartCoroutine(ExpandingRing(blue.capital + Vector3.up * 0.2f, color, 3.2f));
                StartCoroutine(ExpandingRing(red.capital + Vector3.up * 0.2f, color, 3.2f));
                break;
            case 11:
                StartCoroutine(ExpandingRing(red.capital + Vector3.up * 0.2f, color, 3.2f));
                break;
            case 12:
                StartCoroutine(ExpandingRing(poolCenter + Vector3.up * 0.6f, new Color(1f, 0.3f, 0.2f), PoolRadius + 1.5f));
                break;
        }
    }

    void UpdateSide(Side side, Party party)
    {
        if (party == null)
            return;

        float dt = WorldDelta;


        UpdateTiers(side, party);

        int flagsUp = Mathf.CeilToInt(Mathf.Clamp(ShownHappiness(side, party), 0, 100) / 20f);
        for (int i = 0; i < side.flags.Length; i++)
        {
            bool shouldShow = i < flagsUp;
            if (side.flags[i].activeSelf != shouldShow)
                side.flags[i].SetActive(shouldShow);
        }

        UpdateProtest(side, party);

        if (party.president != null && party.president != side.president)
        {
            if (side.president != null)
                OnPresidentChanged(side);
            side.president = party.president;
        }

        SyncActions(side, party);
    }


    // Fires break out at street level all around the capital when its people turn on it. Unhappier means more fires.
    void UpdateProtest(Side side, Party party)
    {
        int happiness = ShownHappiness(side, party);
        int wanted = happiness < UnrestBelow ? 4 + (UnrestBelow - happiness) / 5 : 0;
        while (side.fires.Count > wanted)
        {
            int last = side.fires.Count - 1;
            Destroy(side.fires[last].transform.parent.gameObject);
            side.fires.RemoveAt(last);
        }

        while (side.fires.Count < wanted)
        {
            int index = side.fires.Count;
            float angle = index * 2.39996f;
            float radius = 2f + (index % 3) * 0.5f;
            Vector2 spot = Flat(side.capital) + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
            Vector3 ground = board.TileTop(board.NearestTile(spot)) + Vector3.up * 0.05f;

            var site = new GameObject("Riot").transform;
            site.SetParent(transform, false);
            site.position = ground;
            ParticleSystem flame = fx.Flame(ground, site, 32f, 1.35f);
            fx.Smoke(ground + Vector3.up * 0.5f, site, 6f, 1.1f, new Color(0.08f, 0.07f, 0.07f, 0.75f));
            side.fires.Add(flame);
        }
    }

    void OnPresidentChanged(Side side)
    {
        Vector3 spot = side.capital + Vector3.up * 1.5f;
        fx.Burst(spot, new Color(1f, 0.82f, 0.25f), 30, 0.28f, 3.2f, 0.9f);
        fx.Burst(spot, Color.white, 8, 0.8f, 0.6f, 0.5f);
        StartCoroutine(ExpandingRing(side.capital + Vector3.up * 0.2f, new Color(1f, 0.82f, 0.25f), 5f));
    }

    void SyncActions(Side side, Party party)
    {
        seen.Clear();
        foreach (ActionFunction action in party.Actions)
        {
            if (action == null)
                continue;

            seen.Add(action);
            if (!side.actions.TryGetValue(action, out ActionVisual visual))
            {
                visual = CreateActionVisual(side, action);
                side.actions[action] = visual;
            }

            float total = Mathf.Max(visual.totalMonths, 1f);
            visual.onProgress?.Invoke(Mathf.Clamp01(1f - action.time / total));
        }

        List<ActionFunction> finished = null;
        foreach (ActionFunction action in side.actions.Keys)
        {
            if (!seen.Contains(action))
                (finished ??= new List<ActionFunction>()).Add(action);
        }

        if (finished == null)
            return;

        foreach (ActionFunction action in finished)
        {
            ActionVisual visual = side.actions[action];
            side.actions.Remove(action);
            // Assassinations resolve early by design. Other actions only count if their timer ran out.
            if (action.time <= 0 || action.name == "Assassin")
                visual.onDone?.Invoke();
            else
                visual.onCancel?.Invoke();
        }
    }

    ActionVisual CreateActionVisual(Side side, ActionFunction action)
    {
        var visual = new ActionVisual { totalMonths = action.time };
        switch (action.name)
        {
            case "Factory": BuildFactoryVisual(side, visual); break;
            case "War": BuildWarVisual(side, visual); break;
            case "Spy": BuildSpyVisual(side, visual); break;
            case "Research": BuildResearchVisual(side, visual); break;
            case "Assassin": BuildAssassinVisual(side, visual); break;
        }

        return visual;
    }

    Side Enemy(Side side)
    {
        return side == blue ? red : blue;
    }

    void BuildFactoryVisual(Side side, ActionVisual visual)
    {
        if (AtCap(side.factories, Tuning.maxFactoriesShown) || art.factories.Length == 0)
            return;

        side.factories++;
        int tile = ClaimHomeTile(side);
        GameObject factory = NewFactory(side, tile);
        Vector3 fullScale = factory.transform.localScale;
        Vector3 top = board.TileTop(tile);
        Vector3 standing = factory.transform.position;
        float height = WorldBounds(factory).size.y + 0.05f;
        factory.transform.position = standing - Vector3.up * height;

        // The construction site: a ring of lights around the plot that fills up as the work progresses,
        // while the factory rises out of the ground.
        Transform site = BuildSiteRing(side, top);
        Renderer[] lights = site.GetComponentsInChildren<Renderer>();
        fx.Burst(top + Vector3.up * 0.15f, new Color(0.75f, 0.72f, 0.66f), 16, 0.2f, 1.3f, 0.7f, false);
        float shown = 0f;
        int lit = -1;

        visual.onProgress = progress =>
        {
            shown = Mathf.MoveTowards(shown, Mathf.Clamp01(progress), WorldDelta * 1.2f);
            float eased = Mathf.SmoothStep(0f, 1f, shown);
            factory.transform.position = standing - Vector3.up * (height * (1f - Mathf.Max(0.06f, eased)));
            int nowLit = Mathf.FloorToInt(Mathf.Clamp01(progress) * lights.Length);
            if (nowLit != lit)
            {
                lit = nowLit;
                for (int i = 0; i < lights.Length; i++)
                    lights[i].GetComponent<MeshFilter>().sharedMesh = i < lit ? siteLitMesh[side.isRed ? 1 : 0] : siteDimMesh;
            }
        };
        visual.onDone = () =>
        {
            factory.transform.position = standing;
            factory.transform.localScale = fullScale;
            Destroy(site.gameObject);
            side.upgradable.Add(factory.transform);
            side.upgradableScales.Add(fullScale);
            side.factorySpots.Add(new FactorySpot { factory = factory, tile = tile });
            fx.Burst(top + Vector3.up * 0.5f, side.color + Color.white * 0.25f, 18, 0.22f, 2f, 0.7f);
            fx.Smoke(WorldBounds(factory).max - new Vector3(0.15f, 0f, 0.15f), null, 3f, 0.35f, new Color(0.65f, 0.65f, 0.68f, 0.55f));
        };
        visual.onCancel = () =>
        {
            Destroy(site.gameObject);
            Destroy(factory);
        };
    }

    readonly Mesh[] siteLitMesh = new Mesh[2];
    Mesh siteDimMesh;

    // Twelve short dashes in a circle round a building plot.
    Transform BuildSiteRing(Side side, Vector3 center)
    {
        if (siteDimMesh == null)
        {
            siteDimMesh = new ProcMesh().Box(Vector3.zero, new Vector3(0.22f, 0.05f, 0.08f), new Color(0.2f, 0.21f, 0.24f)).ToMesh("SiteDim");
            siteLitMesh[0] = new ProcMesh().Box(Vector3.zero, new Vector3(0.22f, 0.06f, 0.08f), ProcMesh.Glow(Color.Lerp(HexBoard.BlueTop, Color.white, 0.35f), 0.95f)).ToMesh("SiteLitBlue");
            siteLitMesh[1] = new ProcMesh().Box(Vector3.zero, new Vector3(0.22f, 0.06f, 0.08f), ProcMesh.Glow(Color.Lerp(HexBoard.RedTop, new Color(1f, 0.8f, 0.3f), 0.35f), 0.95f)).ToMesh("SiteLitRed");
        }

        var root = new GameObject("BuildSite").transform;
        root.SetParent(transform, false);
        root.position = center + Vector3.up * 0.04f;
        const int count = 12;
        for (int i = 0; i < count; i++)
        {
            // Clockwise from the front, like a progress dial.
            float angle = -90f - i * 360f / count;
            float rad = angle * Mathf.Deg2Rad;
            GameObject dash = ProcMesh.Spawn("Dash", siteDimMesh, art.boardMaterial, root);
            dash.transform.localPosition = new Vector3(Mathf.Cos(rad), 0f, Mathf.Sin(rad)) * 0.72f;
            dash.transform.localRotation = Quaternion.Euler(0f, -angle + 90f, 0f);
        }

        return root;
    }

    // Spirals outward from the capital so new buildings don't hide behind the city.
    int ClaimHomeTile(Side side)
    {
        side.homeBuilds++;
        const float goldenAngle = 2.39996f;
        float angle = side.homeBuilds * goldenAngle;
        float distance = 4.6f + side.homeBuilds * 0.55f;   // just outside the capital's skyline
        Vector2 point = Flat(side.capital) + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
        var half = new Vector2(0.7f, 0.7f);
        int tile = ClaimNear(point, i => (side.isRed ? board.ThresholdOf(i) < 0.45f : board.ThresholdOf(i) > 0.55f) && IsClear(board.TileTop(i), half));
        AddFootprint(board.TileTop(tile), half);
        return tile;
    }

    void BuildWarVisual(Side side, ActionVisual visual)
    {
        if (AtCap(side.wars, Tuning.maxWarsShown))
            return;

        side.wars++;
        // About four wars in ten go by sea, when there is enemy coast to reach, so the land doesn't fill with tanks.
        if (UnityEngine.Random.value < Tuning.fleetChance && BuildNavalVisual(side, visual))
            return;

        // Tanks drive into the enemy's land and stay there, so the number of wars each side runs shows at a glance.
        // They only ever travel over land: they roll out from the nearest land their side holds (or a staging point
        // a few tiles back) and follow the tiles to the target.
        int tankCount = Mathf.Max(1, Tuning.tanksPerColumn);
        foreach (int _ in LandNeighbours(0)) { }   // builds the land grid
        int[] tiles = ColumnTiles(side, tankCount, Enemy(side).capital);
        int tile = tiles[0];
        Vector3 target = board.TileTop(tile);
        List<Vector3> route = LandRoute(side, tile);
        Vector3 from = route[0];

        var column = new GameObject("TankColumn").transform;
        column.SetParent(transform, false);
        for (int i = 0; i < tankCount; i++)
        {
            GameObject tank = UnitContainer("Tank", side, column).gameObject;
            tank.transform.localPosition = ArrowSpot(i);
            tank.transform.localScale = Vector3.one * 1.9f;
        }
        side.tankColumns.Add(column);

        // Distance along the route, so the column moves at an even speed.
        var along = new float[route.Count];
        for (int i = 1; i < route.Count; i++)
            along[i] = along[i - 1] + (Flat(route[i]) - Flat(route[i - 1])).magnitude;
        float length = Mathf.Max(0.001f, along[route.Count - 1]);

        // Where a tank is when it has driven d along the route, and which way it faces. Past the end of the road each
        // tank drives on to its own tile (the leader's is the end of the road), so they never bunch up.
        (Vector3, Vector3) OnRoute(float d, int who = 0)
        {
            if (d > length)
            {
                Vector3 end = route[route.Count - 1];
                Vector3 home = board.TileTop(tiles[Mathf.Min(who, tiles.Length - 1)]);
                float leg = (Flat(home) - Flat(end)).magnitude;
                if (leg > 0.01f)
                {
                    Vector3 toHome = home - end;
                    toHome.y = 0f;
                    return (Vector3.Lerp(end, home, Mathf.Clamp01((d - length) / leg)) + Vector3.up * 0.1f, toHome);
                }
                d = length;
            }
            d = Mathf.Clamp(d, 0f, length);
            int i = 1;
            while (i < route.Count - 1 && along[i] < d)
                i++;
            float span = Mathf.Max(0.001f, along[i] - along[i - 1]);
            Vector3 position = Vector3.Lerp(route[i - 1], route[i], Mathf.Clamp01((d - along[i - 1]) / span)) + Vector3.up * 0.1f;
            Vector3 heading = route[i] - route[i - 1];
            heading.y = 0f;
            return (position, heading);
        }

        // The tanks drive in an arrowhead, the leader first and the others a little behind on either side.
        float Gap = Tuning.tankGap;
        Transform[] tanks = new Transform[column.childCount];
        for (int i = 0; i < tanks.Length; i++)
            tanks[i] = column.GetChild(i);
        Vector3[] formation = new Vector3[tanks.Length];
        for (int i = 0; i < tanks.Length; i++)
            formation[i] = tanks[i].localPosition;

        // The arrowhead follows the road: each tank keeps its place behind and beside the leader, measured along the
        // route. Where the ground beside the road is water (or a building), a wing tank pulls in behind the leader,
        // so no tank ever drives over water.
        int rows = (tanks.Length) / 2;
        float[] wing = new float[tanks.Length];
        // In single file each tank keeps a full tank length behind the one ahead.
        const float FileGap = 1.25f;
        float lastLeg = 0f;
        foreach (int home in tiles)
            lastLeg = Mathf.Max(lastLeg, (Flat(board.TileTop(home)) - Flat(route[route.Count - 1])).magnitude);
        void PlaceOnRoute(float t)
        {
            float lead = Mathf.Clamp01(t) * (length + lastLeg + FileGap * (tanks.Length - 1));
            (Vector3 front, Vector3 frontHeading) = OnRoute(lead);
            column.position = front;
            for (int i = 0; i < tanks.Length; i++)
            {
                Vector3 spot = formation[i];
                // In formation each wing tank sits beside the road; when it can't, it takes its own place in the file.
                // Whether the formation spot beside the road is land is judged at the formation spot itself (not where
                // the tank is now), and the tank eases in and out of formation, so it never flickers between the two.
                (Vector3 slot, Vector3 slotHeading) = OnRoute(lead + spot.z, i);
                bool solid = true;
                if (lead + spot.z <= length && slotHeading.sqrMagnitude > 0.0001f)
                {
                    Vector3 beside = slot + Vector3.Cross(Vector3.up, slotHeading.normalized) * spot.x;
                    solid = IsLand(beside) && !OnBuildingGround(beside, 0.1f);
                }
                wing[i] = Mathf.MoveTowards(wing[i], solid ? 1f : 0f, WorldDelta * 2f);
                float back = Mathf.Lerp(-FileGap * i, spot.z, wing[i]);
                (Vector3 position, Vector3 heading) = OnRoute(lead + back, i);
                if (heading.sqrMagnitude > 0.0001f)
                {
                    Vector3 forward = heading.normalized;
                    if (lead + back <= length)
                        position += Vector3.Cross(Vector3.up, forward) * spot.x * wing[i];
                    tanks[i].rotation = Quaternion.Slerp(tanks[i].rotation, Quaternion.LookRotation(forward), WorldDelta * 6f);
                }
                tanks[i].position = position;
            }
        }

        var war = new Deployment { side = side, root = column, units = tanks, tiles = tiles, site = target };
        deployments.Add(war);

        Vector3 firstHeading = route.Count > 1 ? route[1] - route[0] : target - from;
        firstHeading.y = 0f;
        column.rotation = Quaternion.LookRotation(firstHeading.sqrMagnitude > 0.01f ? firstHeading.normalized : Vector3.forward);
        column.position = from + Vector3.up * 0.1f;
        fx.Burst(from + Vector3.up * 0.2f, new Color(0.75f, 0.72f, 0.66f), 12, 0.2f, 1.2f, 0.6f, false);

        float shown = 0f;
        visual.onProgress = progress =>
        {
            shown = Mathf.MoveTowards(shown, progress, WorldDelta * 0.8f);
            PlaceOnRoute(shown);
        };
        visual.onDone = () =>
        {
            // Each tank parks on its own tile.
            ParkTanks(war);
            war.ready = true;
            warSites.Add(target);
            fx.Explosion(target + Vector3.up * 0.3f, 1f);
            fx.Smoke(target + Vector3.up * 0.1f, null, 7f, 0.75f, new Color(0.1f, 0.09f, 0.09f, 0.75f));
            fx.Flame(target + Vector3.up * 0.1f, null, 16f, 0.42f);
            StartCoroutine(ExpandingRing(target + Vector3.up * 0.15f, side.color, 2.4f));
            shake = Mathf.Max(shake, 0.1f);
        };
        visual.onCancel = () =>
        {
            foreach (int t in war.tiles)
                claimed.Remove(t);
            deployments.Remove(war);
            side.tankColumns.Remove(column);
            Destroy(column.gameObject);
        };
    }

    void BuildSpyVisual(Side side, ActionVisual visual)
    {
        if (AtCap(side.spies, Tuning.maxSpiesShown) || art.drone == null)
            return;

        side.spies++;
        Side enemy = Enemy(side);
        // Takes off over its own capital and flies level, at the one drone height, to patrol enemy land.
        Vector3 start = new Vector3(side.capital.x, DroneFlyHeight, side.capital.z);
        Vector3 target = RandomSpotOver(enemy.isRed, enemy.capital);

        GameObject drone = InstantiateClean(art.drone, start);
        StandUprightFlat(drone, 1.4f);
        TintDrone(drone, side);
        GameObject beacon = ProcMesh.Spawn("Beacon", ProcMesh.Ring(ProcMesh.Glow(side.color, 0.9f), 0.28f, 0.07f, 14), art.boardMaterial, drone.transform);
        beacon.transform.localPosition = Vector3.zero;
        beacon.transform.localScale = Vector3.one / Mathf.Max(drone.transform.localScale.x, 0.001f);

        float shown = 0f;
        visual.onProgress = progress =>
        {
            shown = Mathf.MoveTowards(shown, progress, WorldDelta * 0.6f);
            drone.transform.position = Vector3.Lerp(start, target, shown);
        };
        visual.onDone = () =>
        {
            fx.Burst(target, side.color + Color.white * 0.3f, 16, 0.2f, 1.5f, 0.6f);
            roamers.Add(new Roamer
            {
                side = side,
                transform = drone.transform,
                overRed = enemy.isRed,
                target = target,
                speed = UnityEngine.Random.Range(1.6f, 2.4f),
                phase = UnityEngine.Random.value * 10f,
            });
        };
        visual.onCancel = () => Destroy(drone);
    }

    // Paints the drone in its side's color, keeping some of its own shading.
    // The drone keeps its pack colors; only its shell is recolored to the side's color.
    void TintDrone(GameObject drone, Side side)
    {
        Texture2D shell = side.isRed ? art.droneRed : art.droneBlue;
        if (shell == null)
            return;
        foreach (Renderer part in drone.GetComponentsInChildren<Renderer>())
        {
            if (part.name == "Beacon")
                continue;
            foreach (Material material in part.materials)
            {
                if (material.HasProperty("_BaseMap"))
                    material.SetTexture("_BaseMap", shell);
                if (material.HasProperty("_BaseColor"))
                    material.SetColor("_BaseColor", Color.white);
            }
        }
    }

    class Roamer
    {
        public Side side;
        public Transform transform;
        public bool overRed;
        public Vector3 target;
        public float speed;
        public float phase;
    }

    readonly List<Roamer> roamers = new List<Roamer>();
    // Every drone flies at this one height: just above the tallest capital, so it never needs to climb or dip.
    static float tallestCapital = 10f;
    public static float DroneFlyHeight => tallestCapital + Tuning.droneHeightAboveCapital;

    // Spy drones fly from one random spot of the enemy's territory to the next, following the map as borders move.
    void UpdateRoamers()
    {
        float dt = WorldDelta;
        foreach (Roamer roamer in roamers)
        {
            if (roamer.transform == null)
                continue;

            Vector3 position = roamer.transform.position;
            Vector3 flat = roamer.target - position;
            flat.y = 0f;
            if (flat.magnitude < 0.3f)
            {
                roamer.target = RandomSpotOver(roamer.overRed, roamer.target);
                continue;
            }

            Vector3 step = flat.normalized * Mathf.Min(roamer.speed * dt, flat.magnitude);
            position += step;
            position.y = DroneFlyHeight;
            roamer.transform.position = position;
            Quaternion facing = Quaternion.LookRotation(flat.normalized, Vector3.up);
            roamer.transform.rotation = Quaternion.Slerp(roamer.transform.rotation, facing, dt * 3f);
        }
    }

    // A random land tile currently held by the given side, a fair distance from where the drone is.
    Vector3 RandomSpotOver(bool red, Vector3 from)
    {
        for (int attempt = 0; attempt < 40; attempt++)
        {
            int tile = UnityEngine.Random.Range(0, board.TileCount);
            bool tileIsRed = board.ThresholdOf(tile) < board.Com;
            if (tileIsRed != red)
                continue;
            Vector3 top = board.TileTop(tile);
            if (OnBuildingGround(top, 0.8f))
                continue;
            if (attempt < 30 && (Flat(top) - Flat(from)).magnitude < 3f)
                continue;
            return new Vector3(top.x, DroneFlyHeight, top.z);
        }

        return from;
    }

    // Scales to a footprint without changing orientation, for things that fly level.
    void StandUprightFlat(GameObject go, float width)
    {
        Bounds bounds = WorldBounds(go);
        float footprint = Mathf.Max(bounds.size.x, bounds.size.z, 0.001f);
        go.transform.localScale *= width / footprint;
    }

    // The research rocket: the space shuttle standing nose up on the pad, its wings in the side's color.
    GameObject ResearchRocket(Side side, Vector3 pad)
    {
        if (art.researchRocket == null)
        {
            GameObject old = ProcMesh.Spawn("Rocket", ProcMesh.Rocket(side.color), art.boardMaterial, transform);
            old.transform.position = pad;
            old.transform.localScale = new Vector3(2.8f, 3f, 2.8f);
            return old;
        }
        var rocket = new GameObject("Rocket");
        rocket.transform.SetParent(transform, false);
        rocket.transform.position = pad;
        GameObject model = Instantiate(art.researchRocket, rocket.transform, false);
        CleanModel(model);
        model.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);   // the shuttle lies nose-forward along +z: stand it on its tail
        TintTeam(model, side.color, "Rocket");
        Bounds bounds = WorldBounds(model);
        model.transform.localScale *= Tuning.rocketHeight / Mathf.Max(0.001f, bounds.size.y);
        bounds = WorldBounds(model);
        model.transform.position += pad - new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
        return rocket;
    }

    void BuildResearchVisual(Side side, ActionVisual visual)
    {
        int tile = ClaimHomeTile(side);
        Vector3 pad = board.TileTop(tile);
        GameObject rocket = ResearchRocket(side, pad);
        GameObject platform = ProcMesh.Spawn("Pad", ProcMesh.Silo(), art.boardMaterial, transform);
        platform.transform.position = pad;
        platform.transform.localScale = new Vector3(1.5f, 1f, 1.5f);

        // The rocket rises out of the silo as the research progresses.
        Vector3 standing = rocket.transform.position;
        float height = WorldBounds(rocket).size.y;
        float shownRise = 0f;
        rocket.transform.position = standing - Vector3.up * height * 0.9f;
        visual.onProgress = progress =>
        {
            shownRise = Mathf.MoveTowards(shownRise, Mathf.Clamp01(progress), WorldDelta * 0.8f);
            rocket.transform.position = standing - Vector3.up * height * 0.9f * (1f - shownRise);
        };
        visual.onDone = () =>
        {
            // A president's agenda finishes research instantly, before the rocket has risen, so stand it up first.
            rocket.transform.position = standing;
            StartCoroutine(LaunchRocket(side, rocket));
        };
        visual.onCancel = () =>
        {
            Destroy(rocket);
            Destroy(platform);
        };
    }

    IEnumerator LaunchRocket(Side side, GameObject rocket)
    {
        Vector3 start = rocket.transform.position;
        fx.Burst(start, new Color(1f, 0.6f, 0.2f), 20, 0.5f, 2f, 0.7f);
        ParticleSystem exhaust = fx.Flame(start, rocket.transform, 40f, 0.45f);
        exhaust.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

        float elapsed = 0f;
        const float duration = 2.4f;
        while (elapsed < duration)
        {
            elapsed += WorldDelta;
            float t = elapsed / duration;
            rocket.transform.position = start + Vector3.up * (t * t * 26f);
            yield return null;
        }

        Destroy(rocket);
        if (AtCap(side.satellites, Tuning.maxSatellitesShown))
            yield break;

        side.satellites++;
        GameObject satellite = UnitContainer("Satellite", side, transform).gameObject;
        side.satellites3D.Add(satellite.transform);
        satellite.transform.localScale = Vector3.one * 2.6f * Tuning.satelliteScale;
        // Each satellite circles the upper part of the capitol on its own circle: one step wider and one step higher
        // than the satellite before it, all at the same speed. It starts from where it was launched.
        // A step round the circle from the satellite before (as if launched a little later), so even satellites that
        // launch together spread out.
        int index = side.satellites - 1;
        var orbiter = new Orbiter { transform = satellite.transform, side = side, index = index, angle = index * 2.4f };
        PlaceSatelliteOrbit(orbiter);
        orbiters.Add(orbiter);
    }

    // The circle for a satellite, from the Game Tuning values (read every frame, so changes show at once).
    readonly Dictionary<(Side, float), float> capitolWidth = new Dictionary<(Side, float), float>();

    void PlaceSatelliteOrbit(Orbiter orbiter)
    {
        Side side = orbiter.side;
        Bounds capitol = side.capitol.GetComponent<Renderer>().bounds;
        // Heights repeat every few satellites so they never climb out of the picture; the circles keep widening.
        int layer = orbiter.index % Mathf.Max(1, Tuning.satelliteHeightLayers);
        float height = Mathf.Lerp(capitol.min.y, side.capitalTop, Tuning.satelliteHeight) + layer * Tuning.satelliteStepUp;
        orbiter.center = new Vector3(capitol.center.x, height, capitol.center.z);
        float key = Mathf.Round(height * 10f) / 10f;
        if (!capitolWidth.TryGetValue((side, key), out float width))
        {
            width = CapitolWidthAt(side, orbiter.center, 0.45f);
            capitolWidth[(side, key)] = width;
        }
        orbiter.radius = width + Tuning.satelliteClearance + orbiter.index * Tuning.satelliteStepOut;
        orbiter.speed = Tuning.satelliteSpeed;
        orbiter.transform.localScale = Vector3.one * 2.6f * Tuning.satelliteScale;
    }

    // How far the capitol (with its flags) reaches out from its middle, within a band of heights around a point.
    float CapitolWidthAt(Side side, Vector3 middle, float band)
    {
        float reach = 0.3f;
        foreach (MeshFilter filter in side.capitol.GetComponentsInChildren<MeshFilter>())
        {
            Mesh mesh = filter.sharedMesh;
            if (mesh == null || !mesh.isReadable)
            {
                if (mesh != null)
                {
                    Bounds b = filter.GetComponent<Renderer>().bounds;
                    if (b.max.y > middle.y - band && b.min.y < middle.y + band)
                        reach = Mathf.Max(reach, new Vector2(Mathf.Max(Mathf.Abs(b.min.x - middle.x), Mathf.Abs(b.max.x - middle.x)),
                            Mathf.Max(Mathf.Abs(b.min.z - middle.z), Mathf.Abs(b.max.z - middle.z))).magnitude);
                }
                continue;
            }
            Matrix4x4 toWorld = filter.transform.localToWorldMatrix;
            foreach (Vector3 vertex in mesh.vertices)
            {
                Vector3 p = toWorld.MultiplyPoint3x4(vertex);
                if (p.y < middle.y - band || p.y > middle.y + band)
                    continue;
                reach = Mathf.Max(reach, new Vector2(p.x - middle.x, p.z - middle.z).magnitude);
            }
        }
        return reach;
    }

    void BuildAssassinVisual(Side side, ActionVisual visual)
    {
        Side enemy = Enemy(side);
        GameObject crosshair = ProcMesh.Spawn("Crosshair", ProcMesh.Ring(ProcMesh.Glow(new Color(1f, 0.15f, 0.1f), 0.9f), 1f, 0.09f), art.boardMaterial, transform);
        crosshair.transform.position = enemy.capital + Vector3.up * 0.35f;

        visual.onProgress = _ =>
        {
            float pulse = 1f + 0.18f * Mathf.Sin(WorldTime * 8f);
            crosshair.transform.localScale = new Vector3(pulse, 1f, pulse);
            crosshair.transform.Rotate(0f, WorldDelta * 90f, 0f);
        };
        visual.onDone = () =>
        {
            Destroy(crosshair);
            StartCoroutine(FlyStrike(side.capital + Vector3.up, enemy.capital + Vector3.up * 0.5f, side.color, 1f, 1.8f));
        };
        visual.onCancel = () => Destroy(crosshair);
    }

    IEnumerator FlyStrike(Vector3 from, Vector3 to, Color color, float seconds, float blastScale)
    {
        GameObject rocket = NukeModel("Strike", color).gameObject;
        rocket.transform.localScale = Vector3.one * 1.2f;
        fx.Flame(from, rocket.transform, 30f, 0.3f);

        float elapsed = 0f;
        Vector3 previous = from;
        float arc = Vector3.Distance(from, to) * 0.28f;
        while (elapsed < seconds)
        {
            elapsed += WorldDelta;
            float t = Mathf.Clamp01(elapsed / seconds);
            Vector3 position = Vector3.Lerp(from, to, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * arc);
            rocket.transform.position = position;
            Vector3 heading = position - previous;
            if (heading.sqrMagnitude > 0.0001f)
                rocket.transform.rotation = Quaternion.FromToRotation(Vector3.up, heading.normalized);
            previous = position;
            yield return null;
        }

        Destroy(rocket);
        fx.Explosion(to, blastScale);
        StartCoroutine(ExpandingRing(to, new Color(1f, 0.55f, 0.15f), 4f * blastScale));
        shake = Mathf.Max(shake, 0.28f * blastScale);
    }

    IEnumerator ExpandingRing(Vector3 position, Color color, float maxRadius)
    {
        GameObject ring = ProcMesh.Spawn("Ring", ProcMesh.Ring(ProcMesh.Glow(color, 0.95f), 1f, 0.05f), art.boardMaterial, transform);
        ring.transform.position = position;
        float elapsed = 0f;
        const float duration = 0.7f;
        while (elapsed < duration)
        {
            elapsed += WorldDelta;
            float radius = Mathf.Lerp(0.1f, maxRadius, 1f - Mathf.Pow(1f - elapsed / duration, 2f));
            ring.transform.localScale = new Vector3(radius, 1f, radius);
            yield return null;
        }

        Destroy(ring);
    }

    void UpdateOrbiters()
    {
        foreach (Orbiter orbiter in orbiters)
        {
            if (orbiter.transform == null)
                continue;

            if (orbiter.side != null)
                PlaceSatelliteOrbit(orbiter);
            orbiter.angle += orbiter.speed * WorldDelta;
            Vector3 offset = new Vector3(Mathf.Cos(orbiter.angle), 0f, Mathf.Sin(orbiter.angle)) * orbiter.radius;
            orbiter.transform.position = orbiter.center + offset;
            orbiter.transform.rotation = Quaternion.Euler(0f, -orbiter.angle * Mathf.Rad2Deg, 0f);
        }
    }

    void UpdateWarSites()
    {
        if (warSites.Count == 0 || WorldTime < nextWarBlast)
            return;

        nextWarBlast = WorldTime + UnityEngine.Random.Range(1.4f, 3.2f);
        Vector3 site = warSites[UnityEngine.Random.Range(0, warSites.Count)];
        fx.Burst(site + Vector3.up * 0.2f, new Color(1f, 0.6f, 0.2f), 6, 0.4f, 1.1f, 0.35f);
    }

    void UpdateDanger()
    {
        float danger = Mathf.Clamp01(NukeMeterUpdater.nukeDanger / 100f);
        if (peaceStarted)
            danger = 0f;
        // How many missiles: the danger. Whose: each side's share of the power, so the stronger side's half fills up.
        int total = danger >= 0.005f ? Mathf.Max(1, Mathf.RoundToInt(danger * PoolCapacity)) : 0;
        double redPower = Math.Max(0, country.Communism.Power), bluePower = Math.Max(0, country.Capitalism.Power);
        double share = redPower + bluePower > 0 ? redPower / (redPower + bluePower) : 0.5;
        int redShown = Mathf.Clamp((int)Math.Round(total * share), 0, total);
        FillPool(blue, total - redShown);
        FillPool(red, redShown);
        poolLabel.text = Mathf.RoundToInt(danger * 100f) + "%";
        poolLabel.transform.rotation = cameraRotation;
        poolLabel.color = Color.Lerp(new Color(0.75f, 0.85f, 1f), new Color(1f, 0.3f, 0.2f), danger);

        float alarm = Mathf.InverseLerp(0.4f, 1f, danger);
        grading.saturation.Override(Mathf.Lerp(0f, -28f, alarm));
        grading.colorFilter.Override(Color.Lerp(Color.white, new Color(1f, 0.62f, 0.55f), alarm));
        vignette.color.Override(Color.Lerp(Color.black, new Color(0.45f, 0f, 0f), alarm));
        float heartbeat = alarm > 0.5f ? Mathf.Abs(Mathf.Sin(WorldTime * Mathf.Lerp(2f, 5f, alarm))) * 0.1f : 0f;
        vignette.intensity.Override(Mathf.Lerp(0.28f, 0.52f, alarm) + heartbeat);

        if (danger > 0.9f)
            shake = Mathf.Max(shake, 0.03f);

        if (!endOfWorld && CountryManager.LoseLock)
        {
            endOfWorld = true;
            StartCoroutine(EndOfTheWorld());
        }
    }

    void FillPool(Side side, int shown)
    {
        if (endOfWorld)
            return;

        float dt = WorldDelta;
        for (int i = 0; i < side.missiles.Count; i++)
        {
            bool visible = i < shown;
            Transform missile = side.missiles[i];
            Vector3 target = visible ? side.missileScales[i] : Vector3.zero;
            missile.localScale = Vector3.MoveTowards(missile.localScale, target, dt * 4f * side.missileScales[i].magnitude);
            Vector3 home = side.missileHomes[i];
            float sink = visible ? 0f : 0.6f;
            missile.position = Vector3.MoveTowards(missile.position, home + Vector3.down * sink, dt * 3f);
        }
    }

    IEnumerator EndOfTheWorld()
    {
        int count = Mathf.Max(blue.missiles.Count, red.missiles.Count);
        for (int i = 0; i < count; i++)
        {
            if (i < blue.missiles.Count)
            {
                blue.missiles[i].localScale = blue.missileScales[i];
                StartCoroutine(FlyMissile(blue.missiles[i], red.capital + Random2D(3.5f)));
            }
            if (i < red.missiles.Count)
            {
                red.missiles[i].localScale = red.missileScales[i];
                StartCoroutine(FlyMissile(red.missiles[i], blue.capital + Random2D(3.5f)));
            }
            yield return new WaitForSecondsRealtime(0.06f);
        }

        yield return new WaitForSecondsRealtime(1.1f);
        shake = 0.7f;
        float radius = 0f;
        while (radius < 70f)
        {
            radius += WorldDelta * 13f;
            board.Burn(Flat(red.capital), radius);
            board.Burn(Flat(blue.capital), radius);
            yield return null;
        }
    }

    static Vector3 Random2D(float range)
    {
        return new Vector3(UnityEngine.Random.Range(-range, range), 0.4f, UnityEngine.Random.Range(-range, range));
    }

    IEnumerator FlyMissile(Transform missile, Vector3 to)
    {
        Vector3 from = missile.position + Vector3.up * 0.7f;
        float arc = Vector3.Distance(from, to) * 0.3f;
        fx.Flame(from, missile, 26f, 0.35f);
        Quaternion baseRotation = missile.rotation;
        Vector3 previous = from;
        float elapsed = 0f;
        const float seconds = 2.2f;
        while (elapsed < seconds)
        {
            elapsed += WorldDelta;
            float t = Mathf.Clamp01(elapsed / seconds);
            Vector3 position = Vector3.Lerp(from, to, t) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * arc);
            missile.position = position;
            Vector3 heading = position - previous;
            if (heading.sqrMagnitude > 0.0001f)
                missile.rotation = Quaternion.FromToRotation(Vector3.up, heading.normalized) * baseRotation;
            previous = position;
            yield return null;
        }

        missile.gameObject.SetActive(false);
        fx.Explosion(to, 2.6f);
        StartCoroutine(ExpandingRing(to, new Color(1f, 0.75f, 0.35f), 7f));
        shake = Mathf.Max(shake, 0.5f);
    }

    void UpdateCamera()
    {
        Vector3 offset = Vector3.zero;
        if (IsMenu)
        {
            // A slow drift, so the menu board feels alive.
            offset = new Vector3(Mathf.Sin(WorldTime * 0.1f) * 1.2f, 0f, Mathf.Cos(WorldTime * 0.08f) * 0.5f);
        }

        if (shake > 0.001f)
        {
            offset += UnityEngine.Random.insideUnitSphere * shake;
            shake = Mathf.MoveTowards(shake, 0f, WorldDelta * 0.6f);
        }

        mainCamera.transform.SetPositionAndRotation(cameraHome + offset, cameraRotation);
    }
}
