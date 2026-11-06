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
        if(manager.IsTutorial)
        {
            ShowInfo(true);
            return;
        }
        longPressTriggered = false;
        holdTimerCoroutine = StartCoroutine(HoldTimer());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (manager.IsTutorial) return;
        if (holdTimerCoroutine != null)
        {
            StopCoroutine(holdTimerCoroutine);
            ShowInfo(false);
        }

        if (!longPressTriggered)
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
    private void SetupDetails()
    {
        if (manager.IsTutorial)
        {
            if (Tutorial.Level < 50)
            {
                if (name == "War" && Tutorial.Level == 12)
                    QuotePlay();
                if (name == "Research" && Tutorial.Level == 33)
                    QuotePlay();
                if (name == "Factory" && Tutorial.Level == 20)
                    QuotePlay();
                if (name == "Spy" && Tutorial.Level == 31)
                    QuotePlay();
                if (name == "Assassin" && Tutorial.Level == 36)
                    QuotePlay();
            }

        }

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

        CountryManager.powerReq.text = PowerCost(CountryManager.SelectedParty).ToString();
        CountryManager.MoneyReq.text = MoneyTranslate(MoneyCost(CountryManager.SelectedParty));
        CountryManager.PeopleReq.text = PeopleReq;
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
        if (title == "Build a new Factory")
            CountryManager.PowerGain.text = (int.Parse(PowerGain) + CountryManager.SelectedParty.FactoryLevel * 2).ToString();
        else if (name == "War")
            CountryManager.PowerGain.text = (int.Parse(PowerGain) + CountryManager.SelectedParty.WarLevel * 5).ToString();
        else if (name == "Research")
            CountryManager.PowerGain.text = (int.Parse(PowerGain)+(int.Parse(PowerGain) * CountryManager.SelectedParty.researchLevel)).ToString();
        else if (name == "Spy")
            CountryManager.PowerGain.text = (int.Parse(PowerGain) +((int.Parse(PowerGain) *CountryManager.SelectedParty.SpiesLevel /5))).ToString();
        else
            CountryManager.PowerGain.text = PowerGain;
        if (title == "Build a new Factory")
            CountryManager.MoneyGain.text = MoneyTranslate((ulong)MoneyGain * 10 * (ulong)CountryManager.SelectedParty.FactoryLevel);
        else if (name == "War")
            CountryManager.MoneyGain.text = MoneyTranslate(MoneyGain + ((ulong)CountryManager.SelectedParty.WarLevel * 100000ul));
        else
            CountryManager.MoneyGain.text = MoneyTranslate(MoneyGain);
        CountryManager.PeopleGain.text = PeopleGain;
        CountryManager.monthly.gameObject.SetActive(Monthly);
        CountryManager.oneTime.gameObject.SetActive(!Monthly);
        //if (!manager.IsTutorial)
        //    foreach (Button button in GameManager.Canvas.GetComponentsInChildren<Button>())
        //        if (button.tag != "SpeedUp" && button.tag != "MapSector" && button.tag != "InfoCenter")
        //            button.interactable = false;
    }
    public int PowerCost(Party party)
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
    public float MoneyCost(Party party)
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
#if UNITY_STANDALONE || UNITY_EDITOR
    // With a mouse, resting on a button previews its details without pressing it.
    bool hovering;
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (manager == null || manager.IsTutorial || CountryManager.SelectedParty == null) return;
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
    bool quotePlaying;
    void QuoteEnd(object in_cookie, AkCallbackType in_type, object in_info)
    {

        if (in_type == AkCallbackType.AK_EndOfEvent)
        {
            quotePlaying = false;
            if (name == "War")
            {
                manager.gameObject.GetComponent<Tutorial>().Next(false);
                Time.timeScale = 0.0000000001f;
                GameManager.Info.transform.GetChild(21).GetComponent<Button>().interactable = true;
            }
            else if (name == "Assassin")
            { }
            else
                Time.timeScale = 1;
        }
    }
    void QuotePlay()
    {
        // Each finished quote can advance the tutorial, so repeat taps must not queue more.
        if (quotePlaying) return;
        quotePlaying = true;
        Time.timeScale = 0;
        if (name != "Research"&&name!="Assassin" && name != "War")
            manager.gameObject.GetComponent<Tutorial>().Next(false);
        TuorialSound.Post(gameObject, (uint)AkCallbackType.AK_EndOfEvent, QuoteEnd);
    }
}
