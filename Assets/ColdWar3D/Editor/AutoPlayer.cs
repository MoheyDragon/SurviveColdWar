using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

// The Test Panel's auto-player: one player who controls both superpowers, as in the real game.
// Each month it lists every move both sides are offered (and can afford), projects the next months for each one
// (each side's growth, actions finishing, the war limit closing in, happiness) and makes the moves that grow
// both sides most while keeping the danger under the safe line. It repeats until no move helps.
// The projection is what makes the classic plays emerge: factories early, wars and spies once money allows,
// the weaker side assassinating when the gap is wide, and the stronger side spending power on research to cool off.
public static class AutoPlayer
{
    const BindingFlags Any = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public;
    public const int Horizon = 24;

    public static float SafeDanger = 0.45f;
    // The assassination loss the projection assumes; ScoreMove tries both ends of the game's range and keeps the worse.
    static double AssassinLoss = 55.0;
    public static int HappinessFloor = 30;
    public static int FactoryTarget = 4;
    public static float MaxSpend = 0.2f;

    static CountryManager Country => CountryManager.instance;
    static float YearsNow => GameManager.Singleton.YearsElapsed;

    // ------------------------------------------------------------------ Game hooks

    // Whether this month's opportunities offer a button to the side. Buttons are named for the wheel ("Science",
    // "Factory Variant", "Change President"), not for the action they start.
    public static bool OfferedButton(Party party, string buttonName)
    {
        var available = (Dictionary<Party, HashSet<string>>)typeof(CountryManager).GetField("available", Any).GetValue(null);
        return !available.TryGetValue(party, out HashSet<string> allowed) || allowed.Contains(buttonName);
    }

    public static bool Offered(Party party, HoverInfo template) => template != null && OfferedButton(party, template.gameObject.name);

    // Starts an action the way the action wheel does: checks and pays its cost, takes a slot, raises its next cost.
    // Returns why it could not, or null when it started.
    public static string Take(Party party, HoverInfo template)
    {
        if (template == null) return "No such action";
        double power = template.PowerCost(party);
        float money = template.MoneyCost(party);
        int people = template.PeopleCost();
        if (party.Power < power) return "Not enough power";
        if (party.Money < money) return "Not enough money";
        if (party.PeoplSatsfaction < people) return "Not enough happiness";
        int slot = FreeSlot(party);
        if (slot < 0) return "Queue full";
        party.Power -= power;
        party.Money -= money;
        party.PeoplSatsfaction -= people;
        party.Actions[slot] = template.CreateAction(party);
        party.ActionIndex = Mathf.Max(party.ActionIndex, slot + 1);
        typeof(CountryManager).GetMethod("RaiseLevel", Any).Invoke(null, new object[] { party, template.doneMassage });
        return null;
    }

    static int FreeSlots(Party party)
    {
        int free = 0;
        for (int i = 0; i < Mathf.Min(3, party.Actions.Length); i++)
            if (party.Actions[i] == null) free++;
        return free;
    }

    static double DangerNow()
    {
        Party b = Country.Capitalism, r = Country.Communism;
        float limit = Mathf.Max(1.01f, DifficultyManager.Current.WarLimitAt(YearsNow));
        return Math.Abs(Math.Log(Math.Max(1.0, b.Power) / Math.Max(1.0, r.Power))) / Math.Log(limit);
    }

    static int FreeSlot(Party party)
    {
        for (int i = 0; i < Mathf.Min(3, party.Actions.Length); i++)
            if (party.Actions[i] == null) return i;
        return -1;
    }

    static HoverInfo[] templates;
    static HoverInfo[] Templates
    {
        get
        {
            if (templates == null || templates.Length == 0 || templates[0] == null)
                templates = Country.ActionMenu.GetComponentsInChildren<HoverInfo>(true);
            return templates;
        }
    }

    public static HoverInfo Template(string name)
    {
        foreach (HoverInfo t in Templates)
            if (t.name == name) return t;
        return null;
    }

    static string Name(Party party) => party == Country.Capitalism ? "Blue" : "Red";

    // ------------------------------------------------------------------ The projection

    // One side, as the projection sees it: power, monthly growth (a share of power), happiness and its monthly change,
    // plus changes that land in later months (actions finishing).
    class Side
    {
        public double power, rate, happy, happyRate;
        public readonly List<Change> changes = new List<Change>();
        public Side Copy()
        {
            var c = new Side { power = power, rate = rate, happy = happy, happyRate = happyRate };
            c.changes.AddRange(changes);
            return c;
        }
    }

    struct Change
    {
        public int month;
        public double powerFactor, rate, happy, happyRate;
    }

    static double PresidentRate(Party party) => party.president == null ? 0.0 : party.president.Power / Math.Max(1.0, party.Power);
    static double PresidentHappy(Party party) => party.president == null ? 0.0 : party.president.PeoplSatsfaction;

    // A side as it stands, including what its queued actions will do when they finish.
    static Side Read(Party party, Party other, out Side otherChanges)
    {
        otherChanges = new Side();
        double p = Math.Max(1.0, party.Power);
        var s = new Side
        {
            power = p,
            rate = (party.FactoriesPowerNow + party.SpiesPowerNow + party.WarPowerNow + party.PresidentPower) / p,
            happy = party.PeoplSatsfaction,
            happyRate = party.FactoriesTotalpeople + party.WarTotalpeople + PresidentHappy(party),
        };
        foreach (ActionFunction action in party.Actions)
        {
            if (action == null || action.time <= 0) continue;
            AddEffect(s, otherChanges, action.name, action.Monthly, action.percent ? action.power : action.power / p * 100.0, action.peoplSatsfaction, action.time);
        }
        return s;
    }

    // What an action does when it finishes, `month` months from now.
    static void AddEffect(Side side, Side other, string name, bool monthly, double gainPercent, int people, int month)
    {
        month = Mathf.Max(1, month);
        if (name == "Assassin")
            other.changes.Add(new Change { month = month, powerFactor = 1.0 - AssassinLoss / 100.0 });
        else if (monthly)
            side.changes.Add(new Change { month = month, powerFactor = 1.0, rate = gainPercent / 100.0, happyRate = people });
        else
            side.changes.Add(new Change { month = month, powerFactor = 1.0 + gainPercent / 100.0, happy = people });
    }

    // How good a future is: the growth of both sides, minus a steep price for danger above the safe line
    // (and a huge one for nuclear war), minus a price for unhappy people.
    static double Score(Side blue, Side red)
    {
        double b = blue.power, r = red.power, bRate = blue.rate, rRate = red.rate;
        double bh = blue.happy, rh = red.happy, bhr = blue.happyRate, rhr = red.happyRate;
        double penalty = 0.0;
        float years = YearsNow;
        for (int m = 1; m <= Horizon; m++)
        {
            Apply(blue, m, ref b, ref bRate, ref bh, ref bhr);
            Apply(red, m, ref r, ref rRate, ref rh, ref rhr);
            b *= 1.0 + bRate;
            r *= 1.0 + rRate;
            bh = Math.Min(100.0, bh + bhr);
            rh = Math.Min(100.0, rh + rhr);
            if (bh < 0) { b = Math.Max(1.0, b + bh); bh = 0; }
            if (rh < 0) { r = Math.Max(1.0, r + rh); rh = 0; }

            float limit = Mathf.Max(1.01f, DifficultyManager.Current.WarLimitAt(years + m / 12f));
            double danger = Math.Abs(Math.Log(Math.Max(1.0, b) / Math.Max(1.0, r))) / Math.Log(limit);
            double weight = m <= 6 ? 1.5 : 1.0;   // the near future is surer, and matters most
            if (danger >= 0.97)
            {
                penalty += 200.0 * (Horizon - m + 1);
                break;
            }
            if (danger > SafeDanger)
                penalty += weight * 30.0 * (danger - SafeDanger) * (danger - SafeDanger);
            // Unhappy people block actions (wars, factories and assassinations need their support) and overthrow presidents.
            if (bh < HappinessFloor) penalty += 0.03 * (HappinessFloor - bh);
            if (rh < HappinessFloor) penalty += 0.03 * (HappinessFloor - rh);
        }
        // Beyond the projection, the side that grows faster keeps pulling away: price the gap that the two growth
        // rates would open over the next three years, so the bot keeps the sides growing at the same pace.
        float later = Mathf.Max(1.01f, DifficultyManager.Current.WarLimitAt(years + Horizon / 12f + 3f));
        double drift = Math.Abs(Math.Log(Math.Max(1.0, b) / Math.Max(1.0, r)) + 36.0 * (bRate - rRate)) / Math.Log(later);
        if (drift > SafeDanger)
            penalty += 30.0 * (drift - SafeDanger) * (drift - SafeDanger);
        return 0.5 * (Math.Log(Math.Max(1.0, b)) + Math.Log(Math.Max(1.0, r))) - penalty;
    }

    static void Apply(Side side, int month, ref double power, ref double rate, ref double happy, ref double happyRate)
    {
        foreach (Change c in side.changes)
        {
            if (c.month != month) continue;
            power *= c.powerFactor;
            rate += c.rate;
            happy += c.happy;
            happyRate += c.happyRate;
        }
    }

    // Both sides as they stand.
    static void ReadBoth(out Side blue, out Side red)
    {
        Party b = Country.Capitalism, r = Country.Communism;
        blue = Read(b, r, out Side toRed);
        red = Read(r, b, out Side toBlue);
        blue.changes.AddRange(toBlue.changes);
        red.changes.AddRange(toRed.changes);
    }

    // ------------------------------------------------------------------ A month of decisions

    static int month;

    public static string PlayMonth()
    {
        var notes = new StringBuilder();
        Party blueParty = Country.Capitalism, redParty = Country.Communism;
        var spent = new Dictionary<Party, double> { { blueParty, 0.0 }, { redParty, 0.0 } };
        var budget = new Dictionary<Party, double> { { blueParty, blueParty.Power * MaxSpend }, { redParty, redParty.Power * MaxSpend } };

        CallElectionsIfWise(blueParty, redParty, notes);
        CallElectionsIfWise(redParty, blueParty, notes);

        for (int move = 0; move < 6; move++)
        {
            ReadBoth(out Side blue, out Side red);
            double baseline = Score(blue, red);
            double best = baseline + 0.002;
            Party bestParty = null;
            HoverInfo bestAction = null;

            // Neither side always gets first pick: the order changes every month.
            Party[] order = (month++ & 1) == 0 ? new[] { blueParty, redParty } : new[] { redParty, blueParty };
            foreach (Party party in order)
            {
                if (FreeSlot(party) < 0) continue;
                Party other = party == blueParty ? redParty : blueParty;
                // One slot always stays free for an emergency (an assassination, or research to cool the leader down),
                // because most actions hold their slot for months.
                bool lastSlot = FreeSlots(party) == 1;
                bool emergency = DangerNow() > SafeDanger;
                foreach (HoverInfo t in Templates)
                {
                    string why = WhyNot(party, other, t, spent[party], budget[party], lastSlot, emergency);
                    if (why != null)
                    {
                        if (explain != null) explain.AppendLine("    " + Name(party) + " " + t.name + ": " + why);
                        continue;
                    }

                    double score = ScoreMove(blue, red, party, t);
                    if (explain != null) explain.AppendLine("    " + Name(party) + " " + t.name + " " + (score - baseline).ToString("0.0000"));
                    if (score > best)
                    {
                        best = score;
                        bestParty = party;
                        bestAction = t;
                    }
                }
            }

            if (bestAction == null)
                break;
            double paid = bestAction.PowerCost(bestParty);
            if (Take(bestParty, bestAction) != null)
                break;
            spent[bestParty] += paid;
            notes.AppendLine("  " + Name(bestParty) + ": " + bestAction.name);
        }
        return notes.ToString();
    }

    // Why a move can't or shouldn't be made now, or null when it may be considered.
    static string WhyNot(Party party, Party other, HoverInfo t, double spent, double budget, bool lastSlot, bool emergency)
    {
        if (!Offered(party, t)) return "not offered";
        double cost = t.PowerCost(party);
        if (party.Power < cost) return "power";
        if (party.Money < t.MoneyCost(party)) return "money";
        if (party.PeoplSatsfaction < t.PeopleCost()) return "happiness";
        // Research is the one move meant to burn power; everything else keeps to the month's budget.
        bool behind = party.Power < other.Power;
        if (t.name != "Research" && spent + cost > budget * (behind ? 2.0 : 1.0)) return "budget";
        // Factories first: wars and spies wait until the side has enough of them.
        if ((t.name == "War" || t.name == "Spy") && FactoriesIncludingQueued(party) < FactoryTarget) return "factories first";
        // Happiness must hold: a war costs happiness every month, so one only starts when the side's monthly happiness
        // (factories and president included) stays at zero or above with it.
        if (t.name == "War")
        {
            int.TryParse(t.PeopleGain, out int warHappiness);
            double monthly = party.FactoriesTotalpeople + party.WarTotalpeople + (party.president != null ? party.president.PeoplSatsfaction : 0);
            foreach (ActionFunction queued in party.Actions)
                if (queued != null && queued.Monthly) monthly += queued.peoplSatsfaction;
            if (monthly + warHappiness < 0) return "happiness would fall";
        }
        // An assassination only makes sense from behind.
        if (t.name == "Assassin" && party.Power >= other.Power) return "ahead";
        bool cooling = t.name == "Assassin" || t.name == "Research" && party.Power > other.Power;
        if (lastSlot && !(emergency && cooling)) return "last slot kept free";
        return null;
    }

    static bool GainIsPercent(HoverInfo template) => !Country.IsTutorial;

    static double ScoreMove(Side blue, Side red, Party party, HoverInfo t)
    {
        if (t.name == "Assassin")
        {
            // The result is random: judge it by whichever end of the range turns out worse.
            Vector2Int range = DifficultyManager.Current.assassinPowerLoss;
            AssassinLoss = range.x;
            double low = ScoreMoveOnce(blue, red, party, t);
            AssassinLoss = range.y;
            double high = ScoreMoveOnce(blue, red, party, t);
            AssassinLoss = (range.x + range.y) * 0.5;
            return Math.Min(low, high);
        }
        return ScoreMoveOnce(blue, red, party, t);
    }

    static double ScoreMoveOnce(Side blue, Side red, Party party, HoverInfo t)
    {
        Side b = blue.Copy(), r = red.Copy();
        Side me = party == Country.Capitalism ? b : r, them = party == Country.Capitalism ? r : b;
        me.power = Math.Max(1.0, me.power - t.PowerCost(party));
        me.happy -= t.PeopleCost();
        int.TryParse(t.TimeReq, out int months);
        int.TryParse(t.PeopleGain, out int peopleGain);
        double gain = t.PowerGainFor(party);
        double gainPercent = GainIsPercent(t) ? gain : gain / Math.Max(1.0, party.Power) * 100.0;
        AddEffect(me, them, t.name, t.Monthly, gainPercent, peopleGain, months);
        return Score(b, r);
    }

    // Debug: when set, PlayMonth writes every candidate's score change here.
    public static StringBuilder explain;

    static int FactoriesIncludingQueued(Party party)
    {
        int count = party.FactoriesNumber;
        foreach (ActionFunction action in party.Actions)
            if (action != null && action.name == "Factory") count++;
        return count;
    }

    // Calls an early election when the sitting president is working against the balance: a leader whose president
    // keeps adding power while the danger is high, a president who takes power away, or one who makes people unhappy
    // when they already are.
    static void CallElectionsIfWise(Party party, Party other, StringBuilder notes)
    {
        if (party.Vacant || party.president == null || CountryManager.ElectionsLock || party.ElectionIndex <= party.TempPresedncy
            || !OfferedButton(party, "Change President"))
            return;
        float limit = DifficultyManager.Current.WarLimitAt(YearsNow);
        double danger = Math.Abs(Math.Log(Math.Max(1.0, party.Power) / Math.Max(1.0, other.Power))) / Math.Log(Math.Max(1.01, limit));
        bool leader = party.Power > other.Power;
        // Also: a side that is behind calls an election for the new president's instant actions and fresh power.
        bool wise = leader && danger > SafeDanger && party.president.Power > 0
                    || !leader && party.president.Power < 0
                    || !leader && danger > SafeDanger * 0.5 && party.ElectionIndex > party.TempPresedncy * 2
                    || party.PeoplSatsfaction < HappinessFloor && party.president.PeoplSatsfaction < 0;
        if (!wise)
            return;
        Country.Elections(party, false);
        string pick = PickBestCandidate();
        if (pick != null) notes.AppendLine("  " + pick + " (early election)");
    }

    // When an election ballot is open, elects the candidate whose term projects best for both sides (power, money
    // and their three instant actions all count). Returns who was elected, or null when no ballot is open.
    public static string PickBestCandidate()
    {
        if (!CountryManager.ElectionsLock || Country.ELectionWindow == null || !Country.ELectionWindow.activeSelf)
            return null;
        Party party = CountryManager.SelectedParty;
        bool isBlue = party == Country.Capitalism;
        var rows = (Transform[])typeof(CountryManager).GetField("ElectedPresidents", Any).GetValue(Country);
        ReadBoth(out Side blue, out Side red);
        Side mine = isBlue ? blue : red;
        // Take the sitting president out of the projection.
        mine.rate -= PresidentRate(party);
        mine.happyRate -= PresidentHappy(party);

        int bestRow = -1;
        double bestScore = double.MinValue;
        for (int row = 0; row < rows.Length; row++)
        {
            President candidate = Country.CandidatePresidents[row];
            if (rows[row] == null || candidate == null) continue;
            Side b = blue.Copy(), r = red.Copy();
            Side me = isBlue ? b : r, them = isBlue ? r : b;
            me.rate += candidate.Power / Math.Max(1.0, party.Power);
            me.happyRate += candidate.PeoplSatsfaction;
            string[] agenda = Country.CandidateAgendas[row];
            if (agenda != null)
            {
                foreach (string actionName in agenda)
                {
                    HoverInfo t = Template(actionName);
                    if (t == null) continue;
                    int.TryParse(t.PeopleGain, out int peopleGain);
                    double gain = t.PowerGainFor(party) * (actionName == "Research" ? DifficultyManager.Current.agendaResearchShare : 1.0);
                    AddEffect(me, them, actionName, t.Monthly, GainIsPercent(t) ? gain : gain / Math.Max(1.0, party.Power) * 100.0, peopleGain, 1);
                }
            }
            double score = Score(b, r);
            if (score > bestScore) { bestScore = score; bestRow = row; }
        }
        if (bestRow < 0) return null;
        President chosen = Country.CandidatePresidents[bestRow];
        string note = Name(party) + " elected " + chosen.name + " (" + CountryManager.PowerTranslate(chosen.Power) + "/month, happiness " + chosen.PeoplSatsfaction + ")";
        Country.PresidentPress(rows[bestRow].GetChild(0).gameObject);
        return note;
    }
}
