//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SingleDocAppCore.Settings;

namespace SingleDocAppFramework.Ui.Settings
{
    // Detects changes of the user settings edited in place by ImGui widgets (which do not report all changes),
    // by comparing the serialized state with the one from the previous call.
    // Serializes the settings on each call - use it only while a settings editor is visible.
    public class UserSettingsChangeDetector
    {
        private string?     lastXml_    = null;

        // Forgets the remembered state (the next Check() returns false)
        public void Reset()
        {
            lastXml_ = null;
        }

        // True if the settings differ from the state seen in the previous call (false on the first call)
        public bool Check(UserSettingsBase settings)
        {
            string xml = settings.TakeSnapshot().OuterXml;
            bool changed = (lastXml_ != null && xml != lastXml_);
            lastXml_ = xml;
            return changed;
        }
    }
}
