using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class HoverInfo : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public new string name;
    public string title, breif,Quote,powerReq, PeopleReq, TimeReq, PowerGain, PeopleGain,doneMassage;
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
        CountryManager.title.text = title;
        CountryManager.doneMassage = doneMassage;
        CountryManager.breif.text = breif;
        CountryManager.Quote.text = Quote;
        if (title == "Build a new Factory")
            CountryManager.powerReq.text = (int.Parse(powerReq) + CountryManager.SelectedParty.FactoryLevel * 25).ToString();
        else if (name == "War")
            CountryManager.powerReq.text = (int.Parse(powerReq) + CountryManager.SelectedParty.WarLevel * 25).ToString();
        else if (name == "Research")
            CountryManager.powerReq.text = (int.Parse(powerReq) + ((int.Parse(powerReq)*CountryManager.SelectedParty.researchLevel/5))).ToString();
        else if (name == "Spy")
            CountryManager.powerReq.text = (int.Parse(powerReq) +((int.Parse(powerReq))* CountryManager.SelectedParty.SpiesLevel)).ToString();
        else
            CountryManager.powerReq.text = powerReq;
        if (title == "Build a new Factory")
            CountryManager.MoneyReq.text = MoneyTranslate(MoneyReq + ((ulong)CountryManager.SelectedParty.FactoryLevel * 500000000ul));
        else if (name == "War")
            CountryManager.MoneyReq.text = MoneyTranslate(MoneyReq + (CountryManager.SelectedParty.WarLevel * 1000000));
        else if (name == "Research")
            CountryManager.MoneyReq.text = MoneyTranslate(MoneyReq +(MoneyReq* CountryManager.SelectedParty.researchLevel));
        else if (name == "Spy")
            CountryManager.MoneyReq.text = MoneyTranslate(MoneyReq + (CountryManager.SelectedParty.SpiesLevel * 1000000000));
        else
            CountryManager.MoneyReq.text = MoneyTranslate(MoneyReq);
        CountryManager.PeopleReq.text = PeopleReq;
        CountryManager.TimeReq.text = "after " + TimeReq;
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
        //if (!manager.IsTutorial)
        //    foreach (Button button in GameManager.Canvas.GetComponentsInChildren<Button>())
        //        if (button.tag != "SpeedUp" && button.tag != "MapSector" && button.tag != "InfoCenter")
        //            button.interactable = false;
    }
    private void ShowInfo(bool show)
    {
        GameManager.Info.SetActive(show);
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
    void QuoteEnd(object in_cookie, AkCallbackType in_type, object in_info)
    {

        if (in_type == AkCallbackType.AK_EndOfEvent)
        {
            if (name == "War")
            {
                manager.gameObject.GetComponent<Tutorial>().Next(false);
                Time.timeScale = 0.0000000001f;
                GameManager.Info.transform.GetChild(20).GetComponent<Button>().interactable = true;
            }
            else if (name == "Assassin")
            { }
            else
                Time.timeScale = 1;
        }
    }
    void QuotePlay()
    {
        Time.timeScale = 0;
        if (name != "Research"&&name!="Assassin" && name != "War")
            manager.gameObject.GetComponent<Tutorial>().Next(false);
        TuorialSound.Post(gameObject, (uint)AkCallbackType.AK_EndOfEvent, QuoteEnd);
    }
}
