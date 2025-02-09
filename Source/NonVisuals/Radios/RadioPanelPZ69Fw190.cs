﻿using NonVisuals.BindingClasses.BIP;
using NonVisuals.Helpers;

namespace NonVisuals.Radios
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    using ClassLibraryCommon;

    using DCS_BIOS;
    using DCS_BIOS.EventArgs;
    using DCS_BIOS.Interfaces;

    using MEF;
    using Plugin;
    using Knobs;
    using Panels.Saitek;
    using HID;


    /// <summary>
    /// Pre-programmed radio panel for the FW190. 
    /// </summary>
    public class RadioPanelPZ69Fw190 : RadioPanelPZ69Base, IDCSBIOSStringListener
    {
        private enum CurrentFw190RadioMode
        {
            FUG16ZY,
            IFF,
            HOMING,
            NO_USE
        }

        private CurrentFw190RadioMode _currentUpperRadioMode = CurrentFw190RadioMode.FUG16ZY;
        private CurrentFw190RadioMode _currentLowerRadioMode = CurrentFw190RadioMode.FUG16ZY;

        /*FuG 16ZY*/
        /*38.4 and 42.4 MHz*/
        /*
         *  COM1 Large Freq Sel
         *  COM1 Small Fine Tune
         *  Freq. Selector
         *  Fine Tuning
         *  I Management frequency Withing Squadron
         *  II Group Order frequency Different squadrons
         *  ? Air Traffic Control frequency
         *  ? Reich Fighter Defense Frequency (Country Wide)
         *  Homing:
         *  FT FT
         *  Y ZF
         */

        /*FuG 25a*/
        /*125 +/-1.8 MHz*/

        /* 
        * 
        *  COM1 Large Freq Sel
        *  COM1 Small Fine Tune
        *  COM2 Large IFF Control Switch
        *  COM2 Small Volume
        *  COM2 ACT/STBY IFF Test Button
        *  IFF Control Switch
        *  IFF Test Button
        *  Volume
        *  NAV1
        *  Homing Switch         
        */

        /*FuG 16ZY COM1*/
        // Large dial 0-3 [step of 1]
        // Small dial Fine tuning
        private readonly object _lockFug16ZyPresetDialObject1 = new();
        private DCSBIOSOutput _fug16ZyPresetDcsbiosOutputPresetDial;
        private volatile uint _fug16ZyPresetCockpitDialPos = 1;
        private const string FUG16_ZY_PRESET_COMMAND_INC = "RADIO_MODE INC\n";
        private const string FUG16_ZY_PRESET_COMMAND_DEC = "RADIO_MODE DEC\n";
        private readonly ClickSkipper _fug16ZyPresetDialSkipper = new(2);
        private readonly object _lockFug16ZyFineTuneDialObject1 = new();
        private DCSBIOSOutput _fug16ZyFineTuneDcsbiosOutputDial;
        private volatile uint _fug16ZyFineTuneCockpitDialPos = 1;
        private const string FUG16_ZY_FINE_TUNE_COMMAND_INC = "FUG16_TUNING INC\n";
        private const string FUG16_ZY_FINE_TUNE_COMMAND_DEC = "FUG16_TUNING DEC\n";

        /*Fw 190 FuG 25a IFF COM2*/
        // Large dial 0-1 [step of 1]
        // Small dial Volume control
        // ACT/STBY IFF Test Button
        private readonly object _lockFUG25AIFFDialObject1 = new();
        private DCSBIOSOutput _fug25AIFFDcsbiosOutputDial;
        private volatile uint _fug25AIFFCockpitDialPos = 1;
        private const string FUG25_AIFF_COMMAND_INC = "FUG25_MODE INC\n";
        private const string FUG25_AIFF_COMMAND_DEC = "FUG25_MODE DEC\n";
        private readonly ClickSkipper _fug25AIFFDialSkipper = new(2);
        private const string RADIO_VOLUME_KNOB_COMMAND_INC = "FUG16_VOLUME +1000\n";
        private const string RADIO_VOLUME_KNOB_COMMAND_DEC = "FUG16_VOLUME -1000\n";
        private const string FU_G25_A_TEST_COMMAND_INC = "FUG25_TEST INC\n";
        private const string FU_G25_A_TEST_COMMAND_DEC = "FUG25_TEST DEC\n";

        /*Fw 190 FuG 16ZY Homing Switch NAV1*/
        // Large dial N/A
        // Small dial N/A
        // ACT/STBY Homing Switch
        private readonly object _lockHomingDialObject1 = new();
        private DCSBIOSOutput _homingDcsbiosOutputPresetDial;
        private volatile uint _homingCockpitDialPos = 1;
        private const string HOMING_COMMAND_INC = "FT_ZF_SWITCH INC\n";
        private const string HOMING_COMMAND_DEC = "FT_ZF_SWITCH DEC\n";

        private readonly object _lockShowFrequenciesOnPanelObject = new();
        private long _doUpdatePanelLCD;

        public RadioPanelPZ69Fw190(HIDSkeleton hidSkeleton) : base(hidSkeleton)
        {
            CreateRadioKnobs();
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

        public void DCSBIOSStringReceived(object sender, DCSBIOSStringDataEventArgs e)
        {
            try
            {
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(ex, "DCSBIOSStringReceived()");
            }
        }

        public override void DcsBiosDataReceived(object sender, DCSBIOSDataEventArgs e)
        {
            try
            {
                UpdateCounter(e.Address, e.Data);

                /*
                * IMPORTANT INFORMATION REGARDING THE _*WaitingForFeedback variables
                * Once a dial has been deemed to be "off" position and needs to be changed
                * a change command is sent to DCS-BIOS.
                * Only after a *change* has been acknowledged will the _*WaitingForFeedback be
                * reset. Reading the dial's position with no change in value will not reset.
                */

                // FuG 16ZY Preset Channel Dial
                if (_fug16ZyPresetDcsbiosOutputPresetDial.UIntValueHasChanged(e.Address, e.Data))
                {
                    lock (_lockFug16ZyPresetDialObject1)
                    {
                        _fug16ZyPresetCockpitDialPos = _fug16ZyPresetDcsbiosOutputPresetDial.LastUIntValue;
                        Interlocked.Increment(ref _doUpdatePanelLCD);
                    }
                }

                // FuG 16ZY Fine Tune Dial
                if (_fug16ZyFineTuneDcsbiosOutputDial.UIntValueHasChanged(e.Address, e.Data))
                {
                    lock (_lockFug16ZyFineTuneDialObject1)
                    {
                        _fug16ZyFineTuneCockpitDialPos = _fug16ZyFineTuneDcsbiosOutputDial.LastUIntValue;
                        Interlocked.Increment(ref _doUpdatePanelLCD);
                    }
                }

                // FuG 25A IFF Channel Dial
                if (_fug25AIFFDcsbiosOutputDial.UIntValueHasChanged(e.Address, e.Data))
                {
                    lock (_lockFUG25AIFFDialObject1)
                    {
                        _fug25AIFFCockpitDialPos = _fug25AIFFDcsbiosOutputDial.LastUIntValue;
                        Interlocked.Increment(ref _doUpdatePanelLCD);
                    }
                }

                // FuG 16ZY Homing Switch
                if (_homingDcsbiosOutputPresetDial.UIntValueHasChanged(e.Address, e.Data))
                {
                    lock (_lockHomingDialObject1)
                    {
                        _homingCockpitDialPos = _homingDcsbiosOutputPresetDial.LastUIntValue;
                        Interlocked.Increment(ref _doUpdatePanelLCD);
                    }
                }

                // Set once
                DataHasBeenReceivedFromDCSBIOS = true;
                ShowFrequenciesOnPanel();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        protected override void PZ69KnobChanged(IEnumerable<object> hashSet)
        {
            try
            {
                Interlocked.Increment(ref _doUpdatePanelLCD);
                lock (LockLCDUpdateObject)
                {
                    foreach (var radioPanelKnobObject in hashSet)
                    {
                        var radioPanelKnob = (RadioPanelKnobFw190)radioPanelKnobObject;

                        switch (radioPanelKnob.RadioPanelPZ69Knob)
                        {
                            case RadioPanelPZ69KnobsFw190.UPPER_FUG16ZY:
                                {
                                    if (radioPanelKnob.IsOn)
                                    {
                                        SetUpperRadioMode(CurrentFw190RadioMode.FUG16ZY);
                                    }
                                    break;
                                }

                            case RadioPanelPZ69KnobsFw190.UPPER_IFF:
                                {
                                    if (radioPanelKnob.IsOn)
                                    {
                                        SetUpperRadioMode(CurrentFw190RadioMode.IFF);
                                    }
                                    break;
                                }

                            case RadioPanelPZ69KnobsFw190.UPPER_HOMING:
                                {
                                    if (radioPanelKnob.IsOn)
                                    {
                                        SetUpperRadioMode(CurrentFw190RadioMode.HOMING);
                                    }
                                    break;
                                }

                            case RadioPanelPZ69KnobsFw190.UPPER_NO_USE1:
                            case RadioPanelPZ69KnobsFw190.UPPER_NO_USE2:
                            case RadioPanelPZ69KnobsFw190.UPPER_NO_USE3:
                            case RadioPanelPZ69KnobsFw190.UPPER_NO_USE4:
                                {
                                    if (radioPanelKnob.IsOn)
                                    {
                                        SetUpperRadioMode(CurrentFw190RadioMode.NO_USE);
                                    }
                                    break;
                                }

                            case RadioPanelPZ69KnobsFw190.LOWER_FUG16ZY:
                                {
                                    if (radioPanelKnob.IsOn)
                                    {
                                        SetLowerRadioMode(CurrentFw190RadioMode.FUG16ZY);
                                    }
                                    break;
                                }

                            case RadioPanelPZ69KnobsFw190.LOWER_IFF:
                                {
                                    if (radioPanelKnob.IsOn)
                                    {
                                        SetLowerRadioMode(CurrentFw190RadioMode.IFF);
                                    }
                                    break;
                                }

                            case RadioPanelPZ69KnobsFw190.LOWER_HOMING:
                                {
                                    if (radioPanelKnob.IsOn)
                                    {
                                        SetLowerRadioMode(CurrentFw190RadioMode.HOMING);
                                    }
                                    break;
                                }

                            case RadioPanelPZ69KnobsFw190.LOWER_NO_USE1:
                            case RadioPanelPZ69KnobsFw190.LOWER_NO_USE2:
                            case RadioPanelPZ69KnobsFw190.LOWER_NO_USE3:
                            case RadioPanelPZ69KnobsFw190.LOWER_NO_USE4:
                                {
                                    if (radioPanelKnob.IsOn)
                                    {
                                        SetLowerRadioMode(CurrentFw190RadioMode.NO_USE);
                                    }
                                    break;
                                }

                            case RadioPanelPZ69KnobsFw190.UPPER_LARGE_FREQ_WHEEL_INC:
                            case RadioPanelPZ69KnobsFw190.UPPER_LARGE_FREQ_WHEEL_DEC:
                            case RadioPanelPZ69KnobsFw190.UPPER_SMALL_FREQ_WHEEL_INC:
                            case RadioPanelPZ69KnobsFw190.UPPER_SMALL_FREQ_WHEEL_DEC:
                            case RadioPanelPZ69KnobsFw190.LOWER_LARGE_FREQ_WHEEL_INC:
                            case RadioPanelPZ69KnobsFw190.LOWER_LARGE_FREQ_WHEEL_DEC:
                            case RadioPanelPZ69KnobsFw190.LOWER_SMALL_FREQ_WHEEL_INC:
                            case RadioPanelPZ69KnobsFw190.LOWER_SMALL_FREQ_WHEEL_DEC:
                                {
                                    // Ignore
                                    break;
                                }

                            case RadioPanelPZ69KnobsFw190.UPPER_FREQ_SWITCH:
                                {
                                    if (_currentLowerRadioMode == CurrentFw190RadioMode.IFF)
                                    {
                                        DCSBIOS.Send(radioPanelKnob.IsOn ? FU_G25_A_TEST_COMMAND_INC : FU_G25_A_TEST_COMMAND_DEC);
                                    }

                                    if (_currentUpperRadioMode == CurrentFw190RadioMode.HOMING)
                                    {
                                        if (radioPanelKnob.IsOn)
                                        {
                                            lock (_lockHomingDialObject1)
                                            {
                                                DCSBIOS.Send(_homingCockpitDialPos == 1 ? HOMING_COMMAND_DEC : HOMING_COMMAND_INC);
                                            }
                                        }
                                    }
                                    break;
                                }

                            case RadioPanelPZ69KnobsFw190.LOWER_FREQ_SWITCH:
                                {
                                    if (_currentLowerRadioMode == CurrentFw190RadioMode.IFF)
                                    {
                                        DCSBIOS.Send(radioPanelKnob.IsOn ? FU_G25_A_TEST_COMMAND_INC : FU_G25_A_TEST_COMMAND_DEC);
                                    }

                                    if (_currentLowerRadioMode == CurrentFw190RadioMode.HOMING)
                                    {
                                        if (radioPanelKnob.IsOn)
                                        {
                                            lock (_lockHomingDialObject1)
                                            {
                                                DCSBIOS.Send(_homingCockpitDialPos == 1 ? HOMING_COMMAND_DEC : HOMING_COMMAND_INC);
                                            }
                                        }
                                    }
                                    break;
                                }
                        }

                        if (PluginManager.PlugSupportActivated && PluginManager.HasPlugin())
                        {
                            PluginManager.DoEvent(DCSAircraft.SelectedAircraft.Description, HIDInstance, PluginGamingPanelEnum.PZ69RadioPanel_PreProg_FW190, (int)radioPanelKnob.RadioPanelPZ69Knob, radioPanelKnob.IsOn, null);
                        }
                    }

                    AdjustFrequency(hashSet);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private void AdjustFrequency(IEnumerable<object> hashSet)
        {
            try
            {
                if (SkipCurrentFrequencyChange())
                {
                    return;
                }

                foreach (var o in hashSet)
                {
                    var radioPanelKnobFw190 = (RadioPanelKnobFw190)o;
                    if (radioPanelKnobFw190.IsOn)
                    {
                        switch (radioPanelKnobFw190.RadioPanelPZ69Knob)
                        {
                            case RadioPanelPZ69KnobsFw190.UPPER_LARGE_FREQ_WHEEL_INC:
                                {
                                    switch (_currentUpperRadioMode)
                                    {
                                        case CurrentFw190RadioMode.FUG16ZY:
                                            {
                                                // Presets
                                                _fug16ZyPresetDialSkipper.Click(FUG16_ZY_PRESET_COMMAND_INC);
                                                break;
                                            }

                                        case CurrentFw190RadioMode.IFF:
                                            {
                                                _fug25AIFFDialSkipper.Click(FUG25_AIFF_COMMAND_INC);
                                                break;
                                            }

                                        case CurrentFw190RadioMode.HOMING:
                                            {
                                                break;
                                            }

                                        case CurrentFw190RadioMode.NO_USE:
                                            {
                                                break;
                                            }
                                    }
                                    break;
                                }

                            case RadioPanelPZ69KnobsFw190.UPPER_LARGE_FREQ_WHEEL_DEC:
                                {
                                    switch (_currentUpperRadioMode)
                                    {
                                        case CurrentFw190RadioMode.FUG16ZY:
                                            {
                                                // Presets
                                                _fug16ZyPresetDialSkipper.Click(FUG16_ZY_PRESET_COMMAND_DEC);
                                                break;
                                            }

                                        case CurrentFw190RadioMode.IFF:
                                            {
                                                _fug25AIFFDialSkipper.Click(FUG25_AIFF_COMMAND_DEC);
                                                break;
                                            }

                                        case CurrentFw190RadioMode.HOMING:
                                            {
                                                break;
                                            }

                                        case CurrentFw190RadioMode.NO_USE:
                                            {
                                                break;
                                            }
                                    }
                                    break;
                                }

                            case RadioPanelPZ69KnobsFw190.UPPER_SMALL_FREQ_WHEEL_INC:
                                {
                                    switch (_currentUpperRadioMode)
                                    {
                                        case CurrentFw190RadioMode.FUG16ZY:
                                            {
                                                // Fine tuning
                                                DCSBIOS.Send(FUG16_ZY_FINE_TUNE_COMMAND_INC);
                                                break;
                                            }

                                        case CurrentFw190RadioMode.IFF:
                                            {
                                                DCSBIOS.Send(RADIO_VOLUME_KNOB_COMMAND_INC);
                                                break;
                                            }

                                        case CurrentFw190RadioMode.HOMING:
                                            {
                                                break;
                                            }

                                        case CurrentFw190RadioMode.NO_USE:
                                            {
                                                break;
                                            }
                                    }
                                    break;
                                }

                            case RadioPanelPZ69KnobsFw190.UPPER_SMALL_FREQ_WHEEL_DEC:
                                {
                                    switch (_currentUpperRadioMode)
                                    {
                                        case CurrentFw190RadioMode.FUG16ZY:
                                            {
                                                // Fine tuning
                                                DCSBIOS.Send(FUG16_ZY_FINE_TUNE_COMMAND_DEC);
                                                break;
                                            }

                                        case CurrentFw190RadioMode.IFF:
                                            {
                                                DCSBIOS.Send(RADIO_VOLUME_KNOB_COMMAND_DEC);
                                                break;
                                            }

                                        case CurrentFw190RadioMode.HOMING:
                                            {
                                                break;
                                            }

                                        case CurrentFw190RadioMode.NO_USE:
                                            {
                                                break;
                                            }
                                    }
                                    break;
                                }

                            case RadioPanelPZ69KnobsFw190.LOWER_LARGE_FREQ_WHEEL_INC:
                                {
                                    switch (_currentLowerRadioMode)
                                    {
                                        case CurrentFw190RadioMode.FUG16ZY:
                                            {
                                                // Presets
                                                _fug16ZyPresetDialSkipper.Click(FUG16_ZY_PRESET_COMMAND_INC);
                                                break;
                                            }

                                        case CurrentFw190RadioMode.IFF:
                                            {
                                                _fug25AIFFDialSkipper.Click(FUG25_AIFF_COMMAND_INC);
                                                break;
                                            }

                                        case CurrentFw190RadioMode.HOMING:
                                            {
                                                break;
                                            }

                                        case CurrentFw190RadioMode.NO_USE:
                                            {
                                                break;
                                            }
                                    }
                                    break;
                                }

                            case RadioPanelPZ69KnobsFw190.LOWER_LARGE_FREQ_WHEEL_DEC:
                                {
                                    switch (_currentLowerRadioMode)
                                    {
                                        case CurrentFw190RadioMode.FUG16ZY:
                                            {
                                                // Presets
                                                _fug16ZyPresetDialSkipper.Click(FUG16_ZY_PRESET_COMMAND_DEC);
                                                break;
                                            }

                                        case CurrentFw190RadioMode.IFF:
                                            {
                                                _fug25AIFFDialSkipper.Click(FUG25_AIFF_COMMAND_DEC);
                                                break;
                                            }

                                        case CurrentFw190RadioMode.HOMING:
                                            {
                                                break;
                                            }

                                        case CurrentFw190RadioMode.NO_USE:
                                            {
                                                break;
                                            }
                                    }
                                    break;
                                }

                            case RadioPanelPZ69KnobsFw190.LOWER_SMALL_FREQ_WHEEL_INC:
                                {
                                    switch (_currentLowerRadioMode)
                                    {
                                        case CurrentFw190RadioMode.FUG16ZY:
                                            {
                                                // Fine tuning
                                                DCSBIOS.Send(FUG16_ZY_FINE_TUNE_COMMAND_INC);
                                                break;
                                            }

                                        case CurrentFw190RadioMode.IFF:
                                            {
                                                DCSBIOS.Send(RADIO_VOLUME_KNOB_COMMAND_INC);
                                                break;
                                            }

                                        case CurrentFw190RadioMode.HOMING:
                                            {
                                                break;
                                            }

                                        case CurrentFw190RadioMode.NO_USE:
                                            {
                                                break;
                                            }
                                    }
                                    break;
                                }

                            case RadioPanelPZ69KnobsFw190.LOWER_SMALL_FREQ_WHEEL_DEC:
                                {
                                    switch (_currentLowerRadioMode)
                                    {
                                        case CurrentFw190RadioMode.FUG16ZY:
                                            {
                                                // Fine tuning
                                                DCSBIOS.Send(FUG16_ZY_FINE_TUNE_COMMAND_DEC);
                                                break;
                                            }

                                        case CurrentFw190RadioMode.IFF:
                                            {
                                                DCSBIOS.Send(RADIO_VOLUME_KNOB_COMMAND_DEC);
                                                break;
                                            }

                                        case CurrentFw190RadioMode.HOMING:
                                            {
                                                break;
                                            }

                                        case CurrentFw190RadioMode.NO_USE:
                                            {
                                                break;
                                            }
                                    }
                                    break;
                                }
                        }
                    }
                }
                ShowFrequenciesOnPanel();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private void ShowFrequenciesOnPanel()
        {
            try
            {
                lock (_lockShowFrequenciesOnPanelObject)
                {
                    if (Interlocked.Read(ref _doUpdatePanelLCD) == 0)
                    {
                        return;
                    }

                    if (!FirstReportHasBeenRead)
                    {
                        return;
                    }

                    var bytes = new byte[21];
                    bytes[0] = 0x0;

                    switch (_currentUpperRadioMode)
                    {
                        case CurrentFw190RadioMode.FUG16ZY:
                            {
                                // 1-4
                                string modeDialPostionAsString;
                                string fineTunePositionAsString;
                                lock (_lockFug16ZyPresetDialObject1)
                                {
                                    modeDialPostionAsString = (_fug16ZyPresetCockpitDialPos + 1).ToString();
                                }

                                lock (_lockFug16ZyFineTuneDialObject1)
                                {

                                    fineTunePositionAsString = (_fug16ZyFineTuneCockpitDialPos / 10).ToString();
                                }

                                SetPZ69DisplayBytesUnsignedInteger(ref bytes, Convert.ToUInt32(modeDialPostionAsString), PZ69LCDPosition.UPPER_ACTIVE_LEFT);
                                SetPZ69DisplayBytesUnsignedInteger(ref bytes, Convert.ToUInt32(fineTunePositionAsString), PZ69LCDPosition.UPPER_STBY_RIGHT);
                                break;
                            }

                        case CurrentFw190RadioMode.IFF:
                            {
                                // Preset Channel Selector
                                // 0-1
                                string positionAsString;
                                lock (_lockFUG25AIFFDialObject1)
                                {
                                    positionAsString = (_fug25AIFFCockpitDialPos + 1).ToString().PadLeft(2, ' ');
                                }

                                SetPZ69DisplayBytesUnsignedInteger(ref bytes, Convert.ToUInt32(positionAsString), PZ69LCDPosition.UPPER_STBY_RIGHT);
                                SetPZ69DisplayBlank(ref bytes, PZ69LCDPosition.UPPER_ACTIVE_LEFT);
                                break;
                            }

                        case CurrentFw190RadioMode.HOMING:
                            {
                                // Switch
                                // 0-1
                                string positionAsString;
                                lock (_lockHomingDialObject1)
                                {
                                    positionAsString = (_homingCockpitDialPos + 1).ToString().PadLeft(2, ' ');
                                }

                                SetPZ69DisplayBytesUnsignedInteger(ref bytes, Convert.ToUInt32(positionAsString), PZ69LCDPosition.UPPER_STBY_RIGHT);
                                SetPZ69DisplayBlank(ref bytes, PZ69LCDPosition.UPPER_ACTIVE_LEFT);
                                break;
                            }

                        case CurrentFw190RadioMode.NO_USE:
                            {
                                SetPZ69DisplayBlank(ref bytes, PZ69LCDPosition.UPPER_ACTIVE_LEFT);
                                SetPZ69DisplayBlank(ref bytes, PZ69LCDPosition.UPPER_STBY_RIGHT);
                                break;
                            }
                    }
                    switch (_currentLowerRadioMode)
                    {
                        case CurrentFw190RadioMode.FUG16ZY:
                            {
                                // 1-4
                                string modeDialPostionAsString;
                                string fineTunePositionAsString;
                                lock (_lockFug16ZyPresetDialObject1)
                                {
                                    modeDialPostionAsString = (_fug16ZyPresetCockpitDialPos + 1).ToString();
                                }

                                lock (_lockFug16ZyFineTuneDialObject1)
                                {

                                    fineTunePositionAsString = (_fug16ZyFineTuneCockpitDialPos / 10).ToString();
                                }

                                SetPZ69DisplayBytesUnsignedInteger(ref bytes, Convert.ToUInt32(modeDialPostionAsString), PZ69LCDPosition.LOWER_ACTIVE_LEFT);
                                SetPZ69DisplayBytesUnsignedInteger(ref bytes, Convert.ToUInt32(fineTunePositionAsString), PZ69LCDPosition.LOWER_STBY_RIGHT);
                                break;
                            }

                        case CurrentFw190RadioMode.IFF:
                            {
                                // Preset Channel Selector
                                // 0-1
                                string positionAsString;
                                lock (_lockFUG25AIFFDialObject1)
                                {
                                    positionAsString = (_fug25AIFFCockpitDialPos + 1).ToString().PadLeft(2, ' ');
                                }

                                SetPZ69DisplayBytesUnsignedInteger(ref bytes, Convert.ToUInt32(positionAsString), PZ69LCDPosition.LOWER_STBY_RIGHT);
                                SetPZ69DisplayBlank(ref bytes, PZ69LCDPosition.LOWER_ACTIVE_LEFT);
                                break;
                            }

                        case CurrentFw190RadioMode.HOMING:
                            {
                                // Switch
                                // 0-1
                                string positionAsString;
                                lock (_lockHomingDialObject1)
                                {
                                    positionAsString = (_homingCockpitDialPos + 1).ToString().PadLeft(2, ' ');
                                }

                                SetPZ69DisplayBytesUnsignedInteger(ref bytes, Convert.ToUInt32(positionAsString), PZ69LCDPosition.LOWER_STBY_RIGHT);
                                SetPZ69DisplayBlank(ref bytes, PZ69LCDPosition.LOWER_ACTIVE_LEFT);
                                break;
                            }

                        case CurrentFw190RadioMode.NO_USE:
                            {
                                SetPZ69DisplayBlank(ref bytes, PZ69LCDPosition.LOWER_ACTIVE_LEFT);
                                SetPZ69DisplayBlank(ref bytes, PZ69LCDPosition.LOWER_STBY_RIGHT);
                                break;
                            }
                    }
                    SendLCDData(bytes);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            Interlocked.Decrement(ref _doUpdatePanelLCD);
        }


        public sealed override void Startup()
        {
            try
            {
                // COM1
                _fug16ZyPresetDcsbiosOutputPresetDial = DCSBIOSControlLocator.GetDCSBIOSOutput("RADIO_MODE");
                _fug16ZyFineTuneDcsbiosOutputDial = DCSBIOSControlLocator.GetDCSBIOSOutput("FUG16_TUNING");

                // COM2
                _fug25AIFFDcsbiosOutputDial = DCSBIOSControlLocator.GetDCSBIOSOutput("FUG25_MODE");

                // NAV1
                _homingDcsbiosOutputPresetDial = DCSBIOSControlLocator.GetDCSBIOSOutput("FT_ZF_SWITCH");

                StartListeningForHidPanelChanges();

                // IsAttached = true;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        public override void ClearSettings(bool setIsDirty = false) { }

        public override DcsOutputAndColorBinding CreateDcsOutputAndColorBinding(SaitekPanelLEDPosition saitekPanelLEDPosition, PanelLEDColor panelLEDColor, DCSBIOSOutput dcsBiosOutput)
        {
            throw new Exception("Radio Panel does not support color bindings with DCS-BIOS.");
        }

        private void CreateRadioKnobs()
        {
            SaitekPanelKnobs = RadioPanelKnobFw190.GetRadioPanelKnobs();
        }

        private void SetUpperRadioMode(CurrentFw190RadioMode currentFw190RadioMode)
        {
            try
            {
                _currentUpperRadioMode = currentFw190RadioMode;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        private void SetLowerRadioMode(CurrentFw190RadioMode currentFw190RadioMode)
        {
            try
            {
                _currentLowerRadioMode = currentFw190RadioMode;

                // If NO_USE then send next round of data to the panel in order to clear the LCD.
                // _sendNextRoundToPanel = true;catch (Exception ex)
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        public override void RemoveSwitchFromList(object controlList, PanelSwitchOnOff panelSwitchOnOff) { }
        public override void AddOrUpdateKeyStrokeBinding(PanelSwitchOnOff panelSwitchOnOff, string keyPress, KeyPressLength keyPressLength) { }
        public override void AddOrUpdateSequencedKeyBinding(PanelSwitchOnOff panelSwitchOnOff, string description, SortedList<int, IKeyPressInfo> keySequence) { }
        public override void AddOrUpdateDCSBIOSBinding(PanelSwitchOnOff panelSwitchOnOff, List<DCSBIOSInput> dcsbiosInputs, string description, bool isSequenced) { }
        public override void AddOrUpdateBIPLinkBinding(PanelSwitchOnOff panelSwitchOnOff, BIPLinkBase bipLink) { }
        public override void AddOrUpdateOSCommandBinding(PanelSwitchOnOff panelSwitchOnOff, OSCommand operatingSystemCommand) { }
    }
}
