using UnityEngine;

namespace EasterIsland.src.EasterIslandScripts.BookLogic
{
    class GenericInitScript : MonoBehaviour
    {
        public GrabbableObject item;

        public void Start()
        {
            if (RoundManager.Instance.IsHost && !item.NetworkObject.IsSpawned)
            {
                item.NetworkObject.Spawn();
                Destroy(this);

                ScanNodeProperties scanNode = item.gameObject.GetComponentInChildren<ScanNodeProperties>();
                scanNode.subText = "Value: " + scanNode.scrapValue + " gum gum";
                return;
            }

            ScanNodeProperties scanNode2 = item.gameObject.GetComponentInChildren<ScanNodeProperties>();
            scanNode2.subText = "Value: " + scanNode2.scrapValue + " gum gum";
        }
    }
}
