using GameNetcodeStuff;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.AI;
using UnityEngine;
using ExampleEnemy.src.MoaiRed;

namespace ExampleEnemy.src.MoaiNormal
{
    internal class MoaiRedNet
    {

        [Serializable]
        public class redMoaiSoundPkg
        {
            public ulong netId { get; set; }
            public string soundName { get; set; }

            public redMoaiSoundPkg(ulong _netId, string _soundName)
            {
                this.netId = _netId;
                this.soundName = _soundName;
            }
        }

        [Serializable]
        public class redMoaiSizePkg
        {
            public ulong netId { get; set; }
            public float size { get; set; }

            public float pitchAlter { get; set; }

            public redMoaiSizePkg(ulong _netId, float _size, float _pitchAlter)
            {
                this.netId = _netId;
                this.size = _size;
                this.pitchAlter = _pitchAlter;
            }
        }

        [Serializable]
        public class redMoaiAttachBodyPkg
        {
            public ulong netId { get; set; }
            public ulong humanNetId { get; set; }

            public redMoaiAttachBodyPkg(ulong _netId, ulong _humanNetId)
            {
                this.netId = _netId;
                this.humanNetId = _humanNetId;
            }
        }

        public static void setup()
        {
        }
    }
}
