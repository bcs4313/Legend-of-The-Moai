using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

namespace EasterIsland.src.EasterIslandScripts.Heaven
{
    public class HeavenSkyScript : MonoBehaviour
    {
        public Volume HeavenVolume;
        public Volume GlobalVolume;
        public Volume EclipsedVolume;
        public Volume QuantumVolume;

        private void OnTriggerEnter(Collider other)
        {
            if (IsLocalPlayer(other))
            {
                HeavenVolume.weight = 1;
                if (GlobalVolume != null) GlobalVolume.weight = 0;
                if (EclipsedVolume != null) EclipsedVolume.weight = 0;
                if (QuantumVolume != null) QuantumVolume.weight = 0;

                Debug.Log("Local player entered Heaven volume area. Heaven sky active.");
            }
        }

        private void OnTriggerExit(Collider other)
        {
            if (IsLocalPlayer(other))
            {
                HeavenVolume.weight = 0;
                if (GlobalVolume != null) GlobalVolume.weight = 1;
                if (EclipsedVolume != null) EclipsedVolume.weight = 1;
                if (QuantumVolume != null) QuantumVolume.weight = 1;

                Debug.Log("Local player exited Heaven volume area. Heaven sky disabled.");
            }
        }

        private bool IsLocalPlayer(Collider other)
        {
            var plyGO = other.gameObject;

            var ply = plyGO.GetComponent<PlayerControllerB>();
            if (ply != null)
            {
                var enteringPlayerUID = ply.NetworkObject.NetworkObjectId;
                var localPlayerUID = RoundManager.Instance.playersManager.localPlayerController.NetworkObject.NetworkObjectId;
                return enteringPlayerUID == localPlayerUID;
            }

            return false;
        }
    }
}
