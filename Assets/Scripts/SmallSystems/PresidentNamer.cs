using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using MMG;
namespace SurviveColdWar
{
    public class PresidentNamer : Singleton<PresidentNamer>
    {
        [SerializeField] PartyPresidentsNames communismNames,capitalismNames;
        public  string GetPresidentName(Parties partyName)
        {
            if (partyName==Parties.Communism)
                return GetPresidentName(communismNames);
            else
                return GetPresidentName(capitalismNames);
        }
        private string GetPresidentName(PartyPresidentsNames partyPresidentsNames)
        {
            string presidentName = "";
            int randomIndex = Random.Range(0,partyPresidentsNames.FirstName.Length);
            presidentName += partyPresidentsNames.FirstName[randomIndex];
            randomIndex = Random.Range(0, partyPresidentsNames.FirstName.Length);
            presidentName += " " + partyPresidentsNames.LastName[randomIndex];
            return presidentName;
        }
    }
}
