using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class LocalizedElement : MonoBehaviour
{
    GameObject arabicVersion;
    GameObject englishVersion;
    bool assigned;
    private void Start()
    {
        Assign();
        SetVersion(LanguageManager.Singlton.GetSelectedLanguag);
    }
    private void Assign()
    {
        assigned=true;
        if (arabicVersion == null)
        {
            englishVersion = transform.GetChild(0).gameObject;
            arabicVersion = transform.GetChild(1).gameObject;
        }
    }
    public void SetVersion(Language language)
    {
        if (!assigned) Assign();
        if (LanguageManager.Singlton.GetSelectedLanguag == Language.Arabic)
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
