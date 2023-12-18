using System;
using System.Collections;
using MMG;
namespace SurviveColdWar
{
    public class DateManager : Singleton<DateManager>
    {
        int currentMonth;
        int currentYear;

        const int coldWarStartYear = 1945;
        const int coldWarEndYear = 1991;

        public Action<int,int> OnMonthEnd;
        public Action OnMonthEndLate;

        public void EndMonth()
        {
            if (currentMonth == 12)
            {
                EndYear();
            }
            else
                currentMonth++;
            OnMonthEnd?.Invoke(currentYear,currentMonth);
            StartCoroutine(CO_LateEndMonth());
        }
        IEnumerator CO_LateEndMonth()
        {
            yield return null;
            OnMonthEndLate?.Invoke();
        }
        private void Start()
        {
            currentMonth = 1;
            currentYear = coldWarStartYear;
            OnMonthEnd?.Invoke(currentYear, currentMonth);
        }
        private void EndYear()
        {
            currentMonth = 1;
            currentYear++;
            if (currentYear == coldWarEndYear)
            {
                WarManager.Instance.HandleWin();
            }
        }

    } 
}