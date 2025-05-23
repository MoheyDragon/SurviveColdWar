using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LanguageManager:MonoBehaviour
{
    public static LanguageManager Singlton;
    [SerializeField] Language selectedLanguage;
    [SerializeField] Sprite arabicIcon;
    [SerializeField] Sprite englishIcon;
    public Language GetSelectedLanguag => selectedLanguage;
    const string playerPrefLang = "playerLanguage";
    const string languageTag = "Language";
    private bool playerSelectedEndlessMode;
    Button languageButton;
    private void Awake()
    {
        if(Singlton == null)
            Singlton = this;
        else
            Destroy(gameObject);
        DontDestroyOnLoad(gameObject);
        
        string startingLanguage = PlayerPrefs.GetString(playerPrefLang, Language.Arabic.ToString());
        if (startingLanguage==Language.Arabic.ToString())
            SetLanguage(Language.Arabic);
        else
            SetLanguage(Language.English);
        CacheSceneElements();
    }
    private void Start()
    {
        SceneManager.sceneLoaded += LocalizeScene;
        ChangeSceneElements();
        SetupLanguageButton();
    }
    private void SetupLanguageButton()
    {
        languageButton = GameObject.FindGameObjectWithTag(languageTag).GetComponent<Button>();
        languageButton.onClick.AddListener(SwitchLanguage);
        GameObject.FindGameObjectWithTag(languageTag).GetComponent<Image>().sprite =
            selectedLanguage == Language.Arabic ? englishIcon : arabicIcon;
    }
    LocalizedElement[] sceneElements;
    private void CacheSceneElements()
    {
        sceneElements = FindObjectsOfType<LocalizedElement>();        
    }
   public void ChangeSceneElements()
    {
        if (sceneElements == null) CacheSceneElements();
        foreach (LocalizedElement element in sceneElements)
            element.SetVersion(selectedLanguage);
        ChangeNotification();
        ChangeTutorial();
    }
    private void ChangeNotification()
    {
        Notification[] notifications = FindObjectsOfType<Notification>();
        foreach (Notification element in notifications)
            element.ChangeLanguage();
    }
    private void ChangeTutorial()
    {
        if(CountryManager.instance!=null)
        if(CountryManager.instance.IsTutorial)
            Tutorial.instance.OnLanguageChanged();
    }
    private void LocalizeScene(Scene arg0, LoadSceneMode arg1)
    {
        CacheSceneElements();
        ChangeSceneElements();
        SetupLanguageButton();
    }
    public void SwitchLanguage()
    {
        print("switch");
        SetLanguage(selectedLanguage==Language.Arabic?Language.English:Language.Arabic);
    }
    public void SetLanguage(Language language)
    {
        selectedLanguage = language;
        PlayerPrefs.SetString(playerPrefLang, language.ToString());

        GameObject.FindGameObjectWithTag(languageTag).GetComponent<Image>().sprite =
            selectedLanguage == Language.Arabic ?  englishIcon: arabicIcon;
        PlayerPrefs.Save();
        ChangeSceneElements();
    }
    
    public void OnLaunchGame(bool endless)
    {
        playerSelectedEndlessMode = endless;
        SceneManager.LoadScene(2);
    }
    public void EnableButton(bool enabled)
    {
        languageButton.interactable = enabled;
    }
    public void HideButton()
    {
        languageButton.gameObject.SetActive(false);
    }
    public bool IsEndless => playerSelectedEndlessMode;
}
public enum Language { English,Arabic}