using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BuyingManager : MonoBehaviour
{
    [SerializeField] GameObject shopWindow;
    [SerializeField] GameObject errorMessage;
    [SerializeField] GameObject noAdsButton;
    private float messageShowDuration=3;
    WaitForSeconds messageShowWait;
    private void Start()
    {
        shopWindow.SetActive(false);
        errorMessage.SetActive(false);
        messageShowWait = new WaitForSeconds(messageShowDuration);
        noAdsButton.SetActive(LanguageManager.Singlton.HaveAds);

    }
    public void ToggleShopWindow()
    {
        shopWindow.SetActive(!shopWindow.activeSelf);
    }
    public void ShowErrorMessage()
    {
        StartCoroutine(CO_AutoHideErrorMessage());
    }
    public void OnBuying()
    {
        LanguageManager.Singlton.BuyNoAds();
        noAdsButton.SetActive(false);
        shopWindow.SetActive(false);
        errorMessage.SetActive(false);
    }
    public virtual
    IEnumerator CO_AutoHideErrorMessage()
    {
        errorMessage.SetActive(true);
        yield return messageShowWait;
        errorMessage.SetActive(false);
    }
}
