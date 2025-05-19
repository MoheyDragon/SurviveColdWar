using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Notification:MonoBehaviour
{
    [SerializeField] TextMeshProUGUI message;
    [SerializeField] ArabicFixerTMPRO fixer;
    [SerializeField] Image image;
    [SerializeField] float lifeTime=3;
    private void Start()
    {
        Destroy(gameObject,lifeTime);
    }
    public void InitiateMessage(ActionFunction action,Party party)
    {
        if (LanguageManager.Singlton.GetSelectedLanguag == Language.Arabic)
        {
            fixer.fixedText = action.arabicDone;
        }
        else
            message.text = action.done;

        image.color = party.color;
    }
}