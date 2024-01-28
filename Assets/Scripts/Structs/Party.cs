using System.Collections.Generic;
namespace SurviveColdWar
{
    public class Party
    {
        public Parties partyName;
        public President currentPresident;
        Value overallValue;
        List<Entity> entities;
        public void Initialize(Parties party)
        {
            partyName = party;
            overallValue = new Value(10,1000000,50);
            CreateNewPresident(new Value(10, 1000000, 50));
            DateManager.Instance.OnMonthEnd += OnMonthEnd;
        }
        private void CreateNewPresident(Value value)
        {
            currentPresident = new President(PresidentNamer.Instance.GetPresidentName(partyName), value);
        }
        public void OnMonthEnd(int year,int month)
        {
            //foreach (Entity factory in entities)
            //{
            //    overallValue += factory.monthlyValue;
            //}
        }
        public int GetPower() => overallValue.power;
    }
}
