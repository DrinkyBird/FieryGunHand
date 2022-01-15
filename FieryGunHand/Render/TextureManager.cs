using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenTK.Graphics.OpenGL4;
using Buffer = OpenTK.Graphics.OpenGL4.Buffer;
using PixelFormat = OpenTK.Graphics.OpenGL4.PixelFormat;

namespace FieryGunHand.Render
{
    class TextureManager : IDisposable
    {
        private Dictionary<string, Texture> cache = new Dictionary<string, Texture>();
        private WadArchive archive;
        private byte[] palette;
        private Color[] gdiPalette = new Color[256];
        private string[] pnames;
        private Dictionary<string, DoomTexture> textures = new Dictionary<string, DoomTexture>();
        private Dictionary<int, Tuple<Bitmap, Bitmap>> patchCache = new Dictionary<int, Tuple<Bitmap, Bitmap>>();

        public TextureManager(WadArchive archive)
        {
            this.archive = archive;
            LoadPalette();
            LoadPnames();

            int index;
            if (archive.FindLump("TEXTURE1", out index))
            {
                ReadTextureLump(index);
            }
            if (archive.FindLump("TEXTURE2", out index))
            {
                ReadTextureLump(index);
            }
        }

        private void LoadPalette()
        {
            palette = archive.ReadLump("PLAYPAL");
            for (int i = 0; i < 256; i++)
            {
                gdiPalette[i] = Color.FromArgb(
                    255,
                    palette[i * 3 + 0],
                    palette[i * 3 + 1],
                    palette[i * 3 + 2]
                );
            }
        }

        private void SetImagePalette(Image image)
        {
            var p = image.Palette;
            for (int i = 0; i < gdiPalette.Length; i++)
            {
                p.Entries[i] = gdiPalette[i];
            }

            image.Palette = p;
        }

        private void LoadPnames()
        {
            using (var stream = archive.OpenLump("PNAMES"))
            using (var reader = new BinaryReader(stream))
            {
                pnames = new string[reader.ReadInt32()];
                for (int i = 0; i < pnames.Length; i++)
                {
                    pnames[i] = WadArchive.ReadLumpName(reader);
                }
            }
        }

        private void ReadTextureLump(int index)
        {
            using (var stream = archive.OpenLump(index))
            using (var reader = new BinaryReader(stream))
            {
                int count = reader.ReadInt32();
                int[] offsets = new int[count];
                for (int i = 0; i < count; i++)
                {
                    offsets[i] = reader.ReadInt32();
                }

                for (int i = 0; i < count; i++)
                {
                    stream.Seek(offsets[i], SeekOrigin.Begin);

                    DoomTexture texture = new DoomTexture()
                    {
                        Name = WadArchive.ReadLumpName(reader),
                        Masked = reader.ReadInt32(), // Unused
                        Width = reader.ReadInt16(),
                        Height = reader.ReadInt16(),
                        ColumnDirectory = reader.ReadInt32(), // Unused
                        Patches = new DoomTexture.Patch[reader.ReadInt16()]
                    };

                    for (int j = 0; j < texture.Patches.Length; j++)
                    {
                        texture.Patches[j] = new DoomTexture.Patch()
                        {
                            OriginX = reader.ReadInt16(),
                            OriginY = reader.ReadInt16(),
                            PatchIndex = reader.ReadInt16(),
                            StepDir = reader.ReadInt16(), // Unused
                            ColourMap = reader.ReadInt16(), // Unused
                        };
                    }

                    textures[texture.Name] = texture;
                }
            }
        }

        public Texture LoadTexture(string name)
        {
            name = name.ToUpper();
            if (cache.ContainsKey(name))
            {
                return cache[name];
            }

            Texture t = null;

            if (textures.ContainsKey(name))
            {
                t = LoadDoomTexture(name);
            }

            int index;
            if (archive.FindLump(name, out index))
            {
                var lump = archive.Lumps[index];

                if (t == null)
                {
                    using (var stream = archive.OpenLump(index))
                    {
                        try
                        {
                            var bmp = new Bitmap(stream);
                            t = LoadBitmap(bmp, $"Bitmap: {name}");
                            bmp.Dispose();
                        }
                        catch (Exception e)
                        {
                            // Don't care, fall through.
                        }
                    }
                }

                if (t == null && lump.Size == 64 * 64)
                {
                    t = LoadFlat(index);
                }

                if (t == null)
                {
                    t = LoadDoomPicture(index, $"DoomPicture: {name}");
                }
            }

            if (t == null)
            {
                Console.WriteLine($"No suitable format for {name}");
            }

            cache[name] = t;
            return t;
        }

        private Texture LoadFlat(int index)
        {
            var lump = archive.Lumps[index];

            byte[] raw = archive.ReadLump(lump.Name);

            byte[] pixels = new byte[raw.Length * 3];
            for (int i = 0; i < raw.Length; i++)
            {
                pixels[i * 3 + 0] = palette[raw[i] * 3 + 0];
                pixels[i * 3 + 1] = palette[raw[i] * 3 + 1];
                pixels[i * 3 + 2] = palette[raw[i] * 3 + 2];
            }

            int texture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, texture);
            GL.TextureParameter(texture, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TextureParameter(texture, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TextureParameter(texture, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TextureParameter(texture, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgb, 64, 64, 0, PixelFormat.Rgb, PixelType.UnsignedByte, pixels);
            Renderer.ObjectLabel(ObjectLabelIdentifier.Texture, texture, $"Flat: {lump.Name}");

            return new Texture(texture, 64, 64);
        }

        private Texture LoadBitmap(Bitmap bmp, string label = null)
        {
            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            var data = bmp.LockBits(rect, ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            int texture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, texture);
            GL.TextureParameter(texture, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TextureParameter(texture, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TextureParameter(texture, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TextureParameter(texture, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bmp.Width, bmp.Height, 0, PixelFormat.Bgra, PixelType.UnsignedByte, data.Scan0);
            if (label != null) Renderer.ObjectLabel(ObjectLabelIdentifier.Texture, texture, label);
            bmp.UnlockBits(data);

            return new Texture(texture, bmp.Width, bmp.Height);
        }

        private Texture LoadMaskedBitmap(Bitmap bmp, Bitmap mask, string label = null)
        {
            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            var data = bmp.LockBits(rect, ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            var maskdata = mask.LockBits(rect, ImageLockMode.ReadOnly, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);

            bool hasAlpha = false;
            byte[] buf = new byte[bmp.Width * bmp.Height * 4];
            unsafe {
                for (int y = 0; y < bmp.Height; y++)
                {
                    byte* prow = (byte *)data.Scan0 + y * data.Stride;
                    byte* mrow = (byte *)maskdata.Scan0 + y * maskdata.Stride;
                    for (int x = 0; x < bmp.Width; x++)
                    {
                        buf[(x + y * bmp.Width) * 4 + 0] = prow[x * 3 + 2];
                        buf[(x + y * bmp.Width) * 4 + 1] = prow[x * 3 + 1];
                        buf[(x + y * bmp.Width) * 4 + 2] = prow[x * 3 + 0];
                        buf[(x + y * bmp.Width) * 4 + 3] = mrow[x];

                        if (mrow[x] != 255)
                        {
                            hasAlpha = true;
                        }
                    }
                }
            }

            int texture = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture2D, texture);
            GL.TextureParameter(texture, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
            GL.TextureParameter(texture, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
            GL.TextureParameter(texture, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
            GL.TextureParameter(texture, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);
            GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, bmp.Width, bmp.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, buf);
            if (label != null) Renderer.ObjectLabel(ObjectLabelIdentifier.Texture, texture, label);
            bmp.UnlockBits(data);

            return new Texture(texture, bmp.Width, bmp.Height, hasAlpha);
        }

        private Bitmap ReadDoomPicture(int index, out Bitmap transparencyMask)
        {
            using (var stream = archive.OpenLump(index))
            using (var reader = new BinaryReader(stream))
            {
                int width = reader.ReadInt16();
                int height = reader.ReadInt16();
                int left = reader.ReadInt16();
                int top = reader.ReadInt16();

                byte[] indices = new byte[width * height];
                byte[] mask = new byte[width * height];

                int[] offsets = new int[width];
                for (int i = 0; i < width; i++)
                {
                    offsets[i] = reader.ReadInt32();
                }

                for (int i = 0; i < width; i++)
                {
                    stream.Seek(offsets[i], SeekOrigin.Begin);
                    int rowstart = 0;

                    while ((rowstart = reader.ReadByte()) != 255)
                    {
                        int pixelCount = reader.ReadByte();
                        stream.Seek(1, SeekOrigin.Current);

                        for (int j = 0; j < pixelCount; j++)
                        {
                            indices[i + (j + rowstart) * width] = reader.ReadByte();
                            mask[i + (j + rowstart) * width] = 0xFF;
                        }

                        stream.Seek(1, SeekOrigin.Current);
                    }
                }

                var bmp = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
                SetImagePalette(bmp);
                var rect = new Rectangle(0, 0, width, height);
                var data = bmp.LockBits(rect, ImageLockMode.WriteOnly,
                    System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
                for (int i = 0; i < height; i++)
                {
                    Marshal.Copy(indices, i * width, data.Scan0 + (data.Stride * i), width);
                }
                bmp.UnlockBits(data);

                transparencyMask = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
                data = transparencyMask.LockBits(rect, ImageLockMode.WriteOnly,
                    System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
                for (int y = 0; y < height; y++)
                {
                    unsafe
                    {
                        byte* row = (byte *)data.Scan0 + y * data.Stride;
                        for (int x = 0; x < width; x++)
                        {
                            row[x] = mask[x + y * width];
                        }
                    }
                }
                transparencyMask.UnlockBits(data);

                return bmp;
            }

            return null;
        }

        private Texture LoadDoomPicture(int index, string label = null)
        {
            Bitmap transparencyMask;
            var bmp = ReadDoomPicture(index, out transparencyMask);
            var tex = LoadMaskedBitmap(bmp, transparencyMask, label);
            bmp.Dispose();
            return tex;
        }

        private Texture LoadDoomTexture(string name)
        {
            if (!textures.ContainsKey(name))
            {
                return null;
            }

            var def = textures[name];
            for (int i = 0; i < def.Patches.Length; i++)
            {
                int pname = def.Patches[i].PatchIndex;

                if (!patchCache.ContainsKey(pname))
                {
                    int index;
                    if (!archive.FindLump(pnames[pname], out index))
                    {
                        return null;
                    }

                    Bitmap transparencyMask;
                    var bmp = ReadDoomPicture(index, out transparencyMask);
                    patchCache[pname] = new Tuple<Bitmap, Bitmap>(bmp, transparencyMask);
                }
            }

            Bitmap bitmap = new Bitmap(def.Width, def.Height, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
            Bitmap mask = new Bitmap(def.Width, def.Height, System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
            var write = bitmap.LockBits(new Rectangle(0, 0, def.Width, def.Height), ImageLockMode.WriteOnly,
                System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
            var maskWrite = mask.LockBits(new Rectangle(0, 0, def.Width, def.Height), ImageLockMode.WriteOnly,
                System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
            SetImagePalette(bitmap);

            // Manually blit each patch into the final texture
            // Used to use Graphics.DrawImage, but that required converting each patch to 24 bpp first.
            // This does the entire thing as palette indices - faster, but more code.
            for (int i = 0; i < def.Patches.Length; i++)
            {
                var patch = def.Patches[i];
                var image = patchCache[patch.PatchIndex].Item1;
                var patchMask = patchCache[patch.PatchIndex].Item2;
                var read = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format8bppIndexed);
                var maskRead = patchMask.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly,
                    System.Drawing.Imaging.PixelFormat.Format8bppIndexed);

                unsafe
                {
                    for (int y = 0; y < image.Height; y++)
                    {
                        int ay = patch.OriginY + y;
                        byte* sourceLine = (byte*)read.Scan0 + (y * read.Stride);
                        byte* destLine = (byte*)write.Scan0 + (ay * write.Stride);
                        byte* maskSourceLine = (byte*)maskRead.Scan0 + (y * maskRead.Stride);
                        byte* maskDestLine = (byte*)maskWrite.Scan0 + (ay * maskWrite.Stride);
                        for (int x = 0; x < image.Width; x++)
                        {
                            int ax = patch.OriginX + x;

                            if (ax >= 0 && ax < write.Width && ay >= 0 && ay < write.Height)
                            {
                                destLine[ax] = sourceLine[x];
                                maskDestLine[ax] = maskSourceLine[x];
                            }
                        }
                    }
                }

                patchMask.UnlockBits(read);
                image.UnlockBits(read);
            }

            mask.UnlockBits(write);
            bitmap.UnlockBits(write);

            var tex = LoadMaskedBitmap(bitmap, mask, $"DoomTexture: {name}");
            bitmap.Dispose();

            return tex;
        }

        public void Dispose()
        {
            foreach (var texture in cache)
            {
                texture.Value?.Dispose();
            }

            cache.Clear();
        }
    }
}
