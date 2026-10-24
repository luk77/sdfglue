//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
namespace SingleDocAppAnimation.Ui
{
    // The colors of the timeline (ImGui ABGR)
    public sealed class AnimTimelineStyle
    {
        public uint     Accent          = 0xFF3EA6FF;       // the selection
        public uint     Back            = 0xFF262626;
        public uint     Row             = 0xFF2C2C2C;
        public uint     RowAlt          = 0xFF303030;
        public uint     RowActive       = 0xFF4A3A30;       // the active section
        public uint     RowSection      = 0xFF383838;
        public uint     RowSelected     = 0xFF3A3A48;       // a selected target
        public uint     Ruler           = 0xFF1C1C1C;
        public uint     Tick            = 0xFF707070;
        public uint     Text            = 0xFFC8C8C8;
        public uint     TextDim         = 0xFF909090;
        public uint     TextHidden      = 0xFF707070;
        public uint     AfterEnd        = 0x90000000;
        public uint     End             = 0xFF909090;
        public uint     Playhead        = 0xFF4646F0;
        public uint     Marker          = 0xFF3CC8E6;
        public uint     TrackKey        = 0xFFDCDCDC;
        public uint     AggregateKey    = 0xFFA0A0A0;
        public uint     TrackSpan       = 0x50FFFFFF;
        public uint     Value           = 0xFF909090;
        public uint     AnimatedValue   = 0xFFE8C080;
        public uint     Box             = 0xFFFFA040;
        public uint     Splitter        = 0xFF505050;

        // the key buttons
        public uint     KeyNone         = 0xFF909090;
        public uint     KeyAnimated     = 0xFF3C96E6;
        public uint     KeyOn           = 0xFF40E0F0;

        // the widgets of the animated properties in the property panels
        public uint     FrameAnimated   = 0xFF3A5A2A;
        public uint     FrameOnKey      = 0xFF2A6E8C;

        public static readonly AnimTimelineStyle    Default     = new AnimTimelineStyle();
    }
}
