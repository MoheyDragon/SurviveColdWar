using UnityEngine;

public class LargeMoneyAmountsTranslator : Singletones<LargeMoneyAmountsTranslator>
{
    public string GetString(float mon)
    {
        if (mon >= 1000000000000000)
            return (Mathf.Round(mon * 10 / 1000000000000000) / 10).ToString() + " Q";
        else if (mon >= 1000000000000)
            return (Mathf.Round(mon * 10 / 1000000000000) / 10).ToString() + " T";
        else if (mon >= 1000000000)
            return (Mathf.Round(mon * 10 / 1000000000) / 10).ToString() + " B";
        else if (mon >= 1000000)
            return (Mathf.Round(mon * 10 / 1000000) / 10).ToString() + " M";
        else
            return (Mathf.Round(mon * 10 / 1000) / 10).ToString() + " K";
    }
    public float GetFloat(string mon)
    {
        if (mon.Contains("Q"))
            return (float.Parse(mon.Substring(0, mon.Length - 2)) * 1000000000000000);
        else if (mon.Contains("T"))
            return (float.Parse(mon.Substring(0, mon.Length - 2)) * 1000000000000);
        else if (mon.Contains("B"))
            return (float.Parse(mon.Substring(0, mon.Length - 2)) * 1000000000);
        else if (mon.Contains("M"))
            return (float.Parse(mon.Substring(0, mon.Length - 2)) * 1000000);
        else
            return (float.Parse(mon.Substring(0, mon.Length - 2)) * 1000);
    }
}
