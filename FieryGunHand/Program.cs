using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

namespace FieryGunHand
{
    public class Program
    {
        public static Program Instance { get; private set; }

        public bool Running = true;
        
        public GameWindow Window { get; private set; }
        public Renderer Renderer { get; private set; }

        private WadArchive archive;
        private Level level;

        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Program program = new Program();
            program.Run();
        }

        private Program()
        {
            Instance = this;
            Window = new GameWindow(1280, 720, new GraphicsMode(32, 24, 0, 0), "test", GameWindowFlags.Default, DisplayDevice.Default, 3, 3, GraphicsContextFlags.Debug);

            archive = new WadArchive(@"doomgl.wad");
            //archive = new WadArchive(@"C:\Users\Sean\Desktop\jpx01gl.wad");
            //archive = new WadArchive(@"C:\Users\Sean\Desktop\testg.wad");
            var map = archive.Maps[0];
            level = new Level(archive, map.StartLump, map.Format, map.GlNodesLabel);
            Renderer = new Renderer(archive, Window, level);
            Window.VSync = VSyncMode.Off;

            Window.RenderFrame += WindowOnRenderFrame;
            Window.KeyDown += WindowOnKeyDown;

            Console.WriteLine($"GL_VENDOR: {GL.GetString(StringName.Vendor)}");
            Console.WriteLine($"GL_RENDERER: {GL.GetString(StringName.Renderer)}");
            Console.WriteLine($"GL_VERSION: {GL.GetString(StringName.Version)}");
            GL.DebugMessageCallback((source, type, id, severity, length, message, param) =>
            {
                var msg = Marshal.PtrToStringAnsi(message);
                Console.WriteLine("GL error (" + source + ") (" + severity + "): " + msg);
            }, IntPtr.Zero);
        }

        private void WindowOnKeyDown(object sender, KeyboardKeyEventArgs e)
        {
            if (e.Key == Key.F3)
            {
                Renderer.DoFrustumUpdate = !Renderer.DoFrustumUpdate;
            }

            if (e.Key == Key.F2)
            {
                OpenMapForm form = new OpenMapForm(archive);
                DialogResult result = form.ShowDialog();
                if (result == DialogResult.OK && form.SelectedMap != null)
                {
                    Renderer.Dispose();
                    
                    var map = (WadArchive.Map)form.SelectedMap;
                    level = new Level(archive, map.StartLump, map.Format, map.GlNodesLabel);
                    Renderer = new Renderer(archive, Window, level);
                }
            }

            if (e.Key == Key.F4)
            {
                Renderer.EnableWallCull = !Renderer.EnableWallCull;
            }

            if (e.Key == Key.F1)
            {
                string s = "";
                s += "WASD: move around\n";
                s += "R: move up\n";
                s += "F: move down\n";
                s += "F1: show this dialog\n";
                s += "F2: change map\n";
                s += "F3: lock/unlock culling frustum\n";
                s += "F4: toggle wall backface culling\n";
                MessageBox.Show(s, "Halp", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void WindowOnRenderFrame(object sender, FrameEventArgs e)
        {
            float n = 1.0f * Renderer.delta;
            if (Keyboard.GetState().IsKeyDown(Key.ShiftLeft)) n *= 2.5f;
            if (Keyboard.GetState().IsKeyDown(Key.W))
            {
                Renderer.cz -= n;
            }
            if (Keyboard.GetState().IsKeyDown(Key.S))
            {
                Renderer.cz += n;
            }
            if (Keyboard.GetState().IsKeyDown(Key.A))
            {
                Renderer.cx -= n;
            }
            if (Keyboard.GetState().IsKeyDown(Key.D))
            {
                Renderer.cx += n;
            }
            if (Keyboard.GetState().IsKeyDown(Key.R))
            {
                Renderer.cx += n;
                Renderer.cy += n;
                Renderer.cz += n;
            }
            if (Keyboard.GetState().IsKeyDown(Key.F))
            {
                Renderer.cx -= n;
                Renderer.cy -= n;
                Renderer.cz -= n;
            }
            Renderer.Render();
            Window.Title = $"{Renderer.fps} FPS / {Renderer.delta} ms / {Renderer.drawCalls} draw calls ({Renderer.AlphaDraws} alpha) / {Renderer.cx}, {Renderer.cy}, {Renderer.cz}";
        }

        private void Run()
        {
            Window.Run();
            Renderer.Dispose();
        }
    }
}
