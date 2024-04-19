using UnityEngine;
using RoR2;
using System.Collections.Generic;

namespace AdaptiveDirector
{
    internal class CreditAdaptation : MonoBehaviour
    {
        public float enemyDeathCounter = 0.0f;
        public float enemiesNeeded = 8f;
        public float creditMultiplier = 1f;
        public float stopwatch = 0.0f;
        public float decayStopwatch = 0.0f;
        public float delayAfterIncrease = 60f;
        public bool increasedCredits = false;
        public List<CombatDirector> combatDirectors = new List<CombatDirector>();
        // public readonly HashSet<BodyIndex> exception = new HashSet<BodyIndex>();

        private const float BaseEnemiesNeeded = 6f;
        private const float MultiplierIncrement = 0.25f;

        private void Start()
        {
            /*
            string[] enemyBodyNames = { "WispBody", "MinorConstructBody", "HermitCrabBody", "JellyfishBody", "VoidBarnacleBody", "RoboBallMiniBody", "GipBody", "MinorConstructAttachableBody" };
            foreach (string bodyName in enemyBodyNames)
                exception.Add(BodyCatalog.FindBodyIndex(bodyName));
            */
            enemiesNeeded = BaseEnemiesNeeded + (float)(1 * (Run.instance.stageClearCount + 1.0));
        }

        private void FixedUpdate()
        {
            stopwatch += Time.fixedDeltaTime;
            decayStopwatch += Time.fixedDeltaTime;

            if (stopwatch >= delayAfterIncrease && increasedCredits)
                ResetCreditMultiplier();

            if (stopwatch < 5.0 && enemyDeathCounter >= enemiesNeeded && !increasedCredits)
                IncreaseCreditMultiplier();

            if (stopwatch >= 5.0 && enemyDeathCounter < enemiesNeeded && !increasedCredits)
                ResetCounters();

            if (decayStopwatch >= delayAfterIncrease * 2.0 && creditMultiplier > 1.5)
                ResetMultiplierAndCounters();
        }

        private void ResetCreditMultiplier()
        {
            creditMultiplier = 1f;
            enemiesNeeded = 8f;
            Chat.SendBroadcastChat(new Chat.SimpleChatMessage() { baseToken = "Credit Max Reached, Resetting, Current Multiplier: 1 | Current Armor Cap: 0" });
            ResetCounters();
        }

        private void IncreaseCreditMultiplier()
        {
            increasedCredits = true;
            stopwatch = 0.0f;
            decayStopwatch = 0.0f;
            enemyDeathCounter = 0.0f;
            creditMultiplier += MultiplierIncrement / (Run.instance.stageClearCount + 1);
            creditMultiplier = Mathf.Clamp(creditMultiplier, 1f, 2f);
            string message = creditMultiplier == 2.0 ?
                $"Credit Max Reached, Current Multiplier: {creditMultiplier} | Current Armor Cap: 0" :
                $"Current Multiplier: {creditMultiplier} | Current Armor Cap: {(float)((creditMultiplier - 1.0) * 4.0 * 100.0)}";
            Chat.SendBroadcastChat(new Chat.SimpleChatMessage() { baseToken = message });
            ApplyMultiplierToCombatDirectors();
        }

        private void ResetCounters()
        {
            stopwatch = 0.0f;
            decayStopwatch = 0.0f;
            enemyDeathCounter = 0.0f;
        }

        private void ResetMultiplierAndCounters()
        {
            creditMultiplier = 1f;
            enemiesNeeded = 5f;
            enemyDeathCounter = 0.0f;
            stopwatch = 0.0f;
            Chat.SendBroadcastChat(new Chat.SimpleChatMessage() { baseToken = "No boost in 2 mins, Resetting, Current Multiplier: 1 | Current Armor Cap: 0" });
        }

        private void ApplyMultiplierToCombatDirectors()
        {
            foreach (CombatDirector combatDirector in combatDirectors)
            {
                foreach (CombatDirector.DirectorMoneyWave moneyWave in combatDirector.moneyWaves)
                    moneyWave.multiplier *= creditMultiplier;
            }
        }
    }
}
