using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEngine;

// Window > Cold War > Test Panel. A control room for testing the game quickly in the Editor:
//  - Quick: power, money, happiness, hidden level, date and game speed for either side.
//  - Units & Actions: build factories, wars, spies and satellites at once (free) or as a player would (paid),
//    trigger every event, call elections, win or lose.
//  - Auto-play: plays months or years as one player controlling both sides (see AutoPlayer), instantly or
//    watchable, and logs how the game went.
// Two switches keep tests quick: "No lose" stops nuclear war from ending the game, and "Keep balance" makes every
// button keep the power gap between the sides as it was, so testing one side never tips the other into war.
//  - Game Tuning / Difficulty: every value as sliders, live while playing, saved to the assets.
public class TestPanelWindow : EditorWindow
{
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
    static readonly string[] Tabs = { "Quick", "Units & Actions", "Auto-play", "Game Tuning", "Difficulty" };
    static readonly string[] ActionNames = { "Factory", "War", "Spy", "Research", "Assassin" };

    int tab;
    bool red;
    Vector2 scroll;
    int year = 1960;
    string powerText = "";
    string status = "";
    Editor tuningEditor, difficultyEditor;

    // Auto-play
    int playMonths = 120;
    bool watch;
    float monthsPerSecond = 4f;
    int monthsLeft;
    double nextStep;
    readonly StringBuilder log = new StringBuilder();
    Vector2 logScroll;

    [MenuItem("Window/Cold War/Test Panel %#t")]
    static void Open() => GetWindow<TestPanelWindow>("Test Panel");

    void OnEnable() => EditorApplication.update += Tick;
    void OnDisable() => EditorApplication.update -= Tick;

    void OnInspectorUpdate()
    {
        if (Application.isPlaying)
            Repaint();
    }

    static CountryManager Country => CountryManager.instance;
    static GameManager Game => GameManager.Singleton;
    static bool Live => Application.isPlaying && Country != null && Game != null && Country.Communism != null;
    Party Side => red ? Country.Communism : Country.Capitalism;
    static Party Other(Party party) => party == Country.Capitalism ? Country.Communism : Country.Capitalism;

    // ======================================================================== Layout

    void OnGUI()
    {
        tab = GUILayout.Toolbar(tab, Tabs, GUILayout.Height(26f));
        if (Live && tab < 3)
            Overview();
        scroll = EditorGUILayout.BeginScrollView(scroll);
        // Keep balance: note the gap before the controls run, and put it back after any button that changed it.
        allowImbalance = false;
        Party acting = Live ? Side : null;
        double gapBefore = Live ? Math.Max(1.0, acting.Power) / Math.Max(1.0, Other(acting).Power) : 0.0;
        try
        {
            if (tab == 3)
                Inspector(GameTuning.Current, ref tuningEditor);
            else if (tab == 4)
                Inspector(DifficultyManager.Current, ref difficultyEditor);
            else if (!Live)
                EditorGUILayout.HelpBox("Press Play and start a game: these controls work on the running game.\n" +
                                        "Game Tuning and Difficulty work any time. Shortcut: Ctrl+Shift+T.", MessageType.Info);
            else if (tab == 0) Quick(Side);
            else if (tab == 1) UnitsAndActions(Side);
            else AutoPlay();
        }
        catch (Exception e)
        {
            status = e.GetType().Name + ": " + (e.InnerException ?? e).Message;
        }
        EditorGUILayout.EndScrollView();
        if (Live && KeepBalance && !allowImbalance && acting != null)
        {
            double gapAfter = Math.Max(1.0, acting.Power) / Math.Max(1.0, Other(acting).Power);
            if (Math.Abs(gapAfter / gapBefore - 1.0) > 1e-6)
                Other(acting).Power = Math.Max(1.0, acting.Power / gapBefore);
        }
        if (status.Length > 0)
            EditorGUILayout.HelpBox(status, MessageType.None);
    }

    // Set by controls that are meant to change the gap between the sides (or that play months), so Keep balance leaves them be.
    bool allowImbalance;
    static bool KeepBalance
    {
        get => EditorPrefs.GetBool("ColdWar.KeepBalance", true);
        set => EditorPrefs.SetBool("ColdWar.KeepBalance", value);
    }

    // Both sides at a glance, plus the side the buttons act on.
    void Overview()
    {
        Party blue = Country.Capitalism, redParty = Country.Communism;
        double ratio = Math.Max(blue.Power, redParty.Power) / Math.Max(1.0, Math.Min(blue.Power, redParty.Power));
        float limit = DifficultyManager.Current.WarLimitAt(Game.YearsElapsed);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField(Date() + "    Danger " + Mathf.RoundToInt(NukeMeterUpdater.nukeDanger) + "%    Ratio " +
                                   ratio.ToString("0.00") + "x of " + limit.ToString("0.0") + "x limit" +
                                   (WarTable.ForcedLevel.HasValue ? "    Level held at " + WarTable.ForcedLevel : ""), EditorStyles.boldLabel);
        Row(blue, "Blue");
        Row(redParty, "Red");
        EditorGUILayout.EndVertical();
        GUILayout.BeginHorizontal();
        if (GUILayout.Toggle(!red, "Acting on BLUE", "Button", GUILayout.Height(24f))) red = false;
        if (GUILayout.Toggle(red, "Acting on RED", "Button", GUILayout.Height(24f))) red = true;
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        GameTuning.NoLose = GUILayout.Toggle(GameTuning.NoLose, new GUIContent(" No lose", "Nuclear war never ends the game while this is on (the meter can sit at 100%)."));
        KeepBalance = GUILayout.Toggle(KeepBalance, new GUIContent(" Keep balance", "Buttons keep the power gap between the sides as it was: the other side's power moves with the side you change, so tests never start a nuclear war. Danger and Balance buttons, time and Auto-play are not affected."));
        GUILayout.EndHorizontal();
    }

    static void Row(Party party, string label)
    {
        int queued = 0;
        foreach (ActionFunction action in party.Actions)
            if (action != null) queued++;
        EditorGUILayout.LabelField(label + ":  Level " + WarTable.LevelOf(party) + "   Power " + CountryManager.PowerTranslate(party.Power) +
                                   "   Money " + Country.MoneyTranslate(party.Money) + "   Happy " + party.PeoplSatsfaction +
                                   "   Factories " + party.FactoriesNumber + "  Wars " + party.WarNumber + "  Spies " + party.SpiesNumber +
                                   "  Queue " + queued + "/3" + (party.president != null ? "   Pres " + party.president.name : "   (no president)"));
    }

    static void Inspector(ScriptableObject asset, ref Editor editor)
    {
        Editor.CreateCachedEditor(asset, null, ref editor);
        EditorGUILayout.HelpBox("Changes apply at once, also while playing, and are saved to the asset.", MessageType.None);
        if (GUILayout.Button("Select asset"))
            Selection.activeObject = asset;
        EditorGUILayout.Space();
        editor.OnInspectorGUI();
    }

    static bool Button(string text) => GUILayout.Button(text, GUILayout.Height(24f));

    static void Header(string text)
    {
        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField(text, EditorStyles.boldLabel);
    }

    // ======================================================================== Quick

    void Quick(Party side)
    {
        GameTuning tuning = GameTuning.Current;

        Header("Hidden level (sets this side's power to the middle of the level)");
        GUILayout.BeginHorizontal();
        for (int level = 1; level <= tuning.maxLevel; level++)
            if (Button("Level " + level)) SetLevel(side, level);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (Button("Level -1")) side.Power = Math.Max(1.0, side.Power / Math.Pow(10.0, tuning.zerosPerLevel));
        if (Button("Level +1")) side.Power = Math.Max(1.0, side.Power) * Math.Pow(10.0, tuning.zerosPerLevel);
        if (Button("Both sides to this level")) SetLevel(Other(side), WarTable.LevelOf(side));
        GUILayout.EndHorizontal();
        EditorGUILayout.LabelField("Show both sides at a level, keeping their power:", EditorStyles.miniLabel);
        GUILayout.BeginHorizontal();
        if (GUILayout.Toggle(WarTable.ForcedLevel == null, "Off", "Button", GUILayout.Height(24f)) && WarTable.ForcedLevel != null) WarTable.ForcedLevel = null;
        for (int level = 1; level <= tuning.maxLevel; level++)
            if (GUILayout.Toggle(WarTable.ForcedLevel == level, "L" + level, "Button", GUILayout.Height(24f)) && WarTable.ForcedLevel != level)
                WarTable.ForcedLevel = level;
        GUILayout.EndHorizontal();

        Header("Power");
        double exponent = Math.Log10(Math.Max(1.0, side.Power));
        double slid = EditorGUILayout.Slider("Power (zeros)", (float)exponent, 0f, tuning.zerosPerLevel * tuning.maxLevel);
        if (Math.Abs(slid - exponent) > 0.001) side.Power = Math.Pow(10.0, slid);
        GUILayout.BeginHorizontal();
        if (Button("÷10")) side.Power = Math.Max(1.0, side.Power / 10.0);
        if (Button("×2")) side.Power *= 2.0;
        if (Button("×10")) side.Power *= 10.0;
        if (Button("×1000")) side.Power *= 1000.0;
        if (Button("Match other side")) { side.Power = Other(side).Power; allowImbalance = true; }
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (Button("Push danger to 50%")) { PushRatio(side, 0.5f); allowImbalance = true; }
        if (Button("Push danger to 90%")) { PushRatio(side, 0.9f); allowImbalance = true; }
        if (Button("Balance both")) { double mid = Math.Sqrt(side.Power * Other(side).Power); side.Power = mid; Other(side).Power = mid; allowImbalance = true; }
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        powerText = EditorGUILayout.TextField(powerText);
        if (GUILayout.Button("Set power", GUILayout.Width(90f)))
        {
            if (CountryManager.TryParsePower(powerText, out double typed))
            {
                side.Power = typed;
                status = "Power set to " + CountryManager.PowerTranslate(typed);
            }
            else
                status = "Type digits or a short form like 25T";
        }
        GUILayout.EndHorizontal();

        Header("Money");
        float moneyZeros = Mathf.Log10(Mathf.Max(1f, side.Money));
        float newZeros = EditorGUILayout.Slider("Money (zeros)", moneyZeros, 0f, 15f);
        if (Mathf.Abs(newZeros - moneyZeros) > 0.001f) side.Money = Mathf.Pow(10f, newZeros);
        GUILayout.BeginHorizontal();
        if (Button("÷10")) side.Money /= 10f;
        if (Button("×10")) side.Money = Mathf.Max(1f, side.Money) * 10f;
        if (Button("+1B")) side.Money += 1e9f;
        if (Button("Zero")) side.Money = 0f;
        GUILayout.EndHorizontal();

        Header("Happiness");
        int happy = EditorGUILayout.IntSlider("Happiness", side.PeoplSatsfaction, 0, 100);
        if (happy != side.PeoplSatsfaction) side.PeoplSatsfaction = happy;
        GUILayout.BeginHorizontal();
        foreach (int preset in new[] { 0, 10, 20, 50, 80, 100 })
            if (Button(preset.ToString())) side.PeoplSatsfaction = preset;
        if (Button("Both 100")) { Country.Capitalism.PeoplSatsfaction = 100; Country.Communism.PeoplSatsfaction = 100; }
        GUILayout.EndHorizontal();

        Header("Time");
        GUILayout.BeginHorizontal();
        if (Button("Next month")) Months(1);
        if (Button("+6 months")) Months(6);
        if (Button("+1 year")) Months(12);
        if (Button("+5 years")) Months(60);
        if (Button("+10 years")) Months(120);
        GUILayout.EndHorizontal();
        EditorGUILayout.LabelField("Those play every month through with no one acting (idle). Use Auto-play for a played game. Jump only changes the date:", EditorStyles.wordWrappedMiniLabel);
        GUILayout.BeginHorizontal();
        year = EditorGUILayout.IntSlider(year, DifficultyManager.FirstYear, DifficultyManager.LastYear + 10);
        if (GUILayout.Button("Jump", GUILayout.Width(60f), GUILayout.Height(20f))) JumpTo(year);
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (Button("-1 year")) JumpTo(Year() - 1);
        if (Button("+1 year")) JumpTo(Year() + 1);
        if (Button("1950")) JumpTo(1950);
        if (Button("1970")) JumpTo(1970);
        if (Button("1990")) JumpTo(1990);
        GUILayout.EndHorizontal();

        Header("Game speed");
        float scale = EditorGUILayout.Slider("Time scale", Time.timeScale, 0f, 20f);
        if (!Mathf.Approximately(scale, Time.timeScale)) Time.timeScale = scale;
        GUILayout.BeginHorizontal();
        foreach (float speed in new[] { 0f, 1f, 2f, 5f, 10f, 20f })
            if (GUILayout.Toggle(Mathf.Approximately(Time.timeScale, speed), speed == 0f ? "Pause" : speed + "x", "Button", GUILayout.Height(24f)) && !Mathf.Approximately(Time.timeScale, speed))
                Time.timeScale = speed;
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (Button(MuteLabel())) ToggleMute();
        GUILayout.EndHorizontal();
    }

    static bool muted;
    static string MuteLabel() => muted ? "Sound: off (click to unmute)" : "Sound: on (click to mute)";
    static void ToggleMute()
    {
        muted = !muted;
        AkSoundEngine.SetOutputVolume(0, muted ? 0f : 1f);
    }

    void SetLevel(Party party, int level)
    {
        GameTuning tuning = GameTuning.Current;
        party.Power = tuning.PowerForLevel(level) * Math.Pow(10.0, tuning.zerosPerLevel * 0.5);
        status = "Power set to " + CountryManager.PowerTranslate(party.Power) + " (level " + level + ")";
    }

    // Makes this side stronger than the other by the share of the war limit that gives this danger.
    void PushRatio(Party party, float share)
    {
        float limit = DifficultyManager.Current.WarLimitAt(Game.YearsElapsed);
        party.Power = Other(party).Power * Mathf.Lerp(1f, limit, share);
        status = "Ratio set to " + Mathf.Lerp(1f, limit, share).ToString("0.00") + "x";
    }

    // ======================================================================== Units & actions

    void UnitsAndActions(Party side)
    {
        Header("Instant and free (ignores cost, opportunities and the 3-slot limit)");
        foreach (string name in ActionNames)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(name == "Research" ? "Research / satellite" : name, GUILayout.Width(140f));
            if (Button("+1 done")) Many(side, name, 1);
            if (Button("+3 done")) Many(side, name, 3);
            if (Button("+5 done")) Many(side, name, 5);
            if (Button("Start (build up)")) status = QueueFree(side, name);
            GUILayout.EndHorizontal();
        }
        GUILayout.BeginHorizontal();
        if (Button("Both: +3 of everything")) { foreach (Party p in new[] { Country.Capitalism, Country.Communism }) foreach (string n in new[] { "Factory", "War", "Spy", "Research" }) Many(p, n, 3); }
        if (Button("Finish all queued now")) FinishAll(side);
        if (Button("Clear queue")) { for (int i = 0; i < side.Actions.Length; i++) side.Actions[i] = null; side.ActionIndex = 0; }
        GUILayout.EndHorizontal();

        Header("As a player (pays the cost; respects this month's opportunities)");
        GUILayout.BeginHorizontal();
        foreach (string name in ActionNames)
        {
            HoverInfo template = Template(name);
            bool offered = Offered(side, name);
            GUI.enabled = template != null && offered;
            string cost = template == null ? "" : "\n" + CountryManager.PowerTranslate(template.PowerCost(side));
            if (GUILayout.Button(name + cost, GUILayout.Height(36f)))
                status = AutoPlayer.Take(side, template) ?? (name + " started");
            GUI.enabled = true;
        }
        GUILayout.EndHorizontal();
        EditorGUILayout.LabelField("Greyed out = not offered to this side this month. The second line is the power cost.", EditorStyles.miniLabel);

        Header("Events (strike the side you are acting on)");
        ChaosKind[] kinds = (ChaosKind[])Enum.GetValues(typeof(ChaosKind));
        for (int i = 0; i < kinds.Length; i += 4)
        {
            GUILayout.BeginHorizontal();
            for (int j = i; j < Mathf.Min(i + 4, kinds.Length); j++)
                if (Button(ObjectNames.NicifyVariableName(kinds[j].ToString())))
                    Chaos(kinds[j], side);
            GUILayout.EndHorizontal();
        }

        Header("Presidents and the end");
        GUILayout.BeginHorizontal();
        if (Button("Elections now")) Country.Elections(side, true);
        if (Button("Pick best candidate")) status = AutoPlayer.PickBestCandidate() ?? "No election open";
        GUILayout.EndHorizontal();
        GUILayout.BeginHorizontal();
        if (Button("Win")) Country.win();
        if (Button("Lose (nuclear war)")) Country.lose();
        GUILayout.EndHorizontal();
    }

    static HoverInfo Template(string actionName)
    {
        foreach (HoverInfo candidate in Country.ActionMenu.GetComponentsInChildren<HoverInfo>(true))
            if (candidate.name == actionName) return candidate;
        return null;
    }

    static bool Offered(Party party, string actionName) => AutoPlayer.Offered(party, Template(actionName));

    static string QueueFree(Party party, string actionName)
    {
        HoverInfo template = Template(actionName);
        if (template == null) return "No " + actionName + " action here";
        for (int i = 0; i < party.Actions.Length; i++)
        {
            if (party.Actions[i] != null) continue;
            party.Actions[i] = template.CreateAction(party);
            party.ActionIndex = Mathf.Max(party.ActionIndex, i + 1);
            return actionName + " started (" + party.Actions[i].time + " months)";
        }
        return "Queue full";
    }

    // Completes actions one at a time without playing months: each goes into a free slot, finishes, and its effect plays.
    void Many(Party party, string actionName, int count)
    {
        HoverInfo template = Template(actionName);
        if (template == null) { status = "No " + actionName + " action here"; return; }
        for (int i = 0; i < count; i++)
        {
            ActionFunction action = template.CreateAction(party);
            action.time = 0;
            Instant(party, action);
        }
        status = "+" + count + " " + actionName + " done";
    }

    // Finishes one action at once through the same path a president's agenda uses.
    static void Instant(Party party, ActionFunction action)
    {
        MethodInfo agenda = typeof(CountryManager).GetMethod("CarryOutAgenda", Any);
        agenda.Invoke(Country, new object[] { party, new[] { action.name } });
    }

    // Brings every queued action to its last month, then plays one month so they all complete.
    void FinishAll(Party party)
    {
        foreach (ActionFunction action in party.Actions)
            if (action != null && !action.Monthly) action.time = 1;
        Months(1);
    }

    void Months(int count)
    {
        allowImbalance = true;
        for (int i = 0; i < count && !Over(); i++)
            Game.MonthEnd();
        status = "Played " + count + " month" + (count == 1 ? "" : "s") + (Over() ? " (game over)" : "");
    }

    static bool Over() => CountryManager.LoseLock || Country.LoseMenu != null && Country.LoseMenu.activeSelf || NukeMeterUpdater.nukeDanger >= 100f;

    static void Chaos(ChaosKind kind, Party party)
    {
        typeof(CountryManager).GetField("pendingParty", Any).SetValue(Country, party);
        typeof(CountryManager).GetField("pendingChaos", Any).SetValue(Country, (ChaosKind?)kind);
        typeof(CountryManager).GetMethod("RollChaos", Any).Invoke(Country, null);
    }

    static UnityEngine.UI.Text DateText => (UnityEngine.UI.Text)typeof(GameManager).GetField("Date", Any).GetValue(Game);
    static UnityEngine.UI.Text MonthText => (UnityEngine.UI.Text)typeof(GameManager).GetField("Month", Any).GetValue(Game);
    static int Year() => int.TryParse(DateText.text, out int y) ? y : DifficultyManager.FirstYear;
    static string Date() => MonthText.text + " " + DateText.text;

    void JumpTo(int target)
    {
        target = Mathf.Clamp(target, DifficultyManager.FirstYear, 2100);
        DateText.text = target.ToString();
        year = target;
        status = "Date set to " + target;
    }

    // ======================================================================== Auto-play

    void AutoPlay()
    {
        EditorGUILayout.HelpBox("Auto-play plays the game as one player who controls both superpowers, like a real player. Each month it " +
                                "looks at every action both sides are offered, projects the next " + AutoPlayer.Horizon + " months (growth, the war " +
                                "limit closing in, happiness) and takes the moves that grow both sides while keeping the danger low: factories " +
                                "first, then wars and spies when money allows; the weaker side assassinates when the gap gets wide, and the " +
                                "stronger side spends power on research to cool things down. It also picks presidents that keep the balance. " +
                                "If it still loses, the game's numbers are the problem.", MessageType.None);
        playMonths = EditorGUILayout.IntSlider("Months to play", playMonths, 1, 540);
        GUILayout.BeginHorizontal();
        foreach (int years in new[] { 1, 5, 10, 20 })
            if (Button(years + (years == 1 ? " year" : " years"))) playMonths = years * 12;
        if (Button("To 1991")) playMonths = Mathf.Max(1, (DifficultyManager.LastYear - Year()) * 12);
        GUILayout.EndHorizontal();
        AutoPlayer.SafeDanger = EditorGUILayout.Slider(new GUIContent("Keep danger under", "The bot plans to keep the danger below this (0.45 = 45%)."), AutoPlayer.SafeDanger, 0.1f, 0.9f);
        AutoPlayer.HappinessFloor = EditorGUILayout.IntSlider(new GUIContent("Keep happiness above", "The bot avoids letting either side's happiness fall below this."), AutoPlayer.HappinessFloor, 0, 80);
        AutoPlayer.FactoryTarget = EditorGUILayout.IntSlider(new GUIContent("Factories before wars", "Each side builds this many factories before it starts wars and spies."), AutoPlayer.FactoryTarget, 0, 12);
        AutoPlayer.MaxSpend = EditorGUILayout.Slider(new GUIContent("Most power spent a month", "Share of a side's power the bot may spend on new actions in one month."), AutoPlayer.MaxSpend, 0.02f, 0.5f);
        watch = EditorGUILayout.Toggle(new GUIContent("Watch it play", "Off: all months play at once. On: one month at a time so you can watch the board."), watch);
        if (watch)
            monthsPerSecond = EditorGUILayout.Slider("Months per second", monthsPerSecond, 0.25f, 20f);
        logMoves = EditorGUILayout.Toggle(new GUIContent("Log every move", "List each action and election choice, not just the yearly summary."), logMoves);

        GUILayout.BeginHorizontal();
        GUI.enabled = monthsLeft == 0;
        if (GUILayout.Button("▶  Play " + playMonths + " months", GUILayout.Height(32f)))
        {
            allowImbalance = true;
            StartAutoPlay();
        }
        GUI.enabled = monthsLeft > 0;
        if (GUILayout.Button("■  Stop" + (monthsLeft > 0 ? " (" + monthsLeft + " left)" : ""), GUILayout.Height(32f)))
            monthsLeft = 0;
        GUI.enabled = true;
        if (GUILayout.Button("Clear log", GUILayout.Width(80f), GUILayout.Height(32f)))
            log.Length = 0;
        GUILayout.EndHorizontal();

        Header("Log");
        logScroll = EditorGUILayout.BeginScrollView(logScroll, GUILayout.MinHeight(220f));
        EditorGUILayout.TextArea(log.ToString(), EditorStyles.wordWrappedLabel);
        EditorGUILayout.EndScrollView();
    }

    bool logMoves;

    void StartAutoPlay()
    {
        log.AppendLine("— Auto-play " + playMonths + " months from " + Date() + " —");
        Snapshot();
        monthsLeft = playMonths;
        nextStep = 0;
        if (!watch)
        {
            while (monthsLeft > 0)
                Step();
        }
    }

    void Tick()
    {
        if (monthsLeft <= 0 || !watch)
            return;
        if (!Live) { monthsLeft = 0; return; }
        if (EditorApplication.timeSinceStartup < nextStep)
            return;
        nextStep = EditorApplication.timeSinceStartup + 1.0 / monthsPerSecond;
        Step();
        Repaint();
    }

    void Step()
    {
        if (!Live || Over())
        {
            log.AppendLine("Stopped: " + (Over() ? "nuclear war — game lost at " + Date() : "game not running"));
            monthsLeft = 0;
            return;
        }
        string notes = AutoPlayer.PlayMonth();
        if (notes.Length > 0 && logMoves)
            log.Append(notes);
        Game.MonthEnd();
        for (int i = 0; i < 2; i++)
        {
            string pick = AutoPlayer.PickBestCandidate();
            if (pick != null && logMoves) log.AppendLine("  " + pick);
        }
        monthsLeft--;
        if (MonthText.text == "Jan" || monthsLeft == 0)
            Snapshot();
        if (Game.HasWon && !wonLogged)
        {
            wonLogged = true;
            log.AppendLine("★ Survived the Cold War — WON at " + Date());
            monthsLeft = 0;
        }
        if (monthsLeft == 0)
            log.AppendLine("— Done at " + Date() + " —");
    }

    bool wonLogged;

    void Snapshot()
    {
        Party blue = Country.Capitalism, redParty = Country.Communism;
        double ratio = Math.Max(blue.Power, redParty.Power) / Math.Max(1.0, Math.Min(blue.Power, redParty.Power));
        log.AppendLine(string.Format("{0}  danger {1}%  ratio {2:0.00}x/{3:0.0}x  | Blue L{4} {5} ${6} :) {7} F{8} W{9} S{10}  | Red L{11} {12} ${13} :) {14} F{15} W{16} S{17}",
            Date(), Mathf.RoundToInt(NukeMeterUpdater.nukeDanger), ratio, DifficultyManager.Current.WarLimitAt(Game.YearsElapsed),
            WarTable.LevelOf(blue), CountryManager.PowerTranslate(blue.Power), Country.MoneyTranslate(blue.Money), blue.PeoplSatsfaction,
            blue.FactoriesNumber, blue.WarNumber, blue.SpiesNumber,
            WarTable.LevelOf(redParty), CountryManager.PowerTranslate(redParty.Power), Country.MoneyTranslate(redParty.Money), redParty.PeoplSatsfaction,
            redParty.FactoriesNumber, redParty.WarNumber, redParty.SpiesNumber));
        logScroll.y = float.MaxValue;
    }
}
