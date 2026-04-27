using EasterIsland.src.EasterIslandScripts.Heaven.BodyMods;
using EasterIsland.src.EasterIslandScripts.Library_Easter_egg;
using GameNetcodeStuff;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace EasterIsland.src.EasterIslandScripts.Heaven.Surgery
{
    public class SurgeryHitBox : NetworkBehaviour
    {
        public LeverFlickSurgery link;
        public ButtonPressSurgeryClear link2;

        private void OnTriggerEnter(Collider other)
        {
            Debug.Log("SurgeryEnterCollider: " + other.name);
            var ply = GetValidPlayer(other);
            if (ply != null && !link.targetPlayers.Contains(ply))
            {
                link.targetPlayers.Add(ply);
                if (link2) { link2.targetPlayers.Add(ply); }
                Debug.Log($"Player {ply.playerUsername} entered surgery zone.");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            Debug.Log("SurgeryExitCollider: " + other.name);
            var ply = GetValidPlayer(other);
            if (ply != null && link.targetPlayers.Contains(ply))
            {
                link.targetPlayers.Remove(ply);
                if (link2) { link2.targetPlayers.Remove(ply); }
                Debug.Log($"Player {ply.playerUsername} exited surgery zone.");
            }
        }

        private PlayerControllerB GetValidPlayer(Collider other)
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
