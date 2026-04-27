using System;
using Unity.Netcode;
using UnityEngine;

namespace EasterIsland.src.EasterIslandScripts.Cave_Easter_Egg
{
    class ServerOnlyInitializationScript : NetworkBehaviour
    {
        public GrabbableObject item;
        public NetworkObject netObjSelf;

        public void Start()
        {
            // only permit the object if its owned by the server
            if (!IsServer && !netObjSelf.IsSpawned)
            {
                Destroy(this.gameObject);
                return;
            }

            if (RoundManager.Instance.IsHost && !item.NetworkObject.IsSpawned)
            {
                item.NetworkObject.Spawn();
                return;
            }
        }
    }
}
