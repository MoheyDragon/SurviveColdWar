using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ChangePresidentHoverInfo : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [SerializeField] GameObject info;
    [SerializeField] Image[] window;
    private Coroutine holdTimerCoroutine;
    private bool longPressTriggered = false;
    private float holdThreshold = 0.3f; // adjust as needed

    private void Start()
    {
        info.SetActive(false);
    }
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
        for (int i = 0; i < window.Length; i++)
        {
            window[i].color = CountryManager.SelectedParty.color;
        }
        info.SetActive(show);
    }
    public void LaunchAction()
    {
        CountryManager.instance.ElectionInstanatie();
        ShowInfo(false);
    }
}
