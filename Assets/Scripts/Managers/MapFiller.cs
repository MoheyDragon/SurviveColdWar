using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MMG;
using UnityEngine.UI;
namespace SurviveColdWar
{
    public class MapFiller : Singleton<MapFiller>
    {
        [SerializeField] Image[] maps;
        private void Start()
        {
            DateManager.Instance.OnMonthEndLate += FillMap;
        }
        public void FillMap()
        {
            float fillAmount=WarManager.Instance.GetMapsFillAmount();
            foreach (Image map in maps)
            {
                map.fillAmount = fillAmount;
            }
        }
    }
}