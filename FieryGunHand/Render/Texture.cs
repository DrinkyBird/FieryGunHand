using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.OpenGL4;

namespace FieryGunHand.Render
{
    class Texture : IDisposable
    {
        public int Name { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }
        public bool HasAlpha { get; private set; }

        public Texture(int id, int width, int height, bool hasAlpha = false)
        {
            Name = id;
            Width = width;
            Height = height;
            HasAlpha = hasAlpha;
        }

        public void Bind()
        {
            GL.BindTexture(TextureTarget.Texture2D, Name);
        }

        public void Dispose()
        {
            GL.DeleteTexture(Name);
        }
    }
}
