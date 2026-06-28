using System.Collections;
using UnityEngine;

namespace Mkey
{
    public class ScoreController : MonoBehaviour
    {
        [SerializeField]
        private int baseMatchScore = 240;
        [SerializeField]
        private int increaseComboScore = 40;
        [SerializeField]
        private int maxMatchScore = 40;

        private int combo = 0;

        public int BaseMatchScore { get { return baseMatchScore; } }


        private IEnumerator Start()
        {
            yield return null;
            while (!GameBoard.Instance) yield return null;
            GameBoard.Instance.CollectAction += CollectMatcEventHandler;
            GameBoard.Instance.FailedMatchAction += FailedMatcEventHandler;
        }

        public int GetMatchScore()
        {
            return GetMatchScore(combo);
        }

        private int GetMatchScore(int _combo)
        {
            int score = baseMatchScore + increaseComboScore * _combo;
            if (score > maxMatchScore) score = maxMatchScore;
            return score;
        }

        private void CollectMatcEventHandler(Sprite s1, Sprite s2)
        {
            combo++;
        }

        private void FailedMatcEventHandler()
        {
            combo = 0;
        }

        public int GetMaxLevelScore(int matchesCount)
        {
            int score = 0;
            
            for (int _combo = 0; _combo < matchesCount; _combo++)
            {
                score += GetMatchScore(_combo);
            }
            return score;
        }
    }
}