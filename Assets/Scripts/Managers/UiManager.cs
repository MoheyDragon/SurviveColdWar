using UnityEngine;
using MMG;
using TMPro;
namespace SurviveColdWar
{
    public class UiManager : Singleton<UiManager>
    {
        [SerializeField] TextMeshProUGUI date;
        [SerializeField] Color pausedColor, unpausedColor;
        protected override void Awake()
        {
            base.Awake();
            DateManager.Instance.OnMonthEnd += UpdateDate;
            TimeManager.Instance.OnTimePaused += ColorDateText;
        }
        private void UpdateDate(int year,int month)
        {
            date.text = year + " / " + month;
        }
        private void ColorDateText(bool isPaused)
        {
            date.color = isPaused ? pausedColor : unpausedColor;
        }

    }
}