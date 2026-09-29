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
            // GL objects created here - on any error they are deleted in 'finally' (the program only if it was not stored)
            int vertexShader    = 0;
            int fragmentShader  = 0;
            int program         = 0;

            try
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();

                ReleaseProgram();
                uniformLocations_ = new Dictionary<string, int>();

                // Compile vertex shader
                vertexShader = GL.CreateShader(ShaderType.VertexShader);
                GL.ShaderSource(vertexShader, shaderSourceVert);
                bool success = CompileShader(vertexShader);
                if (!success)
                {
                    AppendError(sbErrors, GL.GetShaderInfoLog(vertexShader), "Vertex shader compilation failed.");
                    return false;
                }

                // Compile fragment shader
                fragmentShader = GL.CreateShader(ShaderType.FragmentShader);
                GL.ShaderSource(fragmentShader, shaderSourceFrag);
                success = CompileShader(fragmentShader);
                if (!success)
                {
                    AppendError(sbErrors, GL.GetShaderInfoLog(fragmentShader), "Fragment shader compilation failed.");
                    return false;
                }

                // Merge vertex and fragment shaders into single program:

                // Create empty program
                program = GL.CreateProgram();

                // Attach shaders to program
                GL.AttachShader(program, vertexShader);
                GL.AttachShader(program, fragmentShader);

                // Link program
                GL.LinkProgram(program);
                GL.GetProgram(program, GetProgramParameterName.LinkStatus, out var code);

                // Release resources (shaders are not needed after linking)
                GL.DetachShader(program, vertexShader);
                GL.DetachShader(program, fragmentShader);

                if (code != (int)All.True)
                {
                    AppendError(sbErrors, GL.GetProgramInfoLog(program), "Shader program linking failed.");
                    return false;
                }

                // TODO: display compiled shader assembly or intermediate code...
                // https://computergraphics.stackexchange.com/questions/7594/how-to-get-assembly-code-from-glsl-shader
                //int buffSize = 1024*1024;
                ////IntPtr buffer = new IntPtr(
                //
                //GL.GetProgramBinary(programHandle_, ...


                // Store uniforms locations
                GL.GetProgram(program, GetProgramParameterName.ActiveUniforms, out var numberOfUniforms);

                for (int i = 0; i < numberOfUniforms; i++)
                {
                    int uniformSize = 0;
                    ActiveUniformType uniformType = ActiveUniformType.Float;

                    // Get the name
                    string key = GL.GetActiveUniform(program, i, out uniformSize, out uniformType);

                    // Get the location
                    int location = GL.GetUniformLocation(program, key);

                    // Add to the dictionary.
                    uniformLocations_.Add(key, location);
                }

                // success - the program is owned by this object from now on
                programHandle_  = program;
                program         = 0;

                Debug.WriteLine("Shader for '{0}' compiled in {1} ms. Uniforms: {2}", shaderPassName, (int)sw.Elapsed.TotalMilliseconds, numberOfUniforms);
            }
            catch(Exception ex)
            {
                sbErrors.Append(ex.ToString());
                uniformLocations_ = new Dictionary<string, int>();
                return false;
            }
            finally
            {
                if (program != 0)
                    GL.DeleteProgram(program);
                if (fragmentShader != 0)
                    GL.DeleteShader(fragmentShader);
                if (vertexShader != 0)
                    GL.DeleteShader(vertexShader);
            }

            return true;
        }

        public bool IsValid()
        {
            return programHandle_ != 0;
        }

        // GL info log can be empty - the error text must not be, because an empty error list means "able to render"
        private static void AppendError(StringBuilder sbErrors, string infoLog, string defaultMessage)
        {
            string error = String.IsNullOrWhiteSpace(infoLog) ? defaultMessage : infoLog;
            sbErrors.Append(error);
            Console.WriteLine(error);
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
