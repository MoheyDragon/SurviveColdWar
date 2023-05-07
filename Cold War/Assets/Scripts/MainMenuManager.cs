using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    public AK.Wwise.Event MainMusic, ClickSound;
    float CoolStatic, CoolChange,PartyChange;
    public Text Date;
    float t;
    public Image Cap, Com;
    Image party;
    float ColorUnit,DateUnit;
    bool WaitLock,PartyLock,backLock;
    GameObject Credits;
    // Start is called before the first frame update
    void Start()
    {
        Credits = transform.Find("CreditsText").gameObject;
        Credits.SetActive(false);
        PartyLock =backLock=WaitLock =false;
        ColorUnit = DateUnit= 1;
        PartyChange = 2;
        CoolStatic = CoolChange = 1.5f;
        MainMusic.Post(gameObject);
        Date.material.color = Color.blue;
        party = Com;
        t = 0;
    }

    // Update is called once per frame
    void Update()
    {
        if (Time.time>CoolChange)
        {
            if (Date.text == "1991")
                DateUnit = -1;
            if (Date.text == "1947")
                DateUnit = 1;
            Date.text = (int.Parse(Date.text) + DateUnit).ToString();
            CoolChange = CoolStatic + Time.time;
        }
        if (Time.time > PartyChange)
        {
            if (!PartyLock)
            {
                PartyLock = true;
                if (party == Com)
                    party = Cap;
                else
                    party = Com;
            }
            Fill(party);
            
        }
        if (t > 0.8f)
            StartCoroutine(Wait(false));
        if (t < 0)
            StartCoroutine(Wait(true));
        t = t + 0.0004f*ColorUnit;
        if (!WaitLock)
            Date.material.color = Color.Lerp(Color.blue, Color.red, t);
    }
    public void Play()
    {
        ClickSound.Post(gameObject);
        MainMusic.Stop(gameObject);
        SceneManager.LoadScene(2);
    }
    public void FirstPlay()
    {
        ClickSound.Post(gameObject);
        MainMusic.Stop(gameObject);
        SceneManager.LoadScene(1);
    }
    public void CreditsClick()
    {
        ClickSound.Post(gameObject);
        bool Condition = Credits.activeSelf ? false : true;
        Credits.SetActive(Condition);
        Date.gameObject.SetActive(!Condition);
        Com.gameObject.SetActive(!Condition);
        Cap.gameObject.SetActive(!Condition);
    }
    public void Exit()
    {
        ClickSound.Post(gameObject);
        Application.Quit();
    }
    IEnumerator Wait(bool ForRed)
    {
        WaitLock = true;
        yield return new WaitForSeconds(Random.Range(1, 4));
        int random = Random.Range(1, 10);
        ColorUnit = ForRed ? random : -random;
        WaitLock = false;
    }
    void Fill(Image party)
    {
        if (!backLock)
        {
            if (party.fillAmount < 0.875)
                party.fillAmount = party.fillAmount + 0.0007f;
            else
                backLock = true;
        }
        else
            party.fillAmount = party.fillAmount - 0.001f;
        if (party.fillAmount == 0)
        { 
            PartyLock = false; 
            PartyChange = Random.Range(2,3) + Time.time; 
            backLock = false; 
        }
    }
}
