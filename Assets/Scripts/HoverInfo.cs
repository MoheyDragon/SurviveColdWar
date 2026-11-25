using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class HoverInfo : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
#if UNITY_STANDALONE || UNITY_EDITOR
    , IPointerEnterHandler, IPointerExitHandler
#endif
{
    public new string name;
    public string title, breif,Quote,powerReq, PeopleReq, TimeReq, PowerGain, PeopleGain,doneMassage;
    [Space]
    [Header("Arabic")]
    [Space]
    [SerializeField] string arabicTitle;
    [SerializeField] string arabicBreif,arabicQuote, arabicDoneMassage;

    public float MoneyReq, MoneyGain;
    public bool Monthly;
    CountryManager manager;
    public AK.Wwise.Event ClickSound,TuorialSound;
    private Button button;

    private Coroutine holdTimerCoroutine;
    private bool longPressTriggered = false;
    private float holdThreshold = 0.3f; // adjust as needed

    private void Start()
    {
        manager = transform.parent.GetComponentInParent<CountryManager>();
        SetupButton();
    }

    private void SetupButton()
    {
        button = GetComponent<Button>();
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        SetupDetails();
        longPressTriggered = false;
        holdTimerCoroutine = StartCoroutine(HoldTimer());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (holdTimerCoroutine != null)
        {
            StopCoroutine(holdTimerCoroutine);
            ShowInfo(false);
        }

        if (!longPressTriggered && !Tutorial.ActionsLocked)
        {
            LaunchAction();
        }
    }

    private IEnumerator HoldTimer()
    {
        yield return new WaitForSeconds(holdThreshold);
        longPressTriggered = true;
        ShowInfo(true);
    }
    // The tutorial plays an action's voice line as soon as the action wheel opens, before the action can be pressed.
    public void PlayTutorialLine()
    {
        QuotePlay();
    }
    private void SetupDetails()
    {
        GameManager.nameOfAction = name;
        GameManager.arabicNameOfAction = arabicTitle;
        if(LanguageManager.Singlton.GetSelectedLanguag==Language.Arabic)
        {
            CountryManager.titleFixer.fixedText = arabicTitle;
            CountryManager.briefFixer.fixedText= arabicBreif;
            CountryManager.QuoteFixer.fixedText= arabicQuote;

            CountryManager.breif.alignment = TextAnchor.MiddleRight;
            CountryManager.Quote.alignment = TextAnchor.MiddleRight;
        }
        else
        {
            CountryManager.title.text= title;
            CountryManager.breif.text= breif;
            CountryManager.Quote.text = Quote;

            CountryManager.breif.alignment = TextAnchor.MiddleLeft;
            CountryManager.Quote.alignment = TextAnchor.MiddleLeft;
        }
        CountryManager.doneMassage = doneMassage;
        CountryManager.arabicDone = arabicDoneMassage;

        CountryManager.powerReq.text = CountryManager.PowerText(PowerCost(CountryManager.SelectedParty));
        CountryManager.MoneyReq.text = MoneyTranslate(MoneyCost(CountryManager.SelectedParty));
        CountryManager.PeopleReq.text = PeopleReq;
        // Read directly later, because the Arabic text field is only rewritten on a later frame.
        CountryManager.RequestedMonths = int.Parse(TimeReq);
        if (LanguageManager.Singlton.GetSelectedLanguag == Language.Arabic)
        {
            CountryManager.timeFixer.fixedText= "بعد " + TimeReq;
            CountryManager.TimeReq.alignment = TextAnchor.MiddleRight;
        }
        else
        {
            CountryManager.TimeReq.text = "after " + TimeReq;
            CountryManager.TimeReq.alignment = TextAnchor.MiddleLeft;
        }
        // Percentages keep their decimals (0.3); fixed amounts are whole numbers.
        double gain = PowerGainFor(CountryManager.SelectedParty);
        CountryManager.PowerGain.text = UsesPercent ? gain.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) : CountryManager.PowerText(gain);
        CountryManager.MoneyGain.text = MoneyTranslate(MoneyGainFor(CountryManager.SelectedParty));
        CountryManager.PeopleGain.text = PeopleGain;
        CountryManager.monthly.gameObject.SetActive(Monthly);
        CountryManager.oneTime.gameObject.SetActive(!Monthly);
        //if (!manager.IsTutorial)
        //    foreach (Button button in GameManager.Canvas.GetComponentsInChildren<Button>())
        //        if (button.tag != "SpeedUp" && button.tag != "MapSector" && button.tag != "InfoCenter")
        //            button.interactable = false;
    }
    static DifficultyManager Tuning => DifficultyManager.Current;
    // The manager is found in Start, but a button that has never been shown hasn't run Start yet (presidents' agendas
    // and the Test Panel use these buttons too), so it is looked up here as well.
    bool UsesPercent
    {
        get
        {
            if (manager == null)
                manager = CountryManager.instance;
            return manager != null && !manager.IsTutorial;
        }
    }

    // Main game: a percentage of the side's own power, growing with each earlier use. Tutorial: the original amounts.
    public double PowerCost(Party party)
    {
        if (UsesPercent)
        {
            DifficultyManager d = Tuning;
            double percent = 0, perUse = 0;
            int uses = 0;
            if (name == "Factory") { percent = d.factoryCost; perUse = d.factoryCostPerUse; uses = party.FactoryLevel - 1; }
            else if (name == "War") { percent = d.warCost; perUse = d.warCostPerUse; uses = party.WarLevel; }
            else if (name == "Research") { percent = d.researchCost; perUse = d.researchCostPerUse; uses = party.researchLevel; }
            else if (name == "Spy") { percent = d.spyCost; perUse = d.spyCostPerUse; uses = party.SpiesLevel; }
            else if (name == "Assassin") percent = d.assassinCost;
            double era = d.risingPowerCosts ? Mathf.Lerp(1f, d.powerCostBy1991, d.Era) : 1.0;
            double cost = party.Power * Math.Min(d.maxCostPercent, percent * (1.0 + perUse * Math.Max(0, uses)) * era) / 100.0;
            // Quietly helps the side that is falling behind once war gets close, so a bad stretch can be recovered.
            if (NukeMeterUpdater.nukeDanger > d.catchUpAboveDanger && CountryManager.instance.IsWeaker(party))
                cost *= 1.0 - d.catchUpDiscount;
            return Math.Round(cost);
        }
        return FixedPowerCost(party);
    }

    int FixedPowerCost(Party party)
    {
        int cost = int.Parse(powerReq);
        if (title == "Build a new Factory")
            return cost + party.FactoryLevel * 25;
        if (name == "War")
            return cost + party.WarLevel * 25;
        if (name == "Research")
            return cost + cost * party.researchLevel / 5;
        if (name == "Spy")
            return cost + cost * party.SpiesLevel;
        return cost;
    }
    // Money costs are fixed amounts.
    public float MoneyCost(Party party)
    {
        return FixedMoneyCost(party);
    }

    float FixedMoneyCost(Party party)
    {
        if (title == "Build a new Factory")
            return MoneyReq + ((ulong)party.FactoryLevel * 500000000ul);
        if (name == "War")
            return MoneyReq + (party.WarLevel * 1000000);
        if (name == "Research")
            return MoneyReq + (MoneyReq * party.researchLevel);
        if (name == "Spy")
            return MoneyReq + (party.SpiesLevel * 1000000000);
        if (name == "Assassin")
            return MoneyReq * Mathf.Pow(2, party.assassinLevel);
        return MoneyReq;
    }
    public int PeopleCost()
    {
        return int.Parse(PeopleReq);
    }
    // Main game: a percentage of the side's own power (monthly for factories, wars and spies; once for research).
    // Tutorial: the original fixed amounts.
    public double PowerGainFor(Party party)
    {
        if (UsesPercent)
        {
            DifficultyManager d = Tuning;
            // Diminishing returns: each earlier one makes the next add a little less.
            if (name == "Factory") return d.factoryGain / (1.0 + d.gainFalloff * Math.Max(0, party.FactoryLevel - 1));
            if (name == "War") return d.warGain / (1.0 + d.gainFalloff * party.WarLevel);
            if (name == "Spy") return d.spyGain / (1.0 + d.gainFalloff * party.SpiesLevel);
            if (name == "Research") return d.researchGain;
            return 0;
        }
        return FixedPowerGain(party);
    }

    double FixedPowerGain(Party party)
    {
        int gain = int.Parse(PowerGain);
        if (title == "Build a new Factory")
            return gain + party.FactoryLevel * 2;
        if (name == "War")
            return gain + party.WarLevel * 5;
        if (name == "Research")
        {
            // What finishing research gave before, now shown up front, or a share of current power once that is larger,
            // so research stays worth it as the game grows.
            int level = party.researchLevel;
            return gain * (1 + level) * (2 + level);
        }
        if (name == "Spy")
            return gain + gain * party.SpiesLevel / 5;
        return gain;
    }
    public float MoneyGainFor(Party party)
    {
        if (title == "Build a new Factory")
            return (ulong)MoneyGain * 10 * (ulong)party.FactoryLevel;
        if (name == "War")
            return MoneyGain + ((ulong)party.WarLevel * 100000ul);
        return MoneyGain;
    }
    // Builds this action with the values it would have if the party launched it now.
    public ActionFunction CreateAction(Party party)
    {
        return new ActionFunction(name, arabicTitle, doneMassage, arabicDoneMassage, PowerGainFor(party), int.Parse(PeopleGain),
            int.Parse(TimeReq), MoneyGainFor(party), Monthly) { percent = UsesPercent };
    }
#if UNITY_STANDALONE || UNITY_EDITOR
    // With a mouse, resting on a button previews its details without pressing it.
    bool hovering;
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (manager == null || CountryManager.SelectedParty == null) return;
        hovering = true;
        SetupDetails();
        ShowInfo(true);
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        if (!hovering) return;
        hovering = false;
        ShowInfo(false);
    }
#endif
    private void ShowInfo(bool show)
    {
        GameManager.Singleton.ShowInfo(show);
        if (show) return;
        CountryManager.titleFixer.fixedText = "";
        CountryManager.briefFixer.fixedText = "";
        CountryManager.QuoteFixer.fixedText = "";
        CountryManager.timeFixer.fixedText = "";

    }
    public void LaunchAction()
    {
        CountryManager.instance.Do();
        ShowInfo(false);
    }
    public string MoneyTranslate(float mon)
    {
        return CountryManager.instance.MoneyTranslate(mon);
    }
    bool quotePlaying;
    static float voiceLineEnds;
    // True while a tutorial voice line plays. Input waits for it, with a time limit in case the audio never reports its end.
    public static bool VoiceLinePlaying => Time.realtimeSinceStartup < voiceLineEnds;
    void QuoteEnd(object in_cookie, AkCallbackType in_type, object in_info)
    {

        if (in_type == AkCallbackType.AK_EndOfEvent)
        {
            quotePlaying = false;
            voiceLineEnds = 0f;
            if (name == "War")
            {
                manager.gameObject.GetComponent<Tutorial>().Next(false);
                Time.timeScale = 0.0000000001f;
                GameManager.Info.transform.GetChild(21).GetComponent<Button>().interactable = true;
            }
            else if (name == "Assassin")
            { }
            else
            {
                // The factory line leads into "build 2 factories", so the tutorial moves on once it has been heard.
                if (name == "Factory")
                    manager.gameObject.GetComponent<Tutorial>().Next(false);
                Time.timeScale = 1;
            }
        }
    }
    void QuotePlay()
    {
        // Each finished quote can advance the tutorial, so repeat taps must not queue more.
        if (quotePlaying) return;
        quotePlaying = true;
        voiceLineEnds = Time.realtimeSinceStartup + 15f;
        Time.timeScale = 0;
        TuorialSound.Post(gameObject, (uint)AkCallbackType.AK_EndOfEvent, QuoteEnd);
    }
}
