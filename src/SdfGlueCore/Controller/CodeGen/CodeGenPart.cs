//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
using SingleDocAppCore.Model.DataNodes;

namespace SdfGlueCore.Controller.CodeGen
{
    // Base of the code generator parts. Each part appends its code to CodeGenContext.Code;
    // the parts are generated in the order defined by ShaderCodeGenerator.
    public abstract class CodeGenPart
    {
        // Name of the section in the "Generated Code" window
        public abstract string Name { get; }

        public virtual bool IsEnabled(CodeGenContext ctx)
        {
            return true;
        }

        public abstract void Generate(CodeGenContext ctx);

        protected static string GetNodeComment(TreeNode node)
        {
            return String.Format("//[Id:{0}] {1}", node.Id, node.Name.Val);
        }

        protected static void AppendSectionHeader(CodeGenContext ctx, string title)
        {
            ctx.Code.AppendLine("//---------------------------------------------------------------------------");
            ctx.Code.AppendLine("// " + title);
            ctx.Code.AppendLine("//---------------------------------------------------------------------------");
        }
    }
}
