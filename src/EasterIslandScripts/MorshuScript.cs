using UnityEngine;

namespace EasterIsland.src.EasterIslandScripts
{
    class MorshuScript : MonoBehaviour
    {
        public GrabbableObject item;

        public void Start()
        {
            if (!item.NetworkObject.IsSpawned)
            {
                item.NetworkObject.Spawn();
                Destroy(this);
                return;
            }

            var scanNode = this.item.gameObject.GetComponentInChildren<ScanNodeProperties>();
            scanNode.subText = "Value: " + scanNode.scrapValue + " rupees";
        }
    }
}
