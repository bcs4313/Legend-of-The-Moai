using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace EasterIsland.src.EasterIslandScripts.Technical
{
    public class CUSTOM_ClipboardItem : GrabbableObject
    {
        // Token: 0x0600110A RID: 4362 RVA: 0x000A9590 File Offset: 0x000A7790
        public override void Update()
        {
            base.Update();
            if (!this.parentedToTruck)
            {
                if (StartOfRound.Instance.inShipPhase)
                {
                    this.parentedToTruck = true;
                    return;
                }
            }
        }

        // Token: 0x0600110B RID: 4363 RVA: 0x000A961D File Offset: 0x000A781D
        public override void PocketItem()
        {
            if (base.IsOwner && this.playerHeldBy != null)
            {
                this.playerHeldBy.equippedUsableItemQE = false;
                this.isBeingUsed = false;
            }
            base.PocketItem();
        }

        // Token: 0x0600110C RID: 4364 RVA: 0x000A9650 File Offset: 0x000A7850
        public override void ItemInteractLeftRight(bool right)
        {
            int num = this.currentPage;
            base.RequireCooldown();
            if (right)
            {
                this.currentPage = Mathf.Clamp(this.currentPage + 1, 1, 4);
            }
            else
            {
                this.currentPage = Mathf.Clamp(this.currentPage - 1, 1, 4);
            }
            if (this.currentPage != num)
            {
                RoundManager.PlayRandomClip(this.thisAudio, this.turnPageSFX, true, 1f, 0, 1000);
            }
            this.clipboardAnimator.SetInteger("page", this.currentPage);
        }

        // Token: 0x0600110D RID: 4365 RVA: 0x000A96D7 File Offset: 0x000A78D7
        public override void DiscardItem()
        {
            if (this.playerHeldBy != null)
            {
                this.playerHeldBy.equippedUsableItemQE = false;
            }
            this.isBeingUsed = false;
            base.DiscardItem();
        }

        // Token: 0x0600110E RID: 4366 RVA: 0x000A9700 File Offset: 0x000A7900
        public override void EquipItem()
        {
            base.EquipItem();
            this.playerHeldBy.equippedUsableItemQE = true;
            if (base.IsOwner)
            {
                HUDManager.Instance.DisplayTip("To read the manual:", "Press Z to inspect closely. Press Q and E to flip the pages.", false, true, "LCTip_UseManual");
            }
        }

        // Token: 0x06001110 RID: 4368 RVA: 0x000A9748 File Offset: 0x000A7948
        protected override void __initializeVariables()
        {
            base.__initializeVariables();
        }

        // Token: 0x040010A6 RID: 4262
        public bool truckManual;

        // Token: 0x040010A7 RID: 4263
        private bool parentedToTruck;

        // Token: 0x040010A8 RID: 4264
        public int currentPage = 1;

        // Token: 0x040010A9 RID: 4265
        public Animator clipboardAnimator;

        // Token: 0x040010AA RID: 4266
        public AudioClip[] turnPageSFX;

        // Token: 0x040010AB RID: 4267
        public AudioSource thisAudio;
    }
}
