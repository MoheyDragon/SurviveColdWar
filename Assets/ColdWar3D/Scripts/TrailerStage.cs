using System.Collections;
using System.Reflection;
using UnityEngine;

// Trailer-only: triggers real game moments on cue so the camera can film them.
public static class TrailerStage
{
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
    static CountryManager C => CountryManager.instance;
    static Party P(bool red) => red ? C.Communism : C.Capitalism;

    // Starts an action through the same templates the action menu uses, so the 3D build-up plays.
    public static string Queue(bool red, string actionName)
    {
        Party party = P(red);
        HoverInfo template = null;
        foreach (HoverInfo candidate in C.ActionMenu.GetComponentsInChildren<HoverInfo>(true))
            if (candidate.name == actionName) template = candidate;
        if (template == null) return "no template " + actionName;
        int slot = -1;
        for (int i = 0; i < party.Actions.Length; i++)
            if (party.Actions[i] == null) { slot = i; break; }
        if (slot < 0) return "queue full";
        party.Actions[slot] = template.CreateAction(party);
        party.ActionIndex = Mathf.Max(party.ActionIndex, slot + 1);
        return actionName + " queued in slot " + slot + " for " + party.Actions[slot].time + " months";
    }

    public static void ClearQueue(bool red)
    {
        Party party = P(red);
        for (int i = 0; i < party.Actions.Length; i++)
            if (party.Actions[i] != null) party.Actions[i].time = Mathf.Max(1, party.Actions[i].time);
        for (int i = 0; i < party.Actions.Length; i++) party.Actions[i] = null;
        party.ActionIndex = 0;
    }

    // Lets a queued action's model grow to full size, then finishes it so its "done" effect plays.
    public static string Finish(bool red, string actionName, float after = 1.2f)
    {
        Party party = P(red);
        for (int i = 0; i < party.Actions.Length; i++)
        {
            ActionFunction action = party.Actions[i];
            if (action == null || action.name != actionName) continue;
            action.time = 0;
            Helper.StartCoroutine(RemoveLater(party, i, action, after));
            return "finishing " + actionName;
        }
        return "not queued " + actionName;
    }

    static IEnumerator RemoveLater(Party party, int slot, ActionFunction action, float after)
    {
        yield return new WaitForSecondsRealtime(after);
        if (party.Actions[slot] != action)
            yield break;
        // Completes it through the game's own code, so the counts (factories, wars, spies) match what the board shows.
        typeof(CountryManager).GetMethod("CompleteAction", Any).Invoke(C, new object[] { party, action });
        party.Actions[slot] = null;
    }

    // Trailer only: turns one election candidate into one who costs power, and shows it on the ballot.
    public static string NegativeCandidate(bool red, int row, float percent = -6f)
    {
        President candidate = C.CandidatePresidents[row];
        if (candidate == null) return "no candidate " + row;
        candidate.PowerPercent = percent;
        candidate.Settle(P(red));
        var rows = (Transform[])typeof(CountryManager).GetField("ElectedPresidents", Any).GetValue(C);
        rows[row].GetChild(1).GetComponent<UnityEngine.UI.Text>().text = CountryManager.PowerTranslate(candidate.Power);
        return candidate.name + " " + candidate.Power;
    }

    // A president's agenda: actions that resolve at once.
    public static void Agenda(bool red, params string[] agenda)
    {
        typeof(CountryManager).GetMethod("CarryOutAgenda", Any).Invoke(C, new object[] { P(red), agenda });
    }

    public static void Chaos(ChaosKind kind, bool red)
    {
        typeof(CountryManager).GetField("pendingParty", Any).SetValue(C, P(red));
        typeof(CountryManager).GetField("pendingChaos", Any).SetValue(C, (ChaosKind?)kind);
        typeof(CountryManager).GetMethod("RollChaos", Any).Invoke(C, null);
    }

    public static void Stats(bool red, double power, float money, int people)
    {
        Party party = P(red);
        party.Power = power;
        party.Money = money;
        party.PeoplSatsfaction = people;
    }

    public static bool HideFlyers { get => Helper.hideFlyers; set => Helper.hideFlyers = value; }
    public static bool HideCanvas { get => Helper.hideCanvas; set => Helper.hideCanvas = value; }
    public static bool HideInfo { get => Helper.hideInfo; set => Helper.hideInfo = value; }

    // Keeps the newest object with this name circling a point (radius, height above it, degrees per second).
    public static string Orbit(string objectName, Vector3 center, float radius, float height, float degreesPerSecond)
    {
        Transform newest = null;
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            if (t.name == objectName && t.gameObject.activeInHierarchy && (newest == null || t.GetInstanceID() < newest.GetInstanceID()))
                newest = t;
        if (newest == null) return "not found " + objectName;
        Helper.orbiter = newest;
        Helper.orbitCenter = center;
        Helper.orbitRadius = radius;
        Helper.orbitHeight = height;
        Helper.orbitSpeed = degreesPerSecond;
        Helper.orbitAngle = -90f;
        return "orbiting " + newest.name;
    }

    public static void StopOrbit() => Helper.orbiter = null;

    // Clicks a capital the way a player does: the pointer moves over it, presses and releases, so the capital squashes
    // and springs back and its action ring opens.
    public static string ClickCapital(bool red)
    {
        var table = Object.FindFirstObjectByType<WarTable>();
        var token = table.CapitalRoot(red).GetComponent<Token3D>();
        var data = new UnityEngine.EventSystems.PointerEventData(UnityEngine.EventSystems.EventSystem.current);
        token.OnPointerEnter(data);
        token.OnPointerDown(data);
        token.OnPointerUp(data);
        token.OnPointerClick(data);
        Helper.StartCoroutine(AfterRoutine(0.6f, () => token.OnPointerExit(data)));
        return "clicked " + (red ? "red" : "blue");
    }

    // Moves a parked tank column to a spot on the board (for framing a shot).
    public static string ParkColumn(Vector3 spot, float heading)
    {
        Transform newest = null;
        foreach (var t in Object.FindObjectsByType<Transform>(FindObjectsSortMode.None))
            if (t.name == "TankColumn" && (newest == null || t.GetInstanceID() < newest.GetInstanceID()))
                newest = t;
        if (newest == null) return "no column";
        var board = Object.FindFirstObjectByType<HexBoard>();
        Vector3 top = board.TileTop(board.NearestTile(new Vector2(spot.x, spot.z)));
        newest.SetPositionAndRotation(new Vector3(spot.x, top.y + 0.02f, spot.z), Quaternion.Euler(0f, heading, 0f));
        return "parked at " + newest.position;
    }

    // Runs an action after some game seconds, so it lines up with the recording (which runs on game time).
    public static string After(float seconds, System.Action action)
    {
        Helper.StartCoroutine(AfterRoutine(seconds, action));
        return "scheduled in " + seconds;
    }

    static IEnumerator AfterRoutine(float seconds, System.Action action)
    {
        yield return new WaitForSeconds(seconds);
        action();
    }


    // Keeps the loss window hidden so the nuclear exchange can be filmed.
    public static bool HideLoseWindow { get => Helper.hideLose; set => Helper.hideLose = value; }

    static TrailerHelper helper;
    static TrailerHelper Helper
    {
        get
        {
            if (helper == null)
                helper = new GameObject("TrailerHelper").AddComponent<TrailerHelper>();
            return helper;
        }
    }
}
