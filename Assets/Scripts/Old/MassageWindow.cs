using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; 

public class MassageWindow : MonoBehaviour
{
    //public AK.Wwise.Event ClickSound;
    public void Destroy(GameObject game)
    {
        //ClickSound.Post(gameObject);
        Time.timeScale = 0.09f;
        StartCoroutine(Wait(game));
        
    }
    public void Quit()
    {
        //ClickSound.Post(gameObject);
        //OldGameManager.Canvas.GetComponent<CountryManager>().Music.Stop(OldGameManager.Canvas.gameObject);
        Time.timeScale = 1;
        SceneManager.LoadScene(0);
    }
    public void Restart()
    {
        //ClickSound.Post(gameObject);
        gameObject.SetActive(false);
        foreach (GameObject game in GameObject.FindGameObjectsWithTag("Massage"))
        {
            game.SetActive(false);
        }
        Time.timeScale = 1;
        transform.parent.GetComponent<CountryManager>().ShutUp();
        SceneManager.LoadScene(2);
    }
    IEnumerator Wait(GameObject game)
    {
        yield return new WaitForSeconds(0.011f);
        transform.tag = "Untagged";
        if (GameObject.FindGameObjectsWithTag("Massage").Length == 0)
        {
            if (!OldGameManager.Canvas.GetComponent<CountryManager>().IsTutorial)
                foreach (Button button in OldGameManager.Canvas.GetComponentsInChildren<Button>())
                    button.interactable = true;
            if (!CountryManager.ElectionsLock)
            {
                GameObject.Find("Canvas").GetComponent<CountryManager>().ResumeAll();
                Time.timeScale = 1;
            }
        }
        Destroy(game.gameObject, 0.00000000001f);
    }
}
