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
public class WarTable : MonoBehaviour
{
    const int MaxFactoriesShown = 10;
    const int MaxWarsShown = 8;
    const int MaxSpiesShown = 5;
    const int MaxSatellitesShown = 4;
    const float TileWidth = 0.86f;
    const int UnrestBelow = 25;
    const int NukesPerSide = 12;
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
    }

    class Side
    {
        public bool isRed;
        public Color color;
        public int capitalTile;
        public Vector3 capital;
        public Transform capitalRoot;
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
    }

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

    Mesh tankBlue;
    Mesh tankRed;

    bool IsMenu => country == null;
    public static bool IsRunning { get; private set; }

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
        board.Build(art.data, art.boardMaterial);
        board.SnapTo(IsMenu ? 0.5f : GameManager.Com);

        tankBlue = ProcMesh.Tank(HexBoard.BlueTop);
        tankRed = ProcMesh.Tank(HexBoard.RedTop);

        blue = BuildSide(false, art.data.blueCapital, art.data.blueSilos, art.blueCity);
        red = BuildSide(true, art.data.redCapital, art.data.redSilos, art.redCity);
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
        side.capitalTile = board.NearestTile(capitalPoint);
        side.capital = board.TileTop(side.capitalTile);
        claimed.Add(side.capitalTile);

        side.capitalRoot = new GameObject(isRed ? "RedCapital" : "BlueCapital").transform;
        side.capitalRoot.SetParent(transform, false);
        side.capitalRoot.position = side.capital;

        for (int i = 0; i < city.Length; i++)
        {
            int tile = i == 0 ? side.capitalTile : ClaimNear(capitalPoint, _ => true);
            GameObject building = PlacePrefab(city[i], tile, i == 0 ? 2.9f : 1.9f, i == 0 ? 4.4f : 2.8f);
            building.name = "City";
            building.transform.SetParent(side.capitalRoot, true);
            side.capitalTop = Mathf.Max(side.capitalTop, WorldBounds(building).max.y);
        }

        BuildInfoCenter(side, capitalPoint);
        BuildFlags(side);
        return side;
    }

    // A small radio station beside the capital, with a floating "i" sign. Tapping it opens the country report.
    void BuildInfoCenter(Side side, Vector2 capitalPoint)
    {
        // A few tiles toward the camera and away from the coast, so the capital never hides it.
        Vector2 spot = capitalPoint + new Vector2(side.isRed ? 2.4f : -2.4f, -2.6f);
        int tile = ClaimNear(spot, _ => true);
        var root = new GameObject("InfoCenter").transform;
        root.SetParent(transform, false);
        root.position = board.TileTop(tile);

        Color wall = Color.Lerp(side.color, Color.white, 0.6f);
        Color roof = side.color * 0.55f;
        roof.a = 1f;
        Color steel = new Color(0.3f, 0.32f, 0.36f);
        Color gold = new Color(0.95f, 0.72f, 0.22f);
        Mesh building = new ProcMesh()
            .Box(new Vector3(0f, 0.42f, 0f), new Vector3(1.25f, 0.84f, 1.05f), wall)
            .Box(new Vector3(0f, 0.9f, 0f), new Vector3(1.38f, 0.12f, 1.18f), roof)
            .Frustum(new Vector3(-0.28f, 0.96f, 0f), 0.34f, 0.1f, 0.34f, 10, wall)
            .Box(new Vector3(0.36f, 1.62f, 0f), new Vector3(0.08f, 1.35f, 0.08f), steel)
            .Box(new Vector3(0.36f, 2.32f, 0f), new Vector3(0.16f, 0.16f, 0.16f), ProcMesh.Glow(new Color(1f, 0.3f, 0.2f), 0.9f))
            .ToMesh("InfoCenter");
        ProcMesh.Spawn("Building", building, art.boardMaterial, root);

        var sign = new GameObject("InfoSign").transform;
        sign.SetParent(root, false);
        sign.localPosition = Vector3.up * 2.9f;
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

    // Big flags stand on the rooftops, one for every 20 points of happiness.
    void BuildFlags(Side side)
    {
        Mesh flagMesh = ProcMesh.Flag(side.color);
        side.flags = new GameObject[5];
        for (int i = 0; i < side.flags.Length; i++)
        {
            GameObject flag = ProcMesh.Spawn("Flag", flagMesh, art.boardMaterial, side.capitalRoot);
            // Fans out across the skyline, tallest in the middle, so no flag hides behind another.
            float x = (i - 2) * 1.2f;
            float rise = 0.7f - Mathf.Abs(i - 2) * 0.55f;
            flag.transform.position = new Vector3(side.capital.x + x, side.capitalTop + rise - 0.9f, side.capital.z);
            flag.transform.localScale = Vector3.one * 5.6f;
            side.flags[i] = flag;
        }
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
        const float spacing = 0.82f;
        for (float x = 0.5f; x < PoolRadius - 0.3f; x += spacing)
        {
            for (float z = -PoolRadius; z <= PoolRadius; z += spacing)
            {
                if (new Vector2(x, z).magnitude < PoolRadius - 0.45f)
                    spots.Add(new Vector3(x * direction, 0.3f, z));
            }
        }

        Mesh nukeMesh = ProcMesh.Rocket(side.color);
        spots.Sort((a, b) => (Mathf.Abs(a.x) + Mathf.Abs(a.z) * 0.6f).CompareTo(Mathf.Abs(b.x) + Mathf.Abs(b.z) * 0.6f));
        for (int i = 0; i < NukesPerSide && i < spots.Count; i++)
        {
            Vector3 home = poolCenter + spots[i];
            // Chunky procedural missiles read better at this size than the thin pack model.
            Transform missile = ProcMesh.Spawn("Nuke", nukeMesh, art.boardMaterial, transform).transform;
            missile.position = home;
            missile.localScale = Vector3.one * 1.9f;
            side.missiles.Add(missile);
            side.missileHomes.Add(missile.position);
            side.missileScales.Add(missile.localScale);
            missile.localScale = Vector3.zero;
        }
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
        Renderer[] renderers = go.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0)
            return new Bounds(go.transform.position, Vector3.zero);

        Bounds bounds = renderers[0].bounds;
        foreach (Renderer item in renderers)
            bounds.Encapsulate(item.bounds);
        return bounds;
    }

    GameObject PlacePrefab(GameObject prefab, int tile, float footprintScale, float maxHeight)
    {
        Vector3 top = board.TileTop(tile);
        GameObject instance = InstantiateClean(prefab, top);
        instance.transform.rotation = Quaternion.Euler(0f, 60f * UnityEngine.Random.Range(0, 6), 0f);

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
            float distance = (new Vector2(top.x, top.z) - point).sqrMagnitude;
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = i;
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
            if (claimed.Contains(i))
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

    static Vector2 Flat(Vector3 point)
    {
        return new Vector2(point.x, point.z);
    }

    // ----- Live state -----

    void Update()
    {
        if (Mathf.Abs(mainCamera.aspect - cameraAspect) > 0.001f)
            FrameCamera();

        fx.Tick();
        UpdateOrbiters();
        UpdateRoamers();
        UpdateCamera();
        BobInfoSign(blue);
        BobInfoSign(red);

        if (IsMenu)
        {
            board.SetCom(0.5f + 0.2f * Mathf.Sin(Time.unscaledTime * 0.12f));
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
        side.infoSign.localPosition = Vector3.up * (2.9f + Mathf.Sin(Time.unscaledTime * 2.2f + (side.isRed ? 1f : 0f)) * 0.18f);
        // The sign's face turns toward the camera so it reads as a badge, not a flat disc.
        side.infoSign.rotation = cameraRotation * Quaternion.Euler(-90f, 0f, 0f);
    }

    // Surviving to 1991: the silos close and fireworks rise over both capitals.
    void UpdatePeace()
    {
        if (GameManager.Singleton == null || !GameManager.Singleton.HasWon)
            return;

        peaceStarted = true;
        if (Time.unscaledTime < nextFirework)
            return;

        nextFirework = Time.unscaledTime + UnityEngine.Random.Range(0.25f, 0.6f);
        Side side = UnityEngine.Random.value < 0.5f ? blue : red;
        Vector3 spot = side.capital + new Vector3(UnityEngine.Random.Range(-5f, 5f), UnityEngine.Random.Range(3f, 7f), UnityEngine.Random.Range(-3f, 4f));
        Color color = Color.HSVToRGB(UnityEngine.Random.value, 0.65f, 1f);
        fx.Burst(spot, color, 30, 0.3f, 3.4f, 1.1f);
    }

    // Pulses a ring on whatever the current tutorial caption is talking about.
    void UpdateTutorialFocus()
    {
        if (Time.unscaledTime < nextFocusPulse)
            return;

        nextFocusPulse = Time.unscaledTime + 1.1f;
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

        float dt = Time.unscaledDeltaTime;


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
        if (side.factories >= MaxFactoriesShown || art.factories.Length == 0)
            return;

        side.factories++;
        int tile = ClaimHomeTile(side);
        GameObject prefab = art.factories[UnityEngine.Random.Range(0, art.factories.Length)];
        GameObject factory = PlacePrefab(prefab, tile, 1.9f, 1.7f);
        Vector3 fullScale = factory.transform.localScale;
        Vector3 top = board.TileTop(tile);
        float buildY = factory.transform.position.y;
        factory.transform.localScale = new Vector3(fullScale.x, 0.01f, fullScale.z);

        visual.onProgress = progress =>
        {
            float eased = Mathf.Max(0.05f, Mathf.SmoothStep(0f, 1f, progress));
            Vector3 scale = factory.transform.localScale;
            scale.y = Mathf.MoveTowards(scale.y, fullScale.y * eased, fullScale.y * Time.unscaledDeltaTime * 1.5f);
            factory.transform.localScale = scale;
        };
        visual.onDone = () =>
        {
            factory.transform.localScale = fullScale;
            fx.Burst(top + Vector3.up * 0.5f, side.color + Color.white * 0.25f, 18, 0.22f, 2f, 0.7f);
            fx.Smoke(WorldBounds(factory).max - new Vector3(0.15f, 0f, 0.15f), null, 3f, 0.35f, new Color(0.65f, 0.65f, 0.68f, 0.55f));
        };
        visual.onCancel = () => Destroy(factory);
    }

    // Spirals outward from the capital so new buildings don't hide behind the city.
    int ClaimHomeTile(Side side)
    {
        side.homeBuilds++;
        const float goldenAngle = 2.39996f;
        float angle = side.homeBuilds * goldenAngle;
        float distance = 3.4f + side.homeBuilds * 0.55f;
        Vector2 point = Flat(side.capital) + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * distance;
        return ClaimNear(point, i => side.isRed ? board.ThresholdOf(i) < 0.45f : board.ThresholdOf(i) > 0.55f);
    }

    void BuildWarVisual(Side side, ActionVisual visual)
    {
        if (side.wars >= MaxWarsShown)
            return;

        side.wars++;
        int tile = ClaimFront();
        Vector3 target = board.TileTop(tile);
        Vector3 from = side.capital;
        Mesh mesh = side.isRed ? tankRed : tankBlue;

        var column = new GameObject("TankColumn").transform;
        column.SetParent(transform, false);
        for (int i = 0; i < 3; i++)
        {
            GameObject tank = ProcMesh.Spawn("Tank", mesh, art.boardMaterial, column);
            tank.transform.localPosition = new Vector3((i - 1) * 0.72f, 0f, i == 1 ? 0.3f : -0.3f);
            tank.transform.localScale = Vector3.one * 1.9f;
        }

        Vector3 direction = target - from;
        direction.y = 0f;
        column.rotation = Quaternion.LookRotation(direction.sqrMagnitude > 0.01f ? direction.normalized : Vector3.forward);
        column.position = from;

        float shown = 0f;
        visual.onProgress = progress =>
        {
            shown = Mathf.MoveTowards(shown, progress, Time.unscaledDeltaTime * 0.8f);
            Vector3 position = Vector3.Lerp(from, target, shown);
            position.y = Mathf.Max(from.y, target.y) + 0.1f;
            column.position = position;
        };
        visual.onDone = () =>
        {
            column.position = target + Vector3.up * 0.02f;
            warSites.Add(target);
            fx.Explosion(target + Vector3.up * 0.3f, 1f);
            fx.Smoke(target + Vector3.up * 0.1f, null, 7f, 0.75f, new Color(0.1f, 0.09f, 0.09f, 0.75f));
            fx.Flame(target + Vector3.up * 0.1f, null, 16f, 0.42f);
            StartCoroutine(ExpandingRing(target + Vector3.up * 0.15f, side.color, 2.4f));
            shake = Mathf.Max(shake, 0.1f);
        };
        visual.onCancel = () => Destroy(column.gameObject);
    }

    void BuildSpyVisual(Side side, ActionVisual visual)
    {
        if (side.spies >= MaxSpiesShown || art.drone == null)
            return;

        side.spies++;
        Side enemy = Enemy(side);
        Vector3 start = side.capital + Vector3.up * 1.5f;
        Vector3 target = enemy.capital + new Vector3(UnityEngine.Random.Range(-2f, 2f), 2.2f, UnityEngine.Random.Range(-2f, 2f));

        GameObject drone = InstantiateClean(art.drone, start);
        StandUprightFlat(drone, 1.4f);
        TintDrone(drone, side.color);
        GameObject beacon = ProcMesh.Spawn("Beacon", ProcMesh.Ring(ProcMesh.Glow(side.color, 0.9f), 0.28f, 0.07f, 14), art.boardMaterial, drone.transform);
        beacon.transform.localPosition = Vector3.zero;
        beacon.transform.localScale = Vector3.one / Mathf.Max(drone.transform.localScale.x, 0.001f);

        float shown = 0f;
        visual.onProgress = progress =>
        {
            shown = Mathf.MoveTowards(shown, progress, Time.unscaledDeltaTime * 0.6f);
            Vector3 position = Vector3.Lerp(start, target, shown);
            position.y += Mathf.Sin(shown * Mathf.PI) * 2.5f;
            drone.transform.position = position;
        };
        visual.onDone = () =>
        {
            fx.Burst(target, side.color + Color.white * 0.3f, 16, 0.2f, 1.5f, 0.6f);
            roamers.Add(new Roamer
            {
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
    static void TintDrone(GameObject drone, Color color)
    {
        // The camouflage texture is dropped so the team color reads clearly from the board camera.
        Color tint = Color.Lerp(color, Color.white, 0.2f);
        foreach (Renderer part in drone.GetComponentsInChildren<Renderer>())
        {
            if (part.name == "Beacon")
                continue;
            foreach (Material material in part.materials)
            {
                if (material.HasProperty("_BaseMap"))
                    material.SetTexture("_BaseMap", null);
                if (material.HasProperty("_MainTex"))
                    material.SetTexture("_MainTex", null);
                if (material.HasProperty("_BaseColor"))
                    material.SetColor("_BaseColor", tint);
                if (material.HasProperty("_Color"))
                    material.SetColor("_Color", tint);
            }
        }
    }

    class Roamer
    {
        public Transform transform;
        public bool overRed;
        public Vector3 target;
        public float speed;
        public float phase;
    }

    readonly List<Roamer> roamers = new List<Roamer>();
    const float DroneHeight = 2.2f;

    // Spy drones fly from one random spot of the enemy's territory to the next, following the map as borders move.
    void UpdateRoamers()
    {
        float dt = Time.unscaledDeltaTime;
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
            position.y = Mathf.Lerp(position.y, roamer.target.y + Mathf.Sin(Time.unscaledTime * 2f + roamer.phase) * 0.15f, dt * 2f);
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
            if (attempt < 30 && (Flat(top) - Flat(from)).magnitude < 3f)
                continue;
            return top + Vector3.up * DroneHeight;
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

    void BuildResearchVisual(Side side, ActionVisual visual)
    {
        int tile = ClaimHomeTile(side);
        Vector3 pad = board.TileTop(tile);
        GameObject rocket = ProcMesh.Spawn("Rocket", ProcMesh.Rocket(side.color), art.boardMaterial, transform);
        rocket.transform.position = pad;
        rocket.transform.localScale = new Vector3(2.8f, 0.05f, 2.8f);
        GameObject platform = ProcMesh.Spawn("Pad", ProcMesh.Silo(), art.boardMaterial, transform);
        platform.transform.position = pad;
        platform.transform.localScale = new Vector3(1.5f, 1f, 1.5f);

        visual.onProgress = progress =>
        {
            Vector3 scale = rocket.transform.localScale;
            scale.y = Mathf.MoveTowards(scale.y, Mathf.Lerp(0.3f, 3f, progress), Time.unscaledDeltaTime * 2f);
            rocket.transform.localScale = scale;
        };
        visual.onDone = () => StartCoroutine(LaunchRocket(side, rocket));
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
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            rocket.transform.position = start + Vector3.up * (t * t * 26f);
            yield return null;
        }

        Destroy(rocket);
        if (side.satellites >= MaxSatellitesShown)
            yield break;

        side.satellites++;
        GameObject satellite = ProcMesh.Spawn("Satellite", ProcMesh.Satellite(side.color), art.boardMaterial, transform);
        satellite.transform.localScale = Vector3.one * 2.6f;
        orbiters.Add(new Orbiter
        {
            transform = satellite.transform,
            center = side.capital + Vector3.up * (3.6f + side.satellites * 0.5f),
            radius = 2f + side.satellites * 0.65f,
            speed = 0.8f,
            angle = UnityEngine.Random.value * Mathf.PI * 2f,
        });
    }

    void BuildAssassinVisual(Side side, ActionVisual visual)
    {
        Side enemy = Enemy(side);
        GameObject crosshair = ProcMesh.Spawn("Crosshair", ProcMesh.Ring(ProcMesh.Glow(new Color(1f, 0.15f, 0.1f), 0.9f), 1f, 0.09f), art.boardMaterial, transform);
        crosshair.transform.position = enemy.capital + Vector3.up * 0.35f;

        visual.onProgress = _ =>
        {
            float pulse = 1f + 0.18f * Mathf.Sin(Time.unscaledTime * 8f);
            crosshair.transform.localScale = new Vector3(pulse, 1f, pulse);
            crosshair.transform.Rotate(0f, Time.unscaledDeltaTime * 90f, 0f);
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
        GameObject rocket = ProcMesh.Spawn("Strike", ProcMesh.Rocket(color), art.boardMaterial, transform);
        rocket.transform.localScale = Vector3.one * 1.2f;
        fx.Flame(from, rocket.transform, 30f, 0.3f);

        float elapsed = 0f;
        Vector3 previous = from;
        float arc = Vector3.Distance(from, to) * 0.28f;
        while (elapsed < seconds)
        {
            elapsed += Time.unscaledDeltaTime;
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
            elapsed += Time.unscaledDeltaTime;
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

            orbiter.angle += orbiter.speed * Time.unscaledDeltaTime;
            Vector3 offset = new Vector3(Mathf.Cos(orbiter.angle), 0f, Mathf.Sin(orbiter.angle)) * orbiter.radius;
            offset.y = Mathf.Sin(orbiter.angle * 3f) * orbiter.bob;
            orbiter.transform.position = orbiter.center + offset;
            orbiter.transform.rotation = Quaternion.Euler(0f, -orbiter.angle * Mathf.Rad2Deg, 0f);
        }
    }

    void UpdateWarSites()
    {
        if (warSites.Count == 0 || Time.unscaledTime < nextWarBlast)
            return;

        nextWarBlast = Time.unscaledTime + UnityEngine.Random.Range(1.4f, 3.2f);
        Vector3 site = warSites[UnityEngine.Random.Range(0, warSites.Count)];
        fx.Burst(site + Vector3.up * 0.2f, new Color(1f, 0.6f, 0.2f), 6, 0.4f, 1.1f, 0.35f);
    }

    void UpdateDanger()
    {
        float danger = Mathf.Clamp01(NukeMeterUpdater.nukeDanger / 100f);
        if (peaceStarted)
            danger = 0f;
        FillPool(blue, danger);
        FillPool(red, danger);
        poolLabel.text = Mathf.RoundToInt(danger * 100f) + "%";
        poolLabel.transform.rotation = cameraRotation;
        poolLabel.color = Color.Lerp(new Color(0.75f, 0.85f, 1f), new Color(1f, 0.3f, 0.2f), danger);

        float alarm = Mathf.InverseLerp(0.4f, 1f, danger);
        grading.saturation.Override(Mathf.Lerp(0f, -28f, alarm));
        grading.colorFilter.Override(Color.Lerp(Color.white, new Color(1f, 0.62f, 0.55f), alarm));
        vignette.color.Override(Color.Lerp(Color.black, new Color(0.45f, 0f, 0f), alarm));
        float heartbeat = alarm > 0.5f ? Mathf.Abs(Mathf.Sin(Time.unscaledTime * Mathf.Lerp(2f, 5f, alarm))) * 0.1f : 0f;
        vignette.intensity.Override(Mathf.Lerp(0.28f, 0.52f, alarm) + heartbeat);

        if (danger > 0.9f)
            shake = Mathf.Max(shake, 0.03f);

        if (!endOfWorld && CountryManager.LoseLock)
        {
            endOfWorld = true;
            StartCoroutine(EndOfTheWorld());
        }
    }

    void FillPool(Side side, float danger)
    {
        if (endOfWorld)
            return;

        // Even 1% danger shows a nuke, so any risk at all is visible.
        int shown = danger >= 0.005f ? Mathf.Max(1, Mathf.RoundToInt(danger * NukesPerSide)) : 0;
        float dt = Time.unscaledDeltaTime;
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
        for (int i = 0; i < NukesPerSide; i++)
        {
            blue.missiles[i].localScale = blue.missileScales[i];
            red.missiles[i].localScale = red.missileScales[i];
            StartCoroutine(FlyMissile(blue.missiles[i], red.capital + Random2D(3.5f)));
            StartCoroutine(FlyMissile(red.missiles[i], blue.capital + Random2D(3.5f)));
            yield return new WaitForSecondsRealtime(0.12f);
        }

        yield return new WaitForSecondsRealtime(1.1f);
        shake = 0.7f;
        float radius = 0f;
        while (radius < 70f)
        {
            radius += Time.unscaledDeltaTime * 13f;
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
            elapsed += Time.unscaledDeltaTime;
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
            offset = new Vector3(Mathf.Sin(Time.unscaledTime * 0.1f) * 1.2f, 0f, Mathf.Cos(Time.unscaledTime * 0.08f) * 0.5f);
        }

        if (shake > 0.001f)
        {
            offset += UnityEngine.Random.insideUnitSphere * shake;
            shake = Mathf.MoveTowards(shake, 0f, Time.unscaledDeltaTime * 0.6f);
        }

        mainCamera.transform.SetPositionAndRotation(cameraHome + offset, cameraRotation);
    }
}
