using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine;

namespace EasterIsland.src.EasterIslandScripts.Technical
{
    // Credit to GioSeul for his excellent code on adjusting
    // the resolution of the game!

    // this class intends to dynamically adjust the
    // resolution of easter island to make it look as
    // good as possible. We do this because lethal's
    // resolution settings make the moon look bad.
    /*
    public class EI_Graphics_Adjuster
    {
        // DOESN't WORK. We need a dictionary lol
        public static Dictionary<int, Vector2> baseResDict = new Dictionary<int, Vector2>();

        internal static void UpdateCameras(float ResolutionMultiplier)
        {
           
            // Find all cameras in the scene
            Camera[] cameras = UnityEngine.Object.FindObjectsOfType<Camera>();
            foreach (Camera camera in cameras)
            {
                if (camera.gameObject.name != "MapCamera" && camera.targetTexture)
                {
                    RenderTexture targetTexture = camera.targetTexture;
                    Vector2 baseResolution;

                    // Check if the camera's original resolution has been stored
                    if (baseRes == Vector2.zero)
                    {
                        baseResolution = new Vector2(targetTexture.width, targetTexture.height);
                        baseRes = new Vector2(baseResolution.x, baseResolution.y);
                    }
                    else
                    {
                        baseResolution = new Vector2(baseRes.x, baseRes.y);
                    }

                    // Calculate the new width and height
                    int newWidth = Mathf.RoundToInt(baseResolution.x * ResolutionMultiplier);
                    int newHeight = Mathf.RoundToInt(baseResolution.y * ResolutionMultiplier);

                    // Update the RenderTexture only if the dimensions have changed
                    if (targetTexture.width != newWidth || targetTexture.height != newHeight)
                    {
                        targetTexture.Release(); // Release the current RenderTexture
                        targetTexture.width = newWidth; // Set the new width
                        targetTexture.height = newHeight; // Set the new height
                        targetTexture.Create(); // Recreate the RenderTexture
                    }
                }
            }

            // Optionally free up unused resources (use sparingly to avoid performance hitches)
            Resources.UnloadUnusedAssets();
        }

        /*
        private static void ApplyHDRPSettings(HDAdditionalCameraData hdCameraData)
        {
            // Use descriptive constants for indices
            const uint DecalLayersIndex = 96U;
            const uint SSGIIndex = 95U;
            const uint RayTracingIndex = 92U;
            const uint VolumetricCloudsIndex = 79U;
            const uint SSSIndex = 46U;
            const uint VolumeReprojectionIndex = 29U;
            const uint TransparentPrepassIndex = 8U;
            const uint TransparentPostpassIndex = 9U;

            hdCameraData.customRenderingSettings = true;

            hdCameraData.renderingPathCustomFrameSettingsOverrideMask.mask[DecalLayersIndex] = Imperium.Settings.Rendering.DecalLayers.Value;
            hdCameraData.renderingPathCustomFrameSettings.SetEnabled(DecalLayersIndex, Imperium.Settings.Rendering.DecalLayers.Value);

            hdCameraData.renderingPathCustomFrameSettingsOverrideMask.mask[SSGIIndex] = Imperium.Settings.Rendering.SSGI.Value;
            hdCameraData.renderingPathCustomFrameSettings.SetEnabled(SSGIIndex, Imperium.Settings.Rendering.SSGI.Value);

            hdCameraData.renderingPathCustomFrameSettingsOverrideMask.mask[RayTracingIndex] = Imperium.Settings.Rendering.RayTracing.Value;
            hdCameraData.renderingPathCustomFrameSettings.SetEnabled(RayTracingIndex, Imperium.Settings.Rendering.RayTracing.Value);

            hdCameraData.renderingPathCustomFrameSettingsOverrideMask.mask[VolumetricCloudsIndex] = Imperium.Settings.Rendering.VolumetricClouds.Value;
            hdCameraData.renderingPathCustomFrameSettings.SetEnabled(VolumetricCloudsIndex, Imperium.Settings.Rendering.VolumetricClouds.Value);

            hdCameraData.renderingPathCustomFrameSettingsOverrideMask.mask[SSSIndex] = Imperium.Settings.Rendering.SSS.Value;
            hdCameraData.renderingPathCustomFrameSettings.SetEnabled(SSSIndex, Imperium.Settings.Rendering.SSS.Value);

            hdCameraData.renderingPathCustomFrameSettingsOverrideMask.mask[VolumeReprojectionIndex] = Imperium.Settings.Rendering.VolumeReprojection.Value;
            hdCameraData.renderingPathCustomFrameSettings.SetEnabled(VolumeReprojectionIndex, Imperium.Settings.Rendering.VolumeReprojection.Value);

            hdCameraData.renderingPathCustomFrameSettingsOverrideMask.mask[TransparentPrepassIndex] = Imperium.Settings.Rendering.TransparentPrepass.Value;
            hdCameraData.renderingPathCustomFrameSettings.SetEnabled(TransparentPrepassIndex, Imperium.Settings.Rendering.TransparentPrepass.Value);

            hdCameraData.renderingPathCustomFrameSettingsOverrideMask.mask[TransparentPostpassIndex] = Imperium.Settings.Rendering.TransparentPostpass.Value;
            hdCameraData.renderingPathCustomFrameSettings.SetEnabled(TransparentPostpassIndex, Imperium.Settings.Rendering.TransparentPostpass.Value);
        }
    }
    */
}
