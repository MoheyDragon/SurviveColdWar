using System;
using MMG;
namespace SurviveColdWar
{
    public class DateManager : Singleton<DateManager>
    {
        int currentMonth;
        int currentYear;

        const int coldWarStartYear = 1945;
        const int coldWarEndYear = 1991;

        public Action OnMonthEnd;
        public void EndMonth()
        {
            if (currentMonth == 12)
            {
                EndYear();
            }
            else
                currentMonth++;
            OnMonthEnd?.Invoke();
        }
        private void Start()
        {
            currentMonth = 1;
            currentYear = coldWarStartYear;
        }
        private void EndYear()
        {
            currentMonth = 1;
            currentYear++;
            if (currentYear == coldWarEndYear)
            {
                GameManager.Instance.HandleWin();
            }
        }

    } 
}