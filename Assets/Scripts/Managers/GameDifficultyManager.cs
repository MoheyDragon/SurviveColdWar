using System.Collections;
using System.Collections.Generic;
using MMG;
using UnityEngine;
namespace SurviveColdWar
{
    public class GameDifficultyManager : Singleton<GameDifficultyManager>
    {
        [Range(1, 10)][SerializeField] float GameDifficulty = 8.5f;
        float actualGameDifficulty;
        void Start()
        {
            CalculateActualGameDifficulty();
        }
        public float GetMapFillerDifficultyFactor()
        {
            // 
            return 0.7892f * Mathf.Pow(actualGameDifficulty, -0.502f);
        }
        private void CalculateActualGameDifficulty()
        {
            // Inverse GameDifficulty value from 1-10 to be between 50-2, 
            // Which is the difference between parties power to lose the game
            actualGameDifficulty = -5.333f * GameDifficulty + 55.333f;
        }
    }
}