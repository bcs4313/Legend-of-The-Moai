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

public class BookPageScript : NetworkBehaviour
{
    public Animator livro;
    public GrabbableObject bookRef;

    void Start()
    {
        livro = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        var c = Plugin.controls;
        bookRef.itemProperties.toolTips[0] = "Flip Forward Page: " + c.BookForward.GetBindingDisplayString();
        bookRef.itemProperties.toolTips[1] = "Flip Backward Page: " + c.BookBackward.GetBindingDisplayString();

        // prevent book from flipping if not held
        if (bookRef.playerHeldBy == null) { return; }
        if(bookRef.playerHeldBy.NetworkObject.NetworkObjectId != RoundManager.Instance.playersManager.localPlayerController.NetworkObject.NetworkObjectId) { return; }


        if (c.BookForward.triggered)
        {
            if (RoundManager.Instance.IsHost)
            {
                setLivroClientRpc("go_ahead", true);
            }
            else
            {
                setLivroServerRpc("go_ahead", true);
            }
        }
        else
        {
            if (RoundManager.Instance.IsHost)
            {
                setLivroClientRpc("go_ahead", false);
            }
            else
            {
                setLivroServerRpc("go_ahead", false);
            }
        }
        if (c.BookBackward.triggered)
        {
            if (RoundManager.Instance.IsHost)
            {
                setLivroClientRpc("go_back", true);
            }
            else
            {
                setLivroServerRpc("go_back", true);
            }
        }
        else
        {
            if (RoundManager.Instance.IsHost)
            {
                setLivroClientRpc("go_back", false);
            }
            else
            {
                setLivroServerRpc("go_back", false);
            }
        }

        if (bookRef.itemProperties.toolTips.Length < 2)
        {
            bookRef.itemProperties.toolTips = new string[] { "", "" }; ;
        }
    }

    [ClientRpc]
    public void setLivroClientRpc(String id, bool val)
    {
        livro.SetBool(id, val);
    }

    [ServerRpc(RequireOwnership = false)]
    public void setLivroServerRpc(String id, bool val)
    {
        setLivroClientRpc(id, val);
    }
}
