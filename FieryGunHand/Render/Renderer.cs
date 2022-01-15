using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using FieryGunHand.Map;
using FieryGunHand.Render;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Input;

namespace FieryGunHand
{
    public class Renderer : IDisposable
    {
        private struct RenderVertex
        {
            public float X, Y, Z, U, V, R, G, B, A;
        }

        private WadArchive archive;
        private GameWindow window;
        private Level level;

        private float lastFpsTime;
        public float delta { get; private set; }
        private uint fpsFrames;
        public uint fps { get; private set; }
        public uint drawCalls { get; private set; }
        public uint AlphaDraws { get; private set; }

        public bool EnableWallCull = false;

        // camera pos
        public float cx, cy, cz;

        private int flatsVbo, wallsVbo;
        private int flatsVao, wallsVao;

        private int testVao, testVbo;

        private ShaderProgram program = new ShaderProgram();
        private ShaderProgram programNoTexture = new ShaderProgram();

        private Frustum frustum = new Frustum();
        public bool DoFrustumUpdate = true;

        // Flat name -> OpenGL texture name (might be 0)
        private Dictionary<string, int> FlatCache = new Dictionary<string, int>();

        // TODO: turn these into arrays (of which the index is the index of the subsector/sidedef)
        //       ... and maybe turn these tuples into structs
        // Subsector -> (index, count, bb, flat)
        private Dictionary<SubSector, Tuple<int, int, BoundingBox?, string>[]> SubSectorIndices = new Dictionary<SubSector, Tuple<int, int, BoundingBox?, string>[]>();
        // Sidedef index -> (index, bb)
        //   (walls always have 4 verts)
        private Dictionary<int, Tuple<int, BoundingBox?, Texture>[]> SidedefIndices = new Dictionary<int, Tuple<int, BoundingBox?, Texture>[]>();

        // BB of the entire level
        private BoundingBox levelBoundingBox;

        private TextureManager textureManager;

        private Matrix4 projection, worldView;

        public Renderer(WadArchive archive, GameWindow window, Level level)
        {
            this.archive = archive;
            this.window = window;
            this.level = level;
            this.textureManager = new TextureManager(archive);

            program.AddShaderFile("Assets/world.vert", ShaderType.VertexShader);
            program.AddShaderFile("Assets/world.frag", ShaderType.FragmentShader);
            program.Link();

            programNoTexture.AddShaderFile("Assets/world.vert", ShaderType.VertexShader);
            programNoTexture.AddShaderFile("Assets/worldnotex.frag", ShaderType.FragmentShader);
            programNoTexture.Link();

            GenerateWorld();

            {
                float minx = float.MaxValue, maxx = float.MinValue;
                float miny = float.MaxValue, maxy = float.MinValue;
                float minz = float.MaxValue, maxz = float.MinValue;

                foreach (var v in level.vertexes)
                {
                    minx = Math.Min(minx, v.X);
                    maxx = Math.Max(maxx, v.X);
                    minz = Math.Min(minz, -v.Y);
                    maxz = Math.Max(maxz, -v.Y);
                }

                foreach (var sector in level.sectors)
                {
                    miny = Math.Min(miny, sector.FloorHeight);
                    maxy = Math.Max(maxy, sector.CeilingHeight);
                }

                cx = minx + ((maxx - minx) / 2);
                cy = miny + ((maxy - miny) / 2);
                cz = minz + ((maxz - minz) / 2);

                levelBoundingBox = new BoundingBox(minx, miny, minz, maxx, maxy, maxz);
            }
        }

        private void GenerateWorld()
        {
            List<RenderVertex> flats = new List<RenderVertex>();
            List<RenderVertex> walls = new List<RenderVertex>();

            for (int i = 0; i < level.sectors.Count; i++)
            {
                var sector = level.sectors[i];

                foreach (var subsector in level.SectorSubsectors[sector])
                {
                    BoundingBox? floorBB = null;
                    BoundingBox? ceilBB = null;

                    int floorStart = flats.Count;
                    flats.AddRange(GenerateSubsectorPolygon(subsector, true, out floorBB));
                    int floorCount = flats.Count - floorStart;

                    int ceilStart = -1, ceilCount = -1;
                    if (sector.CeilingTexture != "F_SKY1")
                    {
                        ceilStart = flats.Count;
                        flats.AddRange(GenerateSubsectorPolygon(subsector, false, out ceilBB));
                        ceilCount = flats.Count - ceilStart;
                    }

                    SubSectorIndices[subsector] = new[]
                    {
                        new Tuple<int, int, BoundingBox?, string>(floorStart, floorCount, floorBB, sector.FloorTexture),
                        new Tuple<int, int, BoundingBox?, string>(ceilStart, ceilCount, ceilBB, sector.CeilingTexture)
                    };
                }
            }

            for (int i = 0; i < level.linedefs.Count; i++)
            {
                var linedef = level.linedefs[i];

                if (linedef.FrontSidedef != -1)
                {
                    walls.AddRange(MakeWall(linedef.FrontSidedef, 0, linedef.StartVertex, linedef.EndVertex, walls.Count));
                    walls.AddRange(MakeWall(linedef.FrontSidedef, 1, linedef.StartVertex, linedef.EndVertex, walls.Count));
                    walls.AddRange(MakeWall(linedef.FrontSidedef, 2, linedef.StartVertex, linedef.EndVertex, walls.Count));
                }

                if (linedef.BackSidedef != -1)
                {
                    walls.AddRange(MakeWall(linedef.BackSidedef, 0, linedef.StartVertex, linedef.EndVertex, walls.Count));
                    walls.AddRange(MakeWall(linedef.BackSidedef, 1, linedef.StartVertex, linedef.EndVertex, walls.Count));
                    walls.AddRange(MakeWall(linedef.BackSidedef, 2, linedef.StartVertex, linedef.EndVertex, walls.Count));
                }
            }

            var flatsArray = flats.ToArray();
            var wallsArray = walls.ToArray();

            flatsVbo = GL.GenBuffer();
            flatsVao = GL.GenVertexArray();
            ObjectLabel(ObjectLabelIdentifier.VertexArray, flatsVao, "Flats VAO");
            ObjectLabel(ObjectLabelIdentifier.Buffer, flatsVbo, "Flats VBO");
            GL.BindVertexArray(flatsVao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, flatsVbo);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, Marshal.SizeOf(typeof(RenderVertex)), 0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, Marshal.SizeOf(typeof(RenderVertex)), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, Marshal.SizeOf(typeof(RenderVertex)), 5 * sizeof(float));
            GL.EnableVertexAttribArray(2);
            GL.BufferData(BufferTarget.ArrayBuffer, flatsArray.Length * Marshal.SizeOf(typeof(RenderVertex)), flatsArray, BufferUsageHint.StaticDraw);

            wallsVbo = GL.GenBuffer();
            wallsVao = GL.GenVertexArray();
            ObjectLabel(ObjectLabelIdentifier.VertexArray, wallsVao, "Walls VAO");
            ObjectLabel(ObjectLabelIdentifier.Buffer, wallsVbo, "Walls VBO");
            GL.BindVertexArray(wallsVao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, wallsVbo);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, Marshal.SizeOf(typeof(RenderVertex)), 0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, Marshal.SizeOf(typeof(RenderVertex)), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, Marshal.SizeOf(typeof(RenderVertex)), 5 * sizeof(float));
            GL.EnableVertexAttribArray(2);
            GL.BufferData(BufferTarget.ArrayBuffer, wallsArray.Length * Marshal.SizeOf(typeof(RenderVertex)), wallsArray, BufferUsageHint.StaticDraw);

            testVbo = GL.GenBuffer();
            testVao = GL.GenVertexArray();
            ObjectLabel(ObjectLabelIdentifier.VertexArray, testVao, "Test VAO");
            ObjectLabel(ObjectLabelIdentifier.Buffer, testVbo, "Test VBO");
            GL.BindVertexArray(testVao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, testVbo);
            GL.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, Marshal.SizeOf(typeof(RenderVertex)), 0);
            GL.EnableVertexAttribArray(0);
            GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, Marshal.SizeOf(typeof(RenderVertex)), 3 * sizeof(float));
            GL.EnableVertexAttribArray(1);
            GL.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, Marshal.SizeOf(typeof(RenderVertex)), 5 * sizeof(float));
            GL.EnableVertexAttribArray(2);
            GL.BufferData(BufferTarget.ArrayBuffer, Marshal.SizeOf(typeof(RenderVertex)), IntPtr.Zero, BufferUsageHint.DynamicDraw);
        }

        public static void ObjectLabel(ObjectLabelIdentifier id, int name, string label)
        {
            GL.ObjectLabel(id, name, label.Length, label);
        }

        public void Render()
        {
            var start = TimeUtil.GetTimeInMsF();
            drawCalls = 0;
            AlphaDraws = 0;

            // Prepare 

            GL.Viewport(0, 0, window.ClientSize.Width, window.ClientSize.Height);
            GL.ClearColor(0.0f, 0.0f, 0.0f, 1.0f);
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
            
            projection = Matrix4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(90.0f), (float)window.ClientSize.Width / window.ClientSize.Height, 0.01f, 100000.0f);
            worldView = Matrix4.LookAt(new Vector3(50f + cx, 80f + cy, 50f + cz), new Vector3(cx, 0.0f, cz), new Vector3(0.0f, 1.0f, 0.0f));
            
            program.Bind();
            program.SetUniform("u_projection", ref projection);
            program.SetUniform("u_modelView", ref worldView);
            programNoTexture.Bind();
            programNoTexture.SetUniform("u_projection", ref projection);
            programNoTexture.SetUniform("u_modelView", ref worldView);

            if (DoFrustumUpdate)
            {
                frustum.Update(ref projection, ref worldView);
            }
            
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
            GL.CullFace(CullFaceMode.Back);
            GL.FrontFace(FrontFaceDirection.Ccw);
            
            GL.Enable(EnableCap.Blend);
            GL.Enable(EnableCap.CullFace);
            GL.Enable(EnableCap.DepthTest);
            GL.LineWidth(1.0f);

            // Render floors and ceilings
            GL.BindVertexArray(flatsVao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, flatsVbo);

            for (int i = 0; i < level.sectors.Count; i++)
            {
                var sector = level.sectors[i];

                foreach (var subsector in level.SectorSubsectors[sector])
                {
                    foreach (var tuple in SubSectorIndices[subsector])
                    {
                        if (tuple.Item1 != -1 && tuple.Item2 != -1)
                        {
                            if (tuple.Item3 != null && frustum.BoundingBoxInFrustum((BoundingBox)tuple.Item3))
                            {
                                var index = tuple.Item1;
                                var count = tuple.Item2;
                                var tex = textureManager.LoadTexture(tuple.Item4);

                                if (tex == null)
                                {
                                    programNoTexture.Bind();
                                }
                                else
                                {
                                    program.Bind();
                                    tex.Bind();
                                }

                                GL.DrawArrays(PrimitiveType.Triangles, index, count);
                                drawCalls++;
                            }
                        }
                    }
                }
            }

            // Render walls

            programNoTexture.Bind();

            // TODO: some walls render inside out
            if (EnableWallCull)
            {
                GL.Enable(EnableCap.CullFace);
            }
            else
            {
                GL.Disable(EnableCap.CullFace);
            }

            GL.BindTexture(TextureTarget.Texture2D, 0);
            GL.BindVertexArray(wallsVao);
            GL.BindBuffer(BufferTarget.ArrayBuffer, wallsVbo);
            
            // Render sides without transparency first
            foreach (var linedef in level.linedefs)
            {
                RenderSidedef(linedef.FrontSidedef, false);
                RenderSidedef(linedef.BackSidedef, false);
            }

            // Render sides with transparency
            foreach (var linedef in level.linedefs)
            {
                RenderSidedef(linedef.FrontSidedef, true);
                RenderSidedef(linedef.BackSidedef, true);
            }

            // End

            GL.Flush();
            window.SwapBuffers();

            fpsFrames++;
            var end = TimeUtil.GetTimeInMsF();
            delta = end - start;
            if (end - lastFpsTime >= 1000.0f)
            {
                lastFpsTime = end;
                fps = fpsFrames;
                fpsFrames = 0;
            }
        }

        private void RenderSidedef(int idx, bool ifHasAlpha)
        {
            if (idx != -1)
            {
                foreach (var index in SidedefIndices[idx])
                {
                    if (index?.Item2 != null && frustum.BoundingBoxInFrustum((BoundingBox)index.Item2))
                    {
                        if (index?.Item3 == null)
                        {
                            programNoTexture.Bind();
                        }
                        else
                        {
                            if (index?.Item3.HasAlpha != ifHasAlpha)
                            {
                                continue;
                            }

                            program.Bind();
                            index?.Item3.Bind();

                            if ((bool)index?.Item3.HasAlpha)
                            {
                                AlphaDraws++;
                            }
                        }

                        GL.DrawArrays(PrimitiveType.TriangleFan, index.Item1, 4);
                        drawCalls++;
                    }
                }
            }
        }

        private List<RenderVertex> GenerateSubsectorPolygon(SubSector subSector, bool floor, out BoundingBox? bb)
        {
            List<RenderVertex> r = new List<RenderVertex>();

            RenderVertex v1, v2, v3;
            v1 = MakeSectorVertex(level.segments[subSector.FirstSegment + 0].Line.Start, subSector, floor);
            v2 = MakeSectorVertex(level.segments[subSector.FirstSegment + 0].Line.End, subSector, floor);

            for (int i = 1; i < subSector.SegmentCount - 1; i++)
            {
                v3 = MakeSectorVertex(level.segments[subSector.FirstSegment + i].Line.End, subSector, floor);

                // Make sure the vertices are in anti-clockwise order
                if (floor)
                {
                    r.Add(v3);
                    r.Add(v2);
                    r.Add(v1);
                }
                else
                {
                    r.Add(v1);
                    r.Add(v2);
                    r.Add(v3);
                }

                v2 = v3;
            }

            float minx = float.MaxValue, maxx = float.MinValue;
            float miny = float.MaxValue, maxy = float.MinValue;
            float minz = float.MaxValue, maxz = float.MinValue;

            foreach (var v in r)
            {
                minx = Math.Min(minx, v.X);
                maxx = Math.Max(maxx, v.X);
                miny = Math.Min(miny, v.Y);
                maxy = Math.Max(maxy, v.Y);
                minz = Math.Min(minz, v.Z);
                maxz = Math.Max(maxz, v.Z);
            }

            bb = new BoundingBox(minx, miny, minz, maxx, maxy, maxz);

            return r;
        }

        private RenderVertex MakeSectorVertex(Vertex mapVertex, SubSector subSector, bool floor)
        {
            var sector = level.SubsectorSector[subSector];
            float height = floor ? sector.FloorHeight : sector.CeilingHeight;
            float l = sector.LightLevel / 255.0f;
            Vector4 c = new Vector4(l, l, l, 1);
            string flat = floor ? sector.FloorTexture : sector.CeilingTexture;
            Texture tex = textureManager.LoadTexture(flat);
            if (tex == null)
            {
                c = SetColourToString(floor ? sector.FloorTexture : sector.CeilingTexture, l);
            }

            float txw = tex?.Width ?? 64.0f;
            float txh = tex?.Height ?? 64.0f;

            return new RenderVertex
            {
                X = mapVertex.X,
                Y = height,
                Z = -mapVertex.Y,
                U = mapVertex.X / txw,
                V = -mapVertex.Y / txh,
                R = c.X,
                G = c.Y,
                B = c.Z,
                A = 1.0f
            };
        }

        private List<RenderVertex> MakeWall(int sidedef, int wall, int sv, int ev, int listoffset)
        {
            List<RenderVertex> r = new List<RenderVertex>();
            var sd = level.sidedefs[sidedef];
            var ld = level.SidedefLinedef[sd][0];

            if (!SidedefIndices.ContainsKey(sidedef))
            {
                SidedefIndices[sidedef] = new Tuple<int, BoundingBox?, Texture>[3];
            }

            switch (wall)
            {
                // Upper
                case 0:
                {
                    if (level.SidedefOtherSide.ContainsKey(sidedef))
                    {
                        var mySector = level.sectors[sd.Sector];
                        var otherSector = level.sectors[level.sidedefs[level.SidedefOtherSide[sidedef]].Sector];
                        float l = mySector.LightLevel / 255.0f;
                        Vector4 c = new Vector4(l, l, l, 1.0f);
                        var tex = textureManager.LoadTexture(sd.UpperTexture);
                        if (tex == null)
                        {
                            c = SetColourToString(sd.UpperTexture, l);
                        }

                        float top = mySector.CeilingHeight;
                        float bottom = otherSector.CeilingHeight;

                        if (top > bottom)
                        {
                            float delta = top - bottom;
                            float x0 = level.vertexes[sv].X;
                            float x1 = level.vertexes[ev].X;
                            float y0 = top > bottom ? bottom : top;
                            float y1 = top > bottom ? top : bottom;
                            float z0 = -level.vertexes[sv].Y;
                            float z1 = -level.vertexes[ev].Y;
                            float u0 = 0.0f;
                            float u1 = 1.0f;
                            float v0 = 0.0f;
                            float v1 = 1.0f;

                            if (tex != null)
                            {
                                u0 = (float)sd.XOffset / (float)tex.Width;
                                u1 = (sd.XOffset + ld.Length) / (float)tex.Width;

                                if ((ld.Flags & Linedef.LinedefFlags.UpperUnpegged) ==
                                    Linedef.LinedefFlags.UpperUnpegged)
                                {
                                    v0 = (float)sd.YOffset / (float)tex.Height;
                                    v1 = (v0 + delta) / (float) tex.Height;
                                }
                                else
                                {
                                    v0 = 0 / (float)tex.Height;
                                    v1 = (0 + delta) / (float)tex.Height;
                                }
                            }

                            SidedefIndices[sidedef][wall] = new Tuple<int, BoundingBox?, Texture>(listoffset, new BoundingBox(x0, y0, z0, x1, y1, z1), tex);

                            r.Add(new RenderVertex() { X = x0, Y = y0, Z = z0, U = u0, V = v1, R = c.X, G = c.Y, B = c.Z, A = 1.0f });
                            r.Add(new RenderVertex() { X = x1, Y = y0, Z = z1, U = u1, V = v1, R = c.X, G = c.Y, B = c.Z, A = 1.0f });
                            r.Add(new RenderVertex() { X = x1, Y = y1, Z = z1, U = u1, V = v0, R = c.X, G = c.Y, B = c.Z, A = 1.0f });
                            r.Add(new RenderVertex() { X = x0, Y = y1, Z = z0, U = u0, V = v0, R = c.X, G = c.Y, B = c.Z, A = 1.0f });
                        }
                    }

                    break;
                }

                // Middle
                case 1:
                {
                    if (sd.MiddleTexture != "-")
                    {
                        var mySector = level.sectors[sd.Sector];
                        float l = mySector.LightLevel / 255.0f;
                        Vector4 c = new Vector4(l, l, l, 1.0f);
                        var tex = textureManager.LoadTexture(sd.MiddleTexture);
                        if (tex == null)
                        {
                            c = SetColourToString(sd.MiddleTexture, l);
                        }

                        float top = mySector.CeilingHeight;
                        float bottom = mySector.FloorHeight; 

                        if (top > bottom)
                        {
                            float x0 = level.vertexes[sv].X;
                            float x1 = level.vertexes[ev].X;
                            float y0 = bottom;
                            float y1 = top;
                            float z0 = -level.vertexes[sv].Y;
                            float z1 = -level.vertexes[ev].Y;
                            float u0 = 0.0f;
                            float u1 = 1.0f;
                            float v0 = 0.0f;
                            float v1 = 1.0f;

                            if (tex != null)
                            {
                                u0 = (float)sd.XOffset / (float)tex.Width;
                                u1 = (sd.XOffset + ld.Length) / (float)tex.Width;
                                v0 = (float)sd.YOffset / tex.Height;
                                v1 = v0 + (top - bottom) / tex.Height;
                            }

                            SidedefIndices[sidedef][wall] = new Tuple<int, BoundingBox?, Texture>(listoffset, new BoundingBox(x0, y0, z0, x1, y1, z1), tex);

                            r.Add(new RenderVertex() { X = x0, Y = y0, Z = z0, U = u0, V = v1, R = c.X, G = c.Y, B = c.Z, A = 1.0f });
                            r.Add(new RenderVertex() { X = x1, Y = y0, Z = z1, U = u1, V = v1, R = c.X, G = c.Y, B = c.Z, A = 1.0f });
                            r.Add(new RenderVertex() { X = x1, Y = y1, Z = z1, U = u1, V = v0, R = c.X, G = c.Y, B = c.Z, A = 1.0f });
                            r.Add(new RenderVertex() { X = x0, Y = y1, Z = z0, U = u0, V = v0, R = c.X, G = c.Y, B = c.Z, A = 1.0f });
                        }
                    }


                    break;
                }

                // Lower
                case 2:
                {
                    if (level.SidedefOtherSide.ContainsKey(sidedef))
                    {
                        var mySector = level.sectors[sd.Sector];
                        var otherSector = level.sectors[level.sidedefs[level.SidedefOtherSide[sidedef]].Sector];
                        float l = mySector.LightLevel / 255.0f;
                        Vector4 c = new Vector4(l, l, l, 1.0f);
                        var tex = textureManager.LoadTexture(sd.LowerTexture);
                        if (tex == null)
                        {
                            c = SetColourToString(sd.LowerTexture, l);
                        }

                        float top = Math.Max(otherSector.FloorHeight, mySector.FloorHeight);
                        float bottom = Math.Min(otherSector.FloorHeight, mySector.FloorHeight);

                        if (top > bottom)
                        {
                            float delta = top - bottom;
                            float x0 = level.vertexes[sv].X;
                            float x1 = level.vertexes[ev].X;
                            float y0 = top > bottom ? top : bottom;
                            float y1 = top > bottom ? bottom : top;
                            float z0 = -level.vertexes[sv].Y;
                            float z1 = -level.vertexes[ev].Y;
                            float u0 = 0.0f;
                            float u1 = 1.0f;
                            float v0 = 0.0f;
                            float v1 = 1.0f;

                            if (tex != null)
                            {
                                u0 = (float)sd.XOffset / (float)tex.Width;
                                u1 = (sd.XOffset + ld.Length) / (float)tex.Width;

                                if ((ld.Flags & Linedef.LinedefFlags.LowerUnpegged) == Linedef.LinedefFlags.LowerUnpegged)
                                {
                                    v0 = (float) (sd.YOffset + (mySector.CeilingHeight - otherSector.FloorHeight) + delta) / (float)tex.Height;
                                    v1 = (float) (sd.YOffset + (mySector.CeilingHeight - otherSector.FloorHeight)) / (float)tex.Height;
                                }
                                else
                                {
                                    v0 = (float) (sd.YOffset + delta) / (float)tex.Height;
                                    v1 = (float) (sd.YOffset) / (float)tex.Height;
                                }
                            }

                            SidedefIndices[sidedef][wall] = new Tuple<int, BoundingBox?, Texture>(listoffset, new BoundingBox(x0, y0, z0, x1, y1, z1), tex);

                            r.Add(new RenderVertex() { X = x0, Y = y0, Z = z0, U = u0, V = v1, R = c.X, G = c.Y, B = c.Z, A = 1.0f });
                            r.Add(new RenderVertex() { X = x0, Y = y1, Z = z0, U = u0, V = v0, R = c.X, G = c.Y, B = c.Z, A = 1.0f });
                            r.Add(new RenderVertex() { X = x1, Y = y1, Z = z1, U = u1, V = v0, R = c.X, G = c.Y, B = c.Z, A = 1.0f });
                            r.Add(new RenderVertex() { X = x1, Y = y0, Z = z1, U = u1, V = v1, R = c.X, G = c.Y, B = c.Z, A = 1.0f });
                        }
                    }

                    break;
                }
            }

            return r;
        }

        // Generates a colour from a string, multiplied by given light level
        private Vector4 SetColourToString(string s, float l)
        {
            int hc = s.GetHashCode();
            float r = ((hc >> 16) & 0xFF) / 255.0f;
            float g = ((hc >> 8) & 0xFF) / 255.0f;
            float b = (hc & 0xFF) / 255.0f;
            return new Vector4(r * l, g * l, b * l, 1.0f);
        }

        // Projects from world space to screen space.
        public Vector3 Project(Vector3 pos)
        {
            return Vector3.Project(pos, 0.0f, 0.0f, window.Width, window.Height, -1.0f, 1.0f, projection * worldView);
        }

        // Projects from screen space to world space.
        public Vector3 Unproject(Vector3 pos)
        {
            int[] vp = new int[4];
            GL.GetInteger(GetIndexedPName.Viewport, 0, vp);
            return Vector3.Unproject(pos, vp[0], vp[1], vp[2], vp[3], -1.0f, 1.0f, Matrix4.Invert(projection) * Matrix4.Invert(worldView));
        }

        public void Dispose()
        {
            program.Dispose();
            programNoTexture.Dispose();
            GL.DeleteVertexArray(flatsVao);
            GL.DeleteBuffer(flatsVbo);
            GL.DeleteVertexArray(wallsVao);
            GL.DeleteBuffer(wallsVbo);
            textureManager.Dispose();
        }
    }
}
