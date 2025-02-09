using System;
using ClassLibraryCommon;
using Newtonsoft.Json;
using NonVisuals.KeyEmulation;
using NonVisuals.Panels.Saitek;

namespace NonVisuals.BindingClasses.Key
{
    /// <summary>
    /// This is the base class for all the key binding classes.
    /// It is used when a user binds a physical switch to a virtual keypress. (key emulation)
    /// </summary>
    [Serializable]
    public abstract class KeyBindingBase
    {
        private KeyPress _keyPress;
        private bool _whenOnTurnedOn = true;
        [JsonIgnore] public bool HasSequence => _keyPress is { HasSequence: true };
        [JsonIgnore] public bool IsSequenced => false;

        public int GetHash()
        {
            unchecked
            {
                var result = _keyPress?.GetHash() ?? 0;
                return result;
            }
        }

        internal abstract void ImportSettings(string settings);

        [JsonProperty("OSKeyPress", Required = Required.Always)]
        public KeyPress OSKeyPress
        {
            get => _keyPress;
            set => _keyPress = value;
        }

        public abstract string ExportSettings();

        public bool WhenTurnedOn
        {
            get => _whenOnTurnedOn;
            set => _whenOnTurnedOn = value;
        }

        public Tuple<string, string> ParseSettingV1(string config)
        {
            string mode = "";
            string key;

            if (string.IsNullOrEmpty(config))
            {
                throw new ArgumentException("Import string empty. (KeyBinding)");
            }

            // RadioPanelKeyDialPos{LowerCOM1}\o/{0LowerFreqSwitch}\o/OSKeyPress{ThirtyTwoMilliSec,VK_A}
            // MultiPanelKnob{ALT}\o/{1LCD_WHEEL_DEC}\o/OSKeyPress{ThirtyTwoMilliSec,VK_A}
            // FarmingPanelKey{1KNOB_ENGINE_OFF}\o/OSKeyPress{HalfSecond,VK_I}
            // FarmingPanelKey{0SWITCHKEY_CLOSE_COWL}\o/OSKeyPress{INFORMATION=^key press sequence^[ThirtyTwoMilliSec,VK_A,ThirtyTwoMilliSec][ThirtyTwoMilliSec,VK_B,ThirtyTwoMilliSec]}
            var parameters = config.Split(new[] { SaitekConstants.SEPARATOR_SYMBOL }, StringSplitOptions.RemoveEmptyEntries);

            // Has additional setting which tells which position leftmost dial is in
            // but Radio Panel Emulator does not have (RadioPanelKey)
            if (config.Contains("MultiPanel") || config.Contains("RadioPanelKeyDialPos")) 
            {
                // RadioPanelKeyDialPos{LowerCOM1}
                // MultiPanelKnob{ALT}
                mode = Common.RemoveCurlyBrackets(parameters[0].Substring(parameters[0].IndexOf("{", StringComparison.InvariantCulture))).Trim();

                // {0LowerFreqSwitch}
                // {1LCD_WHEEL_DEC}
                WhenTurnedOn = Common.RemoveCurlyBrackets(parameters[1]).Substring(0, 1) == "1";
                key = Common.RemoveCurlyBrackets(parameters[1]).Substring(1).Trim();

                // OSKeyPress{ThirtyTwoMilliSec,VK_A}
                // OSKeyPress{ThirtyTwoMilliSec,VK_A}
                OSKeyPress = new KeyPress();
                OSKeyPress.ImportString(parameters[2]);
            }
            else
            {
                // FarmingPanelKey{1KNOB_ENGINE_OFF}
                var param = Common.RemoveCurlyBrackets(parameters[0].Substring(parameters[0].IndexOf("{", StringComparison.InvariantCulture))).Trim();

                // 1KNOB_ENGINE_OFF
                WhenTurnedOn = Common.RemoveCurlyBrackets(param).Substring(0, 1) == "1";
                key = Common.RemoveCurlyBrackets(param).Substring(1).Trim();

                // OSKeyPress{HalfSecond,VK_I}    
                OSKeyPress = new KeyPress();
                OSKeyPress.ImportString(parameters[1]);
            }

            return Tuple.Create(mode, key);
        }

        public string GetExportString(string header, string mode, string keyName)
        {
            if (OSKeyPress == null || OSKeyPress.IsEmpty())
            {
                return null;
            }

            var onStr = WhenTurnedOn ? "1" : "0";

            if (!string.IsNullOrEmpty(mode))
            {
                //Multipanel/Radio has one additional setting
                // RadioPanelKeyDialPos{LowerCOM1}\o/{0LowerFreqSwitch}\o/OSKeyPress{ThirtyTwoMilliSec,VK_A}
                return header + "{" + mode + "}" + SaitekConstants.SEPARATOR_SYMBOL + "{" + onStr + keyName + "}" + SaitekConstants.SEPARATOR_SYMBOL + OSKeyPress.ExportString();
            }

            // FarmingPanelKey{1KNOB_ENGINE_OFF}\o/OSKeyPress{HalfSecond,VK_I}
            return header + "{" + onStr + keyName + "}" + SaitekConstants.SEPARATOR_SYMBOL + OSKeyPress.ExportString();
        }
    }
}
