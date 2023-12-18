using System.Collections;
using UnityEngine;
using MMG;
using System;
namespace SurviveColdWar
{
    public class TimeManager : Singleton<TimeManager>
    {
        [SerializeField] float monthLengthInSeconds = 5;
        bool isDateAdvancing;
        Coroutine monthCycle;
        public Action<bool> OnTimePaused;
        private void  Start()
        {
            isDateAdvancing = true;
            PauseTime(false);
            monthCycle= StartCoroutine(CO_NewMonthCycle());
        }
        public void PauseTime(bool isPaused)
        {
            isDateAdvancing=!isPaused;
            OnTimePaused?.Invoke(isPaused);
        }
        public void ForceEndMonth()
        {
            StopCoroutine(monthCycle);
            EndMonth();
        }
        IEnumerator CO_NewMonthCycle()
        {
            float timeElapsed=0;
            while(timeElapsed<=monthLengthInSeconds)
            {
                if (isDateAdvancing)
                {
                    timeElapsed += Time.deltaTime;
                }
                yield return null;
            }
            EndMonth();
        }
        private void EndMonth()
        {
            DateManager.Instance.EndMonth();
            monthCycle= StartCoroutine(CO_NewMonthCycle());
        }
    }
}