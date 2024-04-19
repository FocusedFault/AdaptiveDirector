using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;

#nullable disable
namespace AdaptiveDirector
{
    internal class Limiter : MonoBehaviour
    {
        internal CombatDirector director;
        private int limit;
        private float interval;
        private readonly float delta = Time.deltaTime * 0.5f;
        private readonly HashSet<BodyIndex> exception = new HashSet<BodyIndex>();
        private float previous = -1f;
        private int skipped = -1;

        internal void Start()
        {
            this.director.ignoreTeamSizeLimit = true;
            this.limit = --this.director.maximumNumberToSpawnBeforeSkipping;
            this.interval = this.director.maxRerollSpawnInterval;
            string[] strArray = new string[7]
            {
        "MinorConstructBody",
        "HermitCrabBody",
        "JellyfishBody",
        "VoidBarnacleBody",
        "RoboBallMiniBody",
        "GipBody",
        "MinorConstructAttachableBody"
            };
            foreach (string bodyName in strArray)
                this.exception.Add(BodyCatalog.FindBodyIndex(bodyName));
        }

        internal void FixedUpdate()
        {
            TeamIndex teamIndex = this.director.teamIndex;
            float num1 = 0.0f;
            foreach (Component teamMember in TeamComponent.GetTeamMembers(teamIndex))
            {
                CharacterBody component = teamMember.GetComponent<CharacterBody>();
                if ((bool)(UnityEngine.Object)component && this.exception.Contains(component.bodyIndex))
                    num1 += 0.5f;
                else
                    ++num1;
            }
            if (this.director.spawnCountInCurrentWave <= 0)
                this.director.monsterSpawnTimer -= this.delta * (float)this.director.consecutiveCheapSkips;
            TeamDef teamDef = TeamCatalog.GetTeamDef(teamIndex);
            int num2 = teamDef != null ? teamDef.softCharacterLimit : 40;
            float num3 = (float)((double)num1 / (double)num2 / 0.5);
            int num4 = num2 / (this.limit - 1);
            float num5 = num3 * (num1 / (float)num4);
            this.director.maxRerollSpawnInterval = this.interval * (1.5f + num5);
            int val1 = this.limit - Convert.ToInt32(num1) / num4;
            this.director.maximumNumberToSpawnBeforeSkipping = Math.Max(val1, 1);
            if ((double)num1 != (double)this.previous)
            {
                this.previous = num1;
                System.Console.WriteLine("count: " + num1.ToString() + " increase: " + num5.ToString() + " threshold: " + val1.ToString());
            }
            if (this.director.consecutiveCheapSkips == this.skipped)
                return;
            this.skipped = this.director.consecutiveCheapSkips;
            System.Console.WriteLine("skipped: " + this.skipped.ToString());
        }
    }
}
