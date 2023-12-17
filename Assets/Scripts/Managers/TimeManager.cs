using System.Collections;
using UnityEngine;
using MMG;
namespace SurviveColdWar
{
    public class TimeManager : Singleton<TimeManager>
    {
        [SerializeField] float monthLengthInSeconds = 5;
        bool isDateAdvancing;
        Coroutine monthCycle;
        private void  Start()
        {
            isDateAdvancing = true;
            monthCycle= StartCoroutine(CO_NewMonthCycle());
        }
        public void PauseTime(bool isPaused)
        {
            isDateAdvancing=!isPaused;
        }
        public void ForceEndMonth()
        {
            StopCoroutine(monthCycle);
            EndMonth();
        }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
                PauseTime(true);

            if (Input.GetKeyDown(KeyCode.LeftShift))
                PauseTime(false);

            if (Input.GetKeyDown(KeyCode.E))
                ForceEndMonth();

        }
            float timeElapsed;
        IEnumerator CO_NewMonthCycle()
        {
            timeElapsed = 0;
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