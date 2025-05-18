using UnityEngine;
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

    private void LocalizeScene(Scene arg0, LoadSceneMode arg1)
    {
        
    }

    public void SetLanguage(Language language)
    {
        selectedLanguage = language;
        PlayerPrefs.SetString(playerPrefLang, language.ToString());
        PlayerPrefs.Save();
    }

}
public enum Language { English,Arabic}