﻿namespace NonVisuals.Radios.Knobs
{
    using System;
    using System.Collections.Generic;

    using MEF;

    using NonVisuals.Interfaces;

    public class RadioPanelKnobP51D : ISaitekPanelKnob
    {
        public RadioPanelKnobP51D(int group, int mask, bool isOn, RadioPanelPZ69KnobsP51D radioPanelPZ69Knob)
        {
            Group = group;
            Mask = mask;
            IsOn = isOn;
            RadioPanelPZ69Knob = radioPanelPZ69Knob;
        }

        public int Group { get; set; }

        public int Mask { get; set; }

        public bool IsOn { get; set; }

        public RadioPanelPZ69KnobsP51D RadioPanelPZ69Knob { get; set; }
        
        public static HashSet<ISaitekPanelKnob> GetRadioPanelKnobs()
        {
            // true means clockwise turn
            var result = new HashSet<ISaitekPanelKnob>
            {
                // Group 0
                new RadioPanelKnobP51D(2, Convert.ToInt32("1", 2), true, RadioPanelPZ69KnobsP51D.UPPER_SMALL_FREQ_WHEEL_INC),
                new RadioPanelKnobP51D(2, Convert.ToInt32("10", 2), false, RadioPanelPZ69KnobsP51D.UPPER_SMALL_FREQ_WHEEL_DEC),
                new RadioPanelKnobP51D(2, Convert.ToInt32("100", 2), true, RadioPanelPZ69KnobsP51D.UPPER_LARGE_FREQ_WHEEL_INC),
                new RadioPanelKnobP51D(2, Convert.ToInt32("1000", 2), false, RadioPanelPZ69KnobsP51D.UPPER_LARGE_FREQ_WHEEL_DEC),
                new RadioPanelKnobP51D(2, Convert.ToInt32("10000", 2), true, RadioPanelPZ69KnobsP51D.LOWER_SMALL_FREQ_WHEEL_INC),
                new RadioPanelKnobP51D(2, Convert.ToInt32("100000", 2), false, RadioPanelPZ69KnobsP51D.LOWER_SMALL_FREQ_WHEEL_DEC),
                new RadioPanelKnobP51D(2, Convert.ToInt32("1000000", 2), true, RadioPanelPZ69KnobsP51D.LOWER_LARGE_FREQ_WHEEL_INC),
                new RadioPanelKnobP51D(2, Convert.ToInt32("10000000", 2), false, RadioPanelPZ69KnobsP51D.LOWER_LARGE_FREQ_WHEEL_DEC),
                // Group 1
                new RadioPanelKnobP51D(1, Convert.ToInt32("1", 2), true, RadioPanelPZ69KnobsP51D.LOWER_NO_USE0), // LOWER COM2
                new RadioPanelKnobP51D(1, Convert.ToInt32("10", 2), true, RadioPanelPZ69KnobsP51D.LOWER_NO_USE1), // LOWER NAV1
                new RadioPanelKnobP51D(1, Convert.ToInt32("100", 2), true, RadioPanelPZ69KnobsP51D.LOWER_NO_USE2), // LOWER NAV2
                new RadioPanelKnobP51D(1, Convert.ToInt32("1000", 2), true, RadioPanelPZ69KnobsP51D.LOWER_NO_USE3), // LOWER ADF
                new RadioPanelKnobP51D(1, Convert.ToInt32("10000", 2), true, RadioPanelPZ69KnobsP51D.LOWER_NO_USE4), // LOWER DME
                new RadioPanelKnobP51D(1, Convert.ToInt32("100000", 2), true, RadioPanelPZ69KnobsP51D.LOWER_NO_USE5), // LOWER XPDR
                new RadioPanelKnobP51D(1, Convert.ToInt32("1000000", 2), true, RadioPanelPZ69KnobsP51D.UPPER_FREQ_SWITCH),
                new RadioPanelKnobP51D(1, Convert.ToInt32("10000000", 2), true, RadioPanelPZ69KnobsP51D.LOWER_FREQ_SWITCH),
                // Group 2
                new RadioPanelKnobP51D(0, Convert.ToInt32("1", 2), true, RadioPanelPZ69KnobsP51D.UPPER_VHF), // UPPER COM1
                new RadioPanelKnobP51D(0, Convert.ToInt32("10", 2), true, RadioPanelPZ69KnobsP51D.UPPER_NO_USE0), // UPPER COM2
                new RadioPanelKnobP51D(0, Convert.ToInt32("100", 2), true, RadioPanelPZ69KnobsP51D.UPPER_NO_USE1), // UPPER NAV1
                new RadioPanelKnobP51D(0, Convert.ToInt32("1000", 2), true, RadioPanelPZ69KnobsP51D.UPPER_NO_USE2), // UPPER NAV2
                new RadioPanelKnobP51D(0, Convert.ToInt32("10000", 2), true, RadioPanelPZ69KnobsP51D.UPPER_NO_USE3), // UPPER ADF
                new RadioPanelKnobP51D(0, Convert.ToInt32("100000", 2), true, RadioPanelPZ69KnobsP51D.UPPER_NO_USE4), // UPPER DME
                new RadioPanelKnobP51D(0, Convert.ToInt32("1000000", 2), true, RadioPanelPZ69KnobsP51D.UPPER_NO_USE5), // UPPER XPDR
                new RadioPanelKnobP51D(0, Convert.ToInt32("10000000", 2), true, RadioPanelPZ69KnobsP51D.LOWER_VHF) // LOWER COM1
            };

            return result;
        }
    }


}
