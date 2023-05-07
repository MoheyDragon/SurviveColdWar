using UnityEngine;
using UnityEngine.UI;

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
