﻿using Newtonsoft.Json;

namespace DCS_BIOS.Json
{
    public class DCSBIOSControlOutput
    {
        private string _type;

        [JsonProperty("address", Required = Required.Default)]
        public uint Address { get; set; }

        [JsonProperty("description", Required = Required.Default)]
        public string Description { get; set; }

        [JsonProperty("mask", Required = Required.Default)]
        public uint Mask { get; set; }

        [JsonProperty("max_value", Required = Required.Default)]
        public int MaxValue { get; set; }

        [JsonProperty("shift_by", Required = Required.Default)]
        public int ShiftBy { get; set; }

        [JsonProperty("suffix", Required = Required.Default)]
        public string Suffix { get; set; }

        [JsonProperty("max_length", Required = Required.Default)]
        public int MaxLength { get; set; }

        [JsonProperty("type", Required = Required.Default)]
        public string Type
        {
            get => _type; 
            set
            {
                _type = value;
                if (_type.Equals("string"))
                {
                    OutputDataType = DCSBiosOutputType.StringType;
                }
                if (_type.Equals("integer"))
                {
                    OutputDataType = DCSBiosOutputType.IntegerType;
                }
            }
        }

        public DCSBiosOutputType OutputDataType { get; set; }
    }
}
