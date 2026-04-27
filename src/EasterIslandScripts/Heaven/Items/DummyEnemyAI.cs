using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace EasterIsland.src.EasterIslandScripts.Heaven.Items
{
    public class DummyEnemyAIForNuke : EnemyAI
    {
        /*
        public NuclearBomb bomb;

        // --- Override core EnemyAI systems to prevent them from doing anything ---
        public override void Start() { } // Block navmesh / RoundManager init
        public override void Update() { } // Block AI ticking
        public override void DoAIInterval() { } // Block movement

        public override void OnDestroy() { } // Block cleanup logic
        public override void CancelSpecialAnimationWithPlayer() { } // No animations
        public override void EnableEnemyMesh(bool enable, bool overrideDoNotSet = false) { }
        public override void SetEnemyOutside(bool outside = false) { }
        public override void FinishedCurrentSearchRoutine() { }
        public override void ReachedNodeInSearch() { }
        public override void DaytimeEnemyLeave() { }

        // --- Stun + Hit redirected to the bomb ---
        public override void SetEnemyStunned(bool setToStunned, float setToStunTime = 1f, PlayerControllerB setStunnedByPlayer = null)
        {
            bomb.dangerLevel++;
            bomb.dangerResultActivateClientRpc();
        }

        public override void HitEnemy(int force = 1, PlayerControllerB playerWhoHit = null, bool playHitSFX = false, int hitID = -1)
        {
            bomb.dangerLevel++;
            bomb.dangerResultActivateClientRpc();
        }

        // --- Explosion + noise triggers blocked ---
        public override void HitFromExplosion(float distance) { }

        // --- Death system blocked ---
        public override void KillEnemy(bool destroy = false) { }
        */
    }
}
