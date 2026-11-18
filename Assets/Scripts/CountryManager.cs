using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public enum Refusal { Power, Money, People, QueueFull, PresidentTooNew }
public enum ChaosKind { EconomicCrash, ArmsRace, Scandal, SpyCaught, FactoryDisaster, WarLost, Coup, MissileCrisis, Uprising, NewPresident }

public class CountryManager : MonoBehaviour
{
    public static CountryManager instance;
    public static event Action<Party, Refusal> Refused;
    public static int RequestedMonths;
    public bool IsTutorial;
    Text PowerCapUI, MoneyCapUI, PeoplSatsfactionCapUI, PowerComUI, MoneyComUI, PeoplSatsfactionComUI;
    public static ArabicFixer titleFixer, briefFixer, doneFixer, QuoteFixer,timeFixer, InfoPartyName;
    public static Text title, breif, powerReq, MoneyReq, PeopleReq, TimeReq, PowerGain, MoneyGain, PeopleGain,Quote
        ,InfoPresPower, InfoPresMoney, InfoPresPeople, InfoFacPower, InfoFacMoney, InfoFacPeople
        , InfoTotalPower, InfoTotalMoney, InfoTotalPeople, Presidency_months, Presidency_Remaining,InfoSpiesPower
        , InfoWarPower, InfoWarMoney, InfoWarPeople;
    public static Text[] InfoPresName, InfoFacNumber, InfoWarNumber, InfoSpiesNumber;
    public static Transform[] Actions = new Transform[3];
    public static GameObject monthly, oneTime;
    public static string doneMassage,arabicDone;
    public GameObject MassageWindowPrefabe;
    public Notification notifactionPrefab;
    public Transform notifcationCenter;
    [HideInInspector]
    public GameObject ELectionWindow, LoseMenu, ActionMenu;
    public static GameObject CountryInfo;
    Transform[] ElectedPresidents = new Transform[5];
    public Party Communism, Capitalism;
    public static Party SelectedParty;
    public static bool LoseLock, ElectionsLock, NotificationLock;
    public static int NotificationIndex;
    [SerializeField] private AnimationCurve Nuke;
    float TimeHold=0;
    public AK.Wwise.Event Music,Pause,Resume, ElecStart, ElecEnd,Research,Factory,Spy,Assassin,War,Win,Lose,ClickSound,Warning;
    // Start is called before the first frame update
    private void Awake()
    {
        instance = this;
        PowerCapUI = GameObject.FindGameObjectWithTag("PowerCapUI").GetComponent<Text>();
        MoneyCapUI = GameObject.FindGameObjectWithTag("MoneyCapUI").GetComponent<Text>();
        PeoplSatsfactionCapUI = GameObject.FindGameObjectWithTag("PeoplSatsfactionCapUI").GetComponent<Text>();
        PowerComUI = GameObject.FindGameObjectWithTag("PowerComUI").GetComponent<Text>();
        MoneyComUI = GameObject.FindGameObjectWithTag("MoneyComUI").GetComponent<Text>();
        PeoplSatsfactionComUI = GameObject.FindGameObjectWithTag("PeoplSatsfactionComUI").GetComponent<Text>();
        Communism = new Party("Communism","المعسكر الشرقي",PowerComUI, MoneyComUI, PeoplSatsfactionComUI,Color.red,84);
        Capitalism = new Party("Capitalism","المعسكر الغربي",PowerCapUI, MoneyCapUI, PeoplSatsfactionCapUI,Color.blue,48);
        if (!IsTutorial)
            LoseMenu = transform.Find("LoseMenu").gameObject;
        ELectionWindow = GameObject.FindGameObjectWithTag("ElectionWindow");
        ActionMenu = transform.Find("ActionMenu").gameObject;
        CountryInfo = transform.Find("CountryInfo").gameObject;

        InfoPartyName = CountryInfo.transform.GetChild(0).GetComponent<ArabicFixer>();
        InfoPresName = GetChildTexts(CountryInfo.transform.GetChild(1));
        InfoPresPower = CountryInfo.transform.GetChild(2).GetComponent<Text>();
        InfoPresMoney = CountryInfo.transform.GetChild(3).GetComponent<Text>();
        InfoPresPeople = CountryInfo.transform.GetChild(4).GetComponent<Text>();
        InfoFacNumber = GetChildTexts(CountryInfo.transform.GetChild(5));
        InfoFacPower = CountryInfo.transform.GetChild(6).GetComponent<Text>();
        InfoFacMoney = CountryInfo.transform.GetChild(7).GetComponent<Text>();
        InfoFacPeople = CountryInfo.transform.GetChild(8).GetComponent<Text>();
        InfoTotalPower = CountryInfo.transform.GetChild(9).GetComponent<Text>();
        InfoTotalMoney = CountryInfo.transform.GetChild(10).GetComponent<Text>();
        InfoTotalPeople = CountryInfo.transform.GetChild(11).GetComponent<Text>();
        Presidency_months= CountryInfo.transform.GetChild(12).GetChild(0).GetComponent<Text>();
        Presidency_Remaining = CountryInfo.transform.GetChild(13).GetChild(0).GetComponent<Text>();
        
        InfoSpiesPower= CountryInfo.transform.GetChild(14).GetComponent<Text>();
        InfoSpiesNumber= GetChildTexts(CountryInfo.transform.GetChild(15));

        InfoWarNumber = GetChildTexts(CountryInfo.transform.GetChild(16));
        InfoWarPower= CountryInfo.transform.GetChild(17).GetComponent<Text>();
        InfoWarMoney= CountryInfo.transform.GetChild(18).GetComponent<Text>();
        InfoWarPeople= CountryInfo.transform.GetChild(19).GetComponent<Text>();
        foreach (Transform item in GameObject.FindGameObjectWithTag("Actions").transform)
            if (item.GetSiblingIndex() != 3)
                Actions[item.GetSiblingIndex()] = item;
        foreach (Transform item in ELectionWindow.transform)
            if (item.GetSiblingIndex() != 5)
                ElectedPresidents[item.GetSiblingIndex()] = item;
        title = GameManager.Info.transform.GetChild(0).GetComponent<Text>();
        breif = GameManager.Info.transform.GetChild(1).GetComponent<Text>();
        powerReq = GameManager.Info.transform.GetChild(2).GetComponent<Text>();
        MoneyReq = GameManager.Info.transform.GetChild(3).GetComponent<Text>();
        PeopleReq = GameManager.Info.transform.GetChild(4).GetComponent<Text>();
        TimeReq = GameManager.Info.transform.GetChild(5).GetComponent<Text>();
        PowerGain = GameManager.Info.transform.GetChild(6).GetComponent<Text>();
        MoneyGain = GameManager.Info.transform.GetChild(7).GetComponent<Text>();
        PeopleGain = GameManager.Info.transform.GetChild(8).GetComponent<Text>();
        monthly = GameManager.Info.transform.GetChild(9).gameObject;
        Quote = GameManager.Info.transform.GetChild(10).GetComponent<Text>();
        oneTime = GameManager.Info.transform.GetChild(11).gameObject;
        timeFixer = TimeReq.GetComponent<ArabicFixer>();
        titleFixer=title.GetComponent<ArabicFixer>();
        briefFixer = breif.GetComponent<ArabicFixer>();
        QuoteFixer = Quote.GetComponent<ArabicFixer>();

    }
    private Text[] GetChildTexts(Transform textsParent)
    {
        Text[] texts = new Text[textsParent.childCount];
        for (int i = 0; i < textsParent.childCount; i++)
        {
            texts[i] = textsParent.GetChild(i).GetComponent<Text>();
        }
        return texts;
    }
    private void Start()
    {
        ELectionWindow.SetActive(false);
        ElectionsLock = false;
        NotificationLock = false;
        NotificationIndex = 0;
        if (!IsTutorial)
        {
            Communism.Power = UnityEngine.Random.Range(800, 1200);
            Capitalism.Power = UnityEngine.Random.Range(800, 1200);
            Capitalism.Money = UnityEngine.Random.Range(10000000000, 30000000000);
            Communism.Money = UnityEngine.Random.Range(10000000000, 30000000000);
            Capitalism.PeoplSatsfaction = UnityEngine.Random.Range(30, 50);
            Communism.PeoplSatsfaction = UnityEngine.Random.Range(30, 50);

            PresidentChoosing(Communism, new President(PresidentNameRandomizer(UnityEngine.Random.Range(1, 25), Communism), RoundHalf(UnityEngine.Random.Range(0.5f, 4f)), UnityEngine.Random.Range(1, 5), RoundTenth(UnityEngine.Random.Range(0.2f, 2f))).Settle(Communism));
            PresidentChoosing(Capitalism, new President(PresidentNameRandomizer(UnityEngine.Random.Range(1, 25), Capitalism), RoundHalf(UnityEngine.Random.Range(0.5f, 4f)), UnityEngine.Random.Range(1, 5), RoundTenth(UnityEngine.Random.Range(0.2f, 2f))).Settle(Capitalism));
            // The first presidents also have six actions waiting for half-term.
            Communism.LateAgenda = RandomLateAgenda();
            Capitalism.LateAgenda = RandomLateAgenda();
            LoseMenu.SetActive(false);
        }
        else
        {
            
            Communism.Power = 1000;
            Capitalism.Power = 1200;
            Capitalism.Money = 10000000000;
            Communism.Money = 50000000000;
            Capitalism.PeoplSatsfaction = 50;
            Communism.PeoplSatsfaction = 50;
            PresidentChoosing(Communism, new President(PresidentNameRandomizer(0, Communism), 6f, -1, 0.2f).Settle(Communism));
            PresidentChoosing(Capitalism, new President(PresidentNameRandomizer(0, Capitalism), 2f, 2, 1f).Settle(Capitalism));
            PowerCapUI.transform.parent.parent.gameObject.SetActive(false);
            foreach (GameObject button in GameObject.FindGameObjectsWithTag("InfoCenter"))
                button.SetActive(false);
            foreach (GameObject button in GameObject.FindGameObjectsWithTag("SpeedUp"))
                button.SetActive(false);
            GameObject.Find("NukeMeter").SetActive(false);
        }
        

        
        
        ActionMenu.SetActive(false);
        CountryInfo.SetActive(false);
        LoseLock = false;
        CrisisMonthsLeft = 0;
        RollOpportunities();
        Lose.Stop(gameObject);
        Music.Post(gameObject);
    }

    // Update is called once per frame
    void Update()
    {
        if (!LoseLock)
        {
            uiUpdater(Communism);
            uiUpdater(Capitalism);
        }
    }
    void uiUpdater(Party party)
    {
        party.PowerUI.text = PowerTranslate(party.Power);
        party.MoneyUI.text = MoneyTranslate(party.Money);
        party.PeoplSatsfactionUI.text = party.PeoplSatsfaction.ToString();
        
    }
    // Power is a decimal number with no practical ceiling. These write it as plain digits and read it back.
    public static string PowerText(double power)
    {
        return Math.Round(power).ToString("0", System.Globalization.CultureInfo.InvariantCulture);
    }

    public static double ParsePower(string text)
    {
        return double.Parse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture);
    }

    public static bool TryParsePower(string text, out double power)
    {
        return double.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out power);
    }

    // Power reads like money: plain below 1,000, then K, M, B, T, Q with one decimal.
    public static string PowerTranslate(double power)
    {
        double size = Math.Abs(power);
        string sign = power < 0 ? "-" : "";
        if (size >= 1e15)
            return sign + (Math.Round(size * 10 / 1e15) / 10).ToString() + " Q";
        if (size >= 1e12)
            return sign + (Math.Round(size * 10 / 1e12) / 10).ToString() + " T";
        if (size >= 1e9)
            return sign + (Math.Round(size * 10 / 1e9) / 10).ToString() + " B";
        if (size >= 1e6)
            return sign + (Math.Round(size * 10 / 1e6) / 10).ToString() + " M";
        if (size >= 1e3)
            return sign + (Math.Round(size * 10 / 1e3) / 10).ToString() + " K";
        return sign + Math.Round(size).ToString();
    }

    public string MoneyTranslate(float mon)
    {
        if (mon >= 1000000000000000)
            return (Mathf.Round(mon * 10 / 1000000000000000) / 10).ToString() + " Q";
        else if (mon >= 1000000000000)
            return (Mathf.Round(mon * 10 / 1000000000000) / 10).ToString() + " T";
        else if (mon >= 1000000000)
            return (Mathf.Round(mon * 10 / 1000000000) / 10).ToString() + " B";
        else if (mon >= 1000000)
            return (Mathf.Round(mon * 10 / 1000000) / 10).ToString() + " M";
        else
            return (Mathf.Round(mon * 10 / 1000) / 10).ToString() + " K";
    }
    public float ReverseMoneyTranslate(string mon)
    {
        if (mon.Contains("Q"))
            return (float.Parse(mon.Substring(0, mon.Length - 2)) * 1000000000000000);
        else if (mon.Contains("T"))
            return (float.Parse(mon.Substring(0, mon.Length - 2)) * 1000000000000);
        else if (mon.Contains("B"))
            return (float.Parse(mon.Substring(0, mon.Length - 2)) * 1000000000);
        else if (mon.Contains("M"))
            return (float.Parse(mon.Substring(0, mon.Length - 2)) * 1000000);
        else
            return (float.Parse(mon.Substring(0, mon.Length - 2)) * 1000);
    }
    public void MonthInstansaite()
    {
        NotificationIndex = 0;
        CheckForAssassination();
        MonthRevnue(Capitalism);
        MonthRevnue(Communism);
        RollChaos();
        Drift();
        RollOpportunities();
        UpdateWorldBalance();
        if (GameManager.Com > 0.5f)
        {
            Capitalism.Power -= (int)((1 - GameManager.Com) * 2);
            Communism.Power+= (int)(GameManager.Com * 2);
        }
        else
        {
            Capitalism.Power+= (int)((1 - GameManager.Com) * 2);
            Communism.Power-= (int)(GameManager.Com * 2);
        }
        Capitalism.ClampStats();
        Communism.ClampStats();
        NukeMeterUpdater.nukeDanger = Nuke.Evaluate(GameManager.Com);
    }
    // All the difficulty numbers live in the Difficulty Manager asset (Assets/ColdWar3D/Resources/DifficultyManager).
    static DifficultyManager Tuning => DifficultyManager.Current;

    // Nuclear war breaks out when one side is this many times stronger than the other. The limit for each year is
    // drawn as a curve in the Difficulty Manager.
    public static float LosingRatio
    {
        get
        {
            DifficultyManager d = Tuning;
            float years = GameManager.Singleton != null ? Mathf.Max(0f, GameManager.Singleton.YearsElapsed) : 0f;
            float ratio = d.WarLimitAt(years);
            // A missile crisis brings war closer for a few months.
            if (CrisisMonthsLeft > 0)
                ratio = Mathf.Max(d.minimumRatio, ratio * d.crisisRatioFactor);
            return Mathf.Max(1.01f, ratio);
        }
    }

    // ----- Chaos -----
    // Random events hit one side or the other. They are rare in 1947 and grow more frequent every decade,
    // so the late game keeps throwing the balance off and the player has to keep correcting it.
    // The party is null for events that hit both sides. The text is ready to show, in English and Arabic.
    public static event Action<Party, ChaosKind, string, string> ChaosStruck;
    public static int CrisisMonthsLeft;

    // Each side's power wanders up or down a little every month, more and more as the decades pass.
    void Drift()
    {
        if (IsTutorial || LoseLock)
            return;
        float size = Mathf.Lerp(Tuning.driftStart, Tuning.driftEnd, Tuning.Era);
        foreach (Party party in new[] { Capitalism, Communism })
        {
            party.Power *= 1.0 + UnityEngine.Random.Range(-size, size);
            party.ClampStats();
        }
    }

    static int RandomIn(Vector2Int range) => UnityEngine.Random.Range(Mathf.Min(range.x, range.y), Mathf.Max(range.x, range.y) + 1);
    static float RandomIn(Vector2 range) => UnityEngine.Random.Range(Mathf.Min(range.x, range.y), Mathf.Max(range.x, range.y));

    void RollChaos()
    {
        if (CrisisMonthsLeft > 0)
            CrisisMonthsLeft--;
        if (IsTutorial || LoseLock)
            return;

        DifficultyManager d = Tuning;
        if (UnityEngine.Random.value > Mathf.Lerp(d.chaosChanceStart, d.chaosChanceEnd, d.Era))
            return;

        // Chaos always makes things worse: it strikes the weaker side, which widens the gap between the two.
        Party party = WeakerSide();
        var possible = new List<ChaosKind> { ChaosKind.EconomicCrash, ChaosKind.ArmsRace, ChaosKind.Scandal, ChaosKind.MissileCrisis };
        if (party.SpiesNumber > 0) possible.Add(ChaosKind.SpyCaught);
        if (party.FactoriesNumber > 0) possible.Add(ChaosKind.FactoryDisaster);
        if (party.WarNumber > 0) possible.Add(ChaosKind.WarLost);
        if (!party.Vacant) possible.Add(ChaosKind.Coup);
        ChaosKind kind = possible[UnityEngine.Random.Range(0, possible.Count)];

        string en = SideName(party, false), ar = SideName(party, true);
        switch (kind)
        {
            case ChaosKind.EconomicCrash:
            {
                int lost = RandomIn(d.crashLoss);
                party.Money *= 1f - lost / 100f;
                Announce(party, kind, "Economic crash! " + en + " loses " + lost + "% of its money", "انهيار اقتصادي! " + ar + " يخسرون " + lost + "% من أموالهم");
                break;
            }
            case ChaosKind.ArmsRace:
            {
                // The stronger side grows and the weaker side shrinks, by random amounts, pushing the two apart.
                bool redStronger = WeakerSide() == Capitalism;
                int up = RandomIn(d.armsRaceSwing), down = -RandomIn(d.armsRaceSwing);
                int blue = redStronger ? down : up, red = redStronger ? up : down;
                Capitalism.Power *= 1.0 + blue / 100.0;
                Communism.Power *= 1.0 + red / 100.0;
                foreach (Party side in new[] { Capitalism, Communism })
                {
                    side.sources();
                    side.ClampStats();
                }
                Announce(null, kind, "Arms race! Blue power " + Signed(blue) + "%, Red power " + Signed(red) + "%",
                    "سباق تسلح! قوة الزرق " + Signed(blue) + "%، قوة الحمر " + Signed(red) + "%");
                return;
            }
            case ChaosKind.Scandal:
                party.PeoplSatsfaction = 0;
                Announce(party, kind, "Scandal in " + en + "! Happiness collapses to 0", "فضيحة لدى " + ar + "! السعادة تنهار إلى 0");
                break;
            case ChaosKind.SpyCaught:
            {
                int caught = LoseShare(party.SpiesNumber, d.spiesLost);
                float share = caught / (float)party.SpiesNumber;
                party.SpiesTotalPower -= party.SpiesTotalPower * share;
                party.SpiesPowerPercent -= party.SpiesPowerPercent * share;
                party.SpiesNumber -= caught;
                Announce(party, kind, en + "'s spy network exposed: " + caught + " spies caught", "كشف شبكة تجسس لـ" + ar + ": القبض على " + caught + " من الجواسيس");
                break;
            }
            case ChaosKind.FactoryDisaster:
            {
                int destroyed = LoseShare(party.FactoriesNumber, d.factoriesLost);
                float share = destroyed / (float)party.FactoriesNumber;
                party.FactoriesTotalPower -= party.FactoriesTotalPower * share;
                party.FactoriesPowerPercent -= party.FactoriesPowerPercent * share;
                party.FactoriesTotalMoney -= party.FactoriesTotalMoney * share;
                party.FactoriesTotalpeople -= Mathf.RoundToInt(party.FactoriesTotalpeople * share);
                party.FactoriesNumber -= destroyed;
                Announce(party, kind, "Disaster destroys " + destroyed + " of " + en + "'s factories", "كارثة تدمر " + destroyed + " من مصانع " + ar);
                break;
            }
            case ChaosKind.WarLost:
            {
                int lost = LoseShare(party.WarNumber, d.warsLost);
                float share = lost / (float)party.WarNumber;
                party.WarTotalPower -= party.WarTotalPower * share;
                party.WarPowerPercent -= party.WarPowerPercent * share;
                party.WarTotalMoney -= party.WarTotalMoney * share;
                party.WarTotalpeople -= Mathf.RoundToInt(party.WarTotalpeople * share);
                party.WarNumber -= lost;
                Announce(party, kind, en + " lost " + lost + " proxy " + (lost == 1 ? "war" : "wars"), ar + " خسروا " + lost + " من الحروب بالوكالة");
                break;
            }
            case ChaosKind.Coup:
            {
                int months = StartVacancy(party);
                Announce(party, kind, "Coup in " + en + "! No president for " + months + " months", "انقلاب لدى " + ar + "! بلا رئيس لمدة " + months + " شهرًا");
                return;
            }
            case ChaosKind.MissileCrisis:
                CrisisMonthsLeft = d.crisisMonths;
                Announce(null, kind, "Missile crisis! War is closer for " + d.crisisMonths + " months", "أزمة صواريخ! الحرب أقرب لمدة " + d.crisisMonths + " أشهر");
                return;
        }

        party.sources();
        party.ClampStats();
    }

    static string SideName(Party party, bool arabic) => party != null && party.name == "Communism" ? (arabic ? "الحمر" : "Red") : (arabic ? "الزرق" : "Blue");
    static string Signed(int value) => (value > 0 ? "+" : "") + value;

    // The side with less power; a coin toss when they are exactly equal.
    Party WeakerSide()
    {
        if (Communism.Power == Capitalism.Power)
            return UnityEngine.Random.value < 0.5f ? Communism : Capitalism;
        return Communism.Power < Capitalism.Power ? Communism : Capitalism;
    }

    // How many of `count` are lost, between the two percentages, and always at least one.
    static int LoseShare(int count, Vector2Int percent)
    {
        float share = RandomIn(percent) / 100f;
        return Mathf.Clamp(Mathf.RoundToInt(count * share), 1, count);
    }

    void Announce(Party party, ChaosKind kind, string english, string arabic)
    {
        ChaosStruck?.Invoke(party, kind, english, arabic);
    }

    // ----- Vacant presidency -----
    // A coup, or an uprising of unhappy people, leaves the seat empty for a while. No election can be called
    // in that time, and when it ends a new president simply takes power.
    int StartVacancy(Party party)
    {
        int months = Mathf.Max(1, RandomIn(Tuning.vacancyMonths));
        PresidentOusted?.Invoke(party);
        party.president = new President("—", 0f, 0, 0f);
        party.LateAgenda = null;
        party.VacantMonths = months;
        party.VacantTotal = months;
        party.ElectionIndex = 0;
        party.sources();
        return months;
    }

    void ImposePresident(Party party)
    {
        President press = RandomCandidate(party);
        PresidentChoosing(party, press);
        // A new leader gets a fresh start: the people rally, so the side isn't overthrown again straight away.
        party.PeoplSatsfaction = Mathf.Max(party.PeoplSatsfaction, Tuning.newLeaderHappiness);
        party.LateAgenda = RandomLateAgenda();
        CarryOutAgenda(party, PresidentAgendas[UnityEngine.Random.Range(0, PresidentAgendas.Length)]);
        Announce(party, ChaosKind.NewPresident, SideName(party, false) + ": " + press.name + " seizes power", SideName(party, true) + ": " + press.name + " يستولي على السلطة");
    }

    President RandomCandidate(Party party)
    {
        // Power and money are a percentage of what the party has now, fixed for the term.
        DifficultyManager d = Tuning;
        return new President(PresidentNameRandomizer(UnityEngine.Random.Range(1, 26), party), RarelyNegative(RoundHalf(RandomIn(d.presidentPowerPercent))),
            RandomIn(d.presidentHappiness), RoundTenth(RandomIn(d.presidentMoneyPercent))).Settle(party);
    }

    // ----- Opportunities -----
    // Each month every side can only use some of its actions, picked at random.
    static readonly Dictionary<Party, HashSet<string>> available = new Dictionary<Party, HashSet<string>>();

    void RollOpportunities()
    {
        available.Clear();
        if (IsTutorial || !Tuning.useOpportunities)
            return;

        var all = new List<string>();
        foreach (Transform action in ActionMenu.transform)
        {
            if (action.GetComponent<Button>() != null)
                all.Add(action.name);
        }

        foreach (Party party in new[] { Capitalism, Communism })
        {
            int count = Mathf.Clamp(RandomIn(Tuning.actionsPerMonth), 1, all.Count);
            var pool = new List<string>(all);
            var chosen = new HashSet<string>();
            while (chosen.Count < count)
            {
                int pick = UnityEngine.Random.Range(0, pool.Count);
                chosen.Add(pool[pick]);
                pool.RemoveAt(pick);
            }
            available[party] = chosen;
        }

        ShowOpportunities();
    }

    // Shows only the selected side's available actions on the wheel.
    void ShowOpportunities()
    {
        if (IsTutorial || SelectedParty == null)
            return;
        bool limited = available.TryGetValue(SelectedParty, out HashSet<string> allowed);
        foreach (Transform action in ActionMenu.transform)
        {
            if (action.GetComponent<Button>() == null)
                continue;
            bool show = !limited || allowed.Contains(action.name);
            if (action.gameObject.activeSelf != show)
                action.gameObject.SetActive(show);
        }
    }

    // GameManager.Com is Red's share of the world (0.5 is balanced) and drives both the map and the danger meter.
    // Danger depends only on the current power ratio, measured against this year's limit on a log scale:
    // equal power is 0%, and reaching the limit is 100%. Balancing the powers brings the danger straight back down.
    void UpdateWorldBalance()
    {
        if (IsTutorial)
        {
            // The tutorial is scripted around the original rule, where the power gap adds up month after month.
            GameManager.Com = Mathf.Clamp01(GameManager.Com + (float)(0.0005 * (Communism.Power - Capitalism.Power) / 100));
            return;
        }

        double red = Math.Max(1.0, Communism.Power);
        double blue = Math.Max(1.0, Capitalism.Power);
        double ratio = Math.Max(red, blue) / Math.Min(red, blue);
        float danger = Mathf.Clamp01((float)(Math.Log(ratio) / Math.Log(LosingRatio)));
        // The danger curve is 200 x |Com - 0.5|, so this puts the meter, and the map, exactly at that danger.
        GameManager.Com = 0.5f + (red >= blue ? 1f : -1f) * danger * 0.5f;
    }

    private void CheckForAssassination()
    {
        //yield return new WaitForSeconds(0.01f * WaitTime);
        if (LoseLock) return;
        CheckAssassinationForParty(Capitalism);
        CheckAssassinationForParty(Communism);
    }
    private void CheckAssassinationForParty(Party party)
    {
        if(party.Actions.Length>0)
        foreach (ActionFunction action in party.Actions)
        {
            if (action == null) continue;
            if (action.done == "Assassination completed")
            {
                Pause.Post(gameObject);
                InfoNotifaction(action, party);
                Assassin.Stop(gameObject);
                Assassin.Post(gameObject);
                GetEnemy(party).Power = (GetEnemy(party).Power / 10) * UnityEngine.Random.Range(3, 7);
                party.RemoveAction(action);
            }
        }
    }
    public void MonthRevnue(Party party)
    {
        // The president's share is a percentage of the party's current power and money, so it is worked out fresh each month.
        party.sources();
        party.Power += party.powerSources;
        party.Money += party.MoneySources;
        party.PeoplSatsfaction += party.peopleSources;
        if (party.PeoplSatsfaction < 0)
            party.Power += party.PeoplSatsfaction;
        party.Power = Math.Max(0, party.Power);
        party.Money = Mathf.Clamp(party.Money, 0, 10000000000000000000);
        party.PeoplSatsfaction = Mathf.Clamp(party.PeoplSatsfaction, 0, 100);
        if (!LoseLock)
        {
            party.ElectionIndex++;
            if (party.Vacant)
            {
                party.VacantMonths--;
                if (party.VacantMonths == 0)
                    ImposePresident(party);
            }
            else if (party.ElectionIndex == party.PresedncyPeriod)
                if (!ElectionsLock)
                    Elections(party);
                else
                    StartCoroutine(MultiElections(party, false));
            else if (IsForcedOut(party))
            {
                int months = StartVacancy(party);
                Announce(party, ChaosKind.Uprising, "Unhappy people overthrow " + SideName(party, false) + "'s president! No president for " + months + " months",
                    "الشعب الغاضب يُسقط رئيس " + SideName(party, true) + "! بلا رئيس لمدة " + months + " شهرًا");
            }

            // Halfway through the term, the president's second set of actions happens at once.
            if (party.LateAgenda != null && party.ElectionIndex >= party.PresedncyPeriod / 2)
            {
                string[] late = party.LateAgenda;
                party.LateAgenda = null;
                CarryOutAgenda(party, late);
            }


            foreach (ActionFunction action in party.Actions)
            {
                if (action != null)
                {
                    if (action.time > 0)
                    {
                        action.time--;
                        if (action.time == 0)
                        {
                            if (ElectionsLock && !NotificationLock)
                            {
                                NotificationIndex++;
                                NotificationLock = true;
                                StartCoroutine(MultiActions(party, action, NotificationIndex));
                            }
                            else
                            {
                                StartCoroutine(MultiActions(party, action, NotificationIndex));
                            }

                            if (!action.Monthly)
                            {
                                party.Power += action.PowerFor(party);
                                party.Money += action.money;
                                party.PeoplSatsfaction += action.peoplSatsfaction;
                            }

                        }
                    }
                }
            }
        }
    }
    public void Do()
    {
        ClickSound.Post(gameObject);
        DOInstansaite(SelectedParty);
    }
    public void DOInstansaite(Party party)
    {
        if (party.Power >= ParsePower(powerReq.text))
        {
            if (party.Money >= ReverseMoneyTranslate(MoneyReq.text))
            {
                if (party.PeoplSatsfaction >= int.Parse(PeopleReq.text))
                {
                    if (party.ActionIndex < 3)
                    {
                        party.Power -= ParsePower(powerReq.text);
                        party.Money -= ReverseMoneyTranslate(MoneyReq.text);
                        party.PeoplSatsfaction -= int.Parse(PeopleReq.text);
                        int time = RequestedMonths;
                        party.Actions[party.ActionIndex] = new ActionFunction(GameManager.nameOfAction,GameManager.arabicNameOfAction,doneMassage,arabicDone, ParsePower(PowerGain.text), int.Parse(PeopleGain.text), time, ReverseMoneyTranslate(MoneyGain.text), monthly.gameObject.activeSelf) { percent = !IsTutorial };
                        RaiseLevel(party, doneMassage);
                        party.ActionIndex++;
                        if (!IsTutorial)
                            foreach (Button button in this.GetComponentsInChildren<Button>())
                                button.interactable = true;
                        GameManager.Singleton.ShowInfo(false);
                    }
                    else
                        Refuse(party, Refusal.QueueFull);
                }
                else
                    Refuse(party, Refusal.People);
            }
            else
                Refuse(party, Refusal.Money);
        }
        else
            Refuse(party, Refusal.Power);
    }

    // Each use of an action makes the next one cost more.
    static void RaiseLevel(Party party, string done)
    {
        if (done == "Factory Building Finished")
            party.FactoryLevel++;
        else if (done == "Proxy war started.")
            party.WarLevel++;
        else if (done == "Research Completed")
            party.researchLevel++;
        else if (done == "Spy is in place")
            party.SpiesLevel++;
        else if (done == "Assassination completed")
            party.assassinLevel++;
    }

    // Presidents arrive with three actions that take effect at once and don't use the three action slots.
    public static readonly string[][] PresidentAgendas =
    {
        new[] { "War", "War", "Spy" },
        new[] { "Factory", "Factory", "Factory" },
        new[] { "Research", "Factory", "Spy" },
        new[] { "Spy", "Spy", "War" },
        new[] { "Factory", "War", "Research" },
        new[] { "Factory", "Factory", "War" },
    };
    public string[][] CandidateAgendas = new string[5][];
    public string[][] CandidateLateAgendas = new string[5][];
    public President[] CandidatePresidents = new President[5];
    public static event Action<Party> PresidentOusted;

    // The second set of actions a president carries out halfway through the term.
    public const int LateAgendaSize = 6;
    static readonly string[] LateAgendaPool = { "Factory", "Factory", "Factory", "War", "War", "War", "Spy", "Spy", "Research" };
    static readonly string[] TutorialAgenda = { "Factory", "Factory", "Factory" };

    // Unhappy people can force a president out before the term ends.

    static string[] RandomLateAgenda()
    {
        var agenda = new string[LateAgendaSize];
        bool hasResearch = false;
        for (int i = 0; i < agenda.Length; i++)
        {
            string pick = LateAgendaPool[UnityEngine.Random.Range(0, LateAgendaPool.Length)];
            // One research at most, because research is the strongest action.
            if (pick == "Research" && hasResearch)
                pick = "Factory";
            hasResearch |= pick == "Research";
            agenda[i] = pick;
        }
        return agenda;
    }

    // The people only start to rise up after happiness has stayed below the line for a long stretch.
    // From then on, every month carries a chance of an uprising.
    bool IsForcedOut(Party party)
    {
        if (party.PeoplSatsfaction >= Tuning.unpopularBelow)
        {
            party.UnhappyMonths = 0;
            return false;
        }

        party.UnhappyMonths++;
        if (IsTutorial || party.UnhappyMonths <= Tuning.unhappyMonthsBeforeUprising)
            return false;
        if (party.ElectionIndex >= party.PresedncyPeriod)
            return false;
        if (UnityEngine.Random.value >= Tuning.uprisingChance)
            return false;
        party.UnhappyMonths = 0;
        return true;
    }

    static float RoundHalf(float value) => Mathf.Round(value * 2f) / 2f;
    static float RarelyNegative(float value) => UnityEngine.Random.value < Tuning.negativePresidentChance ? -value : value;
    static float RoundTenth(float value) => Mathf.Round(value * 10f) / 10f;

    public static string Percent(float value)
    {
        return (value > 0f ? "+" : "") + value.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "%";
    }
    public static event Action<Party, ActionFunction> ActionCompletedInstantly;

    void CarryOutAgenda(Party party, string[] agenda)
    {
        if (agenda == null) return;
        foreach (string actionName in agenda)
        {
            HoverInfo template = null;
            foreach (HoverInfo candidate in ActionMenu.GetComponentsInChildren<HoverInfo>(true))
                if (candidate.name == actionName) template = candidate;
            if (template == null) continue;

            ActionFunction action = template.CreateAction(party);
            // A president's research is worth half of one the party pays for.
            if (action.name == "Research")
                action.power *= IsTutorial ? 0.5 : Tuning.agendaResearchShare;
            RaiseLevel(party, action.done);
            CompleteAction(party, action);
            action.time = 0;
            ActionCompletedInstantly?.Invoke(party, action);
        }
        party.sources();
        party.ClampStats();
    }

    // What finishing an action does, shared by the normal timer and a president's agenda.
    void CompleteAction(Party party, ActionFunction action)
    {
        if (!action.Monthly)
        {
            party.Power += action.PowerFor(party);
            party.Money += action.money;
            party.PeoplSatsfaction += action.peoplSatsfaction;
        }
        if (action.done == "Factory Building Finished")
        {
            Factory.Stop(gameObject);
            Factory.Post(gameObject);
            party.FactoriesNumber++;
            if (action.percent) party.FactoriesPowerPercent += action.power; else party.FactoriesTotalPower += action.power;
            party.FactoriesTotalMoney += action.money;
            party.FactoriesTotalpeople += action.peoplSatsfaction;
        }
        else if (action.done == "Research Completed")
        {
            Research.Stop(gameObject);
            Research.Post(gameObject);
        }
        else if (action.done == "Spy is in place")
        {
            Spy.Stop(gameObject);
            Spy.Post(gameObject);
            party.SpiesNumber++;
            if (action.percent) party.SpiesPowerPercent += action.power; else party.SpiesTotalPower += action.power;
        }
        else if (action.done == "Proxy war started.")
        {
            War.Stop(gameObject);
            War.Post(gameObject);
            party.WarNumber++;
            if (action.percent) party.WarPowerPercent += action.power; else party.WarTotalPower += action.power;
            party.WarTotalMoney += action.money;
            party.WarTotalpeople += action.peoplSatsfaction;
        }
        else if (action.done == "Assassination completed")
        {
            Assassin.Stop(gameObject);
            Assassin.Post(gameObject);
            GetEnemy(party).Power = (GetEnemy(party).Power / 10) * UnityEngine.Random.Range(3, 7);
        }
    }

    // The board and HUD show why an action was refused, so play isn't interrupted by a message.
    void Refuse(Party party, Refusal reason)
    {
        Warning.Post(gameObject);
        Refused?.Invoke(party, reason);
    }

    public void CanText(string message,string arabic_message, Party party,bool IsWarning)
    {
        if (IsWarning)
        {
            Warning.Post(gameObject);
        }
        foreach (Button button in this.GetComponentsInChildren<Button>())
        {
            if (!IsTutorial)
                if (button.tag != "MassageButton"&&button.tag!="MapSector")
                    button.interactable = false;
        }
        GameObject game = Instantiate(MassageWindowPrefabe, transform);
        if (LanguageManager.Singlton.GetSelectedLanguag == Language.Arabic)
                game.transform.GetChild(0).GetComponent<ArabicFixer>().fixedText = arabic_message;
        else
                game.transform.GetChild(0).GetComponent<Text>().text = message;

        Time.timeScale = 0;
        if (message== "Are you sure you want to exit")
            game.transform.GetChild(2).gameObject.SetActive(true);
        else
        {
            game.transform.GetChild(2).gameObject.SetActive(false);
            game.GetComponent<Image>().color = party.color;
        }
    }
    public void SelectParty(string party)
    {
        if (party == "Com")
            SelectedParty = Communism;
        else
            SelectedParty = Capitalism;
            ActionMenu.GetComponent<Image>().color = SelectedParty.color;
            GameManager.Info.GetComponent<Image>().color = SelectedParty.color;
        ShowOpportunities();
    }
    public string PresidentNameRandomizer(int ran, Party party)
    {
        string returned_name;
        if (party.name == "Capitalism")
        {
            if (ran == 0)
                returned_name = "Thomas";
            else if (ran == 1)
                returned_name = "James";
            else if (ran == 2)
                returned_name = "Benjamin";
            else if (ran == 3)
                returned_name = "Philip";
            else if (ran == 4)
                returned_name = "Alexander";
            else if (ran == 5)
                returned_name = "Joseph";
            else if (ran == 6)
                returned_name = "Edward";
            else if (ran == 7)
                returned_name = "Henry";
            else if (ran == 8)
                returned_name = "William";
            else if (ran == 9)
                returned_name = "Harry";
            else if (ran == 10)
                returned_name = "George";
            else if (ran == 11)
                returned_name = "Robert";
            else if (ran == 12)
                returned_name = "Fred";
            else if (ran == 13)
                returned_name = "Albert";
            else if (ran == 14)
                returned_name = "Arthur";
            else if (ran == 15)
                returned_name = "Paul";
            else if (ran == 16)
                returned_name = "Clarence";
            else if (ran == 17)
                returned_name = "Leo";
            else if (ran == 18)
                returned_name = "Sam";
            else if (ran == 19)
                returned_name = "Charlie";
            else if (ran == 20)
                returned_name = "Louis";
            else if (ran == 21)
                returned_name = "Jack";
            else if (ran == 22)
                returned_name = "Jesse";
            else if (ran == 23)
                returned_name = "Richard";
            else if (ran == 24)
                returned_name = "Oscar";
            else if (ran == 25)
                returned_name = "Leon";
            else
                returned_name = "";
            int lastName = UnityEngine.Random.Range(0, 26);

            if (lastName == 0)
                returned_name += " Martinez";
            else if (lastName == 1)
                returned_name += " Hughes";
            else if (lastName == 2)
                returned_name += " Brown";
            else if (lastName == 3)
                returned_name += " Myers";
            else if (lastName == 4)
                returned_name += " Torres";
            else if (lastName == 5)
                returned_name += " Rogers";
            else if (lastName == 6)
                returned_name += " Adams";
            else if (lastName == 7)
                returned_name += " Richardson";
            else if (lastName == 8)
                returned_name += " Cook";
            else if (lastName == 9)
                returned_name += " Jenkins";
            else if (lastName == 10)
                returned_name += " Campbell";
            else if (lastName == 11)
                returned_name += " Hill";
            else if (lastName == 12)
                returned_name += " Gomez";
            else if (lastName == 13)
                returned_name += " Bell";
            else if (lastName == 14)
                returned_name += " Collins";
            else if (lastName == 15)
                returned_name += " Lopez";
            else if (lastName == 16)
                returned_name += " Cox";
            else if (lastName == 17)
                returned_name += " Edwards";
            else if (lastName == 18)
                returned_name += " Wood";
            else if (lastName == 19)
                returned_name += " Perez";
            else if (lastName == 20)
                returned_name += " Butler";
            else if (lastName == 21)
                returned_name += " Moore";
            else if (lastName == 22)
                returned_name += " Stewart";
            else if (lastName == 23)
                returned_name += " Walsh";
            else if (lastName == 24)
                returned_name += " Hopkins";
            else if (lastName == 25)
                returned_name += " Williams";
        }
        else 
        {
            if (ran == 0)
                returned_name= "Alvin ";
            else if (ran == 1)
                returned_name= "Eoin ";
            else if (ran == 2)
                returned_name= "Yann ";
            else if (ran == 3)
                returned_name= "Elias ";
            else if (ran == 4)
                returned_name= "Vladimir ";
            else if (ran == 5)
                returned_name= "Joseph ";
            else if (ran == 6)
                returned_name= "Oriel ";
            else if (ran == 7)
                returned_name= "Lennox ";
            else if (ran == 8)
                returned_name= "Ryan ";
            else if (ran == 9)
                returned_name= "Zibiah ";
            else if (ran == 10)
                returned_name= "Mel ";
            else if (ran == 11)
                returned_name= "Slavik ";
            else if (ran == 12)
                returned_name= "Ivan ";
            else if (ran == 13)
                returned_name= "Henrique ";
            else if (ran == 14)
                returned_name= "Adam ";
            else if (ran == 15)
                returned_name= "Shawn ";
            else if (ran == 16)
                returned_name= "Dionte ";
            else if (ran == 17)
                returned_name= "Oriel ";
            else if (ran == 18)
                returned_name= "Stefan ";
            else if (ran == 19)
                returned_name= "Vladimir ";
            else if (ran == 20)
                returned_name= "Vadim ";
            else if (ran == 21)
                returned_name= "Fyodor ";
            else if (ran == 22)
                returned_name= "Maksimillian ";
            else if (ran == 23)
                returned_name= "Connor ";
            else if (ran == 24)
                returned_name= "Venus ";
            else if (ran == 25)
                returned_name= "Stephen Mendoza";
            else returned_name = "";

            int lastName = UnityEngine.Random.Range(0, 26);
            if (IsTutorial)
                lastName = 2;
            if (lastName == 0)
                returned_name += "Solovyov";
            else if (lastName == 1)
                returned_name += "Kozlov";
            else if (lastName == 2)
                returned_name += "Golubev";
            else if (lastName == 3)
                returned_name += "Volkov";
            else if (lastName == 4)
                returned_name += "Smirnov";
            else if (lastName == 5)
                returned_name += "Rogers";
            else if (lastName == 6)
                returned_name += "Ivanov";
            else if (lastName == 7)
                returned_name += "Morozov";
            else if (lastName == 8)
                returned_name += "Petrov";
            else if (lastName == 9)
                returned_name += "Vinogradov";
            else if (lastName == 10)
                returned_name += "Balliol";
            else if (lastName == 11)
                returned_name += "York";
            else if (lastName == 12)
                returned_name += "Alpin";
            else if (lastName == 13)
                returned_name += "Bruce";
            else if (lastName == 14)
                returned_name += "Hanover";
            else if (lastName == 15)
                returned_name += "Sokolov";
            else if (lastName == 16)
                returned_name += "Vinogradov";
            else if (lastName == 17)
                returned_name += "Zaytsev";
            else if (lastName == 18)
                returned_name += "Ivanov";
            else if (lastName == 19)
                returned_name += "Dunkeld";
            else if (lastName == 20)
                returned_name += "Balliol";
            else if (lastName == 21)
                returned_name += "Oneill";
            else if (lastName == 22)
                returned_name += "Kaufman";
            else if (lastName == 23)
                returned_name += "Patrick";
            else if (lastName == 24)
                returned_name += "Gaines";
            else if (lastName == 25)
                returned_name += "Mendoza";
        }
        return returned_name;
    }
    public void PresidentChoosing(Party party, President pres)
    {
        party.president = pres;
        party.sources();
        party.ElectionIndex = 0;
    }
    public void Elections(Party party)
    {
        Elections(party, false);
    }
    public void Elections(Party party, bool forced)
    {
        // No election can be called while the presidency is vacant.
        if (party.Vacant)
        {
            Refuse(party, Refusal.PresidentTooNew);
            return;
        }
        if (forced || party.ElectionIndex>party.TempPresedncy)
        {
            Time.timeScale = 0;
            ActionMenu.SetActive(false);
            ElectionsLock = true;
            ELectionWindow.GetComponent<Image>().color = party.color;
            ElecStart.Post(gameObject);
            ELectionWindow.SetActive(true);
            
            for (int row = 0; row < ElectedPresidents.Length; row++)
            {
                Transform item = ElectedPresidents[row];
                if (item == null) continue;
                // Power and money are a monthly percentage of what the party has, so presidents matter in every era.
                // Power is almost always positive: one candidate in a hundred takes it away instead. Happiness is anywhere from -20 to +20.
                President press = RandomCandidate(party);
                CandidateAgendas[row] = PresidentAgendas[UnityEngine.Random.Range(0, PresidentAgendas.Length)];
                // The tutorial's chosen president weakens Red's power but builds three factories on arrival.
                if (IsTutorial && row == 0)
                {
                    press = new President(PresidentNameRandomizer(2, party), -8f, UnityEngine.Random.Range(-20, 20), 0.5f);
                    CandidateAgendas[row] = TutorialAgenda;
                }
                press.Settle(party);
                CandidateLateAgendas[row] = RandomLateAgenda();
                CandidatePresidents[row] = press;
                item.GetChild(0).gameObject.GetComponent<Text>().text = press.name;
                item.GetChild(1).gameObject.GetComponent<Text>().text = (press.Power > 0 ? "+" : "") + PowerTranslate(press.Power);
                item.GetChild(2).gameObject.GetComponent<Text>().text = "+" + MoneyTranslate(press.Money);
                item.GetChild(3).gameObject.GetComponent<Text>().text = press.PeoplSatsfaction.ToString();
            }
            if (!IsTutorial)
            {
                foreach (Button button in GameManager.Canvas.GetComponentsInChildren<Button>())
                    if (button.tag == "SpeedUp")
                        button.interactable = false;
            }
            SelectedParty = party;
        }
        else
        {
            Refuse(party, Refusal.PresidentTooNew);
        }

    }
    public void ElectionInstanatie()
    {
        Elections(SelectedParty);
        NotificationIndex++;
    }
    public void PresidentPress(GameObject button)
    {
        ClickSound.Post(gameObject);
        int row = System.Array.IndexOf(ElectedPresidents, button.transform.parent);
        if (row < 0 || row >= CandidatePresidents.Length || CandidatePresidents[row] == null)
            return;
        PresidentChoosing(SelectedParty, CandidatePresidents[row]);
        SelectedParty.LateAgenda = CandidateLateAgendas[row];
        CarryOutAgenda(SelectedParty, CandidateAgendas[row]);
        ELectionWindow.SetActive(false);
        GameManager.Singleton.ShowInfo(false);
        ActionMenu.SetActive(false);
        ElecEnd.Post(gameObject);
        ElectionsLock = false;
        Time.timeScale = 1;
        NotificationLock = false;
        if (!IsTutorial)
        foreach (Button speed in GameManager.Canvas.GetComponentsInChildren<Button>())
            if (speed.tag == "SpeedUp")
                speed.interactable = true;
    }
    public void CenterPress(string party)
    {
        if (Time.timeScale>0)
        {
            // Tapping the other capital while a wheel is open switches to that capital's wheel instead of closing it.
            bool switching = ActionMenu.activeSelf && SelectedParty != (party == "Com" ? Communism : Capitalism);
            SelectParty(party);
            CountryInfo.SetActive(false);
            ActionMenu.SetActive(switching || !ActionMenu.activeSelf);
            GameManager.Singleton.ShowInfo(false);
            LanguageManager.Singlton.EnableButton(false);
            ClickSound.Post(gameObject);
            if (!IsTutorial)
                foreach (Button button in GameManager.Canvas.GetComponentsInChildren<Button>())
                    button.interactable = true;
        }
    }
    public void MapPress()
    {
        if (Time.timeScale > 0)
        {
            if (ActionMenu.activeSelf||CountryInfo.activeSelf)
                ClickSound.Post(gameObject);
            ActionMenu.SetActive(false);
            CountryInfo.SetActive(false);
            GameManager.Singleton.ShowInfo(false);
        }
    }
    public void win()
    {
        //foreach (GameObject button in GameObject.FindGameObjectsWithTag("SpeedUp"))
        //    button.SetActive(false);
        Time.timeScale = 0;
        Win.Post(gameObject);
        PlayerPrefs.SetInt("EndlessMode", 1);
    }
    public void ContinueGame()
    {
        Win.Stop(gameObject);
        Music.Post(gameObject);
        Time.timeScale = 1;
    }
    public void lose()
    {
        Time.timeScale = 0;
        foreach (GameObject button in GameObject.FindGameObjectsWithTag("SpeedUp"))
            button.SetActive(false);
        LanguageManager.Singlton.HideButton();
        ActionMenu.SetActive(false);
        ELectionWindow.SetActive(false);
        Lose.Post(gameObject);
        LoseMenu.SetActive(true);
        LoseLock = true;
    }
    public void ShutUp()
    {
        Lose.Stop(gameObject);
    }
    IEnumerator MultiElections(Party party, bool forced)
    {
        yield return new WaitForSeconds(0.1f);
        Elections(party, forced);
    }
    IEnumerator MultiActions(Party party, ActionFunction action, int WaitTime)
    {
        NotificationIndex++;
        yield return new WaitForSeconds(0.5f * WaitTime);
        if (!LoseLock)
        {
            InfoNotifaction(action, party);
            CompleteMonthlyEffects(party, action);

            party.RemoveAction(action);
            party.sources();
            party.ClampStats();
        }
    }
    // The one-time gains are applied when the timer runs out, so only the ongoing part is added here.
    void CompleteMonthlyEffects(Party party, ActionFunction action)
    {
        bool monthly = action.Monthly;
        action.Monthly = true;
        CompleteAction(party, action);
        action.Monthly = monthly;
    }
    private void InfoNotifaction(ActionFunction action, Party party)
    {
        // The 3D board shows every finished action, so the text ribbons only appear without it.
        if (WarTable.IsRunning) return;
        Notification notification = Instantiate(notifactionPrefab, notifcationCenter);
        notification.InitiateMessage(action, party);
    }
    Party GetEnemy(Party party)
    {
        if (party.name == Communism.name)
            return Capitalism;
        else
            return Communism;
    }
    public void ResumeAll()
    {
        Resume.Post(gameObject);
    }
    public void PauseFunction()
    {
        if (Time.timeScale!=0)
        {
            Pause.Post(gameObject);
            TimeHold = Time.timeScale;
            Time.timeScale = 0;
        }
        else
        {
            Resume.Post(gameObject);
            Time.timeScale = TimeHold;
        }
            
        
    }
    public static float TimeBeforeExit = 1f;
    public void Exit()
    {
        TimeBeforeExit = Time.timeScale;
        CanText("Are you sure you want to exit","هل انت متأكد من رغبتك في الخروج", SelectedParty, true);
    }
}

public class ActionFunction
{
    public string name;
    public string arabicName;
    public double power;
    // When true, power is a percentage of the side's own power (main game); otherwise a fixed amount (tutorial).
    public bool percent;
    public double PowerFor(Party party) => percent ? party.Power * power / 100.0 : power;
    public int peoplSatsfaction,time;
    public float money;
    public bool Monthly;
    public string done;
    public string arabicDone;
    public ActionFunction( string _name,string _arabicName,string _done,string _arabic, double _power, int _peoplSatsfaction, int _time, float _money,bool _monthly)
    {
        name = _name;
        arabicName = _arabicName;
        done = _done;
        arabicDone = _arabic;
        power = _power;
        peoplSatsfaction = _peoplSatsfaction;
        money = _money;
        time = _time;
        Monthly = _monthly;
    }
}
public class Party
{
    public string name;
    public string arabicName;
    // Power is a decimal number, like money, so it has no practical ceiling.
    public double Power, powerSources;
    public int PeoplSatsfaction, peopleSources,ActionIndex, PresedncyPeriod;
    public  float Money, MoneySources;
    public  Text PowerUI, MoneyUI, PeoplSatsfactionUI;
    public ActionFunction[] Actions ;
    public Color color;

    public President president;
    public string[] LateAgenda;
    // Months left with no president, after a coup or an uprising.
    public int VacantMonths, VacantTotal;
    // Months in a row with happiness below the uprising line.
    public int UnhappyMonths;
    public bool Vacant => VacantMonths > 0;
    public int ElectionIndex=0;
    public int TempPresedncy=12;

    public int FactoryLevel = 1;
    public int FactoriesNumber = 0;
    public double FactoriesTotalPower = 0;
    // In the main game, factories, wars and spies add a percentage of the side's own power every month.
    public double FactoriesPowerPercent, WarPowerPercent, SpiesPowerPercent;
    public double FactoriesPowerNow => FactoriesTotalPower + Power * FactoriesPowerPercent / 100.0;
    public double WarPowerNow => WarTotalPower + Power * WarPowerPercent / 100.0;
    public double SpiesPowerNow => SpiesTotalPower + Power * SpiesPowerPercent / 100.0;
    public float FactoriesTotalMoney = 0;
    public int FactoriesTotalpeople = 0;

    public int SpiesLevel = 0;
    public int SpiesNumber = 0;
    public double SpiesTotalPower = 0;

    public int WarLevel = 0;
    public int WarNumber = 0;
    public double WarTotalPower = 0;
    public float WarTotalMoney = 0;
    public int WarTotalpeople = 0;

    public int researchLevel;
    public int assassinLevel;
    public Party(string _name,string _arabicName,Text powerUI, Text moneyUI, Text peoplSatsfactionUI,Color _color, int _PresedncyPeriod)
    {
        name = _name;   
        arabicName = _arabicName;
        ActionIndex = 0;
        Actions = new ActionFunction[3];
        PowerUI = powerUI;
        MoneyUI = moneyUI;
        PeoplSatsfactionUI = peoplSatsfactionUI;
        color = _color;
        PresedncyPeriod = _PresedncyPeriod;
    }
    public void ClampStats()
    {
        Power = Math.Max(0, Power);
        Money = Mathf.Clamp(Money, 0, 10000000000000000000);
        PeoplSatsfaction = Mathf.Clamp(PeoplSatsfaction, 0, 100);
    }
    // Closes the gap and recounts, so ActionIndex can't drift from what's in the slots.
    public void RemoveAction(ActionFunction action)
    {
        int index = System.Array.IndexOf(Actions, action);
        if (index >= 0)
            Actions[index] = null;
        int count = 0;
        for (int i = 0; i < Actions.Length; i++)
        {
            ActionFunction current = Actions[i];
            if (current == null) continue;
            Actions[i] = null;
            Actions[count++] = current;
        }
        ActionIndex = count;
    }
    // What the president adds this month: a percentage of the party's current power and money.
    // A president gives (or takes) the same amount every month of the term, fixed when they stood for election.
    public double PresidentPower => president == null ? 0 : president.Power;
    public float PresidentMoney => president == null ? 0f : president.Money;
    public void sources()
    {
        powerSources = FactoriesPowerNow + SpiesPowerNow + PresidentPower + WarPowerNow;
        MoneySources = FactoriesTotalMoney + PresidentMoney+WarTotalMoney;
        peopleSources = FactoriesTotalpeople + president.PeoplSatsfaction+WarTotalpeople;
    }
}
public class President
{
    public string name;
    // A candidate is drawn as a percentage of the party's power and money; Settle turns that into fixed monthly amounts.
    public float PowerPercent, MoneyPercent;
    public double Power;
    public float Money;
    public int PeoplSatsfaction;

    public President(string name, float powerPercent, int peoplSatsfaction, float moneyPercent)
    {
        this.name = name;
        PowerPercent = powerPercent;
        PeoplSatsfaction = peoplSatsfaction;
        MoneyPercent = moneyPercent;
    }

    // Fixes the monthly amounts from the party's power and money right now. They stay the same for the whole term.
    public President Settle(Party party)
    {
        Power = Math.Round(party.Power * PowerPercent / 100.0);
        Money = party.Money * MoneyPercent / 100f;
        return this;
    }
}
