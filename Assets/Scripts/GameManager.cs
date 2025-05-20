using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static float Com, DoubleGate,MainDoubleGate;
    public static GameObject Info,Canvas;
    GameObject WinMenu;
    Image[] ComunitsisTerretory;
    Text Date, Month;
    int monthCount;
    float MonthCycle;
    public float mainMonthCycle;
    string[] months=new string[12];
    CountryManager country;
    public static string nameOfAction,arabicNameOfAction;
    public static bool AccelLock = false;
    public AK.Wwise.Event ClickSound, MonthEnded;
    public AK.Wwise.RTPC MonthEndAccelerator;
    // Start is called before the first frame update
    private void Awake()
    {
        MainDoubleGate=DoubleGate = 0.2f;
        MonthCycle = mainMonthCycle;
        Date = GameObject.Find("Date").GetComponent<Text>();
        Month = GameObject.Find("Month").GetComponent<Text>();
        int counter = 0;
        foreach (GameObject filler in GameObject.FindGameObjectsWithTag("ComFill"))
        {
            counter++;
        }
        ComunitsisTerretory = new Image[counter];
        counter = 0;
        foreach (GameObject filler in GameObject.FindGameObjectsWithTag("ComFill"))
        {
            ComunitsisTerretory[counter] = filler.GetComponent<Image>();
            counter++;
        }
        Com = 0.5f;
        months[0] = "Jan"; months[1] = "Feb"; months[2] = "Mar"; months[3] = "Apr"; months[4] = "May"; months[5] = "Jun";
        months[6] = "Jul"; months[7] = "Aug"; months[8] = "Sep"; months[9] = "Oct"; months[10] = "Nov"; months[11] = "Dec";
        monthCount = 0;
        Info = transform.Find("Info").gameObject;
        country = this.gameObject.GetComponent<CountryManager>();
        if (!country.IsTutorial)
        {
            WinMenu = transform.Find("WinMenu").gameObject;
            WinMenu.SetActive(false);
        }
        Canvas = this.gameObject;
        LandsFill();
    }
    private void Start()
    {
        if (country.IsTutorial)
        {
            Date.gameObject.SetActive(false);
            Month.gameObject.SetActive(false);
            Info.gameObject.SetActive(false);    
        } 
        mainMonthCycle = 10;
        MonthCycle = mainMonthCycle + Time.time;
        Info.SetActive(false);
        MonthEndAccelerator.SetGlobalValue(-100);
    }
    public void LandsFill()
    {
        foreach (Image terr in ComunitsisTerretory)
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
            Info.SetActive(false);
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
            if(int.Parse(Date.text)>=1953)
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
