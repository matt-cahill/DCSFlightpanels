﻿namespace DCSFlightpanels.Radios.PreProgrammed
{
    using System;
    using System.Collections.Generic;
    using System.Windows;
    using System.Windows.Controls;

    using ClassLibraryCommon;

    using Interfaces;
    using Properties;
    using NonVisuals.EventArgs;
    using NonVisuals.Interfaces;
    using NonVisuals.Radios;
    using NonVisuals.Radios.Knobs;
    using NonVisuals.Panels;
    using NonVisuals.HID;

    /// <summary>
    /// Interaction logic for RadioPanelPZ69UserControlA10C.xaml
    /// </summary>
    public partial class RadioPanelPZ69UserControlA10C : IGamingPanelListener, IProfileHandlerListener, IGamingPanelUserControl
    {
        private readonly RadioPanelPZ69A10C _radioPanelPZ69;

        public RadioPanelPZ69UserControlA10C(HIDSkeleton hidSkeleton)
        {
            InitializeComponent();
            
            HideAllImages();
            _radioPanelPZ69 = new RadioPanelPZ69A10C(hidSkeleton)
            {
                FrequencyKnobSensitivity = Settings.Default.RadioFrequencyKnobSensitivity
            };
            
            AppEventHandler.AttachGamingPanelListener(this);
        }

        private bool _disposed;
        // Protected implementation of Dispose pattern.
        protected override void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    _radioPanelPZ69.Dispose();
                    AppEventHandler.DetachGamingPanelListener(this);
                }

                _disposed = true;
            }

            // Call base class implementation.
            base.Dispose(disposing);
        }
        
        public override GamingPanel GetGamingPanel()
        {
            return _radioPanelPZ69;
        }

        public override GamingPanelEnum GetPanelType()
        {
            return GamingPanelEnum.PZ69RadioPanel;
        }

        public string GetName()
        {
            return GetType().Name;
        }

        public void UpdatesHasBeenMissed(object sender, DCSBIOSUpdatesMissedEventArgs e) { }
        
        public void SwitchesChanged(object sender, SwitchesChangedEventArgs e)
        {
            try
            {
                if (e.PanelType == GamingPanelEnum.PZ69RadioPanel && e.HidInstance.Equals(_radioPanelPZ69.HIDInstance))
                {
                    SetGraphicsState(e.Switches);
                }
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox( ex);
            }
        }

        public void ProfileEvent(object sender, ProfileEventArgs e){}
        
        public void SettingsApplied(object sender, PanelInfoArgs e) { }

        public void SettingsModified(object sender, PanelInfoArgs e) { }
        
        private void SetGraphicsState(HashSet<object> knobs)
        {
            try
            {
                foreach (var radioKnobO in knobs)
                {
                    var radioKnob = (RadioPanelKnobA10C)radioKnobO;
                    Dispatcher?.BeginInvoke((Action)delegate
                   {
                        /*if (radioKnob.IsOn)
                        {
                            SetGroupBoxVisibility(radioKnob.RadioPanelPZ69Knob);
                        }*/
                   });
                    switch (radioKnob.RadioPanelPZ69Knob)
                    {
                        case RadioPanelPZ69KnobsA10C.UPPER_VHFAM:
                            {
                                var key = radioKnob;
                                Dispatcher?.BeginInvoke(
                                    (Action)delegate
                                    {
                                        TopLeftCom1.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsA10C.UPPER_UHF:
                            {
                                var key = radioKnob;
                                Dispatcher?.BeginInvoke(
                                    (Action)delegate
                                    {
                                        TopLeftCom2.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsA10C.UPPER_VHFFM:
                            {
                                var key = radioKnob;
                                Dispatcher?.BeginInvoke(
                                    (Action)delegate
                                    {
                                        TopLeftNav1.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsA10C.UPPER_ILS:
                            {
                                var key = radioKnob;
                                Dispatcher?.BeginInvoke(
                                    (Action)delegate
                                    {
                                        TopLeftNav2.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsA10C.UPPER_TACAN:
                            {
                                var key = radioKnob;
                                Dispatcher?.BeginInvoke(
                                    (Action)delegate
                                    {
                                        TopLeftADF.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsA10C.UPPER_DME:
                            {
                                var key = radioKnob;
                                Dispatcher?.BeginInvoke(
                                    (Action)delegate
                                    {
                                        TopLeftDME.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsA10C.UPPER_XPDR:
                            {
                                var key = radioKnob;
                                Dispatcher?.BeginInvoke(
                                    (Action)delegate
                                    {
                                        TopLeftXPDR.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsA10C.LOWER_VHFAM:
                            {
                                var key = radioKnob;
                                Dispatcher?.BeginInvoke(
                                    (Action)delegate
                                    {
                                        LowerLeftCom1.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsA10C.LOWER_UHF:
                            {
                                var key = radioKnob;
                                Dispatcher?.BeginInvoke(
                                    (Action)delegate
                                    {
                                        LowerLeftCom2.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsA10C.LOWER_VHFFM:
                            {
                                var key = radioKnob;
                                Dispatcher?.BeginInvoke(
                                    (Action)delegate
                                    {
                                        LowerLeftNav1.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsA10C.LOWER_ILS:
                            {
                                var key = radioKnob;
                                Dispatcher?.BeginInvoke(
                                    (Action)delegate
                                    {
                                        LowerLeftNav2.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsA10C.LOWER_TACAN:
                            {
                                var key = radioKnob;
                                Dispatcher?.BeginInvoke(
                                    (Action)delegate
                                    {
                                        LowerLeftADF.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsA10C.LOWER_DME:
                            {
                                var key = radioKnob;
                                Dispatcher?.BeginInvoke(
                                    (Action)delegate
                                    {
                                        LowerLeftDME.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsA10C.LOWER_XPDR:
                            {
                                var key = radioKnob;
                                Dispatcher?.BeginInvoke(
                                    (Action)delegate
                                    {
                                        LowerLeftXPDR.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsA10C.UPPER_SMALL_FREQ_WHEEL_INC:
                            {
                                var key = radioKnob;
                                Dispatcher?.BeginInvoke(
                                    (Action)delegate
                                    {
                                        UpperSmallerLCDKnobInc.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsA10C.UPPER_SMALL_FREQ_WHEEL_DEC:
                            {
                                var key = radioKnob;
                                Dispatcher?.BeginInvoke(
                                    (Action)delegate
                                    {
                                        UpperSmallerLCDKnobDec.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsA10C.UPPER_LARGE_FREQ_WHEEL_INC:
                            {
                                var key = radioKnob;
                                Dispatcher?.BeginInvoke(
                                    (Action)delegate
                                    {
                                        UpperLargerLCDKnobInc.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsA10C.UPPER_LARGE_FREQ_WHEEL_DEC:
                            {
                                var key = radioKnob;
                                Dispatcher?.BeginInvoke(
                                    (Action)delegate
                                    {
                                        UpperLargerLCDKnobDec.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsA10C.LOWER_SMALL_FREQ_WHEEL_INC:
                            {
                                var key = radioKnob;
                                Dispatcher?.BeginInvoke(
                                    (Action)delegate
                                    {
                                        LowerSmallerLCDKnobInc.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsA10C.LOWER_SMALL_FREQ_WHEEL_DEC:
                            {
                                var key = radioKnob;
                                Dispatcher?.BeginInvoke(
                                    (Action)delegate
                                    {
                                        LowerSmallerLCDKnobDec.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsA10C.LOWER_LARGE_FREQ_WHEEL_INC:
                            {
                                var key = radioKnob;
                                Dispatcher?.BeginInvoke(
                                    (Action)delegate
                                    {
                                        LowerLargerLCDKnobInc.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsA10C.LOWER_LARGE_FREQ_WHEEL_DEC:
                            {
                                var key = radioKnob;
                                Dispatcher?.BeginInvoke(
                                    (Action)delegate
                                    {
                                        LowerLargerLCDKnobDec.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsA10C.UPPER_FREQ_SWITCH:
                            {
                                var key = radioKnob;
                                Dispatcher?.BeginInvoke(
                                    (Action)delegate
                                    {
                                        UpperRightSwitch.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                        case RadioPanelPZ69KnobsA10C.LOWER_FREQ_SWITCH:
                            {
                                var key = radioKnob;
                                Dispatcher?.BeginInvoke(
                                    (Action)delegate
                                    {
                                        LowerRightSwitch.Visibility = key.IsOn ? Visibility.Visible : Visibility.Collapsed;
                                    });
                                break;
                            }
                    }
                }
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox( ex);
            }
        }
        /*
        private void SetGroupboxVisibility(RadioPanelPZ69KnobsA10C panelPZ69KnobsA10C)
        {
            try
            {
                GroupLowerSelectorKnobUHF.Visibility = panelPZ69KnobsA10C == RadioPanelPZ69KnobsA10C.UPPER_UHF ? Visibility.Visible : Visibility.Hidden;
                GroupLowerSelectorKnobVHFFM.Visibility = panelPZ69KnobsA10C == RadioPanelPZ69KnobsA10C.UPPER_VHFFM ? Visibility.Visible : Visibility.Hidden;
                GroupLowerSelectorKnobVHFAM.Visibility = panelPZ69KnobsA10C == RadioPanelPZ69KnobsA10C.UPPER_VHFAM ? Visibility.Visible : Visibility.Hidden;
                GroupLowerSelectorKnobTACAN.Visibility = panelPZ69KnobsA10C == RadioPanelPZ69KnobsA10C.UPPER_TACAN ? Visibility.Visible : Visibility.Hidden;
                GroupLowerSelectorKnobILS.Visibility = panelPZ69KnobsA10C == RadioPanelPZ69KnobsA10C.UPPER_ILS ? Visibility.Visible : Visibility.Hidden;
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(129814, ex);
            }
        }
        */
        private void HideAllImages()
        {
            TopLeftCom1.Visibility = Visibility.Collapsed;
            TopLeftCom2.Visibility = Visibility.Collapsed;
            TopLeftNav1.Visibility = Visibility.Collapsed;
            TopLeftNav2.Visibility = Visibility.Collapsed;
            TopLeftADF.Visibility = Visibility.Collapsed;
            TopLeftDME.Visibility = Visibility.Collapsed;
            TopLeftXPDR.Visibility = Visibility.Collapsed;
            LowerLeftCom1.Visibility = Visibility.Collapsed;
            LowerLeftCom2.Visibility = Visibility.Collapsed;
            LowerLeftNav1.Visibility = Visibility.Collapsed;
            LowerLeftNav2.Visibility = Visibility.Collapsed;
            LowerLeftADF.Visibility = Visibility.Collapsed;
            LowerLeftDME.Visibility = Visibility.Collapsed;
            LowerLeftXPDR.Visibility = Visibility.Collapsed;
            LowerLargerLCDKnobDec.Visibility = Visibility.Collapsed;
            UpperLargerLCDKnobInc.Visibility = Visibility.Collapsed;
            UpperRightSwitch.Visibility = Visibility.Collapsed;
            UpperSmallerLCDKnobDec.Visibility = Visibility.Collapsed;
            UpperSmallerLCDKnobInc.Visibility = Visibility.Collapsed;
            UpperLargerLCDKnobDec.Visibility = Visibility.Collapsed;
            LowerLargerLCDKnobInc.Visibility = Visibility.Collapsed;
            LowerRightSwitch.Visibility = Visibility.Collapsed;
            LowerSmallerLCDKnobDec.Visibility = Visibility.Collapsed;
            LowerSmallerLCDKnobInc.Visibility = Visibility.Collapsed;
        }
        
        private void ButtonGetId_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_radioPanelPZ69 != null)
                {
                    TextBoxLogPZ69.Text = string.Empty;
                    TextBoxLogPZ69.Text = _radioPanelPZ69.HIDInstance;
                    Clipboard.SetText(_radioPanelPZ69.HIDInstance);
                    MessageBox.Show("The Instance Id for the panel has been copied to the Clipboard.");
                }
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox( ex);
            }
        }

        private void ComboBoxFreqKnobSensitivity_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (UserControlLoaded)
                {
                    Settings.Default.RadioFrequencyKnobSensitivity = int.Parse(ComboBoxFreqKnobSensitivity.SelectedValue.ToString());
                    _radioPanelPZ69.FrequencyKnobSensitivity = int.Parse(ComboBoxFreqKnobSensitivity.SelectedValue.ToString());
                    Settings.Default.Save();
                }
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox( ex);
            }
        }

        private void RadioPanelPZ69UserControlA10C_OnLoaded(object sender, RoutedEventArgs e)
        {
            DarkMode.SetFrameworkElemenDarkMode(this);
            try
            {
                ComboBoxFreqKnobSensitivity.SelectedValue = Settings.Default.RadioFrequencyKnobSensitivity;
                ComboBoxSyncOKDelayTimeout.SelectedValue = Settings.Default.SyncOKDelayTimeout;
                _radioPanelPZ69.SyncOKDelayTimeout = int.Parse(ComboBoxSyncOKDelayTimeout.SelectedValue.ToString());
                /*ComboBoxSynchSleepTime.SelectedValue = Settings.Default.BAKDialSynchSleepTime;
                ComboBoxSynchResetTimeout.SelectedValue = Settings.Default.BAKDialResetSyncTimeout;
                _radioPanelPZ69.ResetSyncTimeout = Settings.Default.BAKDialResetSyncTimeout;
                _radioPanelPZ69.SynchSleepTime = Settings.Default.BAKDialSynchSleepTime;*/
                UserControlLoaded = true;
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox( ex);
            }
        }

        private void ComboBoxSynchSleepTime_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (UserControlLoaded)
                {
                    /*Settings.Default.BAKDialSynchSleepTime = int.Parse(ComboBoxSynchSleepTime.SelectedValue.ToString());
                    _radioPanelPZ69.SynchSleepTime = int.Parse(ComboBoxSynchSleepTime.SelectedValue.ToString());
                    Settings.Default.Save();*/
                }
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox( ex);
            }
        }

        private void ComboBoxSynchResetTimeout_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (UserControlLoaded)
                {
                    /*Settings.Default.BAKDialResetSyncTimeout = int.Parse(ComboBoxSynchResetTimeout.SelectedValue.ToString());
                    _radioPanelPZ69.ResetSyncTimeout = int.Parse(ComboBoxSynchResetTimeout.SelectedValue.ToString());
                    Settings.Default.Save();*/
                }
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox( ex);
            }
        }

        private void ComboBoxSyncOKDelayTimeout_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (UserControlLoaded)
                {
                    Settings.Default.SyncOKDelayTimeout = int.Parse(ComboBoxSyncOKDelayTimeout.SelectedValue.ToString());
                    _radioPanelPZ69.SyncOKDelayTimeout = int.Parse(ComboBoxSyncOKDelayTimeout.SelectedValue.ToString());
                    Settings.Default.Save();
                }
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox( ex);
            }
        }

        private void ButtonGetIdentify_OnClick(object sender, RoutedEventArgs e)
        {
            try
            {
                _radioPanelPZ69.Identify();
            }
            catch (Exception ex)
            {
                Common.ShowErrorMessageBox(ex);
            }
        }
    }
}
