using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace EasterIsland.src.EasterIslandScripts.Cave_Easter_Egg
{
    class ElementalLabSpawnTest : MonoBehaviour
    {
        public GameObject prefab;
        public void Start()
        {
            UnityEngine.Object.Instantiate(prefab, this.transform.position, transform.rotation);
        }
    }
}
