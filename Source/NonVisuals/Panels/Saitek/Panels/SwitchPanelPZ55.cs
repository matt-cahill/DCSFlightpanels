﻿using NonVisuals.BindingClasses.BIP;
using NonVisuals.BindingClasses.DCSBIOSBindings;
using NonVisuals.BindingClasses.Key;
using NonVisuals.BindingClasses.OSCommand;
using NonVisuals.KeyEmulation;

namespace NonVisuals.Panels.Saitek.Panels
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;

    using ClassLibraryCommon;

    using DCS_BIOS;
    using DCS_BIOS.EventArgs;

    using MEF;
    using EventArgs;
    using Plugin;
    using Switches;
    using NonVisuals;
    using Saitek;
    using HID;

    public enum SwitchPanelPZ55LEDPosition : byte
    {
        UP = 0x0,
        LEFT = 0x1,
        RIGHT = 0x2
    }

    /// <summary>
    /// The implementation class for the Logitech Switch Panel (PZ55)
    /// See bottom of file for communication information.
    /// </summary>
    public class SwitchPanelPZ55 : SaitekPanel
    {
        private readonly List<DcsOutputAndColorBindingPZ55> _listColorOutputBinding = new();
        private readonly object _dcsBiosDataReceivedLock = new();
        private HashSet<DCSBIOSActionBindingPZ55> _dcsBiosBindings = new();
        private HashSet<KeyBindingPZ55> _keyBindings = new();
        private List<OSCommandBindingPZ55> _operatingSystemCommandBindings = new();
        private HashSet<BIPLinkPZ55> _bipLinks = new();
        private SwitchPanelPZ55LEDs _ledUpperColor = SwitchPanelPZ55LEDs.ALL_DARK;
        private SwitchPanelPZ55LEDs _ledLeftColor = SwitchPanelPZ55LEDs.ALL_DARK;
        private SwitchPanelPZ55LEDs _ledRightColor = SwitchPanelPZ55LEDs.ALL_DARK;
        private bool _manualLandingGearLeds;
        private PanelLEDColor _manualLandingGearLedsColorDown = PanelLEDColor.GREEN;
        private PanelLEDColor _manualLandingGearLedsColorUp = PanelLEDColor.DARK;
        private PanelLEDColor _manualLandingGearLedsColorTrans = PanelLEDColor.RED;
        private int _manualLandingGearTransTimeSeconds = 5;
        private Thread _manualLandingGearThread;

        private enum ManualGearsStatus
        {
            Down,
            Up,
            Trans
        }

        public SwitchPanelPZ55(HIDSkeleton hidSkeleton) : base(GamingPanelEnum.PZ55SwitchPanel, hidSkeleton)
        {
            if (hidSkeleton.GamingPanelType != GamingPanelEnum.PZ55SwitchPanel)
            {
                throw new ArgumentException($"GamingPanelType {GamingPanelEnum.PZ55SwitchPanel} expected");
            }

            // Fixed values
            VendorId = 0x6A3;
            ProductId = 0xD67;
            CreateSwitchKeys();
            Startup();
            BIOSEventHandler.AttachDataListener(this);
        }

        private bool _disposed;
        // Protected implementation of Dispose pattern.
        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    BIOSEventHandler.DetachDataListener(this);
                }

                _disposed = true;
            }

            // Call base class implementation.
            base.Dispose(disposing);
        }

        public override sealed void Startup()
        {
            try
            {
                StartListeningForHidPanelChanges();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        public override void ImportSettings(GenericPanelBinding genericPanelBinding)
        {
            ClearSettings();

            BindingHash = genericPanelBinding.BindingHash;

            var settings = genericPanelBinding.Settings;

            foreach (var setting in settings)
            {
                if (!setting.StartsWith("#") && setting.Length > 2)
                {

                    if (setting.StartsWith("SwitchPanelKey"))
                    {
                        var keyBinding = new KeyBindingPZ55();
                        keyBinding.ImportSettings(setting);
                        _keyBindings.Add(keyBinding);
                    }
                    else if (setting.StartsWith("SwitchPanelOSPZ55"))
                    {
                        var operatingSystemCommand = new OSCommandBindingPZ55();
                        operatingSystemCommand.ImportSettings(setting);
                        _operatingSystemCommandBindings.Add(operatingSystemCommand);
                    }
                    else if (setting.StartsWith("SwitchPanelLed"))
                    {
                        var colorOutput = new DcsOutputAndColorBindingPZ55();
                        colorOutput.ImportSettings(setting);
                        _listColorOutputBinding.Add(colorOutput);
                    }
                    else if (setting.StartsWith("SwitchPanelDCSBIOSControl"))
                    {
                        var dcsBIOSBindingPZ55 = new DCSBIOSActionBindingPZ55();
                        dcsBIOSBindingPZ55.ImportSettings(setting);
                        _dcsBiosBindings.Add(dcsBIOSBindingPZ55);
                    }
                    else if (setting.StartsWith("SwitchPanelBIPLink"))
                    {
                        var bipLinkPZ55 = new BIPLinkPZ55();
                        bipLinkPZ55.ImportSettings(setting);
                        _bipLinks.Add(bipLinkPZ55);
                    }
                    else if (setting.StartsWith("ManualLandingGearLEDs{"))
                    {
                        _manualLandingGearLeds = setting.Contains("True");
                    }
                    else if (setting.StartsWith("ManualLandingGearLedsColorDown{"))
                    {
                        _manualLandingGearLedsColorDown = GetSettingPanelLEDColor(setting);
                    }
                    else if (setting.StartsWith("ManualLandingGearLedsColorUp{"))
                    {
                        _manualLandingGearLedsColorUp = GetSettingPanelLEDColor(setting);
                    }
                    else if (setting.StartsWith("ManualLandingGearLedsColorTrans{"))
                    {
                        _manualLandingGearLedsColorTrans = GetSettingPanelLEDColor(setting);
                    }
                    else if (setting.StartsWith("ManualLandingGearTransTimeSeconds{"))
                    {
                        _manualLandingGearTransTimeSeconds = Convert.ToInt16(GetValueFromSetting(setting));
                    }
                }
            }

            AppEventHandler.SettingsApplied(this, HIDSkeletonBase.HIDInstance, TypeOfPanel);
            _keyBindings = KeyBindingPZ55.SetNegators(_keyBindings);
        }

        private static string GetValueFromSetting(string setting)
        {
            int pos = setting.IndexOf('{');
            return setting.Substring(pos + 1, setting.LastIndexOf('}') - pos - 1);
        }

        private static PanelLEDColor GetSettingPanelLEDColor(string setting)
        {
            return (PanelLEDColor)Enum.Parse(typeof(PanelLEDColor), GetValueFromSetting(setting));
        }

        public override List<string> ExportSettings()
        {
            if (Closed)
            {
                return null;
            }

            var result = new List<string>();

            foreach (var keyBinding in _keyBindings)
            {
                if (keyBinding.OSKeyPress != null)
                {
                    result.Add(keyBinding.ExportSettings());
                }
            }

            foreach (var operatingSystemCommand in _operatingSystemCommandBindings)
            {
                if (!operatingSystemCommand.OSCommandObject.IsEmpty)
                {
                    result.Add(operatingSystemCommand.ExportSettings());
                }
            }

            foreach (var dcsBiosBinding in _dcsBiosBindings)
            {
                if (dcsBiosBinding.DCSBIOSInputs.Count > 0)
                {
                    result.Add(dcsBiosBinding.ExportSettings());
                }
            }

            foreach (var bipLink in _bipLinks)
            {
                if (bipLink.BIPLights.Count > 0)
                {
                    result.Add(bipLink.ExportSettings());
                }
            }

            foreach (var colorOutputBinding in _listColorOutputBinding)
            {
                result.Add(colorOutputBinding.ExportSettings());
            }

            result.Add("ManualLandingGearLEDs{" + _manualLandingGearLeds + "}");
            result.Add("ManualLandingGearLedsColorUp{" + _manualLandingGearLedsColorUp + "}");
            result.Add("ManualLandingGearLedsColorDown{" + _manualLandingGearLedsColorDown + "}");
            result.Add("ManualLandingGearLedsColorTrans{" + _manualLandingGearLedsColorTrans + "}");
            result.Add("ManualLandingGearTransTimeSeconds{" + _manualLandingGearTransTimeSeconds + "}");
            return result;
        }

        public override void SavePanelSettings(object sender, ProfileHandlerEventArgs e)
        {
            e.ProfileHandlerCaller.RegisterPanelBinding(this, ExportSettings());
        }

        public override void SavePanelSettingsJSON(object sender, ProfileHandlerEventArgs e) { }

        public override void DcsBiosDataReceived(object sender, DCSBIOSDataEventArgs e)
        {

            lock (_dcsBiosDataReceivedLock)
            {
                UpdateCounter(e.Address, e.Data);
                CheckDcsDataForColorChangeHook(e.Address, e.Data);
            }

        }

        public override void Identify()
        {
            try
            {
                var thread = new Thread(ShowIdentifyingValue);
                thread.Start();
            }
            catch (Exception)
            {
                // ignored
            }
        }

        private void ShowIdentifyingValue()
        {
            try
            {
                var spins = 40;
                var random = new Random();
                var ledPositionArray = Enum.GetValues(typeof(SwitchPanelPZ55LEDPosition));
                var panelColorArray = Enum.GetValues(typeof(PanelLEDColor));

                while (spins > 0)
                {
                    var position = (SwitchPanelPZ55LEDPosition)ledPositionArray.GetValue(random.Next(ledPositionArray.Length));
                    var color = (PanelLEDColor)panelColorArray.GetValue(random.Next(panelColorArray.Length));

                    SetLandingGearLED(position, color);

                    Thread.Sleep(50);
                    spins--;
                }

                SetLandingGearLED(SwitchPanelPZ55LEDPosition.UP, PanelLEDColor.DARK);
                SetLandingGearLED(SwitchPanelPZ55LEDPosition.LEFT, PanelLEDColor.DARK);
                SetLandingGearLED(SwitchPanelPZ55LEDPosition.RIGHT, PanelLEDColor.DARK);
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public override void ClearSettings(bool setIsDirty = false)
        {
            _keyBindings.Clear();
            _operatingSystemCommandBindings.Clear();
            _listColorOutputBinding.Clear();
            _dcsBiosBindings.Clear();
            _bipLinks.Clear();

            if (setIsDirty)
            {
                SetIsDirty();
            }
        }

        public HashSet<KeyBindingPZ55> KeyBindingsHashSet
        {
            get => _keyBindings;
            set => _keyBindings = value;
        }

        public HashSet<BIPLinkPZ55> BIPLinkHashSet
        {
            get => _bipLinks;
            set => _bipLinks = value;
        }

        public List<OSCommandBindingPZ55> OSCommandList
        {
            get => _operatingSystemCommandBindings;
            set => _operatingSystemCommandBindings = value;
        }

        private PanelLEDColor GetManualGearsColorForStatus(ManualGearsStatus status)
        {
            switch (status)
            {
                case ManualGearsStatus.Down:
                    return _manualLandingGearLedsColorDown;
                case ManualGearsStatus.Up:
                    return _manualLandingGearLedsColorUp;
                case ManualGearsStatus.Trans:
                    return _manualLandingGearLedsColorTrans;
                default:
                    throw new ArgumentOutOfRangeException(nameof(status));
            }
        }

        private volatile bool _shutdownThread;
        private void SetLandingGearLedsManually(PanelLEDColor panelLEDColor)
        {
            try
            {
                var random = new Random();
                var upSet = false;
                var rightSet = false;
                var leftSet = false;

                int transitionMs = _manualLandingGearTransTimeSeconds * 1000;
                int randomTransitionDeviation = 3000;
                var delayUp = random.Next(transitionMs, transitionMs + randomTransitionDeviation);
                var delayRight = random.Next(transitionMs, transitionMs + randomTransitionDeviation);
                var delayLeft = random.Next(transitionMs, transitionMs + randomTransitionDeviation);
                var millisecsStart = DateTime.Now.Ticks / 10000;

                // Corrected the 'Manual LEDS' operation.
                // Now when the gear knob selection is changed, just like a real aircraft
                // the lights go to their 'Transit' state showing RED.
                // Then afterwards they change to their final colour (GREEN = DOWN, DARK = UP)
                //
                // Update 2021-11: Now the user can select which color to display for the 3 phases.
                // By default it's configured as the description above.
                SetLandingGearLED(SwitchPanelPZ55LEDPosition.UP, GetManualGearsColorForStatus(ManualGearsStatus.Trans));
                SetLandingGearLED(SwitchPanelPZ55LEDPosition.RIGHT, GetManualGearsColorForStatus(ManualGearsStatus.Trans));
                SetLandingGearLED(SwitchPanelPZ55LEDPosition.LEFT, GetManualGearsColorForStatus(ManualGearsStatus.Trans));

                while (!_shutdownThread)
                {
                    var millisecsNow = DateTime.Now.Ticks / 10000;
                    Debug.Print("millisecsNow - millisecsStart > delayUp " + (millisecsNow - millisecsStart) + " " + delayUp);
                    Debug.Print("millisecsNow - millisecsStart > delayRight " + (millisecsNow - millisecsStart) + " " + delayRight);
                    Debug.Print("millisecsNow - millisecsStart > delayLeft " + (millisecsNow - millisecsStart) + " " + delayLeft);
                    if (millisecsNow - millisecsStart > delayUp && !upSet)
                    {
                        SetLandingGearLED(SwitchPanelPZ55LEDPosition.UP, panelLEDColor);
                        upSet = true;
                    }

                    if (millisecsNow - millisecsStart > delayRight && !rightSet)
                    {
                        SetLandingGearLED(SwitchPanelPZ55LEDPosition.RIGHT, panelLEDColor);
                        rightSet = true;
                    }

                    if (millisecsNow - millisecsStart > delayLeft && !leftSet)
                    {
                        SetLandingGearLED(SwitchPanelPZ55LEDPosition.LEFT, panelLEDColor);
                        leftSet = true;
                    }

                    if (leftSet && upSet && rightSet)
                    {
                        break;
                    }

                    if (_shutdownThread)
                    {
                        break;
                    }
                    Thread.Sleep(10);
                }
            }
            catch (ThreadAbortException)
            {
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "SetLandingGearLedsManually");
                throw;
            }
        }

        private void PZ55SwitchChanged(bool isFirstReport, IEnumerable<object> hashSet)
        {
            if (!ForwardPanelEvent)
            {
                return;
            }

            foreach (var switchPanelKeyObject in hashSet)
            {
                // Looks which switches has been switched and sees whether any key emulation has been tied to them.
                var switchPanelKey = (SwitchPanelKey)switchPanelKeyObject;
                
                var found = false;

                // Look if leds are manually operated
                if (!isFirstReport && _manualLandingGearLeds)
                {
                    if (switchPanelKey.SwitchPanelPZ55Key == SwitchPanelPZ55Keys.LEVER_GEAR_UP && switchPanelKey.IsOn)
                    {
                        // Changed Lights to go DARK when gear level is selected to UP, instead of RED.
                        _shutdownThread = true;
                        Thread.Sleep(Constants.ThreadShutDownWaitTime);
                        _shutdownThread = false;
                        _manualLandingGearThread = new Thread(() => SetLandingGearLedsManually(GetManualGearsColorForStatus(ManualGearsStatus.Up)));
                        _manualLandingGearThread.Start();
                    }
                    else if (switchPanelKey.SwitchPanelPZ55Key == SwitchPanelPZ55Keys.LEVER_GEAR_DOWN && switchPanelKey.IsOn)
                    {
                        _shutdownThread = true;
                        Thread.Sleep(Constants.ThreadShutDownWaitTime);
                        _shutdownThread = false;
                        _manualLandingGearThread = new Thread(() => SetLandingGearLedsManually(GetManualGearsColorForStatus(ManualGearsStatus.Down)));
                        _manualLandingGearThread.Start();
                    }
                }

                var keyBindingFound = false;
                foreach (var keyBinding in _keyBindings)
                {
                    if (!isFirstReport && keyBinding.OSKeyPress != null && keyBinding.SwitchPanelPZ55Key == switchPanelKey.SwitchPanelPZ55Key && keyBinding.WhenTurnedOn == switchPanelKey.IsOn)
                    {
                        keyBindingFound = true;
                        if (!PluginManager.DisableKeyboardAPI)
                        {
                            keyBinding.OSKeyPress.Execute(new CancellationToken());
                        }

                        if (PluginManager.PlugSupportActivated && PluginManager.HasPlugin())
                        {
                            PluginManager.DoEvent(
                                DCSAircraft.SelectedAircraft.Description,
                                HIDInstance,
                                PluginGamingPanelEnum.PZ55SwitchPanel,
                                (int)switchPanelKey.SwitchPanelPZ55Key,
                                switchPanelKey.IsOn,
                                keyBinding.OSKeyPress.KeyPressSequence);
                        }

                        found = true;
                        break;
                    }
                }

                if (!isFirstReport && !keyBindingFound && PluginManager.PlugSupportActivated && PluginManager.HasPlugin())
                {
                    PluginManager.DoEvent(
                        DCSAircraft.SelectedAircraft.Description,
                        HIDInstance,
                        PluginGamingPanelEnum.PZ55SwitchPanel,
                        (int)switchPanelKey.SwitchPanelPZ55Key,
                        switchPanelKey.IsOn,
                        null);
                }

                foreach (var operatingSystemCommand in _operatingSystemCommandBindings)
                {
                    if (!isFirstReport && operatingSystemCommand.OSCommandObject != null && operatingSystemCommand.SwitchPanelPZ55Key == switchPanelKey.SwitchPanelPZ55Key && operatingSystemCommand.WhenTurnedOn == switchPanelKey.IsOn)
                    {
                        operatingSystemCommand.OSCommandObject.ExecuteCommand(new CancellationToken());
                        found = true;
                        break;
                    }
                }

                foreach (var bipLinkPZ55 in _bipLinks)
                {
                    if (!isFirstReport && bipLinkPZ55.BIPLights.Count > 0 && bipLinkPZ55.SwitchPanelPZ55Key == switchPanelKey.SwitchPanelPZ55Key && bipLinkPZ55.WhenTurnedOn == switchPanelKey.IsOn)
                    {
                        bipLinkPZ55.Execute();
                        break;
                    }
                }

                if (!isFirstReport && !found)
                {
                    foreach (var dcsBiosBinding in _dcsBiosBindings)
                    {
                        if (dcsBiosBinding.DCSBIOSInputs.Count > 0 && dcsBiosBinding.SwitchPanelPZ55Key == switchPanelKey.SwitchPanelPZ55Key && dcsBiosBinding.WhenTurnedOn == switchPanelKey.IsOn)
                        {
                            dcsBiosBinding.SendDCSBIOSCommands(new CancellationToken());
                            break;
                        }
                    }
                }
            }
        }

        internal void CheckDcsDataForColorChangeHook(uint address, uint data)
        {
            try
            {


                foreach (var cavb in _listColorOutputBinding)
                {
                    if (cavb.DCSBiosOutputLED.UIntConditionIsMet(address, data))
                    {
                        /*
                         * If user tests cockpit lights (especially A-10C and handle light) and triggers (forces) a light change that light
                         * will stay on unless there is a overriding light for example landing gear down.
                         * Landing gear down light is sent regularly but it is processed only if value has changed. This is not the case here 
                         * as it is GREEN and should nevertheless override the RED light.
                         * Will this be efficient?
                         * https://sourceforge.net/p/flightpanels/tickets/13/
                         */
                        var color = cavb.LEDColor;
                        var position = (SwitchPanelPZ55LEDPosition)cavb.SaitekLEDPosition.Position;
                        var color2 = GetSwitchPanelPZ55LEDColor(position, color);

                        if (position == SwitchPanelPZ55LEDPosition.UP && color2 != _ledUpperColor)
                        {
                            SetLandingGearLED(cavb);
                        }
                        else if (position == SwitchPanelPZ55LEDPosition.LEFT && color2 != _ledLeftColor)
                        {
                            SetLandingGearLED(cavb);
                        }
                        else if (position == SwitchPanelPZ55LEDPosition.RIGHT && color2 != _ledRightColor)
                        {
                            SetLandingGearLED(cavb);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "CheckDcsDataForColorChangeHook(uint address, uint data)");
                throw;
            }
        }

        public string GetKeyPressForLoggingPurposes(SwitchPanelKey switchPanelKey)
        {
            var result = string.Empty;
            foreach (var keyBinding in _keyBindings)
            {
                if (keyBinding.OSKeyPress != null && keyBinding.SwitchPanelPZ55Key == switchPanelKey.SwitchPanelPZ55Key && keyBinding.WhenTurnedOn == switchPanelKey.IsOn)
                {
                    result = keyBinding.OSKeyPress.GetNonFunctioningVirtualKeyCodesAsString();
                }
            }

            return result;
        }

        public override void AddOrUpdateKeyStrokeBinding(PanelSwitchOnOff panelSwitchOnOff, string keyPress, KeyPressLength keyPressLength)
        {
            var pz55SwitchOnOff = (PZ55SwitchOnOff)panelSwitchOnOff;

            if (string.IsNullOrEmpty(keyPress))
            {
                RemoveSwitchFromList(ControlList.KEYS, pz55SwitchOnOff);
                SetIsDirty();
                return;
            }

            var found = false;
            foreach (var keyBinding in _keyBindings)
            {
                if (keyBinding.SwitchPanelPZ55Key == pz55SwitchOnOff.Switch && keyBinding.WhenTurnedOn == pz55SwitchOnOff.ButtonState)
                {
                    if (string.IsNullOrEmpty(keyPress))
                    {
                        keyBinding.OSKeyPress = null;
                    }
                    else
                    {
                        keyBinding.OSKeyPress = new KeyPress(keyPress, keyPressLength);
                    }

                    found = true;
                }
            }

            if (!found && !string.IsNullOrEmpty(keyPress))
            {
                var keyBinding = new KeyBindingPZ55
                {
                    SwitchPanelPZ55Key = pz55SwitchOnOff.Switch,
                    OSKeyPress = new KeyPress(keyPress, keyPressLength),
                    WhenTurnedOn = pz55SwitchOnOff.ButtonState
                };
                _keyBindings.Add(keyBinding);
            }

            _keyBindings = KeyBindingPZ55.SetNegators(_keyBindings);
            SetIsDirty();
        }

        public override void AddOrUpdateSequencedKeyBinding(PanelSwitchOnOff panelSwitchOnOff, string description, SortedList<int, IKeyPressInfo> keySequence)
        {
            var pz55SwitchOnOff = (PZ55SwitchOnOff)panelSwitchOnOff;
            if (keySequence.Count == 0)
            {
                RemoveSwitchFromList(ControlList.KEYS, pz55SwitchOnOff);
                SetIsDirty();
                return;
            }

            // This must accept lists
            var found = false;

            foreach (var keyBinding in _keyBindings)
            {
                if (keyBinding.SwitchPanelPZ55Key == pz55SwitchOnOff.Switch && keyBinding.WhenTurnedOn == pz55SwitchOnOff.ButtonState)
                {
                    if (keySequence.Count == 0)
                    {
                        keyBinding.OSKeyPress = null;
                    }
                    else
                    {
                        var keyPress = new KeyPress(description, keySequence);
                        keyBinding.OSKeyPress = keyPress;
                    }

                    found = true;
                    break;
                }
            }

            if (!found && keySequence.Count > 0)
            {
                var keyBinding = new KeyBindingPZ55
                {
                    SwitchPanelPZ55Key = pz55SwitchOnOff.Switch
                };
                var keyPress = new KeyPress(description, keySequence);
                keyBinding.OSKeyPress = keyPress;
                keyBinding.WhenTurnedOn = pz55SwitchOnOff.ButtonState;
                _keyBindings.Add(keyBinding);
            }

            _keyBindings = KeyBindingPZ55.SetNegators(_keyBindings);
            SetIsDirty();
        }

        public override void AddOrUpdateOSCommandBinding(PanelSwitchOnOff panelSwitchOnOff, OSCommand operatingSystemCommand)
        {
            var pz55SwitchOnOff = (PZ55SwitchOnOff)panelSwitchOnOff;

            // This must accept lists
            var found = false;

            foreach (var operatingSystemCommandBinding in _operatingSystemCommandBindings)
            {
                if (operatingSystemCommandBinding.SwitchPanelPZ55Key == pz55SwitchOnOff.Switch && operatingSystemCommandBinding.WhenTurnedOn == pz55SwitchOnOff.ButtonState)
                {
                    operatingSystemCommandBinding.OSCommandObject = operatingSystemCommand;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                var operatingSystemCommandBindingPZ55 = new OSCommandBindingPZ55
                {
                    SwitchPanelPZ55Key = pz55SwitchOnOff.Switch,
                    OSCommandObject = operatingSystemCommand,
                    WhenTurnedOn = pz55SwitchOnOff.ButtonState
                };
                _operatingSystemCommandBindings.Add(operatingSystemCommandBindingPZ55);
            }

            SetIsDirty();
        }

        public override void AddOrUpdateDCSBIOSBinding(PanelSwitchOnOff panelSwitchOnOff, List<DCSBIOSInput> dcsbiosInputs, string description, bool isSequenced)
        {
            var pz55SwitchOnOff = (PZ55SwitchOnOff)panelSwitchOnOff;
            if (dcsbiosInputs.Count == 0)
            {
                RemoveSwitchFromList(ControlList.DCSBIOS, pz55SwitchOnOff);
                SetIsDirty();
                return;
            }

            // !!!!!!!
            // If all DCS-BIOS commands has been deleted then provide a empty list, not null object!!!

            // This must accept lists
            var found = false;
            foreach (var dcsBiosBinding in _dcsBiosBindings)
            {
                if (dcsBiosBinding.SwitchPanelPZ55Key == pz55SwitchOnOff.Switch && dcsBiosBinding.WhenTurnedOn == pz55SwitchOnOff.ButtonState)
                {
                    dcsBiosBinding.DCSBIOSInputs = dcsbiosInputs;
                    dcsBiosBinding.Description = description;
                    dcsBiosBinding.IsSequenced = isSequenced;
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                var dcsBiosBinding = new DCSBIOSActionBindingPZ55
                {
                    SwitchPanelPZ55Key = pz55SwitchOnOff.Switch,
                    DCSBIOSInputs = dcsbiosInputs,
                    WhenTurnedOn = pz55SwitchOnOff.ButtonState,
                    Description = description,
                    IsSequenced = isSequenced
                };
                _dcsBiosBindings.Add(dcsBiosBinding);
            }

            SetIsDirty();
        }

        public override void AddOrUpdateBIPLinkBinding(PanelSwitchOnOff panelSwitchOnOff, BIPLinkBase bipLink)
        {
            var pz55SwitchOnOff = (PZ55SwitchOnOff)panelSwitchOnOff;
            var bipLinkPZ55 = (BIPLinkPZ55)bipLink;
            if (bipLinkPZ55.BIPLights.Count == 0)
            {
                RemoveSwitchFromList(ControlList.BIPS, pz55SwitchOnOff);
                SetIsDirty();
                return;
            }

            // This must accept lists
            var found = false;

            foreach (var tmpBipLink in _bipLinks)
            {
                if (tmpBipLink.SwitchPanelPZ55Key == pz55SwitchOnOff.Switch && tmpBipLink.WhenTurnedOn == pz55SwitchOnOff.ButtonState)
                {
                    tmpBipLink.BIPLights = bipLinkPZ55.BIPLights;
                    tmpBipLink.Description = bipLinkPZ55.Description;
                    found = true;
                    break;
                }
            }

            if (!found && bipLinkPZ55.BIPLights.Count > 0)
            {
                bipLinkPZ55.SwitchPanelPZ55Key = pz55SwitchOnOff.Switch;
                bipLinkPZ55.WhenTurnedOn = pz55SwitchOnOff.ButtonState;
                _bipLinks.Add(bipLinkPZ55);
            }

            SetIsDirty();
        }

        public override void RemoveSwitchFromList(object controlList, PanelSwitchOnOff panelSwitchOnOff)
        {
            var controlListPZ55 = (ControlList)controlList;
            var pz55SwitchOnOff = (PZ55SwitchOnOff)panelSwitchOnOff;

            var found = false;
            if (controlListPZ55 == ControlList.ALL || controlListPZ55 == ControlList.KEYS)
            {
                foreach (var keyBindingPZ55 in _keyBindings)
                {
                    if (keyBindingPZ55.SwitchPanelPZ55Key == pz55SwitchOnOff.Switch && keyBindingPZ55.WhenTurnedOn == pz55SwitchOnOff.ButtonState)
                    {
                        keyBindingPZ55.OSKeyPress = null;
                        found = true;
                    }
                }
            }

            if (controlListPZ55 == ControlList.ALL || controlListPZ55 == ControlList.DCSBIOS)
            {
                foreach (var dcsBiosBinding in _dcsBiosBindings)
                {
                    if (dcsBiosBinding.SwitchPanelPZ55Key == pz55SwitchOnOff.Switch && dcsBiosBinding.WhenTurnedOn == pz55SwitchOnOff.ButtonState)
                    {
                        dcsBiosBinding.DCSBIOSInputs.Clear();
                        found = true;
                    }
                }
            }

            if (controlListPZ55 == ControlList.ALL || controlListPZ55 == ControlList.BIPS)
            {
                foreach (var bipLink in _bipLinks)
                {
                    if (bipLink.SwitchPanelPZ55Key == pz55SwitchOnOff.Switch && bipLink.WhenTurnedOn == pz55SwitchOnOff.ButtonState)
                    {
                        bipLink.BIPLights.Clear();
                        found = true;
                    }
                }
            }

            if (controlListPZ55 == ControlList.ALL || controlListPZ55 == ControlList.OSCOMMANDS)
            {
                OSCommandBindingPZ55 operatingSystemCommandBindingPZ55 = null;
                for (int i = 0; i < _operatingSystemCommandBindings.Count; i++)
                {
                    var operatingSystemCommand = _operatingSystemCommandBindings[i];

                    if (operatingSystemCommand.SwitchPanelPZ55Key == pz55SwitchOnOff.Switch && operatingSystemCommand.WhenTurnedOn == pz55SwitchOnOff.ButtonState)
                    {
                        operatingSystemCommandBindingPZ55 = _operatingSystemCommandBindings[i];
                        found = true;
                    }
                }

                if (operatingSystemCommandBindingPZ55 != null)
                {
                    _operatingSystemCommandBindings.Remove(operatingSystemCommandBindingPZ55);
                }
            }

            if (found)
            {
                SetIsDirty();
            }
        }

        public bool LedIsConfigured(SwitchPanelPZ55LEDPosition switchPanelPZ55LEDPosition)
        {
            return _listColorOutputBinding.Any(colorOutputBinding => (SwitchPanelPZ55LEDPosition)colorOutputBinding.SaitekLEDPosition.GetPosition() == switchPanelPZ55LEDPosition);
        }

        public List<DcsOutputAndColorBinding> GetLedDcsBiosOutputs(SwitchPanelPZ55LEDPosition switchPanelPZ55LEDPosition)
        {
            var result = new List<DcsOutputAndColorBinding>();
            foreach (var colorOutputBinding in _listColorOutputBinding)
            {
                if ((SwitchPanelPZ55LEDPosition)colorOutputBinding.SaitekLEDPosition.GetPosition() == switchPanelPZ55LEDPosition)
                {
                    result.Add(colorOutputBinding);
                }
            }

            return result;
        }

        public void SetLedDcsBiosOutput(SwitchPanelPZ55LEDPosition switchPanelPZ55LEDPosition, List<DcsOutputAndColorBinding> dcsOutputAndColorBindingPZ55List)
        {
            /*
             * Replace all old entries found for this position with the new ones for this particular position
             * If list is empty then so be it
             */
            _listColorOutputBinding.RemoveAll(item => item.SaitekLEDPosition.Position.Equals(new SaitekPanelLEDPosition(switchPanelPZ55LEDPosition).Position));

            foreach (var dcsOutputAndColorBinding in dcsOutputAndColorBindingPZ55List)
            {
                _listColorOutputBinding.Add((DcsOutputAndColorBindingPZ55)dcsOutputAndColorBinding);
            }

            SetIsDirty();
        }


        protected override void GamingPanelKnobChanged(bool isFirstReport, IEnumerable<object> hashSet)
        {
            PZ55SwitchChanged(isFirstReport, hashSet);
        }

        public override DcsOutputAndColorBinding CreateDcsOutputAndColorBinding(SaitekPanelLEDPosition saitekPanelLEDPosition, PanelLEDColor panelLEDColor, DCSBIOSOutput dcsBiosOutput)
        {
            var dcsOutputAndColorBinding = new DcsOutputAndColorBindingPZ55
            {
                DCSBiosOutputLED = dcsBiosOutput,
                LEDColor = panelLEDColor,
                SaitekLEDPosition = saitekPanelLEDPosition
            };
            return dcsOutputAndColorBinding;
        }

        // SetLandingGearLED(cavb.PanelPZ55LEDPosition, cavb.PanelPZ55LEDColor);
        public void SetLandingGearLED(DcsOutputAndColorBindingPZ55 dcsOutputAndColorBindingPZ55)
        {
            SetLandingGearLED((SwitchPanelPZ55LEDPosition)dcsOutputAndColorBindingPZ55.SaitekLEDPosition.GetPosition(), dcsOutputAndColorBindingPZ55.LEDColor);
        }

        public void SetLandingGearLED(SwitchPanelPZ55LEDPosition switchPanelPZ55LEDPosition, PanelLEDColor switchPanelPZ55LEDColor)
        {
            try
            {
                switch (switchPanelPZ55LEDPosition)
                {
                    case SwitchPanelPZ55LEDPosition.UP:
                        {
                            _ledUpperColor = GetSwitchPanelPZ55LEDColor(switchPanelPZ55LEDPosition, switchPanelPZ55LEDColor);
                            break;
                        }

                    case SwitchPanelPZ55LEDPosition.LEFT:
                        {
                            _ledLeftColor = GetSwitchPanelPZ55LEDColor(switchPanelPZ55LEDPosition, switchPanelPZ55LEDColor);
                            break;
                        }

                    case SwitchPanelPZ55LEDPosition.RIGHT:
                        {
                            _ledRightColor = GetSwitchPanelPZ55LEDColor(switchPanelPZ55LEDPosition, switchPanelPZ55LEDColor);
                            break;
                        }
                }

                AppEventHandler.LedLightChanged(this, HIDSkeletonBase.HIDInstance, new SaitekPanelLEDPosition(switchPanelPZ55LEDPosition), switchPanelPZ55LEDColor);
                SetLandingGearLED(_ledUpperColor | _ledLeftColor | _ledRightColor);
            }
            catch (Exception ex)
            {
                SetLastException(ex);
            }
        }

        // Do not use directly !
        private void SetLandingGearLED(SwitchPanelPZ55LEDs switchPanelPZ55LEDs)
        {
            try
            {
                if (HIDSkeletonBase.HIDWriteDevice != null)
                {
                    var array = new[] { (byte)0x0, (byte)switchPanelPZ55LEDs };

                    // Common.DebugP("HIDWriteDevice writing feature data " + TypeOfSaitekPanel);
                    HIDSkeletonBase.HIDWriteDevice.WriteFeatureData(new byte[] { 0, 0 });
                    HIDSkeletonBase.HIDWriteDevice.WriteFeatureData(array);
                }

                // if (IsAttached)
                // {
                // Common.DebugP("Write ending");
                // }
            }
            catch (Exception ex)
            {
                SetLastException(ex);
            }
        }

        public static SwitchPanelPZ55LEDs GetSwitchPanelPZ55LEDColor(SwitchPanelPZ55LEDPosition switchPanelPZ55LEDPosition, PanelLEDColor panelLEDColor)
        {
            return switchPanelPZ55LEDPosition switch
            {
                SwitchPanelPZ55LEDPosition.UP => panelLEDColor switch
                {
                    PanelLEDColor.DARK => SwitchPanelPZ55LEDs.ALL_DARK,
                    PanelLEDColor.GREEN => SwitchPanelPZ55LEDs.UP_GREEN,
                    PanelLEDColor.RED => SwitchPanelPZ55LEDs.UP_RED,
                    PanelLEDColor.YELLOW => SwitchPanelPZ55LEDs.UP_YELLOW,
                    _ => SwitchPanelPZ55LEDs.ALL_DARK
                },
                SwitchPanelPZ55LEDPosition.LEFT => panelLEDColor switch
                {
                    PanelLEDColor.DARK => SwitchPanelPZ55LEDs.ALL_DARK,
                    PanelLEDColor.GREEN => SwitchPanelPZ55LEDs.LEFT_GREEN,
                    PanelLEDColor.RED => SwitchPanelPZ55LEDs.LEFT_RED,
                    PanelLEDColor.YELLOW => SwitchPanelPZ55LEDs.LEFT_YELLOW,
                    _ => SwitchPanelPZ55LEDs.ALL_DARK
                },
                SwitchPanelPZ55LEDPosition.RIGHT => panelLEDColor switch
                {
                    PanelLEDColor.DARK => SwitchPanelPZ55LEDs.ALL_DARK,
                    PanelLEDColor.GREEN => SwitchPanelPZ55LEDs.RIGHT_GREEN,
                    PanelLEDColor.RED => SwitchPanelPZ55LEDs.RIGHT_RED,
                    PanelLEDColor.YELLOW => SwitchPanelPZ55LEDs.RIGHT_YELLOW,
                    _ => SwitchPanelPZ55LEDs.ALL_DARK
                },
                _ => SwitchPanelPZ55LEDs.ALL_DARK
            };
        }

        private void CreateSwitchKeys()
        {
            // _switchPanelKeys = SwitchPanelKey.GetPanelSwitchKeys();
            SaitekPanelKnobs = SwitchPanelKey.GetPanelSwitchKeys();
        }

        public HashSet<DCSBIOSActionBindingPZ55> DCSBiosBindings
        {
            get => _dcsBiosBindings;
            set => _dcsBiosBindings = value;
        }

        public bool ManualLandingGearLEDs
        {
            get => _manualLandingGearLeds;
            set
            {
                _manualLandingGearLeds = value;
                SetIsDirty();
            }
        }

        public PanelLEDColor ManualLandingGearLEDsColorDown
        {
            get => _manualLandingGearLedsColorDown;
            set
            {
                _manualLandingGearLedsColorDown = value;
                SetIsDirty();
            }
        }

        public PanelLEDColor ManualLandingGearLEDsColorUp
        {
            get => _manualLandingGearLedsColorUp;
            set
            {
                _manualLandingGearLedsColorUp = value;
                SetIsDirty();
            }
        }

        public PanelLEDColor ManualLandingGearLEDsColorTrans
        {
            get => _manualLandingGearLedsColorTrans;
            set
            {
                _manualLandingGearLedsColorTrans = value;
                SetIsDirty();
            }
        }

        public int ManualLandingGearTransTimeSeconds
        {
            get => _manualLandingGearTransTimeSeconds;
            set
            {
                _manualLandingGearTransTimeSeconds = value;
                SetIsDirty();
            }
        }
    }

}
/*
Setting the LED lights on Switch Panel PZ55. One byte with one byte report header (0x0)


LED Byte:
* 00000000 0x0 ALL DARK
* 
* 00000001 0x1 UP GREEN
* 00001000 0x8 UP RED
* 00001001 0x9 UP YELLOW
* 
* 00000010 0x2 LEFT GREEN
* 00010000 0x10 LEFT RED
* 00010010 0x12 LEFT YELLOW
* 
* 00100000 0x20 RIGHT RED
* 00000100 0x4 RIGHT GREEN
* 00100100 0x24 RIGHT YELLOW




Switch Panel PZ55 sends 3 bytes representing all the switches and knobs, levers.

0 = Off

1 = On


Byte #1
00000000
||||||||_ SWITCHKEY_MASTER_BAT
|||||||_ SWITCHKEY_MASTER_ALT
||||||_ SWITCHKEY_AVIONICS_MASTER
|||||_ SWITCHKEY_FUEL_PUMP
||||_ SWITCHKEY_DE_ICE
|||_ SWITCHKEY_PITOT_HEAT
||_ SWITCHKEY_CLOSE_COWL ** ~
|_ SWITCHKEY_LIGHTS_PANEL

Byte #2 
00000000
||||||||_ SWITCHKEY_LIGHTS_BEACON
|||||||_ SWITCHKEY_LIGHTS_NAV
||||||_ SWITCHKEY_LIGHTS_STROBE
|||||_ SWITCHKEY_LIGHTS_TAXI
||||_ SWITCHKEY_LIGHTS_LANDING
|||_ KNOB_ENGINE_OFF
||_ KNOB_ENGINE_RIGHT
|_ KNOB_ENGINE_LEFT

Byte #3
00000000
||||||||_ KNOB_ENGINE_BOTH
|||||||_ KNOB_ENGINE_START
||||||_ LEVER_GEAR_UP
|||||_ LEVER_GEAR_DOWN
||||_ 
|||_ 
||_ 
|_


 */
