using System.Collections;
using System.Collections.Generic;
using UnityEngine;
namespace SurviveColdWar
{
    public class Country
    {
        President currentPresident;
        Value overallValue;
        List<Entity> entities;
        public void OnMonthEnd()
        {
            foreach (Entity factory in entities)
            {
                overallValue += factory.monthlyValue;
            }
        }
    }
    public class Entity
    {
        public Value monthlyValue;
    }
}
