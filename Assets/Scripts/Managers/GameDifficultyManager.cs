using System.Collections;
using System.Collections.Generic;
using MMG;
using UnityEngine;
namespace SurviveColdWar
{
    public class GameDifficultyManager : Singleton<GameDifficultyManager>
    {
        [Range(1, 10)][SerializeField] float GameDifficulty = 10;
        float GameLevelEffect;
        public float GetDifficulty => GameLevelEffect;
        void Start()
        {
            CalculateActualGameDifficulty();
        }
        private void CalculateActualGameDifficulty()
        {
            GameLevelEffect = 0.7892f * Mathf.Pow(-5.333f * GameDifficulty + 55.333f, -0.502f);
        }
    }
}