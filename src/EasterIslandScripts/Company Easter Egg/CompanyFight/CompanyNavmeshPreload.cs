using System;
using System.Collections.Generic;
using System.Text;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

namespace EasterIsland.src.EasterIslandScripts.Company_Easter_Egg.CompanyFight
{
    // prevents a lag spike from dynamically loading the thing, which causes an auto-bake.
    public class CompanyNavmeshPreload : MonoBehaviour
    {
        public NavMeshSurface surface;

        public void Start()
        {
            NavMesh.AddNavMeshData(surface.navMeshData);
        }
    }
}
