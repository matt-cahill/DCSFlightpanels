﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ClassLibraryCommon;
using Newtonsoft.Json;

namespace DCS_BIOS
{
    public static class DCSBIOSControlLocator
    {
        private static readonly object LockObject = new object();
        private static DCSFPProfile _dcsfpProfile;
        private static string _jsonDirectory;
        private static readonly List<DCSBIOSControl> DCSBIOSControls = new List<DCSBIOSControl>();
        //private static bool _listOnce = true;
        public static readonly string DCSBIOSNotFoundErrorMessage = "Error loading DCS-BIOS. Check that the DCS-BIOS location setting points to the JSON directory.";


        private static void Reset()
        {
            DCSBIOSProfileLoadStatus.Clear();
            DCSBIOSControls.Clear();
        }

        public static DCSBIOSControl GetControl(string controlId)
        {
            lock (LockObject)
            {
                if (Common.IsOperationModeFlagSet(EmulationMode.KeyboardEmulationOnly))
                {
                    return null;
                }
                try
                {
                    LoadControls();
                    /*if (_listOnce)
                    {
                        _listOnce = false;
                        foreach (var dcsbiosControl in _dcsbiosControls)
                        {
                            if (dcsbiosControl.outputs.Count > 0)
                            {
                                Console.WriteLine(dcsbiosControl.identifier + " " + dcsbiosControl.outputs[0].address);
                            }
                            else
                            {
                                Console.WriteLine(dcsbiosControl.identifier);
                            }
                        }
                    }
                    PrintDuplicateControlIdentifiers(_dcsbiosControls, true);*/
                    if (!DCSBIOSControls.Exists(controlObject => controlObject.identifier.Equals(controlId)))
                    {
                        throw new Exception("Error, control " + controlId + " does not exist. (" + Profile.Description + ")");
                    }
                    return DCSBIOSControls.Single(controlObject => controlObject.identifier.Equals(controlId));
                }
                catch (InvalidOperationException ioe)
                {
                    throw new Exception("Check DCS-BIOS version. Failed to find control " + controlId + " for airframe " + Profile.Description + " (" + Profile.JSONFilename + ". Did you switch airframe type for the profile and have existing control(s) for the previous type saved?" + Environment.NewLine + ioe.Message);
                }
            }
        }

        public static DCSBIOSOutput GetDCSBIOSOutput(string controlId)
        {
            lock (LockObject)
            {
                if (Common.IsOperationModeFlagSet(EmulationMode.KeyboardEmulationOnly))
                {
                    throw new Exception("DCSBIOSControlLocator.GetDCSBIOSOutput() Should not be called when only key emulator is active");
                }
                try
                {
                    var control = GetControl(controlId);
                    var dcsBIOSOutput = new DCSBIOSOutput();
                    dcsBIOSOutput.Consume(control);
                    return dcsBIOSOutput;
                }
                catch (InvalidOperationException ioe)
                {
                    throw new Exception("Check DCS-BIOS version. Failed to create DCSBIOSOutput based on control " + controlId + " for profile " + Profile.JSONFilename + Environment.NewLine + ioe.Message);
                }
            }
        }

        private static void ReadDataFromJsonFile(string filename)
        {
            if (DCSBIOSProfileLoadStatus.IsLoaded(filename) || filename == DCSFPProfile.GetKeyEmulator().JSONFilename || filename == DCSFPProfile.GetKeyEmulatorSRS().JSONFilename || filename == DCSFPProfile.GetNoFrameLoadedYet().JSONFilename)
            {
                return;
            }

            try
            {
                lock (LockObject)
                {
                    var directoryInfo = new DirectoryInfo(_jsonDirectory);
                    IEnumerable<FileInfo> files;
                    try
                    {
                        files = directoryInfo.EnumerateFiles(filename, SearchOption.TopDirectoryOnly);
                    }
                    catch (Exception ex)
                    {
                        throw new Exception("Failed to find DCS-BIOS files. -> " + Environment.NewLine + ex.Message);
                    }

                    foreach (var file in files)
                    {
                        var reader = file.OpenText();
                        string text;
                        try
                        {
                            text = reader.ReadToEnd();
                            //Debug.Print(text);
                        }
                        finally
                        {
                            reader.Close();
                        }

                        var jsonData = DCSBIOSJsonFormatterVersion1.Format(text);

                        //Console.WriteLine(jsonData);
                        /*var newfile = File.CreateText(@"e:\temp\regexp_debug_output.txt.txt");
                        newfile.Write(jsonData);
                        newfile.Close();*/

                        var dcsBiosControlList = JsonConvert.DeserializeObject<DCSBIOSControlRootObject>(jsonData);
                        /*foreach (var control in dcsBiosControlList.DCSBIOSControls)
                        {
                            Debug.Print(control.identifier);
                        }*/
                        //Debug.Print("\n--------------------------\n" + jsonData);
                        DCSBIOSControls.AddRange(dcsBiosControlList.DCSBIOSControls);
                        PrintDuplicateControlIdentifiers(dcsBiosControlList.DCSBIOSControls);
                        /*foreach (var control in _dcsbiosControls)
                        {
                            Debug.Print(control.identifier);
                        }*/
                    }

                    DCSBIOSProfileLoadStatus.SetLoaded(filename, true);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(DCSBIOSNotFoundErrorMessage + " ==>[" + _jsonDirectory + "]<==" + Environment.NewLine + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        public static void LoadControls()
        {
            /*
             * Load profile for current airframe
             * Remove/Add NS430
             * Load Common Data
             * Load Metadata End
             *
             * This function will have to change when (if) new types of profiles are added to DCS-BIOS
             */
            try
            {
                if (DCSFPProfile.IsNoFrameLoadedYet(_dcsfpProfile) ||
                    Common.IsOperationModeFlagSet(EmulationMode.KeyboardEmulationOnly))
                {
                    return;
                }

                if (!Common.IsOperationModeFlagSet(EmulationMode.NS430Enabled))
                {
                    if (DCSBIOSProfileLoadStatus.IsLoaded(DCSFPProfile.GetNS430().JSONFilename))
                    {
                        //Discard all DCS-BIOS controls if user "unloaded" NS430. Not possible to remove them specifically
                        //Better to force load all controls
                        Reset();
                    }
                    else
                    {
                        DCSBIOSProfileLoadStatus.Remove(DCSFPProfile.GetNS430().JSONFilename);
                    }
                }

                if (Common.IsOperationModeFlagSet(EmulationMode.NS430Enabled))
                {
                    ReadDataFromJsonFile(DCSFPProfile.GetNS430().JSONFilename);
                }

                if (DCSFPProfile.IsFlamingCliff(Profile))
                {
                    LoadCommonData(_jsonDirectory);
                    LoadMetaDataEnd(_jsonDirectory);
                }
                else
                {
                    LoadCommonData(_jsonDirectory);
                    LoadMetaDataEnd(_jsonDirectory);
                    //Load the controls for the actual aircraft/helicopter
                    ReadDataFromJsonFile(Profile.JSONFilename);
                }

                //Remove duplicates which may come from loading NS430 or other additional profiles
                while (DCSBIOSControls.Count(controlObject => controlObject.identifier.Equals("_UPDATE_COUNTER")) > 1)
                {
                    DCSBIOSControls.Remove(DCSBIOSControls.FindLast(controlObject =>
                        controlObject.identifier.Equals("_UPDATE_COUNTER")));
                }

                while (DCSBIOSControls.Count(controlObject =>
                           controlObject.identifier.Equals("_UPDATE_SKIP_COUNTER")) > 1)
                {
                    DCSBIOSControls.Remove(DCSBIOSControls.FindLast(controlObject =>
                        controlObject.identifier.Equals("_UPDATE_SKIP_COUNTER")));
                }
            }
            catch (Exception e)
            {
                throw new Exception(DCSBIOSNotFoundErrorMessage + " ==>[" + _jsonDirectory + "]<==" + e.Message);
            }
        }

        static void PrintDuplicateControlIdentifiers(List<DCSBIOSControl> dcsbiosControls, bool printAll = false)
        {
            var result = new List<string>();
            var dupes = new List<string>();
            foreach (var dcsbiosControl in dcsbiosControls)
            {
                if (printAll)
                {
                    result.Add(dcsbiosControl.identifier);
                }

                //Debug.Print(dcsbiosControl.identifier);
                var found = false;
                foreach (var str in result)
                {
                    if (str.Trim() == dcsbiosControl.identifier.Trim())
                    {
                        found = true;
                    }
                }
                if (!found)
                {
                    result.Add(dcsbiosControl.identifier);
                }
                if (found)
                {
                    dupes.Add(dcsbiosControl.identifier);
                }

            }
            if (dupes.Count > 0)
            {
                var message = "Below is a list of duplicate identifiers found in the " + Profile.JSONFilename + " profile (DCS-BIOS)\n";
                message = message + "The identifier must be unique, please correct the profile " + Profile.JSONFilename + " in the DCS-BIOS lib folder\n";
                message = message + "---------------------------------------------\n";
                foreach (var dupe in dupes)
                {
                    message = message + dupe + "\n";
                }
                message = message + "---------------------------------------------\n";
                Common.LogError(message);
            }
        }


        private static void LoadMetaDataEnd(string jsonDirectory)
        {
            if (DCSBIOSProfileLoadStatus.IsLoaded("MetadataEnd") || Common.IsOperationModeFlagSet(EmulationMode.KeyboardEmulationOnly) || DCSFPProfile.IsNoFrameLoadedYet(_dcsfpProfile))
            {
                return;
            }

            try
            {
                lock (LockObject)
                {
                    var metaDataEndText = File.ReadAllText(jsonDirectory + "\\MetadataEnd.json");
                    var metaDataEndControlsText = DCSBIOSJsonFormatterVersion1.Format(metaDataEndText);
                    var metaDataEndControls = JsonConvert.DeserializeObject<DCSBIOSControlRootObject>(metaDataEndControlsText);
                    DCSBIOSControls.AddRange(metaDataEndControls.DCSBIOSControls);
                    DCSBIOSProfileLoadStatus.SetLoaded("MetadataEnd", true);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(DCSBIOSNotFoundErrorMessage + " ==>[" + jsonDirectory + "]<==" + Environment.NewLine + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        private static void LoadCommonData(string jsonDirectory)
        {
            if (DCSBIOSProfileLoadStatus.IsLoaded("CommonData") || Common.IsOperationModeFlagSet(EmulationMode.KeyboardEmulationOnly) || DCSFPProfile.IsNoFrameLoadedYet(_dcsfpProfile))
            {
                return;
            }

            try
            {
                lock (LockObject)
                {
                    var commonDataText = File.ReadAllText(jsonDirectory + "\\CommonData.json");
                    var commonDataControlsText = DCSBIOSJsonFormatterVersion1.Format(commonDataText);
                    var commonDataControls = JsonConvert.DeserializeObject<DCSBIOSControlRootObject>(commonDataControlsText);
                    DCSBIOSControls.AddRange(commonDataControls.DCSBIOSControls);
                    DCSBIOSProfileLoadStatus.SetLoaded("CommonData", true);
                }
            }
            catch (Exception ex)
            {
                throw new Exception(DCSBIOSNotFoundErrorMessage + " ==>[" + jsonDirectory + "]<==" + Environment.NewLine + ex.Message + Environment.NewLine + ex.StackTrace);
            }
        }

        public static DCSFPProfile Profile
        {
            get { return _dcsfpProfile; }
            set
            {
                if (_dcsfpProfile != value)
                {
                    _dcsfpProfile = value;
                    Reset();
                }
            }
        }

        public static string JSONDirectory
        {
            get => _jsonDirectory;
            set => _jsonDirectory = DBCommon.GetDCSBIOSJSONDirectory(value);
        }
        
        public static IEnumerable<DCSBIOSControl> GetControls()
        {
            LoadControls();
            return DCSBIOSControls;
        }

        public static IEnumerable<DCSBIOSControl> GetStringOutputControls()
        {
            /*"CM_CHAFFCNT_DISPLAY": {
                   "category": "Countermeasures",
               "control_type": "display",
                "description": "Chaff Counter",
                 "identifier": "CM_CHAFFCNT_DISPLAY",
                     "inputs": [  ],
                    "outputs": [ {
                                       "address": 5408,
                                   "description": "Chaff Counter",
                                    "max_length": 2, 
                                        "suffix": "",
                                          "type": "string"
                               } ]
                },*/
            if (!Common.IsOperationModeFlagSet(EmulationMode.DCSBIOSOutputEnabled))
            {
                return null;
            }
            LoadControls();
            var result = DCSBIOSControls.Where(controlObject => (controlObject.outputs.Count > 0) && controlObject.outputs[0].OutputDataType == DCSBiosOutputType.STRING_TYPE);
            return result;
        }

        public static IEnumerable<DCSBIOSControl> GetIntegerOutputControls()
        {
            if (!Common.IsOperationModeFlagSet(EmulationMode.DCSBIOSOutputEnabled))
            {
                return null;
            }
            LoadControls();
            return DCSBIOSControls.Where(controlObject => (controlObject.outputs.Count > 0) && controlObject.outputs[0].OutputDataType == DCSBiosOutputType.INTEGER_TYPE);
        }

        public static IEnumerable<DCSBIOSControl> GetInputControls()
        {
            if (!Common.IsOperationModeFlagSet(EmulationMode.DCSBIOSInputEnabled))
            {
                return null;
            }
            LoadControls();
            return DCSBIOSControls.Where(controlObject => (controlObject.inputs.Count > 0));
        }
    }
}
