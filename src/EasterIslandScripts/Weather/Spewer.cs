
using EasterIsland;
using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;
using UnityEngine.Rendering.VirtualTexturing;

public class Spewer : NetworkBehaviour
{
    public int eruptHour;
    public int currentHour = -2;
    public bool noErupt;

    public int hoursForce = 0;

    // Start is called before the first frame update
    void Start()
    {
        Spewer[] spewers = UnityEngine.Object.FindObjectsOfType<Spewer>();
        Plugin.destroyOnLoad.Add(this.gameObject);

        try
        {
            // handler for dropship spawns
            ItemDropship dropShip = UnityEngine.Object.FindObjectsOfType<ItemDropship>()[0];

            var g_obj = new GameObject();
            LineRenderer r = g_obj.AddComponent<LineRenderer>();
            g_obj.SetActive(false);


            LineRenderer[] ropeFix = [r];
            dropShip.ropes = ropeFix;
        }
        catch (Exception e)
        {
            Debug.LogError(e);
        }

        foreach (Spewer s in spewers)
        {
            if(s != this)
            {
                Destroy(s);
            }
        }


        var g = transform.Find("meteors").gameObject;
        g.GetComponent<ParticleSystem>().Stop();
        if (RoundManager.Instance.IsHost)
        {
            // select eruption time
            if (UnityEngine.Random.Range(0.0f, 1.0f) < 0.50)  // 50% chance for no eruption at all
            {
                eruptHour = 2 + 999;
                noErupt = true;
            }
            else
            {
                eruptHour = 2 + UnityEngine.Random.Range(2, 17);  // 2 is the starting hour
                noErupt = false;
            }

            fogTick();  // set fog color
        }
    }

    void fogTick()
    {
        if (RoundManager.Instance.IsHost)
        {
            int caseHour = eruptHour - getHour();

            if (noErupt || caseHour < 0)
            {  // light blue
                setFogColorClientRpc(new Color(0f / 255f, 255f / 255f, 206f / 255f));
            }
            else
            {
                switch (caseHour)
                {
                    case 0:  // orange
                        setFogColorClientRpc(new Color(255f / 255f, 141f / 255f, 0f / 255f));
                        break;
                    case 1:  // orange
                        setFogColorClientRpc(new Color(255f / 255f, 141f / 255f, 0f / 255f));
                        break;
                    case 2:  // red 
                        setFogColorClientRpc(new Color(255f / 255f, 0f / 255f, 0f / 255f));
                        break;
                    case 3:  // green
                        setFogColorClientRpc(new Color(0f / 255f, 255f / 255f, 0f / 255f));
                        break;
                    case 4:  // yellow
                        setFogColorClientRpc(new Color(239f / 255f, 255f / 255f, 0f / 255f));
                        break;
                    default: // purple
                        setFogColorClientRpc(new Color(135f / 255f, 0f / 255f, 255f / 255f));
                        break;
                }
            }
        }
    }

    int getHour()
    {
        return TimeOfDay.Instance.hour;
    }

    // Update is called once per frame
    void Update()
    {
        if (RoundManager.Instance.IsHost)
        {
            // called whenever the hour changes
            if (currentHour != getHour())
            {
                if (hoursForce > 0)
                {
                    hoursForce--;
                }

                currentHour = getHour();
                fogTick();
                var g = transform.Find("meteors").gameObject;
                if (getHour() == eruptHour)
                {
                    var randomSeed = (uint)UnityEngine.Random.Range(0, 255000);
                    playParticleSystemClientRpc(randomSeed);

                }
                else
                {
                    stopParticleSystemClientRpc();
                }
            }

            // just forces the eruption! yay
            if(hoursForce > 0)
            {
                var randomSeed = (uint)UnityEngine.Random.Range(0, 255000);
                playParticleSystemClientRpc(randomSeed);
            }
        }
    }

    [ClientRpc]
    void setFogColorClientRpc(Color color)
    {
        Debug.Log("MOAI: setFogColorClientRpc Called");
        Transform fogParent = GameObject.Find("VolcanoMeters").transform;

        // Loop through each child of the parent GameObject
        foreach (Transform child in fogParent)
        {
            // Do something with each child
            if (child.name.Contains("Fog"))
            {
                LocalVolumetricFog fog = child.GetComponent<LocalVolumetricFog>();

                fog.parameters.albedo = color;
            }
        }
    }

    [ClientRpc]
    void playParticleSystemClientRpc(uint seed)
    {
        Debug.Log("MOAI: playParticleSystemClientRpc Called");
        var g = transform.Find("meteors").gameObject;
        var system = g.GetComponent<ParticleSystem>();
        system.randomSeed = seed;
        system.GetComponent<ParticleSystem>().Play();
    }

    [ClientRpc]
    void stopParticleSystemClientRpc()
    {
        Debug.Log("MOAI: stopParticleSystemClientRpc Called");
        var g = transform.Find("meteors").gameObject;
        var system = g.GetComponent<ParticleSystem>();
        system.Stop();
    }
}
