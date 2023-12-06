using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;

public class SpeedUp : Button
{
    GameManager manager;
    // Start is called before the first frame update
    protected override void Start()
    {
        manager = transform.GetComponentInParent<GameManager>();
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        GameManager.AccelLock = true;
        if (Time.time < GameManager.DoubleGate)
            manager.Accelrator();
        manager.mainMonthCycle = 0.5f;
        if (Time.timeScale == 1)
            manager.MonthEnd();
        GameManager.DoubleGate = Time.time + GameManager.MainDoubleGate;
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);
            manager.NormalFix();
        GameManager.AccelLock = false;
    }
}
