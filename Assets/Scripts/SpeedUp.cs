using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine;

public class SpeedUp : Button
{
    GameManager manager;
    bool holding;
    bool wasTimeRunning = true;

    // Same check that turns the date red in GameManager.Update.
    static bool TimeIsRunning => Time.timeScale == 1;

    // Start is called before the first frame update
    protected override void Start()
    {
        manager = transform.GetComponentInParent<GameManager>();
    }

    // Kept separate from the interactable flag, which Tutorial also sets.
    public override bool IsInteractable()
    {
        return base.IsInteractable() && TimeIsRunning;
    }

    private void Update()
    {
        if (!Application.isPlaying) return;

        bool timeIsRunning = TimeIsRunning;
        if (timeIsRunning == wasTimeRunning) return;
        wasTimeRunning = timeIsRunning;

        // A hold must not carry on through a pause or resume by itself.
        if (!timeIsRunning)
            ReleaseHold();

        DoStateTransition(currentSelectionState, false);
    }

    public override void OnPointerDown(PointerEventData eventData)
    {
        base.OnPointerDown(eventData);
        if (!IsInteractable()) return;

        holding = true;
        GameManager.AccelLock = true;
        if (Time.time < GameManager.DoubleGate)
            manager.Accelrator();
        manager.mainMonthCycle = 0.5f;
        manager.MonthEnd();
        GameManager.DoubleGate = Time.time + GameManager.MainDoubleGate;
    }

    public override void OnPointerUp(PointerEventData eventData)
    {
        base.OnPointerUp(eventData);
        ReleaseHold();
    }

    void ReleaseHold()
    {
        if (!holding) return;
        holding = false;
        manager.NormalFix();
        GameManager.AccelLock = false;
    }
}
