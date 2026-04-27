using EasterIsland;
using LethalCompanyInputUtils.Components;
using LethalCompanyInputUtils.Config;
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using static MonoMod.Cil.RuntimeILReferenceBag.FastDelegateInvokers;

public class LogScript : NetworkBehaviour
{
    public GrabbableObject logRef;
    public String[] messageTitles;
    public String[] messageDescs;
    private int currentLog = 0;

    // Update is called once per frame
    void Update()
    {
        var c = Plugin.controls;
        logRef.itemProperties.toolTips[0] = "Read Log Message: " + c.InspectLog.GetBindingDisplayString();


        if (c.InspectLog.triggered)
        {
            // prevent log from flipping if not held and if not local player
            if (logRef.playerHeldBy == null) { return; }
            if (logRef.playerHeldBy.NetworkObject.NetworkObjectId != RoundManager.Instance.playersManager.localPlayerController.NetworkObject.NetworkObjectId) { return; } 
            if(logRef.isPocketed) { return; }
            // display log
            HUDManager.Instance.DisplayTip(messageTitles[currentLog], messageDescs[currentLog], true);

            currentLog++;
            if (currentLog >= messageDescs.Length)
            {
                currentLog = 0;
            }
        }
    }
}
