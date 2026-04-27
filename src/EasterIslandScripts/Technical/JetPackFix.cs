using GameNetcodeStuff;
using LethalLib.Modules;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace EasterIsland.src.EasterIslandScripts.Technical
{
    // Buggy zeekers jetpacks have been slandering my moon until the end of time
    // TIME TO END THIS
    class JetPackFix
    {
        public static List<PlayerControllerB> playersAdjusted = new List<PlayerControllerB>();  // related to their Box collider
        public static List<GameObject> beltBags = new List<GameObject>();

        public static void setupFix()
        {
            On.JetpackItem.ActivateJetpack += (On.JetpackItem.orig_ActivateJetpack orig, global::JetpackItem self) =>
            {
                orig.Invoke(self);


                Debug.Log("LegendOfTheMoai: ActivateJetpack");
                try
                {
                    // adjust people holding the jetpack
                    var player = self.playerHeldBy;

                    Debug.Log(player);
                    if (player)
                    {
                        var col = getBuggyCollider(player);
                        if (col)
                        {
                            col.enabled = false;
                            Debug.Log(col);
                            if (!playersAdjusted.Contains(player))
                            {
                                playersAdjusted.Add(player);
                            }
                        }
                    }

                    // belt bag logic
                    var inv = player.ItemSlots;
                    foreach(GrabbableObject GO in inv)
                    {
                        if(GO != null)
                        {
                            var name = GO.name.ToLower();
                            if (name.Contains("belt") && name.Contains("bag") && name.Contains("item"))
                            {
                                var trig = GO.gameObject.transform.Find("InteractTrigger");

                                if(trig)
                                {
                                    trig.gameObject.SetActive(false);
                                    beltBags.Add(trig.gameObject);
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }
            };

            On.TimeOfDay.CalculatePlanetTime += (On.TimeOfDay.orig_CalculatePlanetTime orig, global::TimeOfDay self, global::SelectableLevel level) =>
            {
                try
                {
                    if(playersAdjusted.Count > 0)
                    {
                        for (int i = 0; i < playersAdjusted.Count; i++)
                        {
                            var player = playersAdjusted[i];
                            if (!player.jetpackControls && player.gameObject)
                            {
                                var col = getBuggyCollider(player);
                                if (col)
                                {
                                    col.enabled = true;
                                    playersAdjusted.Remove(player);
                                }
                            }

                            if(!player)
                            {
                                playersAdjusted.RemoveAt(i);
                            }
                        }
                    }

                    if (beltBags.Count > 0)
                    {
                        for(int i = 0; i < beltBags.Count; i++)
                        {
                            var obj = beltBags[i];
                            if(!obj)
                            {
                                beltBags.RemoveAt(i);
                            }

                            if(obj)
                            {
                                var trig = obj.gameObject.transform.Find("InteractTrigger");

                                if (trig)
                                {
                                    trig.gameObject.SetActive(true);
                                    beltBags.Add(trig.gameObject);
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError(e);
                }

                return orig.Invoke(self, level);
            };
        }

        public static Collider getBuggyCollider(PlayerControllerB player)
        {
            if (!player.gameObject) { return null; }
            var misc = player.gameObject.transform.Find("Misc");
            if (misc)
            {
                var box = misc.transform.Find("Cube");
                if (box)
                {
                    return box.GetComponent<Collider>();
                }
            }

            return null;
        }
    }
}
