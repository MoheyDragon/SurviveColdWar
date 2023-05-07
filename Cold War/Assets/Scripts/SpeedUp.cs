using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;

public class SpeedUp : Button
{

    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        ScenarioManager.Singletone.AccelLock = true;
        if (Time.time < ScenarioManager.Singletone.DoubleGate)
            ScenarioManager.Singletone.Accelrator();
        ScenarioManager.Singletone.mainMonthCycle = 0.5f;
        if (Time.timeScale == 1)
            ScenarioManager.Singletone.MonthEnd();
        ScenarioManager.Singletone.SetDoubleGate(Time.time + ScenarioManager.Singletone.MainDoubleGate);
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);
        ScenarioManager.Singletone.NormalFix();
        ScenarioManager.Singletone.AccelLock = false;
    }
}
