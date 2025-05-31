using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BuyingManager : MonoBehaviour
{
    public static BuyingManager Singlton;

    [SerializeField] GameObject shopWindow;
    [SerializeField] GameObject errorMessage;
    [SerializeField] GameObject noAdsButton;
    [SerializeField] Sprite englishIcon;
    [SerializeField] Sprite arabicIcon;
    Image noAdsIcon;
    private float messageShowDuration=3;
    WaitForSeconds messageShowWait;
    private void Awake()
    {
        if (Singlton == null)
            Singlton = this;
        else
            Destroy(gameObject);
        noAdsIcon =noAdsButton.GetComponent<Image>();
    }
    private void Start()
    {
        shopWindow.SetActive(false);
        errorMessage.SetActive(false);
        messageShowWait = new WaitForSeconds(messageShowDuration);
        noAdsButton.SetActive(LanguageManager.Singlton.HaveAds);
        
    }
    public void ToggleLanguage(Language selectedLanguage)
    {
        noAdsIcon.sprite=selectedLanguage==Language.Arabic?arabicIcon:englishIcon;
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
