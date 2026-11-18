using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// A clickable 3D object that stands in for one of the game's original 2D buttons.
// It forwards pointer events to that button, so the game logic behind it is unchanged.
public class Token3D : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    const float PressSeconds = 0.32f;

    public GameObject target;
    public Vector3 restScale = Vector3.one;
    public bool animate = true;
    // Most buttons wait while a tutorial voice line plays; this one (the exit) never does.
    public bool ignoresVoiceLine;

    bool Waiting => HoverInfo.VoiceLinePlaying && !ignoresVoiceLine;

    Selectable selectable;
    float pressTimer;
    float hoverAmount;
    bool hovering;
    bool pressForwarded;

    public bool IsInteractable
    {
        get
        {
            if (target == null || !target.activeInHierarchy)
                return false;
            if (selectable == null)
                selectable = target.GetComponent<Selectable>();
            return selectable == null || selectable.IsInteractable();
        }
    }

    public void PlayPress()
    {
        pressTimer = PressSeconds;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        pressForwarded = IsInteractable && !Waiting;
        if (pressForwarded)
            ExecuteEvents.Execute(target, eventData, ExecuteEvents.pointerDownHandler);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!pressForwarded)
            return;

        pressForwarded = false;
        if (target != null && target.activeInHierarchy)
            ExecuteEvents.Execute(target, eventData, ExecuteEvents.pointerUpHandler);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!IsInteractable || Waiting)
            return;

        PlayPress();
        ExecuteEvents.Execute(target, eventData, ExecuteEvents.pointerClickHandler);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovering = true;
        if (Waiting)
            return;
        if (target != null && target.activeInHierarchy)
            ExecuteEvents.Execute(target, eventData, ExecuteEvents.pointerEnterHandler);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovering = false;
        if (target != null && target.activeInHierarchy)
            ExecuteEvents.Execute(target, eventData, ExecuteEvents.pointerExitHandler);
    }

    void OnDisable()
    {
        hovering = false;
        pressTimer = 0f;
        pressForwarded = false;
    }

    void Update()
    {
        if (!animate)
            return;

        float dt = Time.unscaledDeltaTime;
        hoverAmount = Mathf.MoveTowards(hoverAmount, hovering && IsInteractable ? 1f : 0f, dt * 7f);
        pressTimer = Mathf.Max(0f, pressTimer - dt);

        // Squashes flat and widens first, then springs back past its full height.
        float t = 1f - pressTimer / PressSeconds;
        float spring = pressTimer > 0f ? Mathf.Sin(t * Mathf.PI * 2f) * (1f - t) : 0f;
        float grow = 1f + hoverAmount * 0.07f;
        transform.localScale = new Vector3(
            restScale.x * grow * (1f + spring * 0.22f),
            restScale.y * grow * (1f - spring * 0.32f),
            restScale.z * grow * (1f + spring * 0.22f));
    }
}
