using EasterIsland.src.EasterIslandScripts;
using EasterIsland.src.EasterIslandScripts.Technical;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.UIElements;

namespace DayNightCycles
{
    public class DayAndNightCycle : MonoBehaviour
    {
        [Header("Time Settings")]
        [Range(0f, 24f)]
        [Tooltip("Slider allows you to set the starting time. Range 0-24")]
        public float currentTime;
        [Tooltip("Time elapsed multiplier. When set to 1, one second of real time equals one minute of script time. A negative value turns back time.")]
        public float timeSpeed = 1f; // time speed multiplier
        private float timeDivider = 60f; // divides the time so that you can obtain the exact passage of seconds

        [Header("Current Time")]
        [Tooltip("Current time in the hh:mm:ss system.")]
        public string currentTimeString; // shows time in the hh:mm system in the inspector

        [Header("Sun Settings")]
        [Tooltip("A light source simulating the Sun.")]
        public Light sunLight; // sun light object
        [Range(0f, 90f)] // sun latitude range
        [Tooltip("Sun latitude determines the maximum height of the Sun. Range 0-90")]
        public float sunLatitude = 20f; // sun latitude
        [Range(-180f, 180f)] // sun longitude range
        [Tooltip("Sun longitude determines position of the Sun. Range -180, 180")]
        public float sunLongitude = -90f; // sun longitude
        [Tooltip("Basic Sun intensity value. Together with Sun Intensity Multiplier affects the brightness of the Sun during the cycle.")]
        public float sunIntensity = 60000f; // sun base intensity
        [Tooltip("Decreases or increases Sun intensity over time. 0 - Midnight | 0.25 - Dawn | 0.5 - Noon | 0.75 - Dusk | 1 - Midnight")]
        public AnimationCurve sunIntensityMultiplier; // a curve that decreases or increases sun intensity over time
        [Range(1500f, 7000f)]
        [Tooltip("Basic Sun temperature value in Kelvin. Together with Sun Temperature Curve affects the temperature of the Sun during the cycle.")]
        public float sunTemperature = 6500f; // sun base temperature
        [Tooltip("Decreases or increases Sun temperature over time. 0 - Midnight | 0.25 - Dawn | 0.5 - Noon | 0.75 - Dusk | 1 - Midnight")]
        public AnimationCurve sunTemperatureCurve; // a curve that decreases or increases sun temperature over time

        [Header("Moon Settings")]
        [Tooltip("A light source simulating the Moon.")]
        public Light moonLight; // moon light object
        [Range(0f, 90f)] // moon latitude range
        [Tooltip("Moon latitude determines the maximum height of the Moon. For best results, the value should be the same as star latitude. Range 0-90")]
        public float moonLatitude = 40f; // moon latitude
        [Range(-180f, 180f)] //moon latitude range
        [Tooltip("Moon longitude determines position of the Moon. For best results, the value should be the same as star longitude. Range -180, 180")]
        public float moonLongitude = 90f; // moon longitude
        [Tooltip("Basic moon intensity value. Together with Moon Intensity Multiplier affects the brightness of the Moon during the cycle.")]
        public float moonIntensity = 12000f; // moon base intensity
        public AnimationCurve moonIntensityMultiplier; // curve that decreases or increases moon intensity over time
        [Range(6500f, 20000f)] // moon temperature range
        [Tooltip("Basic Moon temperature value in Kelvin. Together with Moon Temperature Curve affects the temperature of the Moon during the cycle.")]
        public float moonTemperature = 10000f; // moon base temperature
        [Tooltip("Decreases or increases Moon temperature over time. 0 - Midnight | 0.25 - Dawn | 0.5 - Noon | 0.75 - Dusk | 1 - Midnight")]
        public AnimationCurve moonTemperatureCurve;  // a curve that decreases or increases moon intensity over time

        [Header("Stars")]
        public VolumeProfile volumeProfile; // volume profile
        private PhysicallyBasedSky skySettings; // access to physically based sky
        [Range(0f, 90f)] // star latitude range
        [Tooltip("Star latitude determines the height of the stars rotation point (Polar Star). Range 0-90")]
        public float polarStarLatitude = 40f; // star latitude
        [Range(-180f, 180f)] // star longitude range
        [Tooltip("Star longitude determines the position of the stars rotation point (Polar Star). Range -180, 180")]
        public float polarStarLongitude = 90f; // star longitude
        [Tooltip("Star intensity value. Together with Star Curve affects the brightness of the skybox during the cycle.")]
        public float starsIntensity = 8000f; // star intensity
        [Tooltip("Decreases or increases skybox intensity over time. 0 - Midnight | 0.25 - Dawn | 0.5 - Noon | 0.75 - Dusk | 1 - Midnight")]
        public AnimationCurve starsCurve; // curve that decreases or increases star intensity over time
        [Tooltip("The curve of the horizon tint changing over time")]
        public AnimationCurve horizonTintCurve; // horizon tint curve
        [Tooltip("The curve of the zenit tint changing over time")]
        public AnimationCurve zenithTintCurve; // zenit tint curve

        [Header("Control Indicators")]
        [Tooltip("Displays a marker whether it is day or night")]
        public bool isDay = true; // displays a marker whether it is day or night
        [Tooltip("Displays a marker whether it Sun is active or not")]
        public bool sunActive = true; //displays in inspector a marker whether it sun is active or not
        [Tooltip("Displays a marker whether it Moon is active or not")]
        public bool moonActive = true; //displays in inspector a marker whether it moon is active or not

        [Header("Moon Activation Trigger")]
        [Tooltip("Determines when the moon turns on and off.")]
        public float moonActivationStart = 6.3f; //displays in inspector a marker whether it moon is active or not
        public float moonActivationEnd = 17.7f; //displays in inspector a marker whether it moon is active or not

        private HDAdditionalLightData sunLightData; // cached HDAdditionalLightData for sun
        private HDAdditionalLightData moonLightData; // cached HDAdditionalLightData for moon
        private Light sunLightComponent; // sun light component
        private Light moonLightComponent; // moon light component

        // volume objects
        public GameObject stormyFog;

        public GameObject normalSun;
        public GameObject normalVolume;
        public GameObject eclipseSun;
        public GameObject eclipseVolume;

        // custom weather vars
        float fluxTimer = 10;
        public GameObject fluxPrefab;
        public GameObject mainVolume;
        public GameObject QuantumVolume;
        public Light trueLightSource;

        public static List<Volume> volumesEnforced = new List<Volume>();

        void Awake()
        {
            sunLightData = sunLight.GetComponent<HDAdditionalLightData>(); // cache HDAdditionalLightData for sun
            moonLightData = moonLight.GetComponent<HDAdditionalLightData>(); // cache HDAdditionalLightData for moon
            sunLightComponent = sunLight.GetComponent<Light>(); // sun light component
            moonLightComponent = moonLight.GetComponent<Light>(); // moon light component

            // Stormy Set
            if (StartOfRound.Instance.currentLevel.currentWeather == LevelWeatherType.Stormy || StartOfRound.Instance.currentLevel.currentWeather == LevelWeatherType.Rainy)
            {
                stormyFog.SetActive(true);
            }
            else
            {
                stormyFog.SetActive(false);
            }

            if (StartOfRound.Instance.currentLevel.currentWeather == LevelWeatherType.Eclipsed)
            {
                eclipseSun.SetActive(true);
                eclipseVolume.SetActive(true);
                Destroy(normalSun);  // can't reenable this!
                Destroy(normalVolume);

                normalSun = eclipseSun;
                normalVolume = eclipseVolume;
                enforceOnlyVolume();
            }

            // NightFall Init
            if (EIWeatherManager.assignedWeather.ToLower().Contains("night"))
            {
                currentTime = 19.28f;
            }

            // Quantum Init
            if (EIWeatherManager.assignedWeather.ToLower().Contains("quantum"))
            {
                mainVolume.SetActive(false);
                QuantumVolume.SetActive(true);
                trueLightSource.color = new Color(1, 0.8f, 1);
            }

            // goofy, but this fix directly fixes fps on Easter Island
            try
            {
                var clipboards = UnityEngine.Object.FindObjectsOfType<ClipboardItem>();
                foreach (var c1 in clipboards)
                {
                    if (c1)
                    {
                        if (c1.gameObject)
                        {
                            c1.enabled = false;
                            var c2 = c1.gameObject.AddComponent<CUSTOM_ClipboardItem>();

                            c2.clipboardAnimator = c1.clipboardAnimator;
                            c2.currentPage = c1.currentPage;
                            c2.currentUseCooldown = c1.currentUseCooldown;
                            c2.customGrabTooltip = c1.customGrabTooltip;
                            c2.fallTime = c1.fallTime;
                            c2.floorYRot = c1.floorYRot;
                            c2.grabbable = c1.grabbable;
                            c2.grabbableToEnemies = c1.grabbableToEnemies;
                            c2.scrapValue = c1.scrapValue;
                            c2.startFallingPosition = c1.startFallingPosition;
                            c2.targetFloorPosition = c1.targetFloorPosition;
                            c2.thisAudio = c1.thisAudio;
                            c2.truckManual = c1.truckManual;
                            c2.turnPageSFX = c1.turnPageSFX;
                            c2.useCooldown = c1.useCooldown;
                            c2.isInShipRoom = c1.isInShipRoom;
                            c2.isInFactory = c1.isInFactory;
                            c2.isInElevator = c1.isInElevator;
                            c2.isHeld = c1.isHeld;
                            c2.isHeldByEnemy = c1.isHeldByEnemy;
                            c2.mainObjectRenderer = c1.mainObjectRenderer;
                            c2.propBody = c1.propBody;
                            c2.parentObject = c1.parentObject;
                            c2.radarIcon = c1.radarIcon;
                            c2.itemProperties = c1.itemProperties;
                            c2.isPocketed = c1.isPocketed;
                            c2.reachedFloorTarget = c1.reachedFloorTarget;
                            c2.scrapPersistedThroughRounds = c1.scrapPersistedThroughRounds;
                            c2.rotateObject = c1.rotateObject;
                            c2.wasOwnerLastFrame = c1.wasOwnerLastFrame;

                        }
                    }
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogError("LegendOfTheMoai Error: Clipboard patch failure. Might cause increased lag on EI.");
            }
        }
        
        // get rid of the ugliest fog known to man
        void enforceOnlyVolume()
        {
            var objs = UnityEngine.Object.FindObjectsOfType<Volume>();

            foreach(var obj in objs)
            {
                if(obj.name.ToLower().Contains("main") && obj.name.ToLower().Contains("volume"))
                {
                    obj.enabled = false;  // hopefully this doesn't screw anything up
                    volumesEnforced.Add(obj);
                }
            }
        }

        // Update is called once per frame
        void Update()
        {
            currentTime += Time.deltaTime * timeSpeed / timeDivider; // time generator

            if (currentTime >= 24)
            {
                currentTime = 0;
            }
            if (currentTime < 0)
            {
                currentTime = 23.99999f;
            }

            UpdateTimeText();
            UpdateLight();
            CheckShadowStatus();
            SkyStar();

            // Quantum Storm update
            if (EIWeatherManager.assignedWeather.ToLower().Contains("quantum"))
            {
                fluxTimer -= Time.deltaTime;
            }
            if (fluxTimer < 0)
            {
                fluxTimer = 10;
                QuantumBurst();
            }
        }

        // spawn quantum fluxes randomly around the map
        private async void QuantumBurst()
        {
            UnityEngine.Debug.Log("EASTER ISLAND: Quantum Burst Activated");

            int randDelay = new System.Random().Next(0, 2000);
            int fluxCount = new System.Random().Next(0, 10);
            for (int i = 0; i < fluxCount; i++)
            {
                var GO = Instantiate(fluxPrefab, selectQuantumPos(), fluxPrefab.transform.rotation);
                GO.GetComponent<NetworkObject>().Spawn();
                await Task.Delay(randDelay);
            }
        }

        private Vector3 selectQuantumPos()
        {
            System.Random r = new System.Random();
            GameObject[] nodes = RoundManager.Instance.outsideAINodes;
            Vector3 node = nodes[r.Next(0, nodes.Length)].transform.position;
            float xVar = (float)(r.NextDouble() * 10 - 5);
            float yVar = (float)(r.NextDouble() * 10 - 5);
            float zVar = (float)(r.NextDouble() * 10 - 5);

            Vector3 genPos = new Vector3(node.x + xVar, node.y + yVar, node.z + zVar);

            NavMeshHit hit;
            bool sample = NavMesh.SamplePosition(genPos, out hit, 30f, NavMesh.AllAreas);

            if (sample)
            {
                return hit.position;
            }
            else
            {
                return node;
            }
        }

        private void OnValidate()  //perform an action after a value changes in the Inspector
        {
            if (sunLightData == null & sunLightComponent == null || moonLightData == null & moonLightComponent == null)
                Awake();
            UpdateLight();
            CheckShadowStatus();
            SkyStar();
        }

        void UpdateTimeText()
        {

            currentTimeString = Mathf.Floor(currentTime).ToString("00") + ":" + Mathf.Floor(currentTime * 60 % 60).ToString("00") + ":" + Mathf.Floor(currentTime * 3600 % 60).ToString("00"); // conversion to a 24-hour system

        }

        void UpdateLight()
        {
            if(eclipseSun == normalSun) { return; }
            float sunRotation = currentTime / 24f * 360f; // the sun's rotation relative to time
            sunLight.transform.localRotation = Quaternion.Euler(sunLatitude - 90, sunLongitude, 0) * Quaternion.Euler(0, sunRotation, 0); // sun rotation with longitude and latitude
            moonLight.transform.localRotation = Quaternion.Euler(90 - moonLatitude, moonLongitude, 0) * Quaternion.Euler(0, sunRotation, 0); // moon rotation with longitude and latitude

            float normalizedTime = currentTime / 24f;
            float sunIntensityCurve = sunIntensityMultiplier.Evaluate(normalizedTime); // sun intensity curve
            float moonIntensityCurve = moonIntensityMultiplier.Evaluate(normalizedTime); // moon intensity curve
            float sunTemperatureMultiplier = sunTemperatureCurve.Evaluate(normalizedTime); // sun temperature
            float moonTemperatureMultiplier = moonTemperatureCurve.Evaluate(normalizedTime); // moon temperature

            if (sunLightData != null)
            {
                sunLightData.intensity = sunIntensityCurve * sunIntensity; // sun intensity considering the curve
            }

            if (moonLightData != null)
            {
                moonLightData.intensity = moonIntensityCurve * moonIntensity; // moon intensity considering the curve
            }

            if (sunLightComponent != null)
            {
                sunLightComponent.colorTemperature = sunTemperatureMultiplier * sunTemperature; // sun light temperature with temperature curve
            }

            if (moonLightComponent != null)
            {
                moonLightComponent.colorTemperature = moonTemperatureMultiplier * moonTemperature; // moon light temperature with temperature curve
            }
        }

        void CheckShadowStatus() // turning sun and moon shadows depending on the current time value
        {
            if (eclipseSun == normalSun) { return; }

            float currentSunRotation = currentTime;
            if (currentSunRotation >= 5.9f && currentSunRotation <= 18.1f)
            {
                sunLightData.EnableShadows(true);
                moonLightData.EnableShadows(false);
                isDay = true;
            }

            else
            {
                sunLightData.EnableShadows(false);
                moonLightData.EnableShadows(true);
                isDay = false;
            }

            if (currentSunRotation >= 5.7f && currentSunRotation <= 18.3f)
            {
                sunLight.gameObject.SetActive(true);
                sunActive = true;
            }

            else
            {
                sunLight.gameObject.SetActive(false);
                sunActive = false;
            }

            if (currentSunRotation >= moonActivationStart && currentSunRotation <= moonActivationEnd)
            {
                moonLight.gameObject.SetActive(false);
                moonActive = false;
            }

            else
            {
                moonLight.gameObject.SetActive(true);
                moonActive = true;
            }
        }

        void SkyStar()
        {
            volumeProfile.TryGet(out skySettings); //  volume profile with physicaly based sky
            skySettings.spaceEmissionMultiplier.value = starsCurve.Evaluate(currentTime / 24.0f) * starsIntensity; // intensity of the skybox with stars taking into account the curve

            skySettings.spaceRotation.value = (Quaternion.Euler(90 - polarStarLatitude, polarStarLongitude, 0) * Quaternion.Euler(0, currentTime / 24.0f * 360.0f, 60)).eulerAngles; // skybox rotation

            float horizonTintCurveValue = horizonTintCurve.Evaluate(currentTime / 24.0f); // changing the horizon tint over time taking into account the curve
            skySettings.horizonTint.value = new Color(horizonTintCurveValue, horizonTintCurveValue, horizonTintCurveValue); // horizon tint
            float zenithTintCurveValue = zenithTintCurve.Evaluate(currentTime / 24.0f); // changing the zenit tint over time taking into account the curve
            skySettings.zenithTint.value = new Color(zenithTintCurveValue, zenithTintCurveValue, zenithTintCurveValue); // zenit tint
        }
    }
}
