using UnityEngine;
using UnityEngine.UI;

public class ScenarioManager : Singletones<ScenarioManager>
{
    [SerializeField] float com, doubleGate;
                     float mainDoubleGate;
    public float MainDoubleGate => mainDoubleGate;
    [SerializeField] private  GameObject info,canvas;
    [SerializeField] GameObject WinMenu;
    Image[] communistTerritory;
    Text Date, Month;
    int monthCount;
    float MonthCycle;
    public float mainMonthCycle;
    string[] months=new string[12];
    [SerializeField] CountryManager country;
    public string nameOfAction;
    public bool AccelLock = false;
    public AK.Wwise.Event ClickSound, MonthEnded;
    public AK.Wwise.RTPC MonthEndAccelerator;
    public GameObject Info => info;
    public float Com => com;
    public void SetCom(float _com) => com += _com;
    public float DoubleGate => doubleGate;
    public void SetDoubleGate(float _doubleGate) => doubleGate = _doubleGate;
    public GameObject Canvas => canvas;
    protected override void Awake()
    {
        base.Awake();
        mainDoubleGate = doubleGate = 0.2f;
        MonthCycle = mainMonthCycle;
        Date = GameObject.Find("Date").GetComponent<Text>();
        Month = GameObject.Find("Month").GetComponent<Text>();
        int counter = 0;
        foreach (GameObject filler in GameObject.FindGameObjectsWithTag("ComFill"))
        {
            counter++;
        }
        communistTerritory = new Image[counter];
        counter = 0;
        foreach (GameObject filler in GameObject.FindGameObjectsWithTag("ComFill"))
        {
            communistTerritory[counter] = filler.GetComponent<Image>();
            counter++;
        }
        com = 0.5f;
        months[0] = "Jan"; months[1] = "Feb"; months[2] = "Mar"; months[3] = "Apr"; months[4] = "May"; months[5] = "Jun";
        months[6] = "Jul"; months[7] = "Aug"; months[8] = "Sep"; months[9] = "Oct"; months[10] = "Nov"; months[11] = "Dec";
        monthCount = 0;
        if (!country.IsTutorial)
        {
            WinMenu.SetActive(false);
        }
        LandsFill();
    }
    private void Start()
    {
        if (country.IsTutorial)
        {
            Date.gameObject.SetActive(false);
            Month.gameObject.SetActive(false);
            info.gameObject.SetActive(false);    
        } 
        mainMonthCycle = 10;
        MonthCycle = mainMonthCycle + Time.time;
        info.SetActive(false);
        MonthEndAccelerator.SetGlobalValue(-100);
    }
    public void LandsFill()
    {
        foreach (Image terr in communistTerritory)
            terr.fillAmount = Com;
    }
    public void  Update()
    {
        if (Time.time>MonthCycle)
            MonthEnd();
        if (Time.time > DoubleGate&&!AccelLock)
            MonthEndAccelerator.SetGlobalValue(-100);
        if (Time.timeScale==1)
        {
            Date.color = Color.green;
            Month.color = Color.green;
            
        }
        else
        {
            Date.color = Color.red;
            Month.color = Color.red;
        }
    }
    public void InfoBack()
    {
        ClickSound.Post(gameObject);
            info.SetActive(false);
        if (!country.IsTutorial)
            foreach (Button button in this.GetComponentsInChildren<Button>())
                button.interactable = true;
    }  
    public void MonthEnd()
    {
        if (AccelLock)
            Accelrator();
        MonthEnded.Post(gameObject);
        if (monthCount > months.Length - 2)
        {
            monthCount = 0;
            Date.text = (int.Parse(Date.text) + 1).ToString();
            if (Date.text == "1991")
            {
                WinMenu.SetActive(true);
                country.win();
                Time.timeScale = 0;
            }
        }
        else
            monthCount++;
        Month.text = months[monthCount];
        MonthCycle = mainMonthCycle + Time.time;
        country.MonthInstansaite();
        LandsFill();
        if (country.IsTutorial)
        {
            if (Tutorial.FirstMonthLock)
                Tutorial.FirstMonthLock = false;
            if (Tutorial.SecondMonthLock && monthCount == 2)
                Tutorial.SecondMonthLock = false;
            if (Tutorial.FirstYear && monthCount == 0)
                Tutorial.FirstYear = false;
            if (Tutorial.ThreeWARS &&Date.text=="1948"&&monthCount>4)
                Tutorial.ThreeWARS = false;
            if (Tutorial.FourYears && Date.text == "1952")
                Tutorial.FourYears = false;
            if (Tutorial.ENDLOCK&& Date.text == "1952"&&monthCount>3)
                Tutorial.ENDLOCK = false;
        }
    }
    public void NormalFix()
    {
        mainMonthCycle = 10;
        MonthCycle = mainMonthCycle + Time.time;   
    }
    public void Accelrator()
    {
        MonthEndAccelerator.SetGlobalValue(MonthEndAccelerator.GetGlobalValue() + 50);
    }
    
}
