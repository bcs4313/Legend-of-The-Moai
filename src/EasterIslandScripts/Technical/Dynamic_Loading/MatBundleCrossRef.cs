using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.TextCore.Text;

namespace EasterIsland.src.EasterIslandScripts.Technical.Dynamic_Loading
{
    // pulls a MATERIAL from another asset bundle and loads it into the material component
    // Expects a Mesh Renderer with a placeholder material or no material
    // Part of an optimization strategy to remove ALL material duplicates in bundles
    public class MatBundleCrossRef : MonoBehaviour
    {
        private MeshRenderer renderToPullInto;
        public String targetMaterialName;

        // bundle to pull from
        public enum AssetBundleSource { 
            eastermoonlevel,  // implemented
            easterheavennetobjs,
            easterislandheavenscene, 
            eastermoonnetobjs 
        }

        private static AssetBundle easterMoonLevelBundle;

        public AssetBundleSource targetBundle;

        public void Start()
        {
            renderToPullInto = GetComponent<MeshRenderer>();

            switch(targetBundle)
            {
                case AssetBundleSource.eastermoonlevel:
                    if(!easterMoonLevelBundle)
                    {
                        lazyPullEasterMoonLevelBundle();
                    }
                    else
                    {
                        var mat = easterMoonLevelBundle.LoadAsset<Material>(targetMaterialName);
                        if(mat)
                        {
                            InjectMaterial(mat);
                        }
                        else
                        {
                            Debug.LogWarning($"Material '{targetMaterialName}' not found in {targetBundle}.");
                        }
                    }
                    break;
                case AssetBundleSource.easterislandheavenscene:
                    if(HeavenLoader.heavenBundle)
                    {
                        var mat = HeavenLoader.heavenBundle.LoadAsset<Material>(targetMaterialName);
                        if (mat)
                        {
                            InjectMaterial(mat);
                        }
                        else
                        {
                            Debug.LogWarning($"Material '{targetMaterialName}' not found in {targetBundle}.");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Material '{targetMaterialName}' not found in {targetBundle}.");
                    }
                    break;
                case AssetBundleSource.easterheavennetobjs:
                    if (Plugin.heavenNetBundle)
                    {
                        var mat = Plugin.heavenNetBundle.LoadAsset<Material>(targetMaterialName);
                        if (mat)
                        {
                            InjectMaterial(mat);
                        }
                        else
                        {
                            Debug.LogWarning($"Material '{targetMaterialName}' not found in {targetBundle}.");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Material '{targetMaterialName}' not found in {targetBundle}.");
                    }
                    break;
                case AssetBundleSource.eastermoonnetobjs:
                    if (Plugin.easterislandBundle)
                    {
                        var mat = Plugin.easterislandBundle.LoadAsset<Material>(targetMaterialName);
                        if (mat)
                        {
                            InjectMaterial(mat);
                        }
                        else
                        {
                            Debug.LogWarning($"Material '{targetMaterialName}' not found in {targetBundle}.");
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Material '{targetMaterialName}' not found in {targetBundle}.");
                    }
                    break;
                default:
                    break;
            }
        }

        public void InjectMaterial(Material newMat)
        {
            if (renderToPullInto == null || newMat == null)
                return;

            renderToPullInto.sharedMaterials = new Material[] { newMat };
        }


        bool lazyMoonLevelPulling = false;
        public async void lazyPullEasterMoonLevelBundle()
        {
            while(lazyMoonLevelPulling)
            {
                await Task.Delay(500);
            }

            if (easterMoonLevelBundle)
            {
                var mat = easterMoonLevelBundle.LoadAsset<Material>(targetMaterialName);
                if (mat)
                {
                    InjectMaterial(mat);
                }
                return;
            }

            lazyMoonLevelPulling = true;

            while (!easterMoonLevelBundle)
            {
                foreach (AssetBundle bundle in AssetBundle.GetAllLoadedAssetBundles())
                {
                    if (bundle.name.Contains("eastermoonlevel"))
                    {
                        var mat = bundle.LoadAsset<Material>(targetMaterialName);
                        try
                        {
                            if (mat)
                            {
                                easterMoonLevelBundle = bundle;
                                InjectMaterial(mat);
                            }
                            break;
                        }
                        catch (Exception e)
                        {

                        }
                    }
                }

                await Task.Delay(500);
            }
            lazyMoonLevelPulling = false;
        }

    }
}
