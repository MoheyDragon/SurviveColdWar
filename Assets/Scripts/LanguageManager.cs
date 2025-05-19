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
        SceneManager.sceneLoaded += LocalizeScene;
        string startingLanguage = PlayerPrefs.GetString(playerPrefLang, Language.Arabic.ToString());
        if (startingLanguage==Language.Arabic.ToString())
            SetLanguage(Language.Arabic);
        else
            SetLanguage(Language.English);
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
        LocalizedElement[] elements = FindObjectsOfType<LocalizedElement>();
        foreach (LocalizedElement element in elements)
        {
            element.SetVersion(selectedLanguage);
        }
    }
    private void LocalizeScene(Scene arg0, LoadSceneMode arg1)
    {
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