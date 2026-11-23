using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

// The in-game difficulty settings, sized for a phone. It adds a "Difficulty" button to the main menu's button list that
// opens every value in the Difficulty Manager, each with a slider and a built-in numpad (the phone's own keyboard never
// opens). Undo and Redo step through changes. The button unlocks once the player has survived the Cold War (the same
// rule as Endless mode); tapping the locked button 7 times quickly unlocks it on that device for good.
// In the Editor it edits the Difficulty Manager asset directly. In a build, "Save" keeps the values on that device.
// Everything lives in this one file: delete it to remove the feature completely.
public class DifficultyMenu : MonoBehaviour
{
    const string SaveKey = "DifficultyMenu.Saved";
    // The settings this build shipped with, when the phone's values were saved. A new build with different
    // settings replaces what was saved on the phone, so updates always bring their new values.
    const string BaseKey = "DifficultyMenu.SavedOver";
    static string buildBaseline;
    // Smaller design height = bigger controls on screen.
    const float DesignHeight = 420f;
    const float RowHeight = 46f;
    const float LabelWidth = 250f;
    const float ValueWidth = 110f;
    const string UnlockKey = "DifficultyMenu.Unlocked";
    const int SecretTaps = 7;
    static readonly Color Gold = new Color(0.95f, 0.72f, 0.22f);
    static readonly Color Panel = new Color(0.07f, 0.09f, 0.14f, 0.98f);
    static readonly Color Row = new Color(0.12f, 0.15f, 0.22f, 1f);

    // One editable number on screen.
    class Entry
    {
        public string header;
        public string label;
        public Func<float> get;
        public Action<float> set;
        public float min, max;
        public bool whole;
        public Func<bool> getFlag;
        public Action<bool> setFlag;
        public Action remove;
        public bool addPoint;
        public string tip;
    }

    bool open;
    Vector2 scroll;
    string status = "";
    readonly List<Entry> entries = new List<Entry>();
    EventSystem blockedEvents;

    // Undo history: whole snapshots of the settings, taken whenever a change is finished.
    readonly List<string> undo = new List<string>();
    readonly List<string> redo = new List<string>();
    string committed;
    bool pendingCommit;

    // Adding or removing a curve point rebuilds the list, so it waits until the list has finished drawing.
    Action deferred;

    // The numpad, open for one entry at a time.
    Entry editing;
    string typed = "";

    GUIStyle label, tipStyle, header, title, value, button, bigButton, slider, thumb, padText, toastStyle;

    // The main menu button and its locked state.
    UnityEngine.UI.Button menuButton;
    UnityEngine.UI.Text[] menuTexts;
    int taps;
    float tapWindowEnds;
    string toast;
    float toastEnds;

    static bool Unlocked => PlayerPrefs.GetInt("EndlessMode", 0) == 1 || PlayerPrefs.GetInt(UnlockKey, 0) == 1;

    void Start()
    {
        BuildMenuButton();
    }

    // A copy of the Credits button, placed just above Exit, so it looks like every other menu button.
    void BuildMenuButton()
    {
        var manager = FindFirstObjectByType<MainMenuManager>(FindObjectsInactive.Include);
        Transform buttons = manager != null ? manager.transform.Find("Buttons") : null;
        Transform credits = buttons != null ? buttons.Find("Credits") : null;
        if (credits == null)
            return;

        GameObject copy = Instantiate(credits.gameObject, buttons);
        copy.name = "Difficulty";
        Transform exit = buttons.Find("Exit");
        if (exit != null)
            copy.transform.SetSiblingIndex(exit.GetSiblingIndex());

        menuButton = copy.GetComponent<UnityEngine.UI.Button>();
        menuButton.onClick = new UnityEngine.UI.Button.ButtonClickedEvent();
        menuButton.onClick.AddListener(OnMenuButton);
        menuTexts = copy.GetComponentsInChildren<UnityEngine.UI.Text>(true);
        foreach (UnityEngine.UI.Text text in menuTexts)
        {
            bool arabic = text.name == "Arabic";
            text.text = arabic ? "الصعوبة" : "Difficulty";
            var fixer = text.GetComponent<ArabicFixer>();
            if (fixer != null)
                fixer.fixedText = text.text;
        }

        // Six buttons share the space five had.
        foreach (Transform child in buttons)
        {
            var rect = child as RectTransform;
            if (rect != null)
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, rect.sizeDelta.y * 5f / 6f);
        }
    }

    void Update()
    {
        if (menuButton == null)
            return;
        // Dimmed while locked; the copy is not part of the language system, so its two labels are switched here.
        var image = menuButton.GetComponent<UnityEngine.UI.Image>();
        if (image != null)
            image.color = Unlocked ? Color.white : new Color(0.5f, 0.5f, 0.55f, 1f);
        bool arabic = LanguageManager.Singlton != null && LanguageManager.Singlton.GetSelectedLanguag == Language.Arabic;
        foreach (UnityEngine.UI.Text text in menuTexts)
        {
            if (text.name == "Arabic" && text.gameObject.activeSelf != arabic)
                text.gameObject.SetActive(arabic);
            else if (text.name == "English" && text.gameObject.activeSelf == arabic)
                text.gameObject.SetActive(!arabic);
        }
    }

    void OnMenuButton()
    {
        if (Unlocked)
        {
            Open();
            return;
        }

        // Locked: a hint for players, and a hidden unlock for the developer (7 quick taps).
        if (Time.unscaledTime > tapWindowEnds)
            taps = 0;
        taps++;
        tapWindowEnds = Time.unscaledTime + 3f;
        if (taps >= SecretTaps)
        {
            PlayerPrefs.SetInt(UnlockKey, 1);
            PlayerPrefs.Save();
            ShowToast("Difficulty settings unlocked", "تم فتح إعدادات الصعوبة");
            return;
        }
        ShowToast("Survive the Cold War to unlock", "انجُ من الحرب الباردة لفتحها");
    }

    void ShowToast(string english, string arabic)
    {
        bool isArabic = LanguageManager.Singlton != null && LanguageManager.Singlton.GetSelectedLanguag == Language.Arabic;
        toast = isArabic ? ArabicSupport.Fix(arabic, false, false) : english;
        toastEnds = Time.unscaledTime + 2.5f;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Install()
    {
        buildBaseline = JsonUtility.ToJson(DifficultyManager.Current);
#if !UNITY_EDITOR
        // Builds start from the values saved on this device, unless this build ships different settings.
        string saved = PlayerPrefs.GetString(SaveKey, "");
        if (!string.IsNullOrEmpty(saved))
        {
            if (PlayerPrefs.GetString(BaseKey, "") == buildBaseline)
                JsonUtility.FromJsonOverwrite(saved, DifficultyManager.Current);
            else
            {
                PlayerPrefs.DeleteKey(SaveKey);
                PlayerPrefs.DeleteKey(BaseKey);
                PlayerPrefs.Save();
            }
        }
#endif
        SceneManager.sceneLoaded += (scene, mode) => SpawnInMenu();
        SpawnInMenu();
    }

    static void SpawnInMenu()
    {
        if (FindFirstObjectByType<DifficultyMenu>() != null || FindFirstObjectByType<MainMenuManager>(FindObjectsInactive.Include) == null)
            return;
        new GameObject("DifficultyMenu").AddComponent<DifficultyMenu>();
    }

    void OnDestroy()
    {
        SetMenuClicks(true);
    }

    // While the panel is open, taps must not reach the main menu buttons underneath it.
    void SetMenuClicks(bool enabled)
    {
        if (!enabled && EventSystem.current != null)
        {
            blockedEvents = EventSystem.current;
            blockedEvents.enabled = false;
        }
        else if (enabled && blockedEvents != null)
        {
            blockedEvents.enabled = true;
            blockedEvents = null;
        }
    }

    static string Snapshot() => JsonUtility.ToJson(DifficultyManager.Current);

    void Open()
    {
        open = true;
        SetMenuClicks(false);
        committed = Snapshot();
        BuildEntries();
    }

    // ----- Building the list -----

    void BuildEntries()
    {
        entries.Clear();
        DifficultyManager d = DifficultyManager.Current;
        foreach (FieldInfo field in typeof(DifficultyManager).GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            // The catch-up help stays secret from players: it is tuned in the Inspector only.
            if (field.Name.StartsWith("catchUp"))
                continue;
            string head = field.GetCustomAttribute<HeaderAttribute>()?.header;
            string tip = field.GetCustomAttribute<TooltipAttribute>()?.tooltip;
            var range = field.GetCustomAttribute<RangeAttribute>();
            string name = Nice(field.Name);
            object v = field.GetValue(d);
            int before = entries.Count;
            switch (v)
            {
                case bool _:
                    entries.Add(new Entry { label = name, getFlag = () => (bool)field.GetValue(d), setFlag = x => field.SetValue(d, x) });
                    break;
                case float f:
                    entries.Add(Number(name, () => (float)field.GetValue(d), x => field.SetValue(d, x), f, false, range));
                    break;
                case int i:
                    entries.Add(Number(name, () => (int)field.GetValue(d), x => field.SetValue(d, Mathf.RoundToInt(x)), i, true, range));
                    break;
                case Vector2 r:
                    entries.Add(Number(name + ": from", () => ((Vector2)field.GetValue(d)).x, x => { var p = (Vector2)field.GetValue(d); p.x = x; field.SetValue(d, p); }, r.x, false, null));
                    entries.Add(Number(name + ": to", () => ((Vector2)field.GetValue(d)).y, x => { var p = (Vector2)field.GetValue(d); p.y = x; field.SetValue(d, p); }, r.y, false, null));
                    break;
                case Vector2Int r:
                    entries.Add(Number(name + ": from", () => ((Vector2Int)field.GetValue(d)).x, x => { var p = (Vector2Int)field.GetValue(d); p.x = Mathf.RoundToInt(x); field.SetValue(d, p); }, r.x, true, null));
                    entries.Add(Number(name + ": to", () => ((Vector2Int)field.GetValue(d)).y, x => { var p = (Vector2Int)field.GetValue(d); p.y = Mathf.RoundToInt(x); field.SetValue(d, p); }, r.y, true, null));
                    break;
                case AnimationCurve curve:
                    AddCurve(curve);
                    break;
            }
            if (head != null && entries.Count > before)
                entries[before].header = head;
            if (tip != null && entries.Count > before)
                entries[before].tip = tip;
        }
    }

    Entry Number(string name, Func<float> get, Action<float> set, float now, bool whole, RangeAttribute range)
    {
        float min, max;
        if (range != null)
        {
            min = range.min;
            max = range.max;
        }
        else
        {
            // No fixed range: the slider covers zero to three times the current value (or the negative side for negatives).
            min = now < 0f ? now * 3f : 0f;
            max = Mathf.Max(whole ? 10f : 1f, Mathf.Abs(now) * 3f);
        }
        return new Entry { label = name, get = get, set = set, min = min, max = max, whole = whole };
    }

    // The war-limit curve is shown point by point: a year and a limit, with a remove button, then "Add point".
    void AddCurve(AnimationCurve curve)
    {
        Keyframe[] keys = curve.keys;
        for (int i = 0; i < keys.Length; i++)
        {
            int index = i;
            entries.Add(new Entry
            {
                label = "Point " + (i + 1) + ": year",
                get = () => curve.keys[index].time,
                set = x => SetKey(curve, index, Mathf.Round(x), curve.keys[index].value),
                min = DifficultyManager.FirstYear,
                max = DifficultyManager.LastYear + 9,
                whole = true,
                remove = curve.length > 1 ? (Action)(() => RemoveKey(curve, index)) : null,
            });
            entries.Add(new Entry
            {
                label = "Point " + (i + 1) + ": limit (x)",
                get = () => curve.keys[index].value,
                set = x => SetKey(curve, index, curve.keys[index].time, x),
                min = 1f,
                max = Mathf.Max(30f, keys[i].value * 1.5f),
            });
        }
        entries.Add(new Entry { label = "", addPoint = true, remove = () => AddKey(curve) });
    }

    static void SetKey(AnimationCurve curve, int index, float time, float value)
    {
        Keyframe[] keys = curve.keys;
        keys[index].time = time;
        keys[index].value = value;
        Smooth(curve, keys, false);
    }

    void RemoveKey(AnimationCurve curve, int index)
    {
        var list = new List<Keyframe>(curve.keys);
        list.RemoveAt(index);
        Smooth(curve, list.ToArray(), true);
        pendingCommit = true;
        BuildEntries();
    }

    void AddKey(AnimationCurve curve)
    {
        Keyframe[] keys = curve.keys;
        Keyframe last = keys[keys.Length - 1];
        var list = new List<Keyframe>(keys) { new Keyframe(last.time + 1f, last.value) };
        Smooth(curve, list.ToArray(), true);
        pendingCommit = true;
        BuildEntries();
    }

    // Gives the points smooth slopes, like the default curve. Sorting is left for the end of an edit so a dragged
    // point keeps its place in the list while it moves.
    static void Smooth(AnimationCurve curve, Keyframe[] keys, bool sort)
    {
        if (sort)
            Array.Sort(keys, (a, b) => a.time.CompareTo(b.time));
        for (int i = 0; i < keys.Length; i++)
        {
            Keyframe before = keys[Mathf.Max(0, i - 1)];
            Keyframe after = keys[Mathf.Min(keys.Length - 1, i + 1)];
            float slope = (after.value - before.value) / Mathf.Max(0.001f, after.time - before.time);
            keys[i].inTangent = slope;
            keys[i].outTangent = slope;
        }
        curve.keys = keys;
    }

    // ----- Undo and redo -----

    // A change counts as finished when the finger lifts or the numpad is confirmed.
    void Commit()
    {
        pendingCommit = false;
        string now = Snapshot();
        if (now == committed)
            return;
        undo.Add(committed);
        redo.Clear();
        committed = now;
        status = "";
    }

    void Undo()
    {
        if (undo.Count == 0)
            return;
        redo.Add(Snapshot());
        Restore(undo[undo.Count - 1]);
        undo.RemoveAt(undo.Count - 1);
    }

    void Redo()
    {
        if (redo.Count == 0)
            return;
        undo.Add(Snapshot());
        Restore(redo[redo.Count - 1]);
        redo.RemoveAt(redo.Count - 1);
    }

    void Restore(string snapshot)
    {
        JsonUtility.FromJsonOverwrite(snapshot, DifficultyManager.Current);
        committed = snapshot;
        BuildEntries();
    }

    // ----- Drawing -----

    void Styles()
    {
        if (label != null)
            return;
        label = new GUIStyle(GUI.skin.label) { fontSize = 17, fontStyle = FontStyle.Bold, alignment = TextAnchor.MiddleLeft, wordWrap = true, normal = { textColor = new Color(0.95f, 0.95f, 0.97f) } };
        tipStyle = new GUIStyle(label) { fontSize = 12, fontStyle = FontStyle.Normal, alignment = TextAnchor.UpperLeft, normal = { textColor = new Color(0.65f, 0.7f, 0.8f) } };
        header = new GUIStyle(label) { fontSize = 20, normal = { textColor = Gold } };
        title = new GUIStyle(label) { fontSize = 26, normal = { textColor = Gold } };
        button = Filled(18, new Color(0.2f, 0.25f, 0.36f), Color.white);
        bigButton = Filled(26, new Color(0.2f, 0.25f, 0.36f), Color.white);
        value = Filled(19, Gold, new Color(0.12f, 0.08f, 0.02f));
        slider = new GUIStyle(GUI.skin.horizontalSlider) { fixedHeight = 10f, margin = new RectOffset(8, 8, 18, 18), normal = { background = Solid(new Color(0.25f, 0.3f, 0.42f)) } };
        thumb = new GUIStyle(GUI.skin.horizontalSliderThumb) { fixedHeight = 34f, fixedWidth = 34f, normal = { background = Solid(Gold) }, hover = { background = Solid(Color.Lerp(Gold, Color.white, 0.3f)) }, active = { background = Solid(Color.white) } };
        padText = new GUIStyle(GUI.skin.box) { fontSize = 30, alignment = TextAnchor.MiddleRight, normal = { textColor = Color.white, background = Solid(new Color(0.04f, 0.05f, 0.08f)) } };
        toastStyle = new GUIStyle(label) { fontSize = 20, alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white, background = Solid(new Color(0.08f, 0.1f, 0.16f, 0.95f)) } };
    }

    static Texture2D Solid(Color color)
    {
        var texture = new Texture2D(1, 1) { hideFlags = HideFlags.HideAndDontSave };
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    static GUIStyle Filled(int size, Color fill, Color text)
    {
        return new GUIStyle(GUI.skin.button)
        {
            fontSize = size,
            fontStyle = FontStyle.Bold,
            normal = { background = Solid(fill), textColor = text },
            hover = { background = Solid(Color.Lerp(fill, Color.white, 0.15f)), textColor = text },
            active = { background = Solid(Color.Lerp(fill, Color.black, 0.2f)), textColor = text },
        };
    }

    void OnGUI()
    {
        Styles();
        // Point changes rebuild the list at the start of a frame, before anything is laid out.
        if (deferred != null && Event.current.type == EventType.Layout)
        {
            Action run = deferred;
            deferred = null;
            run();
        }
        float scale = Screen.height / DesignHeight;
        GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
        float width = Screen.width / scale;

        if (!open)
        {
            if (toast != null && Time.unscaledTime < toastEnds)
                GUI.Label(new Rect(width * 0.5f - 230f, DesignHeight - 70f, 460f, 48f), toast, toastStyle);
            return;
        }

        // A finished slider drag is recorded for undo once the finger lifts.
        if (Event.current.type == EventType.MouseUp)
            pendingCommit = true;

        var area = new Rect(8f, 8f, width - 16f, DesignHeight - 16f);
        Color tint = GUI.color;
        GUI.color = Panel;
        GUI.DrawTexture(area, Texture2D.whiteTexture);
        GUI.color = Gold;
        GUI.DrawTexture(new Rect(area.x, area.y, area.width, 3f), Texture2D.whiteTexture);
        GUI.color = tint;

        GUI.enabled = editing == null;
        GUILayout.BeginArea(new Rect(area.x + 8f, area.y + 6f, area.width - 16f, area.height - 12f));
        DrawTopBar();
        scroll = GUILayout.BeginScrollView(scroll, false, true);
        foreach (Entry entry in entries)
            DrawEntry(entry);
        GUILayout.Space(20f);
        GUILayout.EndScrollView();
        GUILayout.EndArea();
        GUI.enabled = true;

        if (editing != null)
            DrawNumpad(width);

        if (pendingCommit && Event.current.type == EventType.Repaint)
            Commit();
    }

    void DrawTopBar()
    {
        GUILayout.BeginHorizontal(GUILayout.Height(48f));
        GUILayout.Label("Difficulty", title, GUILayout.Width(140f), GUILayout.Height(44f));
        GUI.enabled = editing == null && undo.Count > 0;
        if (GUILayout.Button("Undo", button, GUILayout.Width(90f), GUILayout.Height(44f)))
            Undo();
        GUI.enabled = editing == null && redo.Count > 0;
        if (GUILayout.Button("Redo", button, GUILayout.Width(90f), GUILayout.Height(44f)))
            Redo();
        GUI.enabled = editing == null;
        GUILayout.Label(status, label, GUILayout.Height(44f));
        GUILayout.FlexibleSpace();
        if (GUILayout.Button("Save", button, GUILayout.Width(90f), GUILayout.Height(44f)))
            Save();
        if (GUILayout.Button("Defaults", button, GUILayout.Width(110f), GUILayout.Height(44f)))
            ResetToDefaults();
        if (GUILayout.Button("Close", button, GUILayout.Width(90f), GUILayout.Height(44f)))
        {
            Commit();
            open = false;
            SetMenuClicks(true);
        }
        GUILayout.EndHorizontal();
    }

    void DrawEntry(Entry entry)
    {
        if (entry.header != null)
        {
            GUILayout.Space(8f);
            GUILayout.Label(entry.header, header);
        }

        GUILayout.BeginHorizontal(GUILayout.Height(RowHeight));
        if (entry.addPoint)
        {
            GUILayout.Space(LabelWidth);
            if (GUILayout.Button("Add point", button, GUILayout.Width(160f), GUILayout.Height(40f)))
                deferred = entry.remove;
            GUILayout.EndHorizontal();
            return;
        }

        GUILayout.BeginVertical(GUILayout.Width(LabelWidth));
        GUILayout.Label(entry.label, label, GUILayout.Width(LabelWidth), GUILayout.MinHeight(entry.tip != null ? 26f : RowHeight));
        if (entry.tip != null)
            GUILayout.Label(entry.tip, tipStyle, GUILayout.Width(LabelWidth));
        GUILayout.EndVertical();
        if (entry.getFlag != null)
        {
            bool on = entry.getFlag();
            if (GUILayout.Button(on ? "ON" : "OFF", value, GUILayout.Width(ValueWidth), GUILayout.Height(40f)))
            {
                entry.setFlag(!on);
                pendingCommit = true;
            }
            GUILayout.EndHorizontal();
            return;
        }

        float now = entry.get();
        if (GUILayout.Button(Format(now, entry.whole), value, GUILayout.Width(ValueWidth), GUILayout.Height(40f)))
        {
            editing = entry;
            typed = Format(now, entry.whole);
        }

        GUILayout.Space(10f);
        float slid = GUILayout.HorizontalSlider(Mathf.Clamp(now, entry.min, entry.max), entry.min, entry.max, slider, thumb, GUILayout.ExpandWidth(true), GUILayout.Height(RowHeight));
        if (!Mathf.Approximately(slid, Mathf.Clamp(now, entry.min, entry.max)))
            entry.set(Tidy(slid, entry));

        if (entry.remove != null)
        {
            GUILayout.Space(8f);
            if (GUILayout.Button("X", button, GUILayout.Width(44f), GUILayout.Height(40f)))
                deferred = entry.remove;
        }
        GUILayout.EndHorizontal();
    }

    // Slider values are rounded to a sensible step, so they land on readable numbers.
    static float Tidy(float x, Entry entry)
    {
        if (entry.whole)
            return Mathf.Round(x);
        float span = entry.max - entry.min;
        float step = span <= 1f ? 0.001f : span <= 10f ? 0.01f : 0.1f;
        return Mathf.Round(x / step) * step;
    }

    // ----- Numpad -----

    void DrawNumpad(float width)
    {
        const float pad = 300f;
        float height = DesignHeight - 40f;
        var box = new Rect((width - pad) * 0.5f, 20f, pad, height);
        Color tint = GUI.color;
        GUI.color = new Color(0.12f, 0.14f, 0.2f, 1f);
        GUI.DrawTexture(box, Texture2D.whiteTexture);
        GUI.color = tint;

        GUILayout.BeginArea(new Rect(box.x + 10f, box.y + 8f, box.width - 20f, box.height - 16f));
        GUILayout.Label(editing.label, label, GUILayout.Height(30f));
        GUILayout.Box(typed.Length > 0 ? typed : "0", padText, GUILayout.Height(52f), GUILayout.ExpandWidth(true));

        string[,] keys =
        {
            { "7", "8", "9" },
            { "4", "5", "6" },
            { "1", "2", "3" },
            { "-", "0", "." },
        };
        for (int r = 0; r < 4; r++)
        {
            GUILayout.BeginHorizontal();
            for (int c = 0; c < 3; c++)
            {
                string key = keys[r, c];
                if (GUILayout.Button(key, bigButton, GUILayout.Height(40f)))
                    Press(key);
            }
            GUILayout.EndHorizontal();
        }

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Delete", button, GUILayout.Height(40f)) && typed.Length > 0)
            typed = typed.Substring(0, typed.Length - 1);
        if (GUILayout.Button("Clear", button, GUILayout.Height(40f)))
            typed = "";
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Cancel", button, GUILayout.Height(44f)))
            editing = null;
        if (GUILayout.Button("OK", bigButton, GUILayout.Height(44f)))
            Confirm();
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }

    void Press(string key)
    {
        if (key == "-")
            typed = typed.StartsWith("-") ? typed.Substring(1) : "-" + typed;
        else if (key == ".")
        {
            if (!editing.whole && !typed.Contains("."))
                typed += typed.Length == 0 || typed == "-" ? "0." : ".";
        }
        else if (typed.Length < 12)
            typed += key;
    }

    void Confirm()
    {
        if (float.TryParse(typed, NumberStyles.Float, CultureInfo.InvariantCulture, out float x))
        {
            if (editing.whole)
                x = Mathf.Round(x);
            editing.set(x);
            // A typed value outside the slider's range widens the slider so it can still show it.
            editing.min = Mathf.Min(editing.min, x);
            editing.max = Mathf.Max(editing.max, x);
            pendingCommit = true;
        }
        editing = null;
    }

    // ----- Saving -----

    void Save()
    {
        Commit();
        DifficultyManager d = DifficultyManager.Current;
        foreach (FieldInfo field in typeof(DifficultyManager).GetFields(BindingFlags.Public | BindingFlags.Instance))
        {
            if (field.GetValue(d) is AnimationCurve curve)
                Smooth(curve, curve.keys, true);
        }
        PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(d));
        PlayerPrefs.SetString(BaseKey, buildBaseline);
        PlayerPrefs.Save();
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(d);
        UnityEditor.AssetDatabase.SaveAssetIfDirty(d);
#endif
        committed = Snapshot();
        BuildEntries();
        status = "Saved";
    }

    void ResetToDefaults()
    {
        Commit();
        DifficultyManager defaults = ScriptableObject.CreateInstance<DifficultyManager>();
        JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(defaults), DifficultyManager.Current);
        Destroy(defaults);
        Commit();
        BuildEntries();
        status = "Defaults (Save to keep)";
    }

    static string Format(float x, bool whole)
    {
        return whole ? Mathf.RoundToInt(x).ToString(CultureInfo.InvariantCulture) : x.ToString("0.###", CultureInfo.InvariantCulture);
    }

    // "unhappyMonthsBeforeUprising" -> "Unhappy months before uprising"
    static string Nice(string name)
    {
        var text = new System.Text.StringBuilder();
        for (int i = 0; i < name.Length; i++)
        {
            char c = name[i];
            if (i > 0 && char.IsUpper(c))
                text.Append(' ').Append(char.ToLower(c));
            else
                text.Append(i == 0 ? char.ToUpper(c) : c);
        }
        return text.ToString();
    }
}
