﻿namespace NonVisuals.DCSBIOSBindings
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    using DCS_BIOS;
    using Newtonsoft.Json;
    using NLog;

    [Serializable]
    public abstract class DCSBIOSActionBindingBase : IDisposable
    {
        internal static Logger logger = LogManager.GetCurrentClassLogger();
        private bool _whenOnTurnedOn = true;
        private string _description;
        [NonSerialized] private Thread _sendDCSBIOSCommandsThread;
        private volatile List<DCSBIOSInput> _dcsbiosInputs;
        
        internal abstract void ImportSettings(string settings);
        public abstract string ExportSettings();

        private bool _isSequenced;
        private int _sequenceIndex;
        private volatile bool _shutdownCommandsThread;

        private bool _disposed;
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }

            if (disposing)
            {
                _shutdownCommandsThread = true;
            }

            _disposed = true;
        }

        public void Dispose()
        {
            // Dispose of unmanaged resources.
            Dispose(true);
        }

        public bool IsRunning()
        {
            if (_sendDCSBIOSCommandsThread != null && (_sendDCSBIOSCommandsThread.ThreadState == ThreadState.Running ||
                                                       _sendDCSBIOSCommandsThread.ThreadState == ThreadState.WaitSleepJoin ||
                                                       _sendDCSBIOSCommandsThread.ThreadState == ThreadState.Unstarted))
            {
                return true;
            }

            return false;
        }

        protected Thread DCSBIOSCommandsThread => _sendDCSBIOSCommandsThread;

        protected bool CancelSendDCSBIOSCommands
        {
            get => _shutdownCommandsThread; 
            set => _shutdownCommandsThread = value;
        }


        public void SendDCSBIOSCommands(CancellationToken cancellationToken)
        {
            CancelSendDCSBIOSCommands = true;
            Thread.Sleep(Constants.ThreadShutDownWaitTime);
            CancelSendDCSBIOSCommands = false;
            _sendDCSBIOSCommandsThread = new Thread(() => SendDCSBIOSCommandsThread(DCSBIOSInputs, cancellationToken));
            _sendDCSBIOSCommandsThread.Start();
        }

        private void SendDCSBIOSCommandsThread(List<DCSBIOSInput> dcsbiosInputs, CancellationToken cancellationToken)
        {
            try
            {
                if (_isSequenced)
                {
                    if (dcsbiosInputs.Count == 0)
                    {
                        return;
                    }

                    if (_sequenceIndex <= dcsbiosInputs.Count - 1)
                    {
                        if (CancelSendDCSBIOSCommands || cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }
                        var command = dcsbiosInputs[_sequenceIndex].SelectedDCSBIOSInput.GetDCSBIOSCommand();
                        Thread.Sleep(dcsbiosInputs[_sequenceIndex].SelectedDCSBIOSInput.Delay);
                        if (CancelSendDCSBIOSCommands || cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }

                        DCSBIOS.Send(command);
                        _sequenceIndex++;

                        if (_sequenceIndex >= dcsbiosInputs.Count)
                        {
                            _sequenceIndex = 0;
                        }
                    }
                }
                else
                {
                    foreach (var dcsbiosInput in dcsbiosInputs)
                    {
                        if (CancelSendDCSBIOSCommands || cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }

                        var command = dcsbiosInput.SelectedDCSBIOSInput.GetDCSBIOSCommand();
                        Thread.Sleep(dcsbiosInput.SelectedDCSBIOSInput.Delay);
                        if (CancelSendDCSBIOSCommands || cancellationToken.IsCancellationRequested)
                        {
                            return;
                        }

                        DCSBIOS.Send(command);
                    }
                }
            }
            catch (ThreadAbortException)
            { }
            catch (Exception ex)
            {
                logger.Error(ex);
            }
        }


        [JsonProperty("WhenTurnedOn", Required = Required.Default)]
        public bool WhenTurnedOn
        {
            get => _whenOnTurnedOn;
            set => _whenOnTurnedOn = value;
        }

        [JsonProperty("Description", Required = Required.Default)]
        public string Description
        {
            get => string.IsNullOrWhiteSpace(_description) ? "DCS-BIOS" : _description;
            set => _description = value;
        }

        [JsonProperty("DCSBIOSInputs", Required = Required.Default)]
        public List<DCSBIOSInput> DCSBIOSInputs
        {
            get => _dcsbiosInputs;
            set => _dcsbiosInputs = value;
        }

        [JsonProperty("WhenOnTurnedOn", Required = Required.Default)]
        protected bool WhenOnTurnedOn
        {
            get => _whenOnTurnedOn;
            set => _whenOnTurnedOn = value;
        }

        public bool HasBinding()
        {
            return DCSBIOSInputs != null && DCSBIOSInputs.Count > 0;
        }

        [JsonProperty("IsSequenced", Required = Required.Default)]
        public bool IsSequenced
        {
            get => _isSequenced;
            set => _isSequenced = value;
        }
     

    }
}
