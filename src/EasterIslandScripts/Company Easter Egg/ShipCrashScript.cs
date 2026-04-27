using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace EasterIsland.src.EasterIslandScripts.Company_Easter_Egg
{
    public class ShipCrashScript : NetworkBehaviour
    {
        public AudioSource crashAudio;
        public Animator shipAnimator;


        // Explosions!  + 0.3 sec
        public ParticleSystem frontStartExplosion;  // 5 seconds in
        public ParticleSystem surroundingStartExplosion;  // 5 seconds in
        public ParticleSystem Explosion2;  // 6.25 seconds in
        public ParticleSystem Explosion3;  // 6.5 seconds in
        public ParticleSystem Explosion4;  // 6.9 seconds in

        private bool crashing = false;

        public void Start()
        {
            if(RoundManager.Instance.IsHost)
            {
                //awaitCrash();
            }
        }

        public bool getCrashing()
        {
            return crashing;
        }

        public async void awaitCrash()
        {
            await Task.Delay(5000);
            playCutsceneClientRpc();
        }

        [ClientRpc]
        public void playCutsceneClientRpc()
        {
            cutScenePlay();
        }

        public async void cutScenePlay()
        {
            crashing = true;
            crashAudio.Play();
            shipAnimator.Play("Crash");

            await Task.Delay(5300);
            frontStartExplosion.Play();
            surroundingStartExplosion.Play();
            await Task.Delay(1250);
            Explosion2.Play();
            await Task.Delay(250);
            Explosion3.Play();
            await Task.Delay(400);
            Explosion4.Play();

            crashing = false;
        }
    }
}
