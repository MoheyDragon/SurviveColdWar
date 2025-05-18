using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class ChangePresidentHoverInfo : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] GameObject info;
    private Coroutine holdTimerCoroutine;
    private bool longPressTriggered = false;
    private float holdThreshold = 0.3f; // adjust as needed

  
    public void OnPointerDown(PointerEventData eventData)
    {
        longPressTriggered = false;
        holdTimerCoroutine = StartCoroutine(HoldTimer());
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (holdTimerCoroutine != null)
        {
            StopCoroutine(holdTimerCoroutine);
            ShowInfo(false);
        }

        if (!longPressTriggered)
        {
            LaunchAction();
        }
    }

    private IEnumerator HoldTimer()
    {
        yield return new WaitForSeconds(holdThreshold);
        longPressTriggered = true;
        ShowInfo(true);
    }
    private void ShowInfo(bool show)
    {
        info.SetActive(show);
    }
    public void LaunchAction()
    {
        CountryManager.instance.ElectionInstanatie();
        ShowInfo(false);
    }
}
