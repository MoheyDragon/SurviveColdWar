using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MMG;
namespace SurviveColdWar
{
    public class WarManager : Singleton<WarManager>
    {
        Party communism;
        Party capitalism;
        private void Start()
        {
            communism = new Party();
            communism.Initialize("COM");
            capitalism = new Party();
            capitalism.Initialize("CAP");
        }
        
        public float GetMapsFillAmount()
        {
            float communismPowerPercent = (float)communism.GetPower()/(float)capitalism.GetPower();
            float fillMapAmount  = GameDifficultyManager.Instance.GetDifficulty * Mathf.Log(communismPowerPercent) + 0.5f;
            return fillMapAmount;
            //float com = (float)communism.GetPower();
            //return com/ allPower;
        }
        public void HandleWin()
        {
        }
    }
}