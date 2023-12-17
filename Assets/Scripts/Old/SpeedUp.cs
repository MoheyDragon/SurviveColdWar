using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;

public class SpeedUp : Button
{
    OldGameManager manager;
    // Start is called before the first frame update
    protected override void Start()
    {
        manager = transform.GetComponentInParent<OldGameManager>();
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        OldGameManager.AccelLock = true;
        if (Time.time < OldGameManager.DoubleGate)
            manager.Accelrator();
        manager.mainMonthCycle = 0.5f;
        if (Time.timeScale == 1)
            manager.MonthEnd();
        OldGameManager.DoubleGate = Time.time + OldGameManager.MainDoubleGate;
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);
            manager.NormalFix();
        OldGameManager.AccelLock = false;
    }
}
