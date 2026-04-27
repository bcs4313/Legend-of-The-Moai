using LethalCompanyInputUtils.Api;
using LethalCompanyInputUtils.BindingPathEnums;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.InputSystem;

namespace EasterIsland.src
{
    internal class InputMap : LcInputActions
    {
        // reading keys
        [InputAction(MouseControl.LeftButton, GamepadControl = GamepadControl.RightShoulder, Name = "Moai book: Flip Forward Pages")]
        public InputAction BookForward { get; set; }

        [InputAction(MouseControl.RightButton, GamepadControl = GamepadControl.LeftShoulder, Name = "Moai book: Flip Back Pages")]
        public InputAction BookBackward { get; set; }

        [InputAction(MouseControl.RightButton, GamepadControl = GamepadControl.RightShoulder, Name = "Gold Moai Summon George")]
        public InputAction summonGeorge { get; set; }

        // reading keys
        [InputAction(MouseControl.RightButton, GamepadControl = GamepadControl.LeftTrigger, Name = "Gum Gum: Inspect Gum")]
        public InputAction InspectGum { get; set; }

        // reading keys
        [InputAction(MouseControl.LeftButton, GamepadControl = GamepadControl.LeftShoulder, Name = "Moai Log: Read Log")]
        public InputAction InspectLog { get; set; }

        // golden moai pitch keys
        [InputAction(KeyboardControl.Num1, Name = "Gold Moai Belch Pitch 1")]
        public InputAction K1 { get; set; }

        [InputAction(KeyboardControl.Num2, Name = "Gold Moai Belch Pitch 2")]
        public InputAction K2 { get; set; }

        [InputAction(KeyboardControl.Num3, Name = "Gold Moai Belch Pitch 3")]
        public InputAction K3 { get; set; }

        [InputAction(KeyboardControl.Num4, Name = "Gold Moai Belch Pitch 4")]
        public InputAction K4 { get; set; }

        [InputAction(KeyboardControl.Num5, Name = "Gold Moai Belch Pitch 5")]
        public InputAction K5 { get; set; }

        [InputAction(KeyboardControl.Num6, Name = "Gold Moai Belch Pitch 6")]
        public InputAction K6 { get; set; }

        [InputAction(KeyboardControl.Num7, Name = "Gold Moai Belch Pitch 7")]
        public InputAction K7 { get; set; }

        [InputAction(KeyboardControl.Num8, Name = "Gold Moai Belch Pitch 8")]
        public InputAction K8 { get; set; }

        [InputAction(KeyboardControl.Num9, Name = "Gold Moai Belch Pitch 9")]
        public InputAction K9 { get; set; }

        [InputAction(KeyboardControl.Num0, Name = "Gold Moai Belch Pitch 10")]
        public InputAction K0 { get; set; }

        /*
        [InputAction(KeyboardControl.B, Name = "B")]
        public InputAction BOLT1 { get; set; }

        [InputAction(KeyboardControl.O, Name = "BB")]
        public InputAction BOLT2 { get; set; }

        [InputAction(KeyboardControl.L, Name = "BBB")]
        public InputAction BOLT3 { get; set; }

        [InputAction(KeyboardControl.T, Name = "BBBB")]
        public InputAction BOLT4 { get; set; }

        [InputAction(KeyboardControl.Num0, Name = "BBBBBB")]
        public InputAction BOLT5 { get; set; }


        [InputAction(KeyboardControl.K, Name = "I")]
        public InputAction DEATH1 { get; set; }

        [InputAction(KeyboardControl.I, Name = "II")]
        public InputAction DEATH2 { get; set; }

        [InputAction(KeyboardControl.L, Name = "III")]
        public InputAction DEATH3 { get; set; }

        [InputAction(KeyboardControl.Num0, Name = "IIIII")]
        public InputAction DEATH4 { get; set; }
        */
    }
}
