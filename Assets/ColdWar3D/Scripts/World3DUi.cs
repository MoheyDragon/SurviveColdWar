using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// Builds the game's interface as 3D objects attached to the camera. The original 2D interface stays alive
// but invisible, because the game logic and the tutorial still read from and control it.
public class World3DUi : MonoBehaviour
{
    const float Depth = 40f;
    // The whole heads-up display is drawn this much bigger than its original design, for phone screens.
    const float UiScale = 1.2f;
    const float RingRadius = 3.5f * UiScale;
    const float RingTokenSize = 1.2f * UiScale;
    // The speed-up button is larger again and lengthened sideways, so it is easy to hit.
    const float SpeedScale = 1.2f;
    const float SpeedStretch = 0.9f;
    const int SlotCount = 3;
    static readonly Quaternion FaceCamera = Quaternion.Euler(-78f, 0f, 0f);
    static readonly Color Slate = new Color(0.10f, 0.12f, 0.17f);
    static readonly Color SlateRim = new Color(0.05f, 0.06f, 0.09f);
    static readonly Color Gain = new Color(0.45f, 1f, 0.5f);
    static readonly Color Loss = new Color(1f, 0.42f, 0.36f);
    static readonly Color Gold = new Color(0.95f, 0.72f, 0.22f);
    static readonly Color Cream = new Color(1f, 0.96f, 0.86f);
    static readonly Color WindowColor = new Color(0.13f, 0.17f, 0.29f, 0.96f);
    const float TermBarLength = 1.95f;

    class Plaque
    {
        public Transform root;
        public TextMeshPro label;
        public float shake;
    }

    class Slot
    {
        public GameObject root;
        public MeshRenderer icon;
        public MeshFilter iconFilter;
        public TextMeshPro months;
    }

    class SideHud
    {
        public Party party;
        public bool isRed;
        public Color color;
        public Plaque power;
        public Plaque money;
        public Plaque people;
        public Slot[] slots;
        public Transform queue;
        public Plaque queueShake;
        public Plaque term;
        public Transform termFill;
        public MeshRenderer termRenderer;
        public MeshRenderer termMarker;
        public Token3D capital;
        public Token3D infoCenter;
    }

    class RingToken
    {
        public Token3D token;
        public HoverInfo action;
        public Image source;
        public MeshRenderer body;
        public MeshRenderer icon;
        public GameObject badge;
        public MeshRenderer badgeIcon;
        public MeshFilter badgeFilter;
    }

    class DeckToken
    {
        public Token3D token;
        public Image spriteSource;
        public MeshFilter iconFilter;
        public MeshRenderer iconRenderer;
        public MeshRenderer body;
        public Sprite shown;
        // How much room the button takes along the bottom bar.
        public float width = 2.1f;
    }

    class CandidateCard
    {
        public GameObject root;
        public Transform row;
        public Token3D token;
        public MeshRenderer body;
        public TextMeshPro name;
        public TextMeshPro power;
        public TextMeshPro money;
        public TextMeshPro people;
        public MeshRenderer[] agenda;
        public MeshFilter[] agendaFilters;
        public MeshRenderer[] lateAgenda;
        public MeshFilter[] lateAgendaFilters;
    }

    WarTable table;
    CountryManager country;
    BoardArt art;
    Camera cam;
    Transform rig;
    TMP_FontAsset font;
    MaterialPropertyBlock block;
    readonly Dictionary<Sprite, Mesh> iconMeshes = new Dictionary<Sprite, Mesh>();
    readonly Dictionary<ActionFunction, int> longestTimes = new Dictionary<ActionFunction, int>();
    readonly Dictionary<string, Sprite> actionSprites = new Dictionary<string, Sprite>();
    readonly List<RingToken> ring = new List<RingToken>();
    readonly List<DeckToken> deck = new List<DeckToken>();
    readonly List<Plaque> shaking = new List<Plaque>();
    readonly List<Transform> arrows = new List<Transform>();

    SideHud blue;
    SideHud red;
    Plaque date;
    TextMeshPro dateLabel;
    Text monthSource;
    Text yearSource;
    DeckToken speedToken;
    Transform exitButton;

    Transform card;
    MeshRenderer cardIcon;
    MeshFilter cardIconFilter;
    TextMeshPro costPower;
    TextMeshPro costMoney;
    TextMeshPro costPeople;
    TextMeshPro gainPower;
    TextMeshPro gainMoney;
    TextMeshPro gainPeople;
    TextMeshPro cardMonths;
    GameObject repeatGlyph;
    GameObject onceGlyph;

    Transform electionRoot;
    readonly List<CandidateCard> candidates = new List<CandidateCard>();
    CanvasGroup infoGroup;
    float ringOpen;
    Party ringParty;

    float HalfHeight => Depth * Mathf.Tan(cam.fieldOfView * 0.5f * Mathf.Deg2Rad);
    float HalfWidth => HalfHeight * cam.aspect;

    public void Begin(WarTable warTable, CountryManager countryManager, BoardArt boardArt, Camera camera)
    {
        table = warTable;
        country = countryManager;
        art = boardArt;
        cam = camera;
        font = TMP_Settings.defaultFontAsset;
        block = new MaterialPropertyBlock();

        rig = new GameObject("Ui3D").transform;
        rig.SetParent(cam.transform, false);
        if (cam.GetComponent<PhysicsRaycaster>() == null)
            cam.gameObject.AddComponent<PhysicsRaycaster>();

        // Colliders normally update on physics ticks, which barely run while the game sets timeScale near 0.
        Physics.autoSyncTransforms = true;

        CollectSprites();
        blue = BuildSideHud(country.Capitalism, false, HexBoard.BlueTop);
        red = BuildSideHud(country.Communism, true, HexBoard.RedTop);
        BuildDate();
        BuildDeck();
        BuildRing();
        BuildCard();
        BuildElections();
        BuildReport();
        BuildArrows();
        BuildNewsBanner();
        BuildWorldClicks();
        HideFlatInterface();
        CompactCaption();
        ShortenEndMessages();
        StyleWindows();

        CountryManager.Refused += OnRefused;
        CountryManager.PresidentOusted += OnOusted;
        CountryManager.ChaosStruck += OnChaos;
        CountryManager.CrisisMonthsLeft = 0;
    }

    void OnDestroy()
    {
        CountryManager.Refused -= OnRefused;
        CountryManager.PresidentOusted -= OnOusted;
        CountryManager.ChaosStruck -= OnChaos;
    }

    // ----- Chaos banner -----

    struct ChaosNews
    {
        public string english;
        public string arabic;
        public Color color;
    }

    const float NewsSeconds = 3.5f;
    readonly Queue<ChaosNews> news = new Queue<ChaosNews>();
    RectTransform newsBanner;
    Text newsText;
    CanvasGroup newsGroup;
    float newsTimer;

    void OnChaos(Party party, ChaosKind kind, string english, string arabic)
    {
        switch (kind)
        {
            case ChaosKind.EconomicCrash: Shake(Hud(party).money); break;
            case ChaosKind.ArmsRace: Shake(blue.power); Shake(red.power); break;
            case ChaosKind.Scandal: Shake(Hud(party).people); break;
            case ChaosKind.SpyCaught:
            case ChaosKind.WarLost: Shake(Hud(party).power); break;
            case ChaosKind.FactoryDisaster: Shake(Hud(party).money); break;
            case ChaosKind.Coup:
            case ChaosKind.Uprising:
            case ChaosKind.NewPresident: Shake(Hud(party).term); break;
        }

        // One-sided news in that side's color; news for both sides in dark red.
        Color color = party == null ? new Color(0.55f, 0.12f, 0.08f) : party == country.Communism ? HexBoard.RedTop : HexBoard.BlueTop;
        news.Enqueue(new ChaosNews { english = english, arabic = arabic, color = color * 0.75f + Color.black * 0.25f });
    }

    SideHud Hud(Party party) => party == country.Communism ? red : blue;

    void BuildNewsBanner()
    {
        var banner = new GameObject("ChaosNews", typeof(RectTransform));
        newsBanner = (RectTransform)banner.transform;
        newsBanner.SetParent(country.transform, false);
        newsBanner.anchorMin = newsBanner.anchorMax = new Vector2(0.5f, 1f);
        newsBanner.pivot = new Vector2(0.5f, 1f);
        newsBanner.anchoredPosition = new Vector2(0f, -10f);
        newsBanner.sizeDelta = new Vector2(560f, 54f);
        var image = banner.AddComponent<Image>();
        image.sprite = Rounded;
        image.type = Image.Type.Sliced;
        image.raycastTarget = false;
        var edge = banner.AddComponent<Outline>();
        edge.effectColor = Gold;
        edge.effectDistance = new Vector2(2.5f, -2.5f);
        newsGroup = banner.AddComponent<CanvasGroup>();
        newsGroup.blocksRaycasts = false;
        newsGroup.interactable = false;

        var label = new GameObject("Text", typeof(RectTransform));
        var rect = (RectTransform)label.transform;
        rect.SetParent(newsBanner, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = new Vector2(16f, 4f);
        rect.offsetMax = new Vector2(-16f, -4f);
        newsText = label.AddComponent<Text>();
        newsText.font = monthSource != null ? monthSource.font : Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        newsText.alignment = TextAnchor.MiddleCenter;
        newsText.resizeTextForBestFit = true;
        newsText.resizeTextMinSize = 12;
        newsText.resizeTextMaxSize = 24;
        newsText.color = Cream;
        newsText.raycastTarget = false;
        label.AddComponent<Shadow>().effectDistance = new Vector2(1.5f, -1.5f);
        banner.SetActive(false);
    }

    void UpdateNewsBanner()
    {
        if (newsBanner == null)
            return;

        if (newsTimer <= 0f)
        {
            if (news.Count == 0)
            {
                SetActive(newsBanner, false);
                return;
            }

            ChaosNews next = news.Dequeue();
            newsTimer = NewsSeconds;
            newsBanner.GetComponent<Image>().color = new Color(next.color.r, next.color.g, next.color.b, 0.96f);
            var fixer = newsText.GetComponent<ArabicFixer>();
            if (LanguageManager.Singlton != null && LanguageManager.Singlton.GetSelectedLanguag == Language.Arabic)
            {
                newsText.text = next.arabic;
                if (fixer == null)
                    fixer = newsText.gameObject.AddComponent<ArabicFixer>();
                fixer.fixedText = next.arabic;
            }
            else
            {
                if (fixer != null)
                    Destroy(fixer);
                newsText.text = next.english;
            }
            SetActive(newsBanner, true);
        }

        newsTimer -= Time.unscaledDeltaTime;
        // Fades in and out over a quarter of a second at each end.
        newsGroup.alpha = Mathf.Clamp01(Mathf.Min(NewsSeconds - newsTimer, newsTimer) * 4f);
    }

    // Unhappy people forced the president out: the happiness plaque and the term bar shake as the election opens.
    void OnOusted(Party party)
    {
        SideHud hud = party == country.Communism ? red : blue;
        Shake(hud.people);
        Shake(hud.term);
    }

    void CollectSprites()
    {
        foreach (Transform child in country.ActionMenu.transform)
        {
            var image = child.GetComponent<Image>();
            if (image == null)
                continue;
            var action = child.GetComponent<HoverInfo>();
            actionSprites[action != null ? action.name : "Crown"] = image.sprite;
        }
    }

    // ----- Building blocks -----

    Mesh IconMesh(Sprite sprite)
    {
        if (iconMeshes.TryGetValue(sprite, out Mesh mesh))
            return mesh;

        Rect r = sprite.textureRect;
        float w = sprite.texture.width;
        float h = sprite.texture.height;
        Vector2 min = new Vector2(r.xMin / w, r.yMin / h);
        Vector2 max = new Vector2(r.xMax / w, r.yMax / h);
        mesh = new Mesh { name = sprite.name };
        mesh.vertices = new[] { new Vector3(-0.5f, 0f, -0.5f), new Vector3(-0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, 0.5f), new Vector3(0.5f, 0f, -0.5f) };
        mesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up, Vector3.up };
        mesh.uv = new[] { min, new Vector2(min.x, max.y), max, new Vector2(max.x, min.y) };
        mesh.triangles = new[] { 0, 1, 2, 0, 2, 3 };
        iconMeshes[sprite] = mesh;
        return mesh;
    }

    MeshRenderer AddIcon(Transform parent, Vector3 position, float size, Sprite sprite, out MeshFilter filter)
    {
        var go = new GameObject("Icon");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = new Vector3(size, 1f, size);
        filter = go.AddComponent<MeshFilter>();
        var meshRenderer = go.AddComponent<MeshRenderer>();
        meshRenderer.sharedMaterial = art.iconMaterial;
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
        SetIcon(meshRenderer, filter, sprite, Color.white);
        return meshRenderer;
    }

    void SetIcon(MeshRenderer meshRenderer, MeshFilter filter, Sprite sprite, Color tint)
    {
        if (sprite == null)
        {
            meshRenderer.enabled = false;
            return;
        }

        meshRenderer.enabled = true;
        filter.sharedMesh = IconMesh(sprite);
        meshRenderer.GetPropertyBlock(block);
        block.SetTexture("_BaseMap", sprite.texture);
        block.SetColor("_BaseColor", tint);
        meshRenderer.SetPropertyBlock(block);
    }

    void Tint(MeshRenderer meshRenderer, string property, Color color)
    {
        meshRenderer.GetPropertyBlock(block);
        block.SetColor(property, color);
        meshRenderer.SetPropertyBlock(block);
    }

    TextMeshPro AddLabel(Transform parent, Vector3 position, float fontSize, Color color, TextAlignmentOptions alignment, float width)
    {
        var go = new GameObject("Label");
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
        var label = go.AddComponent<TextMeshPro>();
        label.font = font;
        label.fontSize = fontSize;
        label.fontStyle = FontStyles.Bold;
        label.color = color;
        label.alignment = alignment;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.rectTransform.sizeDelta = new Vector2(width, fontSize * 0.2f);
        // Long values shrink to fit their space instead of running into the icons beside them.
        label.enableAutoSizing = true;
        label.fontSizeMax = fontSize;
        label.fontSizeMin = fontSize * 0.35f;
        label.outlineWidth = 0.18f;
        label.outlineColor = new Color32(0, 0, 0, 200);
        return label;
    }

    static void Unshadowed(GameObject go)
    {
        var meshRenderer = go.GetComponent<MeshRenderer>();
        meshRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        meshRenderer.receiveShadows = false;
    }

    GameObject AddMesh(string objectName, Mesh mesh, Transform parent)
    {
        GameObject go = ProcMesh.Spawn(objectName, mesh, art.boardMaterial, parent);
        Unshadowed(go);
        return go;
    }

    Transform AddRigObject(string objectName, Transform parent = null)
    {
        var go = new GameObject(objectName).transform;
        go.SetParent(parent != null ? parent : rig, false);
        go.localRotation = FaceCamera;
        return go;
    }

    Plaque BuildPlaque(string objectName, Transform parent, float width, float height, Color rim, Sprite sprite)
    {
        Transform root = AddRigObject(objectName, parent);
        Mesh mesh = new ProcMesh()
            .Box(new Vector3(0f, -0.1f, 0f), new Vector3(width, 0.2f, height), rim)
            .Box(new Vector3(0f, 0.02f, 0f), new Vector3(width - 0.12f, 0.06f, height - 0.12f), Slate)
            .ToMesh(objectName);
        AddMesh(objectName, mesh, root);

        float iconSize = height * 0.72f;
        if (sprite != null)
            AddIcon(root, new Vector3(-width * 0.5f + iconSize * 0.62f, 0.07f, 0f), iconSize, sprite, out _);

        float textStart = sprite != null ? iconSize * 0.62f : 0f;
        float textWidth = width - textStart * 2f - 0.2f;
        TextMeshPro label = AddLabel(root, new Vector3(textStart, 0.08f, 0f), height * 4.6f, Color.white, TextAlignmentOptions.Center, textWidth);
        label.enableAutoSizing = true;
        label.fontSizeMin = 1.5f;
        label.fontSizeMax = height * 4.6f;
        label.rectTransform.sizeDelta = new Vector2(textWidth, height);
        root.localScale = Vector3.one * UiScale;
        return new Plaque { root = root, label = label };
    }

    Token3D BuildToken(string objectName, Transform parent, Color rim, Color face, GameObject target, out MeshRenderer body)
    {
        Transform root = AddRigObject(objectName, parent);
        GameObject mesh = AddMesh("Body", ProcMesh.Token(rim, face), root);
        body = mesh.GetComponent<MeshRenderer>();
        var collider = root.gameObject.AddComponent<BoxCollider>();
        collider.size = new Vector3(2f, 0.4f, 1.9f);
        var token = root.gameObject.AddComponent<Token3D>();
        token.target = target;
        return token;
    }

    // ----- Stats, queue and presidential term -----

    SideHud BuildSideHud(Party party, bool isRed, Color color)
    {
        var hud = new SideHud { party = party, isRed = isRed, color = color, slots = new Slot[SlotCount] };
        Color rim = color * 0.7f;
        rim.a = 1f;
        hud.power = BuildPlaque("Power", null, 2.5f, 1.05f, rim, party.PowerUI.transform.parent.GetComponent<Image>().sprite);
        hud.money = BuildPlaque("Money", null, 2.5f, 1.05f, rim, party.MoneyUI.transform.parent.GetComponent<Image>().sprite);
        hud.people = BuildPlaque("Happiness", null, 2.5f, 1.05f, rim, party.PeoplSatsfactionUI.transform.parent.GetComponent<Image>().sprite);

        hud.queue = AddRigObject("Queue");
        hud.queue.localScale = Vector3.one * UiScale;
        hud.queueShake = new Plaque { root = hud.queue };
        for (int i = 0; i < SlotCount; i++)
            hud.slots[i] = BuildSlot(hud.queue, i, rim);

        actionSprites.TryGetValue("Crown", out Sprite crown);
        hud.term = BuildPlaque("Term", null, 2.5f, 0.55f, rim, crown);
        hud.term.label.gameObject.SetActive(false);
        Mesh barMesh = new ProcMesh().Box(new Vector3(0.5f, 0.08f, 0f), new Vector3(1f, 0.03f, 0.22f), Color.white).ToMesh("TermFill");
        GameObject bar = AddMesh("Fill", barMesh, hud.term.root);
        bar.transform.localPosition = new Vector3(-0.8f, 0f, 0f);
        hud.termFill = bar.transform;
        hud.termRenderer = bar.GetComponent<MeshRenderer>();

        // The halfway notch: when the bar reaches it, the president's six gold actions happen.
        Mesh markerMesh = new ProcMesh().Box(Vector3.zero, new Vector3(0.1f, 0.06f, 0.46f), Color.white).ToMesh("TermMarker");
        GameObject marker = AddMesh("HalfTerm", markerMesh, hud.term.root);
        marker.transform.localPosition = new Vector3(-0.8f + TermBarLength * 0.5f, 0.12f, 0f);
        hud.termMarker = marker.GetComponent<MeshRenderer>();
        return hud;
    }

    Slot BuildSlot(Transform parent, int index, Color rim)
    {
        var root = new GameObject("Slot").transform;
        root.SetParent(parent, false);
        root.localPosition = new Vector3(index * 1.35f, 0f, 0f);
        AddMesh("Body", ProcMesh.Token(rim, Slate), root).transform.localScale = Vector3.one * 0.62f;
        MeshRenderer icon = AddIcon(root, new Vector3(0f, 0.14f, 0f), 0.92f, null, out MeshFilter filter);
        TextMeshPro months = AddLabel(root, new Vector3(0f, 0.2f, -0.02f), 4.4f, Color.white, TextAlignmentOptions.Center, 1.2f);
        return new Slot { root = root.gameObject, icon = icon, iconFilter = filter, months = months };
    }

    void BuildDate()
    {
        monthSource = country.transform.Find("Month")?.GetComponent<Text>();
        yearSource = country.transform.Find("Date")?.GetComponent<Text>();
        date = BuildPlaque("Date", null, 3.5f, 1.05f, SlateRim, null);
        dateLabel = date.label;
    }

    // ----- Bottom controls -----

    void BuildDeck()
    {
        speedToken = AddDeckToken("SpeedUp", "speed", new Color(0.2f, 0.65f, 0.95f));
        AddDeckToken("Pause", "pause", new Color(0.95f, 0.8f, 0.3f));
        AddDeckToken("Exit", "exit", new Color(0.9f, 0.9f, 0.92f));
        AddDeckToken("Music", null, Color.white);
        AddDeckToken("SFX", null, Color.white);

        // Language can only change during the tutorial; the main game leaves it to the main menu.
        if (country.IsTutorial)
        {
            foreach (Transform child in country.transform)
            {
                if (child.CompareTag("Language"))
                    AddDeckToken(child.name, null, Color.white);
            }

            // The tutorial can be left at any moment, even while a voice line plays.
            exitButton = country.transform.Find("Exit");
            if (exitButton != null)
            {
                exitButton.gameObject.SetActive(true);
                foreach (DeckToken entry in deck)
                {
                    if (entry.token.target == exitButton.gameObject)
                        entry.token.ignoresVoiceLine = true;
                }
            }
        }

    }

    DeckToken AddDeckToken(string targetName, string glyph, Color color)
    {
        Transform target = country.transform.Find(targetName);
        if (target == null)
        {
            Debug.LogWarning("[World3DUi] No button named " + targetName);
            return null;
        }

        Token3D token = BuildToken(targetName, null, SlateRim, Slate, target.gameObject, out MeshRenderer body);
        var entry = new DeckToken { token = token, body = body, width = 2.1f * UiScale };
        token.restScale = Vector3.one * UiScale;
        if (glyph == "speed")
        {
            // A wide speed-up button: the lengthened hex body, a bigger symbol and a wider hit area.
            body.GetComponent<MeshFilter>().sharedMesh = ProcMesh.WideToken(SlateRim, Slate, SpeedStretch);
            token.GetComponent<BoxCollider>().size = new Vector3(2f + SpeedStretch * 2f, 0.4f, 1.9f);
            token.restScale = Vector3.one * (UiScale * SpeedScale);
            entry.width = (2.1f + SpeedStretch * 2f) * UiScale * SpeedScale;
            AddMesh("Glyph", ProcMesh.Glyph(glyph, color), token.transform).transform.localScale = Vector3.one * 1.25f;
        }
        else if (glyph != null)
        {
            AddMesh("Glyph", ProcMesh.Glyph(glyph, color), token.transform);
        }
        else
        {
            entry.spriteSource = target.GetComponent<Image>();
            entry.iconRenderer = AddIcon(token.transform, new Vector3(0f, 0.2f, 0f), 1.5f, entry.spriteSource.sprite, out entry.iconFilter);
            entry.shown = entry.spriteSource.sprite;
        }

        deck.Add(entry);
        return entry;
    }

    // ----- Action ring -----

    void BuildRing()
    {
        foreach (Transform child in country.ActionMenu.transform)
        {
            var source = child.GetComponent<Image>();
            if (source == null || child.GetComponent<Button>() == null)
                continue;

            Token3D token = BuildToken(child.name, null, SlateRim, new Color(0.86f, 0.87f, 0.9f), child.gameObject, out MeshRenderer body);
            token.restScale = Vector3.one * RingTokenSize;
            MeshRenderer icon = AddIcon(token.transform, new Vector3(0f, 0.2f, 0f), 1.5f, source.sprite, out _);

            var badge = new GameObject("Badge").transform;
            badge.SetParent(token.transform, false);
            badge.localPosition = new Vector3(0.72f, 0.3f, -0.62f);
            AddMesh("Body", ProcMesh.Token(new Color(0.5f, 0.05f, 0.05f), new Color(0.2f, 0.03f, 0.03f)), badge).transform.localScale = Vector3.one * 0.5f;
            MeshRenderer badgeIcon = AddIcon(badge, new Vector3(0f, 0.12f, 0f), 0.72f, null, out MeshFilter badgeFilter);

            ring.Add(new RingToken
            {
                token = token,
                action = child.GetComponent<HoverInfo>(),
                source = source,
                body = body,
                icon = icon,
                badge = badge.gameObject,
                badgeIcon = badgeIcon,
                badgeFilter = badgeFilter,
            });
        }
    }

    void UpdateRing()
    {
        bool open = country.ActionMenu.activeInHierarchy && CountryManager.SelectedParty != null && !country.ELectionWindow.activeInHierarchy;
        // Switching capitals closes the ring at the old capital first, then opens it at the new one.
        if (ringOpen <= 0.01f)
            ringParty = CountryManager.SelectedParty;
        bool switching = ringParty != CountryManager.SelectedParty;
        ringOpen = Mathf.MoveTowards(ringOpen, open && !switching ? 1f : 0f, Time.unscaledDeltaTime * 7f);
        bool isRed = ringParty == country.Communism;
        // The ring circles the middle of the capitol and is wide enough to leave its upper half clear.
        Bounds capitol = table.CapitolBounds(isRed);
        Vector3 center = ViewportToRig(cam.WorldToViewportPoint(capitol.center + Vector3.up * capitol.extents.y * 0.1f));
        Vector3 top = ViewportToRig(cam.WorldToViewportPoint(new Vector3(capitol.center.x, capitol.max.y, capitol.center.z)));
        Vector3 side = ViewportToRig(cam.WorldToViewportPoint(capitol.center + cam.transform.right * Mathf.Max(capitol.extents.x, capitol.extents.z)));
        float towerHalfWidth = Mathf.Abs(side.x - center.x);
        // Close around the tower: the upper pair sits just clear of its sides, level with its upper half.
        float towerHalfHeight = Mathf.Abs(top.y - center.y);
        float radius = Mathf.Clamp((towerHalfWidth + RingTokenSize * 0.6f) / 0.7071f, RingRadius * 0.75f, Mathf.Max(RingRadius * 0.75f, towerHalfHeight));
        float margin = radius + RingTokenSize + 0.2f;
        center.x = Mathf.Clamp(center.x, -HalfWidth + margin, HalfWidth - margin);
        center.y = Mathf.Clamp(center.y, -HalfHeight + margin, HalfHeight - margin - 1.2f);

        Party party = ringParty;
        // Only the actions available right now are on the wheel, spaced evenly around it.
        int visibleCount = 0;
        foreach (RingToken entry in ring)
        {
            if (entry.source.gameObject.activeSelf)
                visibleCount++;
        }

        int slot = 0;
        for (int i = 0; i < ring.Count; i++)
        {
            RingToken entry = ring[i];
            bool show = ringOpen > 0.01f && entry.source.gameObject.activeSelf;
            if (entry.token.gameObject.activeSelf != show)
                entry.token.gameObject.SetActive(show);
            if (!show)
                continue;

            // Evenly spaced, starting half a step to the side of straight up, so no token sits over the top of the tower.
            float step = Mathf.PI * 2f / Mathf.Max(1, visibleCount);
            float angle = Mathf.PI * 0.5f + step * 0.5f + slot++ * step;
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f) * radius * ringOpen;
            entry.token.transform.localPosition = center + offset;
            entry.token.restScale = Vector3.one * (RingTokenSize * ringOpen);

            Sprite missing = entry.action != null && party != null ? MissingFor(entry.action, party) : null;
            bool canPress = entry.token.IsInteractable;
            bool bright = canPress && missing == null;
            if (entry.action == null && party != null)
                bright = canPress && party.ElectionIndex > party.TempPresedncy && !party.Vacant;

            Tint(entry.body, "_Tint", bright ? Color.white : new Color(0.4f, 0.4f, 0.44f));
            Tint(entry.icon, "_BaseColor", bright ? Color.white : new Color(0.42f, 0.42f, 0.46f));
            bool showBadge = missing != null;
            if (entry.badge.activeSelf != showBadge)
                entry.badge.SetActive(showBadge);
            if (showBadge)
                SetIcon(entry.badgeIcon, entry.badgeFilter, missing, Color.white);
        }
    }

    Sprite MissingFor(HoverInfo action, Party party)
    {
        if (party.Power < action.PowerCost(party))
            return party.PowerUI.transform.parent.GetComponent<Image>().sprite;
        if (party.Money < country.ReverseMoneyTranslate(action.MoneyTranslate(action.MoneyCost(party))))
            return party.MoneyUI.transform.parent.GetComponent<Image>().sprite;
        if (party.PeoplSatsfaction < action.PeopleCost())
            return party.PeoplSatsfactionUI.transform.parent.GetComponent<Image>().sprite;
        return null;
    }

    // ----- Action card -----

    void BuildCard()
    {
        Plaque plaque = BuildPlaque("ActionCard", null, 8.6f, 3.3f, SlateRim, null);
        plaque.label.gameObject.SetActive(false);
        card = plaque.root;
        cardIcon = AddIcon(card, new Vector3(-3.15f, 0.08f, 0.1f), 1.9f, null, out cardIconFilter);

        Party party = country.Capitalism;
        Sprite power = party.PowerUI.transform.parent.GetComponent<Image>().sprite;
        Sprite money = party.MoneyUI.transform.parent.GetComponent<Image>().sprite;
        Sprite people = party.PeoplSatsfactionUI.transform.parent.GetComponent<Image>().sprite;
        float[] columns = { -1.55f, 0.55f, 2.65f };
        Sprite[] icons = { power, money, people };
        TextMeshPro[] costs = new TextMeshPro[3];
        TextMeshPro[] gains = new TextMeshPro[3];
        for (int i = 0; i < 3; i++)
        {
            AddIcon(card, new Vector3(columns[i], 0.08f, 0.82f), 0.72f, icons[i], out _);
            AddIcon(card, new Vector3(columns[i], 0.08f, -0.12f), 0.72f, icons[i], out _);
            costs[i] = AddLabel(card, new Vector3(columns[i] + 1.05f, 0.09f, 0.82f), 4.4f, Loss, TextAlignmentOptions.Center, 1.3f);
            gains[i] = AddLabel(card, new Vector3(columns[i] + 1.05f, 0.09f, -0.12f), 4.4f, Gain, TextAlignmentOptions.Center, 1.3f);
        }

        costPower = costs[0];
        costMoney = costs[1];
        costPeople = costs[2];
        gainPower = gains[0];
        gainMoney = gains[1];
        gainPeople = gains[2];

        cardMonths = AddLabel(card, new Vector3(0.9f, 0.09f, -1.05f), 4.6f, Color.white, TextAlignmentOptions.Center, 3f);
        repeatGlyph = AddMesh("Repeat", ProcMesh.Glyph("repeat", Gain), card);
        repeatGlyph.transform.localPosition = new Vector3(-1.55f, -0.1f, -1.05f);
        repeatGlyph.transform.localScale = Vector3.one * 1.1f;
        onceGlyph = AddMesh("Once", ProcMesh.Glyph("once", Gain), card);
        onceGlyph.transform.localPosition = new Vector3(-1.55f, -0.1f, -1.05f);
        card.gameObject.SetActive(false);
    }

    void UpdateCard()
    {
        // The old panel still holds the numbers the game reads back, so it stays alive but unseen.
        if (infoGroup == null)
            infoGroup = GameManager.Info.GetComponent<CanvasGroup>();
        infoGroup.alpha = 0f;
        infoGroup.blocksRaycasts = false;

        bool show = GameManager.Singleton.InfoVisible && country.ActionMenu.activeInHierarchy && !string.IsNullOrEmpty(CountryManager.powerReq.text);
        if (card.gameObject.activeSelf != show)
            card.gameObject.SetActive(show);
        if (!show)
            return;

        bool isRed = CountryManager.SelectedParty == country.Communism;
        card.localPosition = LocalAt(isRed ? -HalfWidth * 0.45f : HalfWidth * 0.45f, -HalfHeight * 0.42f);

        if (GameManager.nameOfAction != null && actionSprites.TryGetValue(GameManager.nameOfAction, out Sprite sprite))
            SetIcon(cardIcon, cardIconFilter, sprite, Color.white);

        costPower.text = CountryManager.TryParsePower(CountryManager.powerReq.text, out double cost) ? CountryManager.PowerTranslate(cost) : CountryManager.powerReq.text;
        costMoney.text = CountryManager.MoneyReq.text;
        costPeople.text = CountryManager.PeopleReq.text;
        SetSignedNumber(gainPower, CountryManager.PowerGain.text);
        if (CountryManager.TryParsePower(CountryManager.PowerGain.text, out double gain))
            gainPower.text = country.IsTutorial ? CountryManager.PowerTranslate(gain) : "+" + gain.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + "%";
        gainMoney.text = CountryManager.MoneyGain.text;
        SetSignedNumber(gainPeople, CountryManager.PeopleGain.text);

        cardMonths.text = CountryManager.RequestedMonths.ToString();
        bool monthly = CountryManager.monthly.activeSelf;
        repeatGlyph.SetActive(monthly);
        onceGlyph.SetActive(!monthly);
    }

    // ----- Choosing a president -----

    const float CandidateWidth = 24f;
    // Raised and shrunk so the last candidate stays above the speed-up button.
    const float ElectionRaise = 0.3f;
    const float ElectionScale = 0.88f;

    void BuildElections()
    {
        electionRoot = new GameObject("Elections").transform;
        electionRoot.SetParent(rig, false);
        Party party = country.Capitalism;
        Sprite power = party.PowerUI.transform.parent.GetComponent<Image>().sprite;
        Sprite money = party.MoneyUI.transform.parent.GetComponent<Image>().sprite;
        Sprite people = party.PeoplSatsfactionUI.transform.parent.GetComponent<Image>().sprite;

        foreach (Transform row in country.ELectionWindow.transform)
        {
            if (!row.name.StartsWith("President"))
                continue;

            int index = candidates.Count;
            Transform root = AddRigObject("Candidate" + index, electionRoot);
            root.localPosition = new Vector3(0f, 3.6f - index * 2.3f, 0f);
            Mesh mesh = new ProcMesh().Box(Vector3.zero, new Vector3(CandidateWidth, 0.3f, 2f), Color.white).ToMesh("Candidate");
            GameObject body = AddMesh("Body", mesh, root);
            var collider = root.gameObject.AddComponent<BoxCollider>();
            collider.size = new Vector3(CandidateWidth, 0.4f, 2f);
            var token = root.gameObject.AddComponent<Token3D>();
            token.target = row.GetChild(7).gameObject;

            var entry = new CandidateCard
            {
                root = root.gameObject,
                row = row,
                token = token,
                body = body.GetComponent<MeshRenderer>(),
                name = AddLabel(root, new Vector3(-7.4f, 0.17f, 0f), 6.6f, Color.white, TextAlignmentOptions.Left, 8.6f),
                power = AddLabel(root, new Vector3(2.4f, 0.17f, 0f), 6.6f, Color.white, TextAlignmentOptions.Right, 2.4f),
                money = AddLabel(root, new Vector3(6.3f, 0.17f, 0f), 6.6f, Color.white, TextAlignmentOptions.Right, 2.9f),
                people = AddLabel(root, new Vector3(9.9f, 0.17f, 0f), 6.6f, Color.white, TextAlignmentOptions.Right, 1.6f),
                agenda = new MeshRenderer[3],
                agendaFilters = new MeshFilter[3],
                lateAgenda = new MeshRenderer[CountryManager.LateAgendaSize],
                lateAgendaFilters = new MeshFilter[CountryManager.LateAgendaSize],
            };
            AddIcon(root, new Vector3(4.2f, 0.17f, 0f), 1.05f, power, out _);
            AddIcon(root, new Vector3(8.3f, 0.17f, 0f), 1.05f, money, out _);
            AddIcon(root, new Vector3(11.3f, 0.17f, 0f), 1.05f, people, out _);

            // Top row: the three actions this president carries out the moment they take office.
            for (int i = 0; i < 3; i++)
                entry.agenda[i] = AddIcon(root, new Vector3(-2.05f + i * 1f, 0.17f, 0.42f), 0.88f, null, out entry.agendaFilters[i]);

            // Bottom row, on a gold strip: the six actions that happen halfway through the term.
            Mesh strip = new ProcMesh().Box(Vector3.zero, new Vector3(4.15f, 0.04f, 0.66f), new Color(0.78f, 0.55f, 0.14f)).ToMesh("LateStrip");
            AddMesh("LateStrip", strip, root).transform.localPosition = new Vector3(-1.05f, 0.16f, -0.5f);
            for (int i = 0; i < CountryManager.LateAgendaSize; i++)
                entry.lateAgenda[i] = AddIcon(root, new Vector3(-2.7f + i * 0.66f, 0.2f, -0.5f), 0.56f, null, out entry.lateAgendaFilters[i]);
            // Long amounts such as "+326.5 M" shrink to fit instead of running into the icon beside them.
            foreach (TextMeshPro number in new[] { entry.power, entry.money })
            {
                number.enableAutoSizing = true;
                number.fontSizeMin = 3f;
                number.fontSizeMax = 6.6f;
                number.rectTransform.sizeDelta = new Vector2(number.rectTransform.sizeDelta.x, 1.6f);
            }

            candidates.Add(entry);
        }

        electionRoot.gameObject.SetActive(false);
    }

    void UpdateElections()
    {
        bool show = country.ELectionWindow.activeInHierarchy;
        if (electionRoot.gameObject.activeSelf != show)
            electionRoot.gameObject.SetActive(show);
        if (!show)
            return;

        electionRoot.localPosition = new Vector3(0f, ElectionRaise, Depth - 5f);
        electionRoot.localScale = Vector3.one * ElectionScale;
        Color side = CountryManager.SelectedParty == country.Communism ? HexBoard.RedTop : HexBoard.BlueTop;
        Color dark = side * 0.42f;
        for (int c = 0; c < candidates.Count; c++)
        {
            CandidateCard entry = candidates[c];
            entry.name.text = entry.row.GetChild(0).GetComponent<Text>().text;
            SetSignedNumber(entry.money, entry.row.GetChild(2).GetComponent<Text>().text);
            SetSignedNumber(entry.power, entry.row.GetChild(1).GetComponent<Text>().text);
            SetSignedNumber(entry.people, entry.row.GetChild(3).GetComponent<Text>().text);

            ShowAgenda(c < country.CandidateAgendas.Length ? country.CandidateAgendas[c] : null, entry.agenda, entry.agendaFilters, Color.white);
            ShowAgenda(c < country.CandidateLateAgendas.Length ? country.CandidateLateAgendas[c] : null, entry.lateAgenda, entry.lateAgendaFilters, Color.white);

            bool canChoose = entry.token.IsInteractable;
            Color tint = canChoose ? dark : dark * 0.45f;
            tint.a = 1f;
            Tint(entry.body, "_Tint", tint);
        }
    }

    void ShowAgenda(string[] agenda, MeshRenderer[] icons, MeshFilter[] filters, Color tint)
    {
        for (int i = 0; i < icons.Length; i++)
        {
            Sprite sprite = null;
            if (agenda != null && i < agenda.Length)
                actionSprites.TryGetValue(agenda[i], out sprite);
            SetIcon(icons[i], filters[i], sprite, tint);
        }
    }

    // Colors a number by its sign. Works for plain numbers and shortened ones such as "+2.5 K" or "-300".
    static void SetSignedNumber(TextMeshPro label, string text)
    {
        label.text = text;
        label.color = text.TrimStart().StartsWith("-") ? Loss : Gain;
    }

    // ----- Tutorial arrows -----

    struct ArrowAim
    {
        public Vector3 tip;
        public bool fromBelow;
        public bool fromLeft;
    }

    const float ArrowLength = 2.2f;
    const float CaptionReserve = 2.4f;
    readonly List<ArrowAim> aims = new List<ArrowAim>();

    void BuildArrows()
    {
        Mesh mesh = ProcMesh.Arrow(new Color(1f, 0.9f, 0.3f));
        for (int i = 0; i < 4; i++)
        {
            GameObject arrow = AddMesh("TutorialArrow", mesh, rig);
            arrow.SetActive(false);
            arrows.Add(arrow.transform);
        }
    }

    void UpdateArrows()
    {
        aims.Clear();
        // Arrows wait for the voice line, so nothing is pointed at while input is paused.
        if (country.IsTutorial && !HoverInfo.VoiceLinePlaying)
            CollectArrowTargets();

        for (int i = 0; i < arrows.Count; i++)
        {
            bool show = i < aims.Count;
            if (arrows[i].gameObject.activeSelf != show)
                arrows[i].gameObject.SetActive(show);
            if (!show)
                continue;

            ArrowAim aim = aims[i];
            Vector3 tip = aim.tip;
            float bob = 0.15f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 3.4f)) * 0.55f;
            if (aim.fromLeft)
                tip.x -= bob;
            else
                tip.y += bob * (aim.fromBelow ? -1f : 1f);

            // Arrows sit in front of everything, even the election cards, and are scaled so they keep their size and place on screen.
            const float nearer = 9f;
            float depthScale = (Depth - nearer) / Depth;
            arrows[i].localPosition = new Vector3(tip.x * depthScale, tip.y * depthScale, Depth - nearer);
            Quaternion spin = Quaternion.Euler(0f, Time.unscaledTime * 120f, 0f);
            Quaternion facing = aim.fromLeft ? Quaternion.Euler(0f, 0f, 90f) : aim.fromBelow ? Quaternion.Euler(0f, 0f, 180f) : Quaternion.identity;
            arrows[i].localRotation = facing * spin;
            arrows[i].localScale = Vector3.one * (1.1f * depthScale);
        }
    }

    // An arrow hangs above the target pointing down, or sits below it pointing up.
    void Aim(Vector3 tip, bool fromBelow)
    {
        aims.Add(new ArrowAim { tip = tip, fromBelow = fromBelow });
    }

    // Chooses the side with room on screen: below for things near the top edge, above for everything else.
    void AimAround(Vector3 center, float radius)
    {
        // The caption window covers the top of the screen during the tutorial.
        float ceiling = HalfHeight - (country.IsTutorial ? CaptionReserve : 0f);
        if (center.y + radius + ArrowLength + 0.6f > ceiling)
            Aim(center + Vector3.down * radius, true);
        else
            Aim(center + Vector3.up * radius, false);
    }

    void CollectArrowTargets()
    {
        bool menuOpen = country.ActionMenu.activeInHierarchy;
        switch (Tutorial.Level)
        {
            case 3:
            case 4:
            case 7:
                AimAround(date.root.localPosition, 0.6f);
                break;
            case 6:
                AimAround(blue.power.root.localPosition, 0.6f);
                AimAround(red.power.root.localPosition, 0.6f);
                break;
            case 10:
            case 18:
            case 39:
                AimAtSpeed();
                break;
            case 8:
                // Both sides' power, compared at the top of the screen.
                AimAround(blue.power.root.localPosition, 0.6f);
                AimAround(red.power.root.localPosition, 0.6f);
                break;
            case 9:
                // Blue's flags, and the fires the tutorial lights around Red's capital to show unrest.
                AimAtWorld(table.FlagsTop(false), false);
                AimAtWorld(table.DemoFireSpot, true);
                break;
            case 12:
                Aim(ViewportToRig(cam.WorldToViewportPoint(table.PoolTop)), false);
                break;
            case 13:
            case 16:
                AimAtAction(false, menuOpen, "War");
                break;
            case 14:
                AimAtRing("War");
                break;
            case 15:
                // What the card's values mean: cost on top, gain in the middle, months at the bottom.
                if (card.gameObject.activeSelf)
                {
                    AimAtWorld(costPower.transform.position + cam.transform.up * 0.35f, false);
                    AimAtWorld(gainPeople.transform.position - cam.transform.up * 0.35f, true);
                    AimAtWorld(cardMonths.transform.position - cam.transform.up * 0.35f, true);
                }

                break;
            case 20:
                AimAtCapital(false);
                break;
            case 21:
                AimAtRing("Research");
                break;
            case 22:
                // The badge on the key shows what is missing, and the money plaque shows why.
                AimAtBadge("Research");
                AimAround(blue.money.root.localPosition, 0.6f);
                break;
            case 23:
                AimAtRing("Factory");
                break;
            case 24:
                if (country.Capitalism.FactoryLevel < 3)
                    AimAtAction(false, menuOpen, "Factory");
                else
                    AimAtSpeed();
                break;
            case 25:
                AimAtCapital(true);
                break;
            case 26:
                AimAtRing(null);
                break;
            case 27:
                // The first candidate's monthly numbers.
                if (candidates.Count > 0 && electionRoot.gameObject.activeSelf)
                {
                    CandidateCard first = candidates[0];
                    foreach (TextMeshPro number in new[] { first.power, first.money, first.people })
                        AimAtWorld(number.transform.position + cam.transform.up * 0.55f, false);
                }

                break;
            case 28:
                // The first candidate's three instant actions, pointed at from the left across the card.
                if (candidates.Count > 0 && electionRoot.gameObject.activeSelf)
                {
                    AimFromLeft(candidates[0].agenda[0].transform.position - cam.transform.right * 0.55f);
                }

                break;
            case 29:
                // The half-term notch on Red's term bar, and the six gold actions on the first card, both from the left.
                if (candidates.Count > 0 && electionRoot.gameObject.activeSelf)
                {
                    AimAtWorld(red.termMarker.transform.position, false);
                    AimFromLeft(candidates[0].lateAgenda[0].transform.position - cam.transform.right * 0.45f);
                }

                break;
            case 30:
                if (candidates.Count > 0 && electionRoot.gameObject.activeSelf)
                {
                    Transform card = candidates[0].root.transform;
                    AimAtWorld(card.position + cam.transform.up * 1.05f - cam.transform.right * 8f, false);
                }

                break;
            case 33:
                if (!report.gameObject.activeSelf)
                    AimAtWorld(table.InfoCenter(true).position + Vector3.up * 3.6f, false);
                break;
            case 34:
                // The total row: what all the sources add up to each month.
                if (report.gameObject.activeSelf)
                {
                    ReportRow total = reportRows[reportRows.Length - 1];
                    AimAtWorld(total.power.transform.position - cam.transform.up * 0.35f, true);
                    AimAtWorld(total.money.transform.position - cam.transform.up * 0.35f, true);
                }

                break;
            case 35:
                AimAround(blue.people.root.localPosition, 0.6f);
                break;
            case 36:
                AimAtAction(true, menuOpen, "Spy");
                break;
            case 38:
                AimAtAction(false, menuOpen, "Research");
                break;
            case 41:
                AimAtAction(true, menuOpen, "Assassin");
                break;
        }
    }

    void AimAtSpeed()
    {
        if (speedToken != null && speedToken.token.IsInteractable)
            AimAround(speedToken.token.transform.localPosition, 1.05f);
    }

    void AimAtCapital(bool isRed)
    {
        Aim(ViewportToRig(cam.WorldToViewportPoint(table.CapitalTop(isRed))), false);
    }

    void AimAtAction(bool isRed, bool menuOpen, string actionName)
    {
        if (menuOpen)
            AimAtRing(actionName);
        else
            AimAtCapital(isRed);
    }

    void AimFromLeft(Vector3 world)
    {
        aims.Add(new ArrowAim { tip = ViewportToRig(cam.WorldToViewportPoint(world)), fromLeft = true });
    }

    void AimAtWorld(Vector3 world, bool fromBelow)
    {
        Aim(ViewportToRig(cam.WorldToViewportPoint(world)), fromBelow);
    }

    void AimAtBadge(string actionName)
    {
        foreach (RingToken entry in ring)
        {
            if (entry.action != null && entry.action.name == actionName && entry.badge.activeInHierarchy)
                AimAround(ViewportToRig(cam.WorldToViewportPoint(entry.badge.transform.position)), 0.35f);
        }
    }

    void AimAtRing(string actionName)
    {
        foreach (RingToken entry in ring)
        {
            bool matches = actionName == null ? entry.action == null : entry.action != null && entry.action.name == actionName;
            if (matches && entry.token.gameObject.activeSelf)
                AimAround(entry.token.transform.localPosition, RingTokenSize * 1.05f);
        }
    }

    // ----- Clicking the world itself -----

    void BuildWorldClicks()
    {
        Transform map = country.transform.Find("Map");
        blue.capital = BuildCapitalClick(false, map.Find("CapCenter"));
        red.capital = BuildCapitalClick(true, map.Find("Com/ComCenter"));

        // The radio station beside each capital opens the country report, through the old green info button.
        foreach (Country_Info info in country.GetComponentsInChildren<Country_Info>(true))
        {
            // The capital stars carry this script too; only the two info buttons open the report.
            if (info.name != "ComCenterInfo" && info.name != "CapCenterInfo")
                continue;
            bool isRed = info.name == "ComCenterInfo";
            (isRed ? red : blue).infoCenter = BuildInfoClick(isRed, info.gameObject);
        }

        // A click that hits nothing else lands on the board and closes any open menu.
        var boardClick = new GameObject("BoardClick");
        boardClick.transform.SetParent(table.transform, false);
        boardClick.transform.position = Vector3.down * 0.5f;
        var collider = boardClick.AddComponent<BoxCollider>();
        collider.size = new Vector3(400f, 0.2f, 400f);
        var token = boardClick.AddComponent<Token3D>();
        token.target = map.Find("BackGround").gameObject;
        token.animate = false;
    }

    Token3D BuildInfoClick(bool isRed, GameObject target)
    {
        Transform root = table.InfoCenter(isRed);
        var collider = root.gameObject.AddComponent<BoxCollider>();
        collider.center = new Vector3(0f, 1.7f, 0f);
        collider.size = new Vector3(1.8f, 3.6f, 1.8f);
        var token = root.gameObject.AddComponent<Token3D>();
        token.target = target;
        return token;
    }

    Token3D BuildCapitalClick(bool isRed, Transform star)
    {
        Transform root = table.CapitalRoot(isRed);
        var collider = root.gameObject.AddComponent<BoxCollider>();
        float height = table.CapitalTop(isRed).y - root.position.y + 1.4f;
        collider.center = new Vector3(0f, height * 0.5f, 0f);
        collider.size = new Vector3(3.6f, height, 3.4f);
        var token = root.gameObject.AddComponent<Token3D>();
        token.target = star.gameObject;
        return token;
    }

    // ----- The old 2D interface -----

    void HideFlatInterface()
    {
        Transform canvas = country.transform;
        foreach (string item in new[] { "UI", "NukeMeter", "Month", "Date", "SpeedUp", "Pause", "Exit", "Music", "SFX", "Map", "ActionMenu", "Elections", "CountryInfo", "PrisedentInfo" })
            HideGraphics(canvas.Find(item));

        foreach (Transform child in canvas)
        {
            if (child.CompareTag("Language"))
                HideGraphics(child);
        }

        Transform tutorialPanel = canvas.Find("Tutorial");
        if (tutorialPanel != null)
        {
            foreach (Image image in tutorialPanel.GetComponentsInChildren<Image>(true))
            {
                if (image.name.Contains("Arrow"))
                    image.enabled = false;
            }
        }
    }

    static void HideGraphics(Transform root)
    {
        if (root == null)
            return;
        foreach (Graphic graphic in root.GetComponentsInChildren<Graphic>(true))
            graphic.enabled = false;
    }

    CanvasGroup captionGroup;

    // A clean rounded window with a gold edge replaces the original tutorial box.
    void CompactCaption()
    {
        var panel = country.transform.Find("Tutorial") as RectTransform;
        if (!country.IsTutorial || panel == null)
            return;

        Sprite rounded = Rounded;
        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 1f);
        panel.pivot = new Vector2(0.5f, 1f);
        panel.anchoredPosition = new Vector2(0f, -8f);
        panel.sizeDelta = new Vector2(500f, 58f);
        var window = panel.GetComponent<Image>();
        window.sprite = rounded;
        window.type = Image.Type.Sliced;
        window.color = new Color(0.13f, 0.17f, 0.29f, 0.96f);
        // A gold edge separates the window from the dark ocean behind it.
        var edge = panel.GetComponent<Outline>();
        if (edge == null)
            edge = panel.gameObject.AddComponent<Outline>();
        edge.effectColor = new Color(0.95f, 0.72f, 0.22f, 0.95f);
        edge.effectDistance = new Vector2(2.5f, -2.5f);
        captionGroup = panel.GetComponent<CanvasGroup>();
        if (captionGroup == null)
            captionGroup = panel.gameObject.AddComponent<CanvasGroup>();

        var text = (RectTransform)panel.GetChild(0);
        text.anchorMin = Vector2.zero;
        text.anchorMax = Vector2.one;
        text.offsetMin = new Vector2(18f, 6f);
        text.offsetMax = new Vector2(-18f, -6f);
        var label = text.GetComponent<Text>();
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 12;
        label.resizeTextMaxSize = 24;
        label.color = new Color(1f, 0.96f, 0.86f);
        var shadow = text.GetComponent<Shadow>();
        if (shadow == null)
            shadow = text.gameObject.AddComponent<Shadow>();
        shadow.effectDistance = new Vector2(1.5f, -1.5f);

        var next = (RectTransform)panel.GetChild(1);
        next.anchorMin = next.anchorMax = new Vector2(0.5f, 0f);
        next.pivot = new Vector2(0.5f, 1f);
        next.anchoredPosition = new Vector2(0f, -6f);
        next.sizeDelta = new Vector2(120f, 34f);
        var nextImage = next.GetComponent<Image>();
        nextImage.sprite = rounded;
        nextImage.type = Image.Type.Sliced;
        nextImage.color = new Color(0.95f, 0.72f, 0.22f);
        var nextLabel = next.GetComponentInChildren<Text>(true);
        if (nextLabel != null)
            nextLabel.color = new Color(0.12f, 0.08f, 0.02f);

        nextButton = next;
        BuildVoiceHint(panel, label);
    }

    // A white rounded rectangle with a lighter rim, meant for 9-slice scaling and tinting.
    static Sprite BuildRoundedSprite()
    {
        const int size = 64;
        const float radius = 18f;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var pixels = new Color32[size * size];
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(radius - (x + 0.5f), 0f, (x + 0.5f) - (size - radius));
                float dy = Mathf.Max(radius - (y + 0.5f), 0f, (y + 0.5f) - (size - radius));
                float distance = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = Mathf.Clamp01(radius - distance + 0.5f);
                float rim = Mathf.Clamp01(1f - Mathf.Abs(radius - 1.5f - distance) / 1.5f);
                if (dx <= 0f && dy <= 0f)
                    rim = Mathf.Clamp01(1f - Mathf.Min(Mathf.Min(x, size - 1 - x), Mathf.Min(y, size - 1 - y)) / 2.5f);
                byte shade = (byte)(Mathf.Lerp(200f, 255f, rim));
                pixels[y * size + x] = new Color32(shade, shade, shade, (byte)(alpha * 255f));
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
    }

    // ----- End of game messages -----

    void ShortenEndMessages()
    {
        Shorten("LoseMenu", "Nuclear war.\nHumanity is gone. You lost.", "اندلعت حرب نووية.\nانتهت البشرية. لقد خسرت.");
        Shorten("WinMenu", "You survived the Cold War!\nEndless mode is unlocked.", "لقد نجوت من الحرب الباردة!\nتم فتح طور اللعب المفتوح.");
    }

    void Shorten(string menuName, string english, string arabic)
    {
        Transform menu = country.transform.Find(menuName);
        if (menu == null)
            return;

        // The long message is the only paragraph in each menu. Button labels are short and stay as they are.
        foreach (Text text in menu.GetComponentsInChildren<Text>(true))
        {
            var fixer = text.GetComponent<ArabicFixer>();
            string current = fixer != null && !string.IsNullOrEmpty(fixer.fixedText) ? fixer.fixedText : text.text;
            if (current.Length < 40)
                continue;

            // The fixer reloads from the label when it first activates, so both get the short text.
            if (fixer != null)
            {
                text.text = arabic;
                fixer.fixedText = arabic;
            }
            else
                text.text = english;
        }

        // Some of these messages are TextMeshPro, with their own Arabic fixer.
        foreach (TMP_Text text in menu.GetComponentsInChildren<TMP_Text>(true))
        {
            var fixer = text.GetComponent<ArabicFixerTMPRO>();
            string current = fixer != null && !string.IsNullOrEmpty(fixer.fixedText) ? fixer.fixedText : text.text;
            if (current.Length < 40)
                continue;

            // The fixer reloads from the label when it first activates, so both get the short text.
            if (fixer != null)
            {
                text.text = arabic;
                fixer.fixedText = arabic;
            }
            else
                text.text = english;
        }
    }

    // ----- Windows -----

    Sprite roundedSprite;
    Sprite Rounded => roundedSprite != null ? roundedSprite : roundedSprite = BuildRoundedSprite();
    readonly HashSet<int> styledWindows = new HashSet<int>();

    // Gives an old 2D window the same look as the tutorial caption: rounded slate, gold edge, gold buttons.
    void StyleWindow(Transform window)
    {
        if (window == null || !styledWindows.Add(window.GetInstanceID()))
            return;

        var image = window.GetComponent<Image>();
        if (image != null)
        {
            image.sprite = Rounded;
            image.type = Image.Type.Sliced;
            image.color = WindowColor;
            var edge = window.GetComponent<Outline>();
            if (edge == null)
                edge = window.gameObject.AddComponent<Outline>();
            edge.effectColor = Gold;
            edge.effectDistance = new Vector2(3f, -3f);
        }

        foreach (Button button in window.GetComponentsInChildren<Button>(true))
        {
            var buttonImage = button.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.sprite = Rounded;
                buttonImage.type = Image.Type.Sliced;
                buttonImage.color = Gold;
            }

            foreach (Text label in button.GetComponentsInChildren<Text>(true))
                label.color = new Color(0.12f, 0.08f, 0.02f);
        }

        foreach (Text text in window.GetComponentsInChildren<Text>(true))
        {
            if (text.GetComponentInParent<Button>(true) != null)
                continue;
            text.color = Cream;
            text.alignment = TextAnchor.MiddleCenter;
            if (text.GetComponent<Shadow>() == null)
                text.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(1.5f, -1.5f);
        }
    }

    void StyleWindows()
    {
        StyleWindow(country.transform.Find("LoseMenu"));
        StyleWindow(country.transform.Find("WinMenu"));
    }

    // The exit question is created when asked, so new copies are styled as they appear.
    void StyleMessageWindows()
    {
        foreach (Transform child in country.transform)
        {
            if (child.name.StartsWith("MassageWindow"))
                StyleWindow(child);
        }
    }

    // ----- Waiting for a voice line -----

    Text voiceHint;
    RectTransform nextButton;
    const string VoiceHintEnglish = "Listen to the voice line";
    const string VoiceHintArabic = "استمع إلى التعليق الصوتي";

    void BuildVoiceHint(RectTransform panel, Text captionLabel)
    {
        var hint = new GameObject("VoiceHint", typeof(RectTransform));
        var rect = (RectTransform)hint.transform;
        rect.SetParent(panel, false);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(500f, 30f);
        voiceHint = hint.AddComponent<Text>();
        voiceHint.font = captionLabel.font;
        voiceHint.fontSize = 18;
        voiceHint.fontStyle = FontStyle.Bold;
        voiceHint.alignment = TextAnchor.MiddleCenter;
        voiceHint.raycastTarget = false;
        hint.AddComponent<Shadow>().effectDistance = new Vector2(1.5f, -1.5f);
        hint.SetActive(false);
    }

    void UpdateVoiceHint()
    {
        if (voiceHint == null)
            return;

        bool listening = HoverInfo.VoiceLinePlaying;
        SetActive(voiceHint.transform, listening);
        if (!listening)
            return;

        float y = nextButton != null && nextButton.gameObject.activeSelf ? -44f : -6f;
        ((RectTransform)voiceHint.transform).anchoredPosition = new Vector2(0f, y);
        Color color = Gold;
        color.a = 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * 4f);
        voiceHint.color = color;

        var fixer = voiceHint.GetComponent<ArabicFixer>();
        if (LanguageManager.Singlton != null && LanguageManager.Singlton.GetSelectedLanguag == Language.Arabic)
        {
            if (fixer == null)
            {
                voiceHint.text = VoiceHintArabic;
                fixer = voiceHint.gameObject.AddComponent<ArabicFixer>();
            }
            fixer.fixedText = VoiceHintArabic;
        }
        else
        {
            if (fixer != null)
                Destroy(fixer);
            voiceHint.text = VoiceHintEnglish;
        }
    }

    // ----- Country report -----

    class ReportRow
    {
        public TextMeshPro count;
        public TextMeshPro power;
        public TextMeshPro money;
        public TextMeshPro people;
        public Transform share;
        public MeshRenderer shareRenderer;
    }

    const float ReportWidth = 13f;
    const float ReportHeight = 9.8f;
    const float ReportBarLength = 11.6f;
    const float ShareBarLength = 2.6f;
    Transform report;
    MeshRenderer reportBand;
    TextMeshPro reportName;
    TextMeshPro reportTerm;
    Transform reportTermFill;
    MeshRenderer reportTermFillRenderer;
    MeshRenderer reportTermMarker;
    MeshRenderer[] reportLate;
    MeshFilter[] reportLateFilters;
    GameObject reportLateStrip;
    readonly ReportRow[] reportRows = new ReportRow[5];

    // Replaces the old green country panel: what each source adds to the party every month, and the president's term.
    void BuildReport()
    {
        Plaque plaque = BuildPlaque("CountryReport", null, ReportWidth, ReportHeight, SlateRim, null);
        plaque.label.gameObject.SetActive(false);
        report = plaque.root;
        float top = ReportHeight * 0.5f;

        Mesh band = new ProcMesh().Box(Vector3.zero, new Vector3(ReportWidth - 0.12f, 0.06f, 1.3f), Color.white).ToMesh("ReportBand");
        GameObject bandObject = AddMesh("Band", band, report);
        bandObject.transform.localPosition = new Vector3(0f, 0.04f, top - 0.71f);
        reportBand = bandObject.GetComponent<MeshRenderer>();

        actionSprites.TryGetValue("Crown", out Sprite crown);
        AddIcon(report, new Vector3(-5.8f, 0.1f, top - 0.71f), 0.95f, crown, out _);
        reportName = AddLabel(report, new Vector3(-1.1f, 0.11f, top - 0.71f), 6.4f, Color.white, TextAlignmentOptions.Left, 8f);
        reportTerm = AddLabel(report, new Vector3(4.5f, 0.11f, top - 0.71f), 5.2f, Cream, TextAlignmentOptions.Right, 3f);

        // The term bar fills toward the gold notch in the middle, where the six half-term actions hang.
        float barZ = top - 1.85f;
        Mesh track = new ProcMesh().Box(Vector3.zero, new Vector3(ReportBarLength + 0.1f, 0.04f, 0.4f), SlateRim).ToMesh("ReportTrack");
        AddMesh("Track", track, report).transform.localPosition = new Vector3(0f, 0.06f, barZ);
        Mesh fill = new ProcMesh().Box(new Vector3(0.5f, 0f, 0f), new Vector3(1f, 0.04f, 0.3f), Color.white).ToMesh("ReportFill");
        GameObject fillObject = AddMesh("Fill", fill, report);
        fillObject.transform.localPosition = new Vector3(-ReportBarLength * 0.5f, 0.09f, barZ);
        reportTermFill = fillObject.transform;
        reportTermFillRenderer = fillObject.GetComponent<MeshRenderer>();
        Mesh notch = new ProcMesh().Box(Vector3.zero, new Vector3(0.14f, 0.06f, 0.62f), Color.white).ToMesh("ReportNotch");
        GameObject notchObject = AddMesh("Notch", notch, report);
        notchObject.transform.localPosition = new Vector3(0f, 0.12f, barZ);
        reportTermMarker = notchObject.GetComponent<MeshRenderer>();

        float lateZ = barZ - 0.85f;
        Mesh strip = new ProcMesh().Box(Vector3.zero, new Vector3(6.5f, 0.04f, 0.92f), new Color(0.78f, 0.55f, 0.14f)).ToMesh("ReportLateStrip");
        reportLateStrip = AddMesh("LateStrip", strip, report);
        reportLateStrip.transform.localPosition = new Vector3(0f, 0.06f, lateZ);
        reportLate = new MeshRenderer[CountryManager.LateAgendaSize];
        reportLateFilters = new MeshFilter[CountryManager.LateAgendaSize];
        for (int i = 0; i < reportLate.Length; i++)
            reportLate[i] = AddIcon(report, new Vector3(-2.65f + i * 1.06f, 0.1f, lateZ), 0.8f, null, out reportLateFilters[i]);

        // Column headings are the three resource icons.
        Party party = country.Capitalism;
        float headZ = lateZ - 1.05f;
        float[] columns = { -0.3f, 2.8f, 5.3f };
        Sprite[] heads =
        {
            party.PowerUI.transform.parent.GetComponent<Image>().sprite,
            party.MoneyUI.transform.parent.GetComponent<Image>().sprite,
            party.PeoplSatsfactionUI.transform.parent.GetComponent<Image>().sprite,
        };
        for (int i = 0; i < heads.Length; i++)
            AddIcon(report, new Vector3(columns[i], 0.1f, headZ), 0.72f, heads[i], out _);

        actionSprites.TryGetValue("Factory", out Sprite factory);
        actionSprites.TryGetValue("War", out Sprite war);
        actionSprites.TryGetValue("Spy", out Sprite spy);
        Sprite[] rowIcons = { crown, factory, war, spy, null };
        for (int r = 0; r < reportRows.Length; r++)
        {
            bool isTotal = r == reportRows.Length - 1;
            float z = headZ - 0.95f - r * 0.92f - (isTotal ? 0.35f : 0f);
            if (isTotal)
            {
                Mesh line = new ProcMesh().Box(Vector3.zero, new Vector3(ReportWidth - 1f, 0.04f, 0.06f), Gold).ToMesh("ReportLine");
                AddMesh("Line", line, report).transform.localPosition = new Vector3(0f, 0.08f, z + 0.6f);
                AddLabel(report, new Vector3(-5.6f, 0.11f, z), 6f, Gold, TextAlignmentOptions.Center, 1f).text = "=";
            }
            else
            {
                AddIcon(report, new Vector3(-5.6f, 0.1f, z), 0.78f, rowIcons[r], out _);
            }

            var row = new ReportRow
            {
                count = AddLabel(report, new Vector3(-3.7f, 0.11f, z), 4.8f, Cream, TextAlignmentOptions.Center, 1.8f),
                power = AddLabel(report, new Vector3(columns[0], 0.11f, z), 4.8f, Color.white, TextAlignmentOptions.Center, 2.8f),
                money = AddLabel(report, new Vector3(columns[1], 0.11f, z), 4.8f, Color.white, TextAlignmentOptions.Center, 2.6f),
                people = AddLabel(report, new Vector3(columns[2], 0.11f, z), 4.8f, Color.white, TextAlignmentOptions.Center, 1.8f),
            };

            // A thin bar under the power number shows this source's share of the monthly power change.
            if (!isTotal)
            {
                Mesh share = new ProcMesh().Box(new Vector3(0.5f, 0f, 0f), new Vector3(1f, 0.03f, 0.1f), Color.white).ToMesh("ReportShare");
                GameObject shareObject = AddMesh("Share", share, report);
                shareObject.transform.localPosition = new Vector3(columns[0] - ShareBarLength * 0.5f, 0.09f, z - 0.36f);
                row.share = shareObject.transform;
                row.shareRenderer = shareObject.GetComponent<MeshRenderer>();
            }

            reportRows[r] = row;
        }

        report.gameObject.SetActive(false);
    }

    void UpdateReport()
    {
        bool show = CountryManager.CountryInfo != null && CountryManager.CountryInfo.activeSelf && !country.ELectionWindow.activeInHierarchy;
        SetActive(report, show);
        if (!show)
            return;

        // The old panel is tinted with the party it was opened for.
        var image = CountryManager.CountryInfo.GetComponent<Image>();
        bool isRed = image != null && image.color == country.Communism.color;
        Party party = isRed ? country.Communism : country.Capitalism;
        report.localPosition = LocalAt(isRed ? -HalfWidth * 0.3f : HalfWidth * 0.3f, -0.9f);

        Color side = isRed ? HexBoard.RedTop : HexBoard.BlueTop;
        Color band = side * 0.55f;
        band.a = 1f;
        Tint(reportBand, "_Tint", band);
        reportName.text = party.president != null ? party.president.name : "";
        reportTerm.text = party.Vacant ? party.VacantMonths.ToString() : party.ElectionIndex + " / " + party.PresedncyPeriod;

        float term = party.PresedncyPeriod > 0 ? Mathf.Clamp01(party.ElectionIndex / (float)party.PresedncyPeriod) : 0f;
        reportTermFill.localScale = new Vector3(Mathf.Max(0.001f, term * ReportBarLength), 1f, 1f);
        bool canReplace = party.ElectionIndex > party.TempPresedncy;
        Tint(reportTermFillRenderer, "_Tint", canReplace ? new Color(0.25f, 0.95f, 0.4f) : new Color(0.62f, 0.62f, 0.66f));
        bool lateAhead = party.LateAgenda != null;
        Tint(reportTermMarker, "_Tint", lateAhead ? Gold : new Color(0.3f, 0.3f, 0.33f));
        if (reportLateStrip.activeSelf != lateAhead)
            reportLateStrip.SetActive(lateAhead);
        ShowAgenda(party.LateAgenda, reportLate, reportLateFilters, Color.white);

        int presidentPeople = party.president != null ? party.president.PeoplSatsfaction : 0;
        double[] power = { party.PresidentPower, party.FactoriesPowerNow, party.WarPowerNow, party.SpiesPowerNow };
        float[] money = { party.PresidentMoney, party.FactoriesTotalMoney, party.WarTotalMoney, 0f };
        int[] people = { presidentPeople, party.FactoriesTotalpeople, party.WarTotalpeople, 0 };
        string[] counts =
        {
            "",
            "x" + party.FactoriesNumber,
            "x" + party.WarNumber,
            "x" + party.SpiesNumber,
            "",
        };

        double powerSwing = 0;
        for (int i = 0; i < power.Length; i++)
            powerSwing += System.Math.Abs(power[i]);

        double totalPower = 0;
        float totalMoney = 0f;
        int totalPeople = 0;
        for (int r = 0; r < reportRows.Length; r++)
        {
            bool isTotal = r == reportRows.Length - 1;
            double rowPower = isTotal ? totalPower : power[r];
            float rowMoney = isTotal ? totalMoney : money[r];
            int rowPeople = isTotal ? totalPeople : people[r];
            if (!isTotal)
            {
                totalPower += rowPower;
                totalMoney += rowMoney;
                totalPeople += rowPeople;
            }

            ReportRow row = reportRows[r];
            row.count.text = counts[r];
            SetSigned(row.power, (float)rowPower, (rowPower > 0 ? "+" : "") + CountryManager.PowerTranslate(rowPower));
            SetSigned(row.money, rowMoney, SignedMoney(rowMoney));
            SetSigned(row.people, rowPeople, rowPeople.ToString("+#;-#;0"));

            if (row.share != null)
            {
                float share = powerSwing > 0 ? (float)(System.Math.Abs(rowPower) / powerSwing) : 0f;
                row.share.localScale = new Vector3(Mathf.Max(0.001f, share * ShareBarLength), 1f, 1f);
                Tint(row.shareRenderer, "_Tint", rowPower < 0 ? Loss : Gain);
            }
        }
    }

    static void SetSigned(TextMeshPro label, float value, string text)
    {
        label.text = text;
        label.color = value < 0f ? Loss : value > 0f ? Gain : Cream;
    }

    string SignedMoney(float value)
    {
        if (Mathf.Abs(value) < 1f)
            return "0";
        return (value < 0f ? "-" : "+") + country.MoneyTranslate(Mathf.Abs(value));
    }

    // ----- Refusals -----

    void OnRefused(Party party, Refusal reason)
    {
        SideHud hud = party == country.Communism ? red : blue;
        switch (reason)
        {
            case Refusal.Power: Shake(hud.power); break;
            case Refusal.Money: Shake(hud.money); break;
            case Refusal.People: Shake(hud.people); break;
            case Refusal.PresidentTooNew: Shake(hud.term); break;
            case Refusal.QueueFull: Shake(hud.queueShake); break;
        }
    }

    void Shake(Plaque plaque)
    {
        plaque.shake = 0.7f;
        if (!shaking.Contains(plaque))
            shaking.Add(plaque);
    }

    void UpdateShakes()
    {
        for (int i = shaking.Count - 1; i >= 0; i--)
        {
            Plaque plaque = shaking[i];
            plaque.shake -= Time.unscaledDeltaTime;
            if (plaque.shake <= 0f)
            {
                plaque.root.localScale = Vector3.one * UiScale;
                plaque.root.localRotation = FaceCamera;
                shaking.RemoveAt(i);
                continue;
            }

            float wave = Mathf.Sin(plaque.shake * 30f);
            plaque.root.localScale = Vector3.one * (UiScale * (1.25f + wave * 0.1f));
            plaque.root.localRotation = FaceCamera * Quaternion.Euler(0f, wave * 7f, 0f);
        }
    }

    // ----- Layout -----

    Vector3 LocalAt(float x, float y)
    {
        return new Vector3(x, y, Depth);
    }

    Vector3 ViewportToRig(Vector3 viewport)
    {
        return new Vector3((viewport.x * 2f - 1f) * HalfWidth, (viewport.y * 2f - 1f) * HalfHeight, Depth);
    }

    void LayoutHud()
    {
        float top = HalfHeight - 0.8f * UiScale;
        float left = -HalfWidth + 1.45f * UiScale;
        float right = HalfWidth - 1.45f * UiScale;
        const float step = 2.65f * UiScale;

        Place(blue, left, step, top);
        Place(red, right - step * 2f, step, top);

        float bottom = -HalfHeight + 1.25f * UiScale;
        date.root.localPosition = LocalAt(HalfWidth - 2.1f * UiScale, bottom);
        // Buttons line up from the date leftwards, and from the left edge rightwards, each taking its own width.
        float rightEdge = HalfWidth - 3.75f * UiScale;
        float leftEdge = -HalfWidth + 0.2f * UiScale;
        foreach (DeckToken entry in deck)
        {
            if (!entry.token.gameObject.activeSelf)
                continue;
            if (entry.spriteSource == null)
            {
                entry.token.transform.localPosition = LocalAt(rightEdge - entry.width * 0.5f, bottom);
                rightEdge -= entry.width;
            }
            else
            {
                entry.token.transform.localPosition = LocalAt(leftEdge + entry.width * 0.5f, bottom);
                leftEdge += entry.width;
            }
        }
    }

    void Place(SideHud hud, float x, float step, float top)
    {
        hud.power.root.localPosition = LocalAt(x, top);
        hud.money.root.localPosition = LocalAt(x + step, top);
        hud.people.root.localPosition = LocalAt(x + step * 2f, top);
        hud.queue.localPosition = LocalAt(x - 0.6f * UiScale, top - 1.35f * UiScale);
        // Below the action queue, so the two never overlap.
        hud.term.root.localPosition = LocalAt(x + 0.75f * UiScale, top - 2.55f * UiScale);
    }

    // ----- Per-frame state -----

    void UpdateHud(SideHud hud)
    {
        Party party = hud.party;
        bool show = party.PowerUI.gameObject.activeInHierarchy;
        SetActive(hud.power.root, show);
        SetActive(hud.money.root, show);
        SetActive(hud.people.root, show);
        SetActive(hud.queue, show);
        SetActive(hud.term.root, show);
        if (!show)
            return;

        hud.power.label.text = party.PowerUI.text;
        hud.money.label.text = party.MoneyUI.text;
        hud.people.label.text = party.PeoplSatsfactionUI.text;

        for (int i = 0; i < SlotCount; i++)
        {
            ActionFunction action = party.Actions[i];
            Slot slot = hud.slots[i];
            bool hasIcon = action != null && actionSprites.ContainsKey(action.name);
            if (slot.root.activeSelf != hasIcon)
                slot.root.SetActive(hasIcon);
            if (!hasIcon)
                continue;

            longestTimes.TryGetValue(action, out int longest);
            if (action.time > longest)
                longestTimes[action] = longest = action.time;

            float progress = longest > 0 ? 1f - action.time / (float)longest : 1f;
            SetIcon(slot.icon, slot.iconFilter, actionSprites[action.name], Color.Lerp(new Color(0.45f, 0.45f, 0.5f), Color.white, progress));
            slot.months.text = action.time.ToString();
        }

        float term = party.PresedncyPeriod > 0 ? Mathf.Clamp01(party.ElectionIndex / (float)party.PresedncyPeriod) : 0f;
        if (party.Vacant)
            term = 1f - party.VacantMonths / (float)Mathf.Max(1, party.VacantTotal);
        hud.termFill.localScale = new Vector3(Mathf.Max(0.001f, term * TermBarLength), 1f, 1f);
        bool canReplace = party.ElectionIndex > party.TempPresedncy;
        Color termColor = party.Vacant ? new Color(0.95f, 0.25f, 0.2f) : canReplace ? new Color(0.25f, 0.95f, 0.4f) : new Color(0.62f, 0.62f, 0.66f);
        Tint(hud.termRenderer, "_Tint", termColor);
        // Gold while the half-term actions are still to come, dim once they have happened.
        bool explaining = country.IsTutorial && Tutorial.Level == 29;
        Tint(hud.termMarker, "_Tint", party.LateAgenda != null || explaining ? Gold : new Color(0.3f, 0.3f, 0.33f));

        hud.capital.restScale = Vector3.one;
    }

    static void SetActive(Transform item, bool active)
    {
        if (item.gameObject.activeSelf != active)
            item.gameObject.SetActive(active);
    }

    void Update()
    {
        LayoutHud();
        UpdateHud(blue);
        UpdateHud(red);

        bool showDate = monthSource != null && yearSource != null && yearSource.gameObject.activeInHierarchy;
        SetActive(date.root, showDate);
        if (showDate)
        {
            dateLabel.text = monthSource.text + "  " + yearSource.text;
            dateLabel.color = Time.timeScale == 1f ? new Color(0.35f, 1f, 0.45f) : new Color(1f, 0.3f, 0.25f);
        }

        foreach (DeckToken entry in deck)
        {
            bool show = entry.token.target != null && entry.token.target.activeInHierarchy;
            SetActive(entry.token.transform, show);
            if (!show)
                continue;

            bool pressable = entry.token.IsInteractable;
            Tint(entry.body, "_Tint", pressable ? Color.white : new Color(0.4f, 0.4f, 0.42f));
            if (entry.spriteSource != null)
            {
                if (entry.spriteSource.sprite != entry.shown)
                    entry.shown = entry.spriteSource.sprite;
                SetIcon(entry.iconRenderer, entry.iconFilter, entry.shown, pressable ? Color.white : new Color(0.5f, 0.5f, 0.52f));
            }
        }

        if (captionGroup != null)
            captionGroup.interactable = !HoverInfo.VoiceLinePlaying;
        if (exitButton != null)
        {
            if (!exitButton.gameObject.activeSelf)
                exitButton.gameObject.SetActive(true);
            var exit = exitButton.GetComponent<Button>();
            if (exit != null && !exit.interactable)
                exit.interactable = true;
        }
        UpdateVoiceHint();
        StyleMessageWindows();

        UpdateRing();
        UpdateCard();
        UpdateElections();
        UpdateReport();
        UpdateNewsBanner();
        UpdateArrows();
        UpdateShakes();
    }
}
