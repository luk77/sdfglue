//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using System.Reflection;

namespace SdfGlueCore
{
    // Version of the application (Directory.Build.props, CI builds set InformationalVersion)
    public static class AppVersion
    {
        // Numeric version - stored in the project files
        public static string GetAppVersion()
        {
            //return Assembly
            //.GetEntryAssembly()?
            //.GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            //?.InformationalVersion
            //?? "unknown";

            String version = Assembly.GetExecutingAssembly()?.GetName().Version?.ToString() ?? "";

            return version;
        }

        // Version for display (window title). CI builds set InformationalVersion, e.g. "1.1.6.12-pr9+a1b2c3d"
        // for pull request builds; local builds fall back to the assembly version.
        // Do not use it in saved files - GetAppVersion() is the numeric version stored in projects.
        public static string GetAppDisplayVersion()
        {
            String? version = Assembly.GetExecutingAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;

            return String.IsNullOrEmpty(version) ? GetAppVersion() : version;
        }
    }
}
