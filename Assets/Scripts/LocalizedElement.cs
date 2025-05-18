using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LocalizedElement : MonoBehaviour
{
    [SerializeField] GameObject arabicVersion;
    [SerializeField] GameObject englishVersion;
    private void Awake()
    {
        if(arabicVersion==null)
        {
            englishVersion=transform.GetChild(0).gameObject;
            arabicVersion=transform.GetChild(1).gameObject;

        }
    }
    private void Start()
    {
        if(LanguageManager.Singlton.GetSelectedLanguag==Language.Arabic)
        {
            arabicVersion.SetActive(true);
            englishVersion.SetActive(false);
        }
        else
        {
            arabicVersion.SetActive(false);
            englishVersion.SetActive(true);
        }
    }
}
