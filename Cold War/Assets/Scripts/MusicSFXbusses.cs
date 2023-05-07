using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicSFXBusses : MonoBehaviour
{
    public AK.Wwise.RTPC Music, Sfx;
    void Valuer(AK.Wwise.RTPC Bus)
    {
        bool Isquite;
        if (Bus.GetGlobalValue() == 0)
            Isquite = true;
        else
            Isquite = false;
        Bus.SetGlobalValue(Isquite ? 100 : 0);
    }
    public void Click(GameObject button)
    {
        if (button.name == "Music")
            Valuer(Music);
        else
            Valuer(Sfx);
    }
}
