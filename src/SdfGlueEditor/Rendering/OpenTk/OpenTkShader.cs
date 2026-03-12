//---------------------------------------------------------------------------
// Copyright (c) 2020–2026 Łukasz Lesicki
// Licensed under the MIT License.
// See LICENSE file in the project root for full license information.
//---------------------------------------------------------------------------
// References:
// - https://github.com/opentk/LearnOpenTK/blob/master/Common/Shader.cs
// - https://learnopengl.com/Getting-started/Shaders

using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using System.Diagnostics;
using System.Text;

namespace SdfGlueEditor.Rendering.OpenTk
{
    public class OpenTkShader
    {
        private         int                         programHandle_          = 0;
        private         Dictionary<string, int>     uniformLocations_       = new Dictionary<string, int>();

        public OpenTkShader()
        {
        }

        public void ReleaseProgram()
        {
            if (programHandle_ != 0)
            {
                GL.DeleteProgram(programHandle_);
                programHandle_ = 0;
            }
        }

        public bool Reinitialize(StringBuilder sbErrors, string shaderSourceVert, string shaderSourceFrag, string shaderPassName)
        {
            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();

                ReleaseProgram();

                // Compile vertex shader
                int vertexShader = GL.CreateShader(ShaderType.VertexShader);
                GL.ShaderSource(vertexShader, shaderSourceVert);
                bool success = CompileShader(vertexShader);
                if (!success)
                {
                    string error = GL.GetShaderInfoLog(vertexShader);
                    sbErrors.Append(error);
                    Console.WriteLine(error);
                    return false;
                }

                // Compile fragment shader
                int fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
                GL.ShaderSource(fragmentShader, shaderSourceFrag);
                success = CompileShader(fragmentShader);
                if (!success)
                {
                    string error = GL.GetShaderInfoLog(fragmentShader);
                    sbErrors.Append(error);
                    Console.WriteLine(error);
                    return false;
                }

                // Merge vertex and fragment shaders into single program:

                // Create empty program
                programHandle_ = GL.CreateProgram();

                // Attach shaders to program
                GL.AttachShader(programHandle_, vertexShader);
                GL.AttachShader(programHandle_, fragmentShader);

                // Link program
                GL.LinkProgram(programHandle_);
                GL.GetProgram(programHandle_, GetProgramParameterName.LinkStatus, out var code);
                if (code != (int)All.True)
                {
                    string error = GL.GetProgramInfoLog(programHandle_);
                    sbErrors.Append(error);
                    Console.WriteLine(error);
                    return false;
                }

                // TODO: display compiled shader assembly or intermediate code...
                // https://computergraphics.stackexchange.com/questions/7594/how-to-get-assembly-code-from-glsl-shader
                //int buffSize = 1024*1024;
                ////IntPtr buffer = new IntPtr(
                //
                //GL.GetProgramBinary(programHandle_, ...


                // Release resources
                GL.DetachShader(programHandle_, vertexShader);
                GL.DetachShader(programHandle_, fragmentShader);
                GL.DeleteShader(fragmentShader);
                GL.DeleteShader(vertexShader);


                // Store uniforms locations
                uniformLocations_ = new Dictionary<string, int>();

                GL.GetProgram(programHandle_, GetProgramParameterName.ActiveUniforms, out var numberOfUniforms);

                for (int i = 0; i < numberOfUniforms; i++)
                {
                    int uniformSize = 0;
                    ActiveUniformType uniformType = ActiveUniformType.Float;

                    // Get the name
                    string key = GL.GetActiveUniform(programHandle_, i, out uniformSize, out uniformType);

                    // Get the location
                    int location = GL.GetUniformLocation(programHandle_, key);

                    // Add to the dictionary.
                    uniformLocations_.Add(key, location);
                }

                Debug.WriteLine("Shader for '{0}' compiled in {1} ms. Uniforms: {2}", shaderPassName, (int)sw.Elapsed.TotalMilliseconds, numberOfUniforms);
            }
            catch(Exception ex)
            {
                sbErrors.Append(ex.ToString());
                return false;
            }

            return true;
        }

        private static bool CompileShader(int shader)
        {
            // Try to compile the shader
            GL.CompileShader(shader);

            // Check for compilation errors
            GL.GetShader(shader, ShaderParameter.CompileStatus, out var code);
            if (code != (int)All.True)
            {
                return false;
            }

            return true;
        }

        public void Use()
        {
            GL.UseProgram(programHandle_);
        }

        public int GetAttribLocation(string attribName)
        {
            return GL.GetAttribLocation(programHandle_, attribName);
        }

        public void SetInt(string name, int data)
        {
            if (!uniformLocations_.ContainsKey(name))
                return;

            //GL.UseProgram(programHandle_);
            GL.Uniform1(uniformLocations_[name], data);
        }

        public void SetFloat(string name, float data)
        {
            if (!uniformLocations_.ContainsKey(name))
                return;

            //GL.UseProgram(programHandle_);
            GL.Uniform1(uniformLocations_[name], data);
        }

        public void SetMatrix4(string name, Matrix4 data)
        {
            if (!uniformLocations_.ContainsKey(name))
                return;

            //GL.UseProgram(programHandle_);
            GL.UniformMatrix4(uniformLocations_[name], true, ref data);
        }

        public void SetVector2(string name, Vector2 data)
        {
            if (!uniformLocations_.ContainsKey(name))
                return;

            //GL.UseProgram(programHandle_);
            GL.Uniform2(uniformLocations_[name], data);
        }

        public void SetVector3(string name, Vector3 data)
        {
            if (!uniformLocations_.ContainsKey(name))
                return;

            //GL.UseProgram(programHandle_);
            GL.Uniform3(uniformLocations_[name], data);
        }

        public void SetVector4(string name, Vector4 data)
        {
            if (!uniformLocations_.ContainsKey(name))
                return;

            //GL.UseProgram(programHandle_);
            GL.Uniform4(uniformLocations_[name], data);
        }

//        public void SetBool(string name, bool data)
//        {
//            if (!uniformLocations_.ContainsKey(name))
//                return;
//
//            //GL.UseProgram(programHandle_);
//            GL.Uniform1(uniformLocations_[name], data ? 1 : 0);
//        }
    }
}
