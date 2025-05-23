using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Country_Info : MonoBehaviour
{
    CountryManager country;
    public AK.Wwise.Event Click;
    private void Awake()
    {
        country = transform.parent.GetComponentInParent<CountryManager>();
    }
    public void InfoClick()
    {
        Click.Post(gameObject);
        if (!CountryManager.CountryInfo.activeSelf)
        {
                if (gameObject.name == "ComCenterInfo")
                    Info(country.Communism);
                else
                    Info(country.Capitalism);
        }
        else
            CountryManager.CountryInfo.SetActive(false);
        LanguageManager.Singlton.EnableButton(!CountryManager.CountryInfo.activeSelf);

    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        
    }
    void Info(Party party)
    {
        CountryManager.InfoPartyName.fixedText = "";
        if (LanguageManager.Singlton.GetSelectedLanguag==Language.Arabic)
            CountryManager.InfoPartyName.fixedText = party.arabicName;
        else
            CountryManager.InfoPartyName.fixedText = party.name;

        CountryManager.CountryInfo.GetComponent<Image>().color = party.color;

        foreach (Text text in CountryManager.InfoPresName)
            text.text = party.president.name;
        CountryManager.InfoPresPower.text = party.president.Power.ToString();
        CountryManager.InfoPresMoney.text = country.MoneyTranslate(party.president.Money);
        CountryManager.InfoPresPeople.text = party.president.PeoplSatsfaction.ToString();

        foreach (Text text in CountryManager.InfoFacNumber)
            text.text = party.FactoriesNumber.ToString();

        CountryManager.InfoFacPower.text = party.FactoriesTotalPower.ToString();
        CountryManager.InfoFacMoney.text = country.MoneyTranslate(party.FactoriesTotalMoney);
        CountryManager.InfoFacPeople.text = party.FactoriesTotalpeople.ToString();

        foreach (Text text in CountryManager.InfoSpiesNumber)
            text.text = party.SpiesNumber.ToString();

        CountryManager.InfoSpiesPower.text = party.SpiesTotalPower.ToString();

        foreach (Text text in CountryManager.InfoWarNumber)
            text.text = party.WarNumber.ToString();

        CountryManager.InfoWarPower.text = party.WarTotalPower.ToString();
        CountryManager.InfoWarMoney.text = country.MoneyTranslate(party.WarTotalMoney);
        CountryManager.InfoWarPeople.text = party.WarTotalpeople.ToString();

        CountryManager.InfoTotalPower.text = party.powerSources.ToString();
        CountryManager.InfoTotalMoney.text = country.MoneyTranslate(party.MoneySources);
        CountryManager.InfoTotalPeople.text = party.peopleSources.ToString();

        CountryManager.Presidency_months.text = party.ElectionIndex.ToString();
        CountryManager.Presidency_Remaining.text = (party.PresedncyPeriod - party.ElectionIndex).ToString();

        foreach (Transform item in CountryManager.Actions)
        {
            if (party.Actions[item.transform.GetSiblingIndex()]!=null)
            {
                CountryManager.Actions[item.transform.GetSiblingIndex()].GetChild(0).GetComponent<ArabicFixer>().fixedText=LanguageManager.Singlton.GetSelectedLanguag==Language.Arabic? party.Actions[item.transform.GetSiblingIndex()].arabicName : party.Actions[item.transform.GetSiblingIndex()].name;
                CountryManager.Actions[item.transform.GetSiblingIndex()].GetChild(1).GetChild(0).GetComponent<Text>().text = party.Actions[item.transform.GetSiblingIndex()].time.ToString();
            }
            else
            {
                CountryManager.Actions[item.transform.GetSiblingIndex()].GetChild(0).GetComponent<ArabicFixer>().fixedText = "";
                CountryManager.Actions[item.transform.GetSiblingIndex()].GetChild(0).GetComponent<ArabicFixer>().fixedText = LanguageManager.Singlton.GetSelectedLanguag==Language.Arabic?"الأمر فارغ":"No action";
                CountryManager.Actions[item.transform.GetSiblingIndex()].GetChild(1).GetChild(0).GetComponent<Text>().text = "0";
            }
        }

        CountryManager.CountryInfo.SetActive(true);
    }
}
