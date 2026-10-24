//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using System.Text.RegularExpressions;

namespace SingleDocAppAnimation.Ui
{
    // The texts of the timeline: English by default, a host can give its own (Override: the text of a key, null: the
    // default). The names of the properties and their groups are looked up as "P.<label>" ("P.Rotation"), a numbered
    // group ("Contour 2") by its name without the number; without a text the label itself is shown.
    public sealed class AnimTexts
    {
        public Func<string, string?>?       Override;

        private static readonly Dictionary<string, string> en_ = new Dictionary<string, string>
        {
            { "TlNoDocument",       "Nothing to animate." },
            { "TlPlay",             "Play (Space / Enter)" },
            { "TlStop",             "Stop (Space / Enter)" },
            { "TlFromStart",        "Play from the start" },
            { "TlLoop",             "Loop" },
            { "TlRecord",           "Record" },
            { "TlRecordTip",        "Record: the edits of the animated properties become keys at the playhead" },
            { "TlFrameOf",          "frame {0} / {1}" },
            { "TlTimeOf",           "{0:0.000} s / {1:0.000} s" },
            { "TlAddLabel",         "Add marker" },
            { "TlRenameLabel",      "Rename marker" },
            { "TlDeleteLabel",      "Delete marker" },
            { "TlLabels",           "markers" },
            { "TlFit",              "Fit" },
            { "TlEaseLinear",       "linear" },
            { "TlEaseIn",           "in" },
            { "TlEaseOut",          "out" },
            { "TlEaseInOut",        "in-out" },
            { "TlTip",              "Click / drag: the playhead (Shift: it snaps to the keys and the markers), a marker, the end on the ruler. Right click: the menu. Wheel: scroll, Ctrl+wheel: zoom, Shift+wheel: the rows, middle drag: pan.\n"
                                  + "Keys: Space / Enter play / stop, , . the previous / next frame, Home / End the start / end, Delete the selected keys / marker, Ctrl+C / Ctrl+V the keys.\n\n"
                                  + "The tree: the arrows open the objects and the groups of their properties. The diamond next to a property keys it at the playhead (empty: static, orange: animated, yellow: a key here). "
                                  + "The value is dragged, a double click types it, a click on a color opens the picker. Keys: click / Shift+drag a box / drag to move; the row menu: interpolation (hold, easing), RGB / HSL colors, remove animation.\n"
                                  + "Rows: a click on a name selects the row (Ctrl: add / remove, Shift: a range, Ctrl+A: all shown, Escape: none); an object or a group means all of its properties." },
            { "TlFilter",           "Show" },
            { "TlFilterAll",        "all" },
            { "TlFilterAnimated",   "animated" },
            { "TlFilterSelected",   "selection" },
            { "TlSearch",           "search" },
            { "TlKeyTip",           "Key / remove the key of this property at the playhead." },
            { "TlKeyGroupTip",      "Key / remove the keys of all properties of the group at the playhead." },
            { "TlAnimatedBy",       "animated by a track" },
            { "TlKeyAdd",           "Key at the playhead" },
            { "TlKeyRemove",        "Remove the key at the playhead" },
            { "TlKeyAll",           "Key all at the playhead" },
            { "TlKeySelected",      "Key the selected properties ({0})" },
            { "TlRemoveAnimation",  "Remove animation (make static)" },
            { "TlInterp",           "Interpolation" },
            { "TlHold",             "Hold (step)" },
            { "TlColorSpace",       "Color interpolation" },
            { "TlRgb",              "RGB" },
            { "TlHsl",              "HSL (hue, saturation, lightness)" },
            { "TlCopyKeys",         "Copy keys" },
            { "TlPasteKeys",        "Paste keys at the playhead" },
            { "TlDeleteKeys",       "Delete keys" },
            { "TlKeysN",            "{0} keys" },
            { "TlKeyTime",          "time {0:0.###} s" },
        };

        private static readonly Regex   numbered_   = new Regex(@"^(.+) (\d+)$");

        public string Get(string key)
        {
            return Override?.Invoke(key) ?? (en_.TryGetValue(key, out string? s) ? s : key);
        }

        // the name of a property or of a group of properties
        public string Property(string label)
        {
            string? s = Override?.Invoke("P." + label);
            if (s != null)
                return s;
            Match m = numbered_.Match(label);
            return m.Success ? Property(m.Groups[1].Value) + " " + m.Groups[2].Value : label;
        }
    }
}
