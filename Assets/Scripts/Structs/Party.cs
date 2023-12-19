using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SurviveColdWar
{
    public class Party
    {
        string name;
        President currentPresident;
        Value overallValue;
        List<Entity> entities;
        public void Initialize(string _name)
        {
            name = _name;
            overallValue = new Value(10,1000000,50);
            DateManager.Instance.OnMonthEnd += OnMonthEnd;
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
