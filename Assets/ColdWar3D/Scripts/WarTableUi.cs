using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Restyles the original 2D interface at runtime: less text, more icons, and clear refusal feedback.
public class WarTableUi : MonoBehaviour
{
    const int SlotCount = 3;
    const float PanelScale = 0.85f;

    class Slot
    {
        public GameObject root;
        public Image icon;
        public Image clock;
        public Text months;
    }

    class Strip
    {
        public Party party;
        public Slot[] slots;
        public RectTransform queueRoot;
        public RectTransform termRoot;
        public Image termFill;
        public RectTransform powerIcon;
        public RectTransform moneyIcon;
        public RectTransform peopleIcon;
    }

    class RingBadge
    {
        public HoverInfo action;
        public Image button;
        public Image missing;
    }

    class Flash
    {
        public RectTransform target;
        public float remaining;
    }

    CountryManager country;
    RectTransform canvas;
    Font font;
    Sprite solid;
    Sprite powerSprite;
    Sprite moneySprite;
    Sprite peopleSprite;
    readonly Dictionary<string, Sprite> actionSprites = new Dictionary<string, Sprite>();
    readonly Dictionary<ActionFunction, int> longestTimes = new Dictionary<ActionFunction, int>();
    readonly List<Flash> flashes = new List<Flash>();
    readonly List<RingBadge> badges = new List<RingBadge>();

    Strip blueStrip;
    Strip redStrip;
    RectTransform info;
    CanvasGroup infoGroup;
    Image infoImage;
    Image infoBacking;
    RectTransform doButton;
    RectTransform backButton;
    RectTransform countryInfo;
    Image countryInfoImage;
    Image crownButton;

    public void Begin(CountryManager countryManager)
    {
        country = countryManager;
        canvas = (RectTransform)country.transform;
        solid = BuildSolidSprite();

        CollectArt();
        CompactCaption();
        CompactActionDetails();
        CompactCountryInfo();
        blueStrip = BuildStrip(country.Capitalism);
        redStrip = BuildStrip(country.Communism);
        BuildRingBadges();

        // Leaves room for the tutorial caption above the election list.
        var elections = (RectTransform)country.ELectionWindow.transform;
        elections.anchoredPosition += new Vector2(0f, -30f);

        CountryManager.Refused += OnRefused;
    }

    void OnDestroy()
    {
        CountryManager.Refused -= OnRefused;
    }

    static Sprite BuildSolidSprite()
    {
        var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
        var pixels = new Color32[16];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color32(255, 255, 255, 255);
        texture.SetPixels32(pixels);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
    }

    void CollectArt()
    {
        Transform menu = country.ActionMenu.transform;
        foreach (Transform child in menu)
        {
            var image = child.GetComponent<Image>();
            if (image == null)
                continue;

            var action = child.GetComponent<HoverInfo>();
            actionSprites[action != null ? action.name : "Crown"] = image.sprite;
            if (action == null)
                crownButton = image;
        }

        // Uses the party's own references because the tutorial hides the HUD, which hides it from tag searches.
        Party blue = country.Capitalism;
        font = blue.PowerUI.font;
        powerSprite = blue.PowerUI.transform.parent.GetComponent<Image>().sprite;
        moneySprite = blue.MoneyUI.transform.parent.GetComponent<Image>().sprite;
        peopleSprite = blue.PeoplSatsfactionUI.transform.parent.GetComponent<Image>().sprite;
    }

    // ----- Tutorial caption -----

    void CompactCaption()
    {
        if (!country.IsTutorial)
            return;

        var panel = country.transform.Find("Tutorial") as RectTransform;
        if (panel == null)
            return;

        panel.anchorMin = panel.anchorMax = new Vector2(0.5f, 1f);
        panel.pivot = new Vector2(0.5f, 1f);
        panel.anchoredPosition = new Vector2(0f, -6f);
        panel.sizeDelta = new Vector2(600f, 66f);

        var text = (RectTransform)panel.GetChild(0);
        text.anchorMin = Vector2.zero;
        text.anchorMax = Vector2.one;
        text.offsetMin = new Vector2(16f, 6f);
        text.offsetMax = new Vector2(-16f, -6f);
        var label = text.GetComponent<Text>();
        label.resizeTextForBestFit = true;
        label.resizeTextMinSize = 12;
        label.resizeTextMaxSize = 26;

        var next = (RectTransform)panel.GetChild(1);
        next.anchorMin = next.anchorMax = new Vector2(0.5f, 0f);
        next.anchoredPosition = new Vector2(0f, -24f);
        next.sizeDelta = new Vector2(140f, 38f);
    }

    // ----- Action details -----

    void CompactActionDetails()
    {
        info = country.transform.Find("Info") as RectTransform;
        infoGroup = info.GetComponent<CanvasGroup>();
        infoImage = info.GetComponent<Image>();

        // These two are the long quote and description. The numbers and icons stay.
        info.Find("breif").gameObject.SetActive(false);
        info.Find("quote").gameObject.SetActive(false);

        var title = (RectTransform)info.Find("Title");
        title.anchoredPosition = new Vector2(title.anchoredPosition.x, 72f);

        doButton = (RectTransform)info.Find("Do");
        doButton.anchoredPosition = new Vector2(210f, doButton.anchoredPosition.y);
        doButton.sizeDelta = new Vector2(200f, doButton.sizeDelta.y);
        backButton = (RectTransform)info.Find("Back");
        backButton.anchoredPosition = new Vector2(-210f, backButton.anchoredPosition.y);
        backButton.sizeDelta = new Vector2(200f, backButton.sizeDelta.y);

        // The original window is tall enough for the quote, so a shorter one replaces it behind the panel.
        var backing = new GameObject("InfoBacking", typeof(RectTransform));
        backing.transform.SetParent(info.parent, false);
        backing.transform.SetSiblingIndex(info.GetSiblingIndex());
        infoBacking = backing.AddComponent<Image>();
        infoBacking.sprite = infoImage.sprite;
        infoBacking.type = infoImage.type;
        infoBacking.raycastTarget = false;
        infoImage.enabled = false;
    }

    void PlaceActionDetails()
    {
        bool isRed = CountryManager.SelectedParty == country.Communism;
        // Opens on the far side of the map from the ring of buttons.
        Vector2 position = new Vector2(isRed ? -215f : 215f, -10f);
        info.anchoredPosition = position;
        info.localScale = Vector3.one * PanelScale;

        var backing = infoBacking.rectTransform;
        // In the main game the card is only a preview with no buttons, so it can be shorter.
        float height = country.IsTutorial ? 345f : 262f;
        backing.anchoredPosition = position + new Vector2(0f, 114f - height * 0.5f) * PanelScale;
        backing.sizeDelta = new Vector2(740f, height) * PanelScale;
        Color color = infoImage.color;
        color.a = infoGroup.alpha;
        infoBacking.color = color;
    }

    // ----- Country details -----

    void CompactCountryInfo()
    {
        countryInfo = country.transform.Find("CountryInfo") as RectTransform;
        countryInfoImage = countryInfo.GetComponent<Image>();
        Transform labels = countryInfo.Find("Static");
        ReplaceLabel(labels, "PresidentName", "Crown");
        ReplaceLabel(labels, "FactoriesNumber", "Factory");
        ReplaceLabel(labels, "SpiesNumber", "Spy");
        ReplaceLabel(labels, "WarsNumber", "War");
    }

    void ReplaceLabel(Transform labels, string labelName, string iconKey)
    {
        var label = labels.Find(labelName) as RectTransform;
        if (label == null || !actionSprites.TryGetValue(iconKey, out Sprite sprite))
            return;

        foreach (Text text in label.GetComponentsInChildren<Text>(true))
            text.enabled = false;

        var icon = NewImage("Icon", labels, sprite);
        icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = label.anchorMin;
        icon.rectTransform.pivot = label.pivot;
        icon.rectTransform.anchoredPosition = label.anchoredPosition + new Vector2(-label.sizeDelta.x * label.pivot.x + 22f, 0f);
        icon.rectTransform.sizeDelta = Vector2.one * 42f;
    }

    void PlaceCountryInfo()
    {
        if (!countryInfo.gameObject.activeSelf)
            return;

        Color color = countryInfoImage.color;
        bool isRed = color.r > color.b;
        countryInfo.anchoredPosition = new Vector2(isRed ? -215f : 215f, -20f);
        countryInfo.localScale = Vector3.one * 0.72f;
    }

    // ----- Action queue and presidential term -----

    Strip BuildStrip(Party party)
    {
        var powerIcon = (RectTransform)party.PowerUI.transform.parent;
        var strip = new Strip
        {
            party = party,
            powerIcon = powerIcon,
            moneyIcon = (RectTransform)party.MoneyUI.transform.parent,
            peopleIcon = (RectTransform)party.PeoplSatsfactionUI.transform.parent,
            slots = new Slot[SlotCount],
        };

        var root = new GameObject("Status", typeof(RectTransform)).GetComponent<RectTransform>();
        root.SetParent(powerIcon.parent, false);
        root.anchorMin = root.anchorMax = powerIcon.anchorMin;
        root.pivot = new Vector2(0f, 1f);
        root.anchoredPosition = powerIcon.anchoredPosition + new Vector2(-24f, -82f);
        root.sizeDelta = new Vector2(150f, 70f);
        strip.queueRoot = root;

        for (int i = 0; i < SlotCount; i++)
            strip.slots[i] = BuildSlot(root, i);

        BuildTermBar(strip, root);
        return strip;
    }

    Slot BuildSlot(RectTransform parent, int index)
    {
        const float size = 44f;
        var icon = NewImage("Slot", parent, null);
        RectTransform rect = icon.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(index * (size + 6f), 0f);
        rect.sizeDelta = Vector2.one * size;

        // A dark sweep shows how much of the action is left, like a cooldown clock.
        var clock = NewImage("Clock", rect, solid);
        clock.color = new Color(0f, 0f, 0f, 0.62f);
        clock.type = Image.Type.Filled;
        clock.fillMethod = Image.FillMethod.Radial360;
        clock.fillOrigin = (int)Image.Origin360.Top;
        Stretch(clock.rectTransform);

        var months = new GameObject("Months", typeof(RectTransform)).AddComponent<Text>();
        months.transform.SetParent(rect, false);
        months.font = font;
        months.fontSize = 22;
        months.alignment = TextAnchor.MiddleCenter;
        months.color = Color.white;
        months.raycastTarget = false;
        months.gameObject.AddComponent<Shadow>().effectDistance = new Vector2(1.5f, -1.5f);
        Stretch(months.rectTransform);

        return new Slot { root = icon.gameObject, icon = icon, clock = clock, months = months };
    }

    void BuildTermBar(Strip strip, RectTransform parent)
    {
        var termRoot = new GameObject("Term", typeof(RectTransform)).GetComponent<RectTransform>();
        termRoot.SetParent(parent, false);
        termRoot.anchorMin = termRoot.anchorMax = new Vector2(0f, 1f);
        termRoot.pivot = new Vector2(0f, 1f);
        termRoot.anchoredPosition = new Vector2(0f, -50f);
        termRoot.sizeDelta = new Vector2(144f, 20f);
        strip.termRoot = termRoot;

        actionSprites.TryGetValue("Crown", out Sprite crown);
        var crownIcon = NewImage("Crown", termRoot, crown);
        crownIcon.rectTransform.anchorMin = crownIcon.rectTransform.anchorMax = new Vector2(0f, 0.5f);
        crownIcon.rectTransform.pivot = new Vector2(0f, 0.5f);
        crownIcon.rectTransform.anchoredPosition = Vector2.zero;
        crownIcon.rectTransform.sizeDelta = Vector2.one * 22f;

        var track = NewImage("Track", termRoot, solid);
        track.color = new Color(0f, 0f, 0f, 0.55f);
        track.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        track.rectTransform.anchorMax = new Vector2(1f, 0.5f);
        track.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        track.rectTransform.offsetMin = new Vector2(26f, -5f);
        track.rectTransform.offsetMax = new Vector2(0f, 5f);

        var fill = NewImage("Fill", track.rectTransform, solid);
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        Stretch(fill.rectTransform);
        strip.termFill = fill;
    }

    void UpdateStrip(Strip strip)
    {
        Party party = strip.party;
        for (int i = 0; i < SlotCount; i++)
        {
            ActionFunction action = party.Actions[i];
            Slot slot = strip.slots[i];
            bool hasIcon = action != null && actionSprites.ContainsKey(action.name);
            if (slot.root.activeSelf != hasIcon)
                slot.root.SetActive(hasIcon);
            if (!hasIcon)
                continue;

            slot.icon.sprite = actionSprites[action.name];
            longestTimes.TryGetValue(action, out int longest);
            if (action.time > longest)
                longestTimes[action] = longest = action.time;
            slot.clock.fillAmount = longest > 0 ? 1f - action.time / (float)longest : 1f;
            slot.months.text = action.time.ToString();
        }

        // Grey until the president can be replaced, then green.
        float fill = party.PresedncyPeriod > 0 ? party.ElectionIndex / (float)party.PresedncyPeriod : 0f;
        strip.termFill.fillAmount = Mathf.Clamp01(fill);
        strip.termFill.color = party.ElectionIndex > party.TempPresedncy
            ? new Color(0.25f, 0.9f, 0.4f)
            : new Color(0.65f, 0.65f, 0.68f);
    }

    // ----- Ring affordability -----

    void BuildRingBadges()
    {
        foreach (HoverInfo action in country.ActionMenu.GetComponentsInChildren<HoverInfo>(true))
        {
            var missing = NewImage("Missing", action.transform, null);
            RectTransform rect = missing.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 0f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(-14f, 14f);
            rect.sizeDelta = Vector2.one * 44f;
            missing.gameObject.SetActive(false);
            badges.Add(new RingBadge { action = action, button = action.GetComponent<Image>(), missing = missing });
        }
    }

    void UpdateRing()
    {
        if (!country.ActionMenu.activeInHierarchy)
            return;

        Party party = CountryManager.SelectedParty;
        if (party == null)
            return;

        foreach (RingBadge badge in badges)
        {
            Sprite missing = MissingFor(badge.action, party);
            bool affordable = missing == null;
            badge.button.color = affordable ? Color.white : new Color(0.42f, 0.42f, 0.46f);
            if (badge.missing.gameObject.activeSelf != !affordable)
                badge.missing.gameObject.SetActive(!affordable);
            if (!affordable)
                badge.missing.sprite = missing;
        }

        if (crownButton != null)
            crownButton.color = party.ElectionIndex > party.TempPresedncy ? Color.white : new Color(0.42f, 0.42f, 0.46f);
    }

    Sprite MissingFor(HoverInfo action, Party party)
    {
        if (party.Power < action.PowerCost(party))
            return powerSprite;
        if (party.Money < country.ReverseMoneyTranslate(action.MoneyTranslate(action.MoneyCost(party))))
            return moneySprite;
        if (party.PeoplSatsfaction < action.PeopleCost())
            return peopleSprite;
        return null;
    }

    // ----- Elections -----

    void TintElections()
    {
        GameObject window = country.ELectionWindow;
        if (window == null || !window.activeInHierarchy)
            return;

        foreach (Transform row in window.transform)
        {
            if (!row.name.StartsWith("President"))
                continue;

            TintNumber(row.GetChild(1));
            TintNumber(row.GetChild(3));
        }
    }

    static void TintNumber(Transform item)
    {
        var text = item.GetComponent<Text>();
        if (int.TryParse(text.text, out int value))
            text.color = value < 0 ? new Color(1f, 0.42f, 0.38f) : new Color(0.5f, 1f, 0.55f);
    }

    // ----- Refusals -----

    void OnRefused(Party party, Refusal reason)
    {
        Strip strip = party == country.Communism ? redStrip : blueStrip;
        switch (reason)
        {
            case Refusal.Power: Pulse(strip.powerIcon); break;
            case Refusal.Money: Pulse(strip.moneyIcon); break;
            case Refusal.People: Pulse(strip.peopleIcon); break;
            case Refusal.QueueFull: Pulse(strip.queueRoot); break;
            case Refusal.PresidentTooNew: Pulse(strip.termRoot); break;
        }
    }

    void Pulse(RectTransform target)
    {
        foreach (Flash flash in flashes)
        {
            if (flash.target == target)
            {
                flash.remaining = 0.7f;
                return;
            }
        }

        flashes.Add(new Flash { target = target, remaining = 0.7f });
    }

    void UpdateFlashes()
    {
        for (int i = flashes.Count - 1; i >= 0; i--)
        {
            Flash flash = flashes[i];
            flash.remaining -= Time.unscaledDeltaTime;
            if (flash.remaining <= 0f)
            {
                flash.target.localScale = Vector3.one;
                flash.target.localRotation = Quaternion.identity;
                flashes.RemoveAt(i);
                continue;
            }

            float wave = Mathf.Sin(flash.remaining * 28f);
            flash.target.localScale = Vector3.one * (1.35f + 0.2f * wave);
            flash.target.localRotation = Quaternion.Euler(0f, 0f, wave * 10f);
        }
    }

    // ----- Helpers -----

    static Image NewImage(string objectName, Transform parent, Sprite sprite)
    {
        var go = new GameObject(objectName, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var image = go.AddComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        return image;
    }

    static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    void Update()
    {
        UpdateStrip(blueStrip);
        UpdateStrip(redStrip);
        UpdateRing();
        TintElections();
        UpdateFlashes();
    }

    void LateUpdate()
    {
        PlaceActionDetails();
        PlaceCountryInfo();
    }
}
