using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class CountryManager : MonoBehaviour
{
    public static CountryManager instance;
    public bool IsTutorial;
    Text PowerCapUI, MoneyCapUI, PeoplSatsfactionCapUI, PowerComUI, MoneyComUI, PeoplSatsfactionComUI;
    public static Text title, breif, powerReq, MoneyReq, PeopleReq, TimeReq, PowerGain, MoneyGain, PeopleGain, monthly,Quote
        ,InfoPartyName,InfoPresName,InfoPresPower, InfoPresMoney, InfoPresPeople, InfoFacNumber, InfoFacPower, InfoFacMoney, InfoFacPeople
        , InfoTotalPower, InfoTotalMoney, InfoTotalPeople, Presidency_months, Presidency_Remaining,InfoSpiesPower,InfoSpiesNumber
        , InfoWarNumber, InfoWarPower, InfoWarMoney, InfoWarPeople;
    public static Transform[] Actions = new Transform[3];
    public static string doneMassage;
    public GameObject MassageWindowPrefabe;
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
        Communism = new Party("Communism",PowerComUI, MoneyComUI, PeoplSatsfactionComUI,Color.red,84);
        Capitalism = new Party("Capitalism",PowerCapUI, MoneyCapUI, PeoplSatsfactionCapUI,Color.blue,48);
        if (!IsTutorial)
            LoseMenu = transform.Find("LoseMenu").gameObject;
        ELectionWindow = GameObject.FindGameObjectWithTag("ElectionWindow");
        ActionMenu = transform.Find("ActionMenu").gameObject;
        CountryInfo = transform.Find("CountryInfo").gameObject;

        InfoPartyName = CountryInfo.transform.GetChild(0).GetComponent<Text>();
        InfoPresName = CountryInfo.transform.GetChild(1).GetComponent<Text>();
        InfoPresPower = CountryInfo.transform.GetChild(2).GetComponent<Text>();
        InfoPresMoney = CountryInfo.transform.GetChild(3).GetComponent<Text>();
        InfoPresPeople = CountryInfo.transform.GetChild(4).GetComponent<Text>();
        InfoFacNumber = CountryInfo.transform.GetChild(5).GetComponent<Text>();
        InfoFacPower = CountryInfo.transform.GetChild(6).GetComponent<Text>();
        InfoFacMoney = CountryInfo.transform.GetChild(7).GetComponent<Text>();
        InfoFacPeople = CountryInfo.transform.GetChild(8).GetComponent<Text>();
        InfoTotalPower = CountryInfo.transform.GetChild(9).GetComponent<Text>();
        InfoTotalMoney = CountryInfo.transform.GetChild(10).GetComponent<Text>();
        InfoTotalPeople = CountryInfo.transform.GetChild(11).GetComponent<Text>();
        Presidency_months= CountryInfo.transform.GetChild(12).GetChild(0).GetComponent<Text>();
        Presidency_Remaining = CountryInfo.transform.GetChild(13).GetChild(0).GetComponent<Text>();
        
        InfoSpiesPower= CountryInfo.transform.GetChild(14).GetComponent<Text>();
        InfoSpiesNumber= CountryInfo.transform.GetChild(15).GetComponent<Text>();

        InfoWarNumber= CountryInfo.transform.GetChild(16).GetComponent<Text>();
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
        monthly = GameManager.Info.transform.GetChild(9).GetComponent<Text>();
        Quote = GameManager.Info.transform.GetChild(10).GetComponent<Text>();
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
            PresidentChoosing(Communism, PresidentNameRandomizer(UnityEngine.Random.Range(1, 25), Communism), UnityEngine.Random.Range(1, 70), UnityEngine.Random.Range(10000000, 1000000000), UnityEngine.Random.Range(1, 5));
            PresidentChoosing(Capitalism, PresidentNameRandomizer(UnityEngine.Random.Range(1, 25), Capitalism), UnityEngine.Random.Range(1, 70), UnityEngine.Random.Range(10000000, 1000000000), UnityEngine.Random.Range(1, 5));
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
            PresidentChoosing(Communism, PresidentNameRandomizer(0, Communism), 100, 100000000, -1);
            PresidentChoosing(Capitalism, PresidentNameRandomizer(0, Capitalism), 20, 100000000, 2);
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
        party.PowerUI.text = party.Power.ToString();
        party.MoneyUI.text = MoneyTranslate(party.Money);
        party.PeoplSatsfactionUI.text = party.PeoplSatsfaction.ToString();
        
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
        GameManager.Com += 0.0005f * (Communism.Power - Capitalism.Power) / 100;
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
        NukeMeterUpdater.nukeDanger = Nuke.Evaluate(GameManager.Com);
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
                CanText(party.name + " : " + action.done, party, false);
                Factory.Stop(gameObject);
                Research.Stop(gameObject);
                Spy.Stop(gameObject);
                Assassin.Stop(gameObject);
                War.Stop(gameObject);

                Assassin.Post(gameObject);
                GetEnemy(party).Power = (GetEnemy(party).Power / 10) * UnityEngine.Random.Range(3, 7);
                if (System.Array.IndexOf(party.Actions, action) == 2)
                    party.Actions[2] = null;
                else if (System.Array.IndexOf(party.Actions, action) == 1)
                {

                    if (party.Actions[2] != null)
                    { 
                        party.Actions[1] = party.Actions[2];
                        party.Actions[2] = null;
                    }
                    else
                        party.Actions[1] = null;
                }
                else if (System.Array.IndexOf(party.Actions, action) == 0)
                {
                    if (party.Actions[1] != null)
                    {
                        party.Actions[0] = party.Actions[1];
                        if (party.Actions[2] != null)
                        {
                            party.Actions[1] = party.Actions[2];
                            party.Actions[2] = null;
                        }
                        else
                            party.Actions[1] = null;
                    }
                    else
                    {
                        party.Actions[0] = null;
                    }
                }
                    party.ActionIndex--;
            }
        }
    }
    public void MonthRevnue(Party party)
    {
        
        if (party.Power!=0)
        {
            party.Power += party.powerSources;
            party.Money += party.MoneySources;
            party.PeoplSatsfaction += party.peopleSources;
            if (party.PeoplSatsfaction < 0)
                party.Power += party.PeoplSatsfaction;
            party.Power = Mathf.Clamp(party.Power, 0, 1000000);
            party.Money = Mathf.Clamp(party.Money, 0, 10000000000000000000);
            party.PeoplSatsfaction = Mathf.Clamp(party.PeoplSatsfaction, -100, 100);
            if (!LoseLock)
            {
                party.ElectionIndex++;
                if (party.ElectionIndex == party.PresedncyPeriod)
                    if (!ElectionsLock)
                        Elections(party);
                    else
                        StartCoroutine(MultiElections(party));


                foreach (ActionFunction action in party.Actions)
                {
                    if (action != null)
                    {
                        if (action.time > 0)
                        {
                            action.time--;
                            if (action.time == 0)
                            {
                                if (ElectionsLock&&!NotificationLock)
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
                                    party.Power += action.power;
                                    party.Money += action.money;
                                    party.PeoplSatsfaction += action.peoplSatsfaction;
                                }

                            }
                        }
                    }
                }
            }
        }
        else
        {
            lose();
        }
        
    }
    public void Do()
    {
        ClickSound.Post(gameObject);
        DOInstansaite(SelectedParty);
    }
    public void DOInstansaite(Party party)
    {
        if (party.Power > int.Parse(powerReq.text))
        {
            if (party.Money > ReverseMoneyTranslate(MoneyReq.text))
            {
                if (party.PeoplSatsfaction > int.Parse(PeopleReq.text))
                {
                    if (party.ActionIndex < 3)
                    {
                        party.Power -= int.Parse(powerReq.text);
                        party.Money -= ReverseMoneyTranslate(MoneyReq.text);
                        party.PeoplSatsfaction -= int.Parse(PeopleReq.text);
                        party.Actions[party.ActionIndex] = new ActionFunction(GameManager.nameOfAction,doneMassage, int.Parse(PowerGain.text), int.Parse(PeopleGain.text), int.Parse(TimeReq.text.Substring(6)), ReverseMoneyTranslate(MoneyGain.text), monthly.gameObject.activeSelf);
                        if (doneMassage== "Factory Building Finished")
                            party.FactoryLevel++;
                        else if (doneMassage== "Proxy war started.")
                            party.WarLevel++;
                            party.ActionIndex++;
                        if (!IsTutorial)
                            foreach (Button button in this.GetComponentsInChildren<Button>())
                                button.interactable = true;
                        GameManager.Info.SetActive(false);
                    }
                    else
                        CanText("You have three actions in progress you have to wait for one of them at least to finish", party,true);
                }
                else
                    CanText("You don't have enough People satisfaction", party,true);
            }
            else
                CanText("You don't have enough Money", party,true);
        }
        else
            CanText("You don't have enough Power", party,true);
    }

    public void CanText(string massage, Party party,bool IsWarning)
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
        game.transform.GetChild(0).GetComponent<Text>().text = massage;
        Time.timeScale = 0;
        if (massage== "Are you sure you want to exit")
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
            else if (ran == 24)
                returned_name += " Hopkins";
            else if (ran == 25)
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
    public void PresidentChoosing(Party party,string name,int _power, float _money, int _people )
    {
        President pres = new President(name, _power, _people, _money);
        party.president = pres;
        party.sources();
        party.ElectionIndex = 0;
    }
    public void Elections(Party party)
    {
        Time.timeScale = 0;
        if (party.ElectionIndex>party.TempPresedncy)
        {
            ActionMenu.SetActive(false);
            ElectionsLock = true;
            ELectionWindow.GetComponent<Image>().color = party.color;
            ElecStart.Post(gameObject);
            ELectionWindow.SetActive(true);
            
            foreach (Transform item in ElectedPresidents)
            {
                President press = new President(PresidentNameRandomizer(UnityEngine.Random.Range(1, 26), party), UnityEngine.Random.Range(-150,150), UnityEngine.Random.Range(-20, 20), UnityEngine.Random.Range(10000000, 1000000000));
                item.GetChild(0).gameObject.GetComponent<Text>().text = press.name;
                item.GetChild(1).gameObject.GetComponent<Text>().text = press.Power.ToString();
                item.GetChild(2).gameObject.GetComponent<Text>().text = MoneyTranslate(press.Money);
                item.GetChild(3).gameObject.GetComponent<Text>().text = press.PeoplSatsfaction.ToString();
            }
            if (IsTutorial)
            {
                President press = new President(PresidentNameRandomizer(2, party), -150, UnityEngine.Random.Range(-20, 20), UnityEngine.Random.Range(10000000, 1000000000));
                ElectedPresidents[0].GetChild(0).gameObject.GetComponent<Text>().text = press.name;
                ElectedPresidents[0].GetChild(1).gameObject.GetComponent<Text>().text = press.Power.ToString();
                ElectedPresidents[0].GetChild(2).gameObject.GetComponent<Text>().text = MoneyTranslate(press.Money);
                ElectedPresidents[0].GetChild(3).gameObject.GetComponent<Text>().text = press.PeoplSatsfaction.ToString();
            }
            else
            {
                foreach (Button button in GameManager.Canvas.GetComponentsInChildren<Button>())
                    if (button.tag == "SpeedUp")
                        button.interactable = false;
            }
            SelectedParty = party;
        }
        else
        {
            CanText("You can't Change the president before one full year of his presidency", party,true);
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
        PresidentChoosing(SelectedParty, button.transform.parent.GetChild(0).GetComponent<Text>().text, int.Parse(button.transform.parent.GetChild(1).GetComponent<Text>().text), ReverseMoneyTranslate(button.transform.parent.GetChild(2).GetComponent<Text>().text), int.Parse(button.transform.parent.GetChild(3).GetComponent<Text>().text));
        ELectionWindow.SetActive(false);
        GameManager.Info.SetActive(false);
        ActionMenu.SetActive(false);
        ElecEnd.Post(gameObject);
        ElectionsLock = false;
        Time.timeScale = NotificationLock?0.09f:1;
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
            SelectParty(party);
            CountryInfo.SetActive(false);
            ActionMenu.SetActive(!ActionMenu.activeSelf);
            GameManager.Info.SetActive(false);
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
            GameManager.Info.SetActive(false);
        }
    }
    public void win()
    {
        foreach (GameObject button in GameObject.FindGameObjectsWithTag("SpeedUp"))
            button.SetActive(false);
        Time.timeScale = 0;
        Win.Post(gameObject);
    }
    public void lose()
    {
        Time.timeScale = 0;
        foreach (GameObject button in GameObject.FindGameObjectsWithTag("SpeedUp"))
            button.SetActive(false);
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
    IEnumerator MultiElections(Party party)
    {
        yield return new WaitForSeconds(0.1f);
        Elections(party);
    }
    IEnumerator MultiActions(Party party,ActionFunction action,int WaitTime)
    {
        NotificationIndex++;
        Pause.Post(gameObject);
        yield return new WaitForSeconds(0.01f * WaitTime);
        if (!LoseLock)
        {
            CanText(party.name + " : " + action.done, party, false);
            Factory.Stop(gameObject);
            Research.Stop(gameObject);
            Spy.Stop(gameObject);
            Assassin.Stop(gameObject);
            War.Stop(gameObject);
            if (action.done == "Factory Building Finished")
            {
                Factory.Post(gameObject);
                party.FactoriesNumber++;
                party.FactoriesTotalPower += action.power;
                party.FactoriesTotalMoney += action.money;
                party.FactoriesTotalpeople += action.peoplSatsfaction;
            }

            else if (action.done == "Research Completed")
                Research.Post(gameObject);
            else if (action.done == "Spy is in place")
            {
                Spy.Post(gameObject);
                party.SpiesNumber++;
                party.SpiesTotalPower += action.power;
            }
            else if (action.done == "Proxy war started.")
            {
                War.Post(gameObject);
                party.WarNumber++;
                party.WarTotalPower += action.power;
                party.WarTotalMoney += action.money;
                party.WarTotalpeople += action.peoplSatsfaction;
            }

            if (System.Array.IndexOf(party.Actions, action) == 2)
                party.Actions[2] = null;
            else if (System.Array.IndexOf(party.Actions, action) == 1)
            {

                if (party.Actions[2] != null)
                {
                    party.Actions[1] = party.Actions[2];
                    party.Actions[2] = null;
                }
                else
                    party.Actions[1] = null;


            }
            else if (System.Array.IndexOf(party.Actions, action) == 0)
            {
                if (party.Actions[1] != null)
                {
                    party.Actions[0] = party.Actions[1];
                    if (party.Actions[2] != null)
                    {
                        party.Actions[1] = party.Actions[2];
                        party.Actions[2] = null;
                    }
                    else
                        party.Actions[1] = null;
                }
                else
                {
                    party.Actions[0] = null;
                }
            }
            party.ActionIndex--;
            party.sources();
        }
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
    public void Exit()
    {
        CanText("Are you sure you want to exit", SelectedParty, true);
    }
}

public class ActionFunction
{
    public string name;
    public int power, peoplSatsfaction,time;
    public float money;
    public bool Monthly;
    public string done;
    public ActionFunction( string _name,string _done, int _power, int _peoplSatsfaction, int _time, float _money,bool _monthly)
    {
        name = _name;
        done = _done;
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
    public int Power, PeoplSatsfaction, powerSources, peopleSources,ActionIndex, PresedncyPeriod;
    public  float Money, MoneySources;
    public  Text PowerUI, MoneyUI, PeoplSatsfactionUI;
    public ActionFunction[] Actions ;
    public Color color;

    public President president;
    public int ElectionIndex=0;
    public int TempPresedncy=12;

    public int FactoryLevel = 1;
    public int FactoriesNumber = 0;
    public int FactoriesTotalPower = 0;
    public float FactoriesTotalMoney = 0;
    public int FactoriesTotalpeople = 0;

    public int SpiesNumber = 0;
    public int SpiesTotalPower = 0;

    public int WarLevel = 0;
    public int WarNumber = 0;
    public int WarTotalPower = 0;
    public float WarTotalMoney = 0;
    public int WarTotalpeople = 0;



    public Party(string _name,Text powerUI, Text moneyUI, Text peoplSatsfactionUI,Color _color, int _PresedncyPeriod)
    {
        name = _name;   
        ActionIndex = 0;
        Actions = new ActionFunction[3];
        PowerUI = powerUI;
        MoneyUI = moneyUI;
        PeoplSatsfactionUI = peoplSatsfactionUI;
        color = _color;
        PresedncyPeriod = _PresedncyPeriod;
    }
    public void sources()
    {
        powerSources = FactoriesTotalPower + SpiesTotalPower + president.Power+WarTotalPower;
        MoneySources = FactoriesTotalMoney + president.Money+WarTotalMoney;
        peopleSources = FactoriesTotalpeople + president.PeoplSatsfaction+WarTotalpeople;
    }
}
public class President
{
    public string name;
    public int Power, PeoplSatsfaction;
    public float Money;

    public President(string name, int power, int peoplSatsfaction, float money)
    {
        this.name = name;
        Power = power;
        PeoplSatsfaction = peoplSatsfaction;
        Money = money;
    }
}
