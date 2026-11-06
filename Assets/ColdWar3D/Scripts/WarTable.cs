using System;
using System.Collections;
using System.Collections.Generic;
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
    const int SilosPerSide = 5;
    const float TileWidth = 0.86f;

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
        public int siloTile;
        public Transform power;
        public Transform coins;
        public GameObject[] flags;
        public readonly List<Transform> missiles = new List<Transform>();
        public readonly List<Vector3> missileHomes = new List<Vector3>();
        public readonly Dictionary<ActionFunction, ActionVisual> actions = new Dictionary<ActionFunction, ActionVisual>();
        public President president;
        public ParticleSystem protest;
        public int factories;
        public int wars;
        public int spies;
        public int satellites;
        public int homeBuilds;
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
    RectTransform capStar;
    RectTransform capInfo;
    RectTransform comStar;
    RectTransform comInfo;
    Image actionPanel;
    RectTransform[] actionButtons;

    Mesh tankBlue;
    Mesh tankRed;

    bool IsMenu => country == null;
    public static bool IsRunning { get; private set; }

    WarTableUi ui;
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

        HideFlatMap();
        FrameCamera();

        if (!IsMenu)
        {
            IsRunning = true;
            ui = gameObject.AddComponent<WarTableUi>();
            ui.enabled = false;
            CountryManager.Refused += OnRefused;
        }
    }

    void Start()
    {
        // Waits for CountryManager.Start, which sets up the parties and hides the tutorial HUD.
        if (ui != null)
        {
            ui.Begin(country);
            ui.enabled = true;
        }
    }

    void OnDestroy()
    {
        IsRunning = false;
        CountryManager.Refused -= OnRefused;
    }

    void OnRefused(Party party, Refusal reason)
    {
        Side side = party == country.Communism ? red : blue;
        Color warning = new Color(1f, 0.2f, 0.12f);
        switch (reason)
        {
            case Refusal.Power: StartCoroutine(ExpandingRing(side.power.position, warning, 1.8f)); break;
            case Refusal.Money: StartCoroutine(ExpandingRing(side.coins.position, warning, 1.8f)); break;
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

        capStar = map.Find("CapCenter") as RectTransform;
        capInfo = map.Find("CapCenterInfo") as RectTransform;
        comStar = map.Find("Com/ComCenter") as RectTransform;
        comInfo = map.Find("Com/ComCenterInfo") as RectTransform;
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

        for (int i = 0; i < city.Length; i++)
        {
            int tile = i == 0 ? side.capitalTile : ClaimNear(capitalPoint, _ => true);
            GameObject building = PlacePrefab(city[i], tile, i == 0 ? 2.1f : 1.5f, i == 0 ? 3.2f : 2.2f);
            building.name = "City";
        }

        // Stat pieces stand on tiles next to the capital.
        Vector3 powerSpot = board.TileTop(ClaimNear(capitalPoint + new Vector2(-1.6f, -0.6f), _ => true));
        Vector3 coinSpot = board.TileTop(ClaimNear(capitalPoint + new Vector2(1.6f, -0.6f), _ => true));
        Color glowing = ProcMesh.Glow(side.color, 0.75f);

        GameObject column = ProcMesh.Spawn("PowerTower", ProcMesh.Column(glowing, 6), art.boardMaterial, transform);
        column.transform.position = powerSpot;
        column.transform.localScale = new Vector3(0.72f, 0.5f, 0.72f);
        side.power = column.transform;

        GameObject coinStack = ProcMesh.Spawn("Coins", ProcMesh.Coins(), art.boardMaterial, transform);
        coinStack.transform.position = coinSpot;
        coinStack.transform.localScale = new Vector3(0.82f, 0.2f, 0.82f);
        side.coins = coinStack.transform;

        Mesh flagMesh = ProcMesh.Flag(side.color);
        side.flags = new GameObject[5];
        for (int i = 0; i < side.flags.Length; i++)
        {
            GameObject flag = ProcMesh.Spawn("Flag", flagMesh, art.boardMaterial, transform);
            float angle = i * Mathf.PI * 2f / side.flags.Length;
            flag.transform.position = side.capital + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * 1.25f;
            flag.transform.localScale = Vector3.one * 2f;
            side.flags[i] = flag;
        }

        BuildSilos(side, siloPoint);
        return side;
    }

    void BuildSilos(Side side, Vector2 site)
    {
        side.siloTile = board.NearestTile(site);
        Mesh siloMesh = ProcMesh.Silo();
        for (int i = 0; i < SilosPerSide; i++)
        {
            int tile = ClaimNear(site, _ => true);
            Vector3 top = board.TileTop(tile);
            GameObject silo = ProcMesh.Spawn("Silo", siloMesh, art.boardMaterial, transform);
            silo.transform.position = top;

            Transform missile = art.missile != null
                ? InstantiateClean(art.missile, top).transform
                : ProcMesh.Spawn("Missile", ProcMesh.Rocket(side.color), art.boardMaterial, transform).transform;
            StandUpright(missile.gameObject, MissileBuried);
            missile.position = top + Vector3.down * MissileBuried;
            side.missiles.Add(missile);
            side.missileHomes.Add(top);
        }
    }

    const float MissileBuried = 2.2f;

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
        UpdateCamera();

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
            case 8:
            case 10:
                StartCoroutine(ExpandingRing(blue.power.position, color, 1.6f));
                StartCoroutine(ExpandingRing(red.power.position, color, 1.6f));
                break;
            case 11:
                StartCoroutine(ExpandingRing(board.TileTop(blue.siloTile), new Color(1f, 0.3f, 0.2f), 3f));
                StartCoroutine(ExpandingRing(board.TileTop(red.siloTile), new Color(1f, 0.3f, 0.2f), 3f));
                break;
        }
    }

    void LateUpdate()
    {
        if (IsMenu)
            return;

        // The buttons follow the capitals, so they stay put on the 3D map on any screen shape.
        AnchorToWorld(capStar, blue.capital, 0f, 0f, 0.15f);
        AnchorToWorld(capInfo, blue.capital, 0f, 0f, -0.1f);
        AnchorToWorld(comStar, red.capital, 0f, 0f, 0.15f);
        AnchorToWorld(comInfo, red.capital, 0f, 0f, -0.1f);
        LayoutActionRing();
    }

    // Shows the action buttons as a ring around the selected capital, leaving the map visible.
    void LayoutActionRing()
    {
        GameObject menu = country.ActionMenu;
        if (menu == null || !menu.activeInHierarchy)
            return;

        if (actionPanel == null)
        {
            actionPanel = menu.GetComponent<Image>();
            actionPanel.enabled = false;
            var buttons = new List<RectTransform>();
            foreach (HoverInfo action in menu.GetComponentsInChildren<HoverInfo>(true))
                buttons.Add((RectTransform)action.transform);
            foreach (Transform child in menu.transform)
            {
                if (child.GetComponent<Button>() != null && !buttons.Contains((RectTransform)child))
                    buttons.Add((RectTransform)child);
            }

            actionButtons = buttons.ToArray();
        }

        Side side = CountryManager.SelectedParty == country.Communism ? red : blue;
        Vector3 center = mainCamera.WorldToScreenPoint(side.capital + Vector3.up * 0.8f);
        float radius = Screen.height * 0.2f;
        float margin = Screen.height * 0.075f;
        float buttonScale = Screen.height * 0.13f;

        // Keeps the whole ring on screen when a capital sits near an edge.
        center.x = Mathf.Clamp(center.x, radius + margin, Screen.width - radius - margin);
        center.y = Mathf.Clamp(center.y, radius + margin, Screen.height - radius - margin);

        for (int i = 0; i < actionButtons.Length; i++)
        {
            RectTransform button = actionButtons[i];
            float angle = Mathf.PI * 0.5f - i * Mathf.PI * 2f / actionButtons.Length;
            button.position = new Vector3(center.x + Mathf.Cos(angle) * radius, center.y + Mathf.Sin(angle) * radius, 0f);

            float width = button.rect.width * button.lossyScale.x / Mathf.Max(button.localScale.x, 0.0001f);
            if (width > 0.001f)
                button.localScale = Vector3.one * (buttonScale / width);
        }
    }

    void AnchorToWorld(RectTransform target, Vector3 world, float lift, float offsetX, float offsetY)
    {
        if (target == null)
            return;

        Vector3 screen = mainCamera.WorldToScreenPoint(world + Vector3.up * lift);
        screen.x += offsetX * Screen.height;
        screen.y += offsetY * Screen.height;
        screen.z = 0f;
        target.position = screen;
    }

    void UpdateSide(Side side, Party party)
    {
        if (party == null)
            return;

        float dt = Time.unscaledDeltaTime;

        // A square root keeps a gap readable at low power without huge towers late in the game.
        float powerHeight = 0.25f + 5.2f * Mathf.Sqrt(Mathf.Max(0, party.Power) / 8000f);
        ScaleY(side.power, Mathf.Min(powerHeight, 8f), dt);

        float magnitude = Mathf.Log10(Mathf.Max(party.Money, 1000000f));
        ScaleY(side.coins, Mathf.Lerp(0.05f, 1.6f, Mathf.InverseLerp(6f, 12f, magnitude)), dt);

        int flagsUp = Mathf.CeilToInt(Mathf.Clamp(party.PeoplSatsfaction, 0, 100) / 20f);
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

    static void ScaleY(Transform target, float height, float dt)
    {
        Vector3 scale = target.localScale;
        scale.y = Mathf.Lerp(scale.y, height, 1f - Mathf.Exp(-5f * dt));
        target.localScale = scale;
    }

    void UpdateProtest(Side side, Party party)
    {
        bool unrest = party.PeoplSatsfaction < 15;
        if (unrest && side.protest == null)
            side.protest = fx.Flame(side.capital + new Vector3(0.45f, 0.1f, -0.45f), null, 22f, 0.5f);
        else if (!unrest && side.protest != null)
        {
            Destroy(side.protest.gameObject);
            side.protest = null;
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
            orbiters.Add(new Orbiter
            {
                transform = drone.transform,
                center = enemy.capital + Vector3.up * 2.2f,
                radius = UnityEngine.Random.Range(1.3f, 2.4f),
                speed = UnityEngine.Random.Range(0.5f, 0.9f),
                angle = UnityEngine.Random.value * Mathf.PI * 2f,
                bob = 0.12f,
            });
        };
        visual.onCancel = () => Destroy(drone);
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
        float rise = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.15f, 0.9f, danger));
        RaiseMissiles(blue, rise);
        RaiseMissiles(red, rise);

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

    void RaiseMissiles(Side side, float rise)
    {
        if (endOfWorld)
            return;

        for (int i = 0; i < side.missiles.Count; i++)
        {
            // Each silo opens a little after the last, so the threat visibly builds.
            float stagger = Mathf.Clamp01(rise * side.missiles.Count - i);
            Vector3 home = side.missileHomes[i];
            Vector3 target = home + Vector3.down * Mathf.Lerp(MissileBuried, 0f, stagger);
            Transform missile = side.missiles[i];
            missile.position = Vector3.MoveTowards(missile.position, target, Time.unscaledDeltaTime * 1.5f);
        }
    }

    IEnumerator EndOfTheWorld()
    {
        for (int i = 0; i < SilosPerSide; i++)
        {
            StartCoroutine(FlyMissile(blue.missiles[i], red.capital + Random2D(3.5f)));
            StartCoroutine(FlyMissile(red.missiles[i], blue.capital + Random2D(3.5f)));
            yield return new WaitForSecondsRealtime(0.28f);
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
