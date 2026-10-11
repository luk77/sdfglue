//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------

namespace SdfGlueCore.Controller
{
    // Code of one generator part (shown separately in the "Generated Code" window)
    public class CodeGenSection
    {
        public  string                  Name;
        public  string                  Code;

        public CodeGenSection(string name, string code)
        {
            Name = name;
            Code = code;
        }
    }

    public class CodeGenResult
    {
        // all generated code (inserted into the template in place of the generated code tag)
        public  string?                 GeneratedCode        = null;
        // template + generated code
        public  string?                 FullShader           = null;
        public  List<CodeGenSection>    Sections             = new List<CodeGenSection>();

        // Incremented on every generation (code derived from the result, e.g. exports, can be cached)
        public  int                     Version              { get; private set; } = 0;

        public void Clean()
        {
            Version++;
            GeneratedCode        = null;
            FullShader           = null;
            Sections.Clear();
        }

        public string? FindSectionCode(string name)
        {
            foreach (CodeGenSection section in Sections)
            {
                if (section.Name == name)
                    return section.Code;
            }
            return null;
        }
    }
}
