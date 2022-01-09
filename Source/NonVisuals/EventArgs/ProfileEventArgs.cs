﻿using ClassLibraryCommon;

namespace NonVisuals.EventArgs
{
    using EventArgs = System.EventArgs;

    public class ProfileEventArgs : EventArgs
    {
        public GenericPanelBinding PanelBinding { get; set; }

        public ProfileEventEnum ProfileEventType { get; set; }

        public DCSFPProfile DCSProfile { get; set; }
    }

    public enum ProfileEventEnum
    {
        ProfileTypeChosen,
        ProfileLoaded,
        ProfileClosed
    }
}
