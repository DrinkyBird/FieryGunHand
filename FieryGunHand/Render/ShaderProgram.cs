using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK;
using OpenTK.Graphics.OpenGL4;

namespace FieryGunHand.Render
{
    class ShaderProgram : IDisposable
    {
        private int program;
        private List<int> shaders = new List<int>();
        public bool Linked { get; private set; } = false;

        public ShaderProgram()
        {
            program = GL.CreateProgram();
        }

        public void Dispose()
        {
            GL.DeleteProgram(program);
        }

        public void Bind()
        {
            GL.UseProgram(program);
        }

        public void AddShaderFile(string path, ShaderType type)
        {
            string source = File.ReadAllText(path);
            AddShader(source, type);
        }

        public void AddShader(string source, ShaderType type)
        {
            if (Linked)
            {
                throw new Exception("Shader already linked!");
            }

            int shader = GL.CreateShader(type);
            GL.ShaderSource(shader, source);
            GL.CompileShader(shader);

            int status;
            GL.GetShader(shader, ShaderParameter.CompileStatus, out status);
            if (status == 0)
            {
                string error = GL.GetShaderInfoLog(shader);
                throw new Exception(error);
            }

            GL.AttachShader(program, shader);
            shaders.Add(shader);
        }

        public void Link()
        {
            if (Linked)
            {
                throw new Exception("Shader already linked!");
            }

            GL.LinkProgram(program);
            int status;
            GL.GetProgram(program, GetProgramParameterName.LinkStatus, out status);
            if (status == 0)
            {
                string error = GL.GetProgramInfoLog(program);
                throw new Exception(error);
            }

            foreach (var shader in shaders)
            {
                GL.DetachShader(program, shader);
                GL.DeleteShader(shader);
            }

            Linked = true;
        }

        private int GetUniformLocation(string name)
        {
            int loc = GL.GetUniformLocation(program, name);
            return loc;
        }

        public void SetUniform(string name, float value)
        {
            GL.Uniform1(GetUniformLocation(name), value);
        }

        public void SetUniform(string name, int value)
        {
            GL.Uniform1(GetUniformLocation(name), value);
        }

        public void SetUniform(string name, Vector2 value)
        {
            GL.Uniform2(GetUniformLocation(name), value);
        }

        public void SetUniform(string name, float x, float y)
        {
            GL.Uniform2(GetUniformLocation(name), x, y);
        }

        public void SetUniform(string name, int x, int y)
        {
            GL.Uniform2(GetUniformLocation(name), x, y);
        }

        public void SetUniform(string name, Vector3 value)
        {
            GL.Uniform3(GetUniformLocation(name), value);
        }

        public void SetUniform(string name, float x, float y, float z)
        {
            GL.Uniform3(GetUniformLocation(name), x, y, z);
        }

        public void SetUniform(string name, int x, int y, int z)
        {
            GL.Uniform3(GetUniformLocation(name), x, y, z);
        }

        public void SetUniform(string name, Vector4 value)
        {
            GL.Uniform4(GetUniformLocation(name), value);
        }

        public void SetUniform(string name, float x, float y, float z, float w)
        {
            GL.Uniform4(GetUniformLocation(name), x, y, z, w);
        }

        public void SetUniform(string name, int x, int y, int z, int w)
        {
            GL.Uniform4(GetUniformLocation(name), x, y, z, w);
        }

        public void SetUniform(string name, ref Matrix4 matrix, bool transpose = false)
        {
            GL.UniformMatrix4(GetUniformLocation(name), transpose, ref matrix);
        }
    }
}
