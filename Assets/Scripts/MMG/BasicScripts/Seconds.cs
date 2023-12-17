using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace MMG
{
    public class Seconds : Singleton<Seconds>
    {
        public static Dictionary<float, WaitForSeconds> seconds;
        private void Start()
        {
            seconds = new Dictionary<float, WaitForSeconds>();
        }
        public static WaitForSeconds GetCachedWaitForSeconds(float secondsToLookFor)
        {
            if (seconds.ContainsKey(secondsToLookFor))
                return seconds[secondsToLookFor];
            else
            {
                WaitForSeconds newSeconds = new WaitForSeconds(secondsToLookFor);
                seconds.Add(secondsToLookFor, newSeconds);
                return newSeconds;
            }
        }
    }
}