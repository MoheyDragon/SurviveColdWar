using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.SceneManagement;

public class LanguageManager:MonoBehaviour
{
    public static LanguageManager Singlton;
    [SerializeField] Language selectedLanguage;
    public Language GetSelectedLanguag => selectedLanguage;
    const string playerPrefLang = "playerLanguage";
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
    }
    LocalizedElement[] sceneElements;
    private void CacheSceneElements()
    {
        sceneElements = FindObjectsOfType<LocalizedElement>();
    }
    private void Update()
    {
        if(Input.GetKeyDown(KeyCode.A))
            SetLanguage(Language.Arabic);
        if(Input.GetKeyDown(KeyCode.E))
            SetLanguage(Language.English);
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
    }

    public void SetLanguage(Language language)
    {
        selectedLanguage = language;
        PlayerPrefs.SetString(playerPrefLang, language.ToString());
        PlayerPrefs.Save();
        ChangeSceneElements();
    }

}
public enum Language { English,Arabic}