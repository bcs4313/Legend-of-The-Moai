using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Rendering;
using UnityEngine;
using GameNetcodeStuff;

namespace EasterIsland.src.EasterIslandScripts.Heaven
{
    public class SpeedBoostArea : MonoBehaviour
    {
        public AudioClip boostAudioClip;
        public AudioClip deboostAudioClip;
        List<PlayerControllerB> players = new List<PlayerControllerB>(); 

        private void Update()
        {
            if(players.Count > 0)
            {
                foreach (PlayerControllerB ply in players)
                {
                    ply.movementSpeed = 15f;  // very fest
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            var ply = GetPlayer(other);
            if (ply)
            {
                AudioSource.PlayClipAtPoint(boostAudioClip, other.transform.position);
                if(!players.Contains(ply)) 
                { 
                    players.Add(ply);
                    ply.movementSpeed = 15f;  // very fest
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            var ply = GetPlayer(other);
            if (ply)
            {
                AudioSource.PlayClipAtPoint(deboostAudioClip, other.transform.position);
                if(players.Contains(ply)) 
                { 
                    players.Remove(ply);
                    ply.movementSpeed = 4.6f;  // default
                }
            }
        }

        private PlayerControllerB GetPlayer(Collider other)
        {
            var plyGO = other.gameObject;

            var ply = plyGO.GetComponent<PlayerControllerB>();
            if (ply != null)
            {
                return ply;
            }

            return null;
        }
    }
}
