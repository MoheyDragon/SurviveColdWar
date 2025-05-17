using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class NukeMeterUpdater : MonoBehaviour
{
    Text text;
    Image fill;
    public static float nukeDanger;
    //public AK.Wwise.RTPC Danger;
    public void Awake()
    {
        text = gameObject.GetComponent<Text>();
        fill = transform.parent.GetChild(0).gameObject.GetComponent<Image>();
    }
    private void Start()
    {
        nukeDanger = 0;
    }
    void Update()
    {
        if (nukeDanger > 99.99f&&!CountryManager.LoseLock)
        {
            Time.timeScale = 0;
            fill.fillAmount = 1;
            text.text = "100 %";
            transform.parent.parent.GetComponent<CountryManager>().lose();
        }
        else
        {
            fill.fillAmount = nukeDanger / 100;
            text.text = Mathf.RoundToInt(nukeDanger).ToString() + " %";
            //Danger.SetGlobalValue(nukeDanger);
        }
    }
}
