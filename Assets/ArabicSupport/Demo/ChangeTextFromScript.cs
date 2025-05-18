using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class ChangeTextFromScript : MonoBehaviour
{
    [SerializeField] ArabicFixerTMPRO arabicFixer;
    public void ChangeText(TMP_InputField input)
    {
        arabicFixer.fixedText = input.text;
    }
}
