//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SdfGlueCore.Controller;
using SdfGlueCore.Model;
using SingleDocAppFramework;
using SingleDocAppFramework.Platform;

namespace SdfGlueEditor.Application
{
    // State shared by the SdfGlue controllers (the application state itself: SdfGlueAppContextBase)
    public class SdfGlueAppContext : SdfGlueAppContextBase
    {
        public  SdAppWindow                 Window;
        public  IPlatformServices           Platform;

        public SdfGlueAppContext(SdAppWindow window, IPlatformServices platform, UserSettingsSdfGlue settings)
            : base(settings)
        {
            Window          = window;
            Platform        = platform;
        }

        protected override void OnDocumentChanged()
        {
            base.OnDocumentChanged();

            Window.RefreshWindowTitle();
        }
    }
}
