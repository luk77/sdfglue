//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
namespace SdfGlueCore.Model
{
    public class ExportAnimationSettings
    {
        public  int         Fps                     = 60;
        public  int         NumOfFramesToPrerender  = 10;
        public  int         NumOfFramesToExport     = 120;
    }
}
