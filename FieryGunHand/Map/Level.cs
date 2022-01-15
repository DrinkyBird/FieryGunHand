using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FieryGunHand.Map;

namespace FieryGunHand
{
    public class Level
    {
        private const ushort VERT_IS_GL = (1 << 15);

        private WadArchive archive;
        private string name;
        private int label;
        private WadArchive.MapFormat format;
        private int gllabel;

        public List<Sector> sectors = new List<Sector>();
        public List<Vertex> vertexes = new List<Vertex>();
        public int firstGlVertex = -1;
        public List<Sidedef> sidedefs = new List<Sidedef>();
        public List<Linedef> linedefs = new List<Linedef>();
        public List<Segment> segments = new List<Segment>();
        public List<SubSector> subsectors = new List<SubSector>();
        public List<Node> nodes = new List<Node>();

        public Dictionary<Sector, List<Linedef>> SectorLines = new Dictionary<Sector, List<Linedef>>();
        public Dictionary<Sector, List<Sidedef>> SectorSides = new Dictionary<Sector, List<Sidedef>>();
        public Dictionary<Sidedef, List<Linedef>> SidedefLinedef = new Dictionary<Sidedef, List<Linedef>>();
        public Dictionary<Segment, Sidedef> SegmentSidedef = new Dictionary<Segment, Sidedef>();
        public Dictionary<SubSector, Sector> SubsectorSector = new Dictionary<SubSector, Sector>();
        public Dictionary<Sector, List<SubSector>> SectorSubsectors = new Dictionary<Sector, List<SubSector>>();
        public Dictionary<SubSector, Segment[]> SubsectorSegments = new Dictionary<SubSector, Segment[]>();
        public Dictionary<int, int> SidedefOtherSide = new Dictionary<int, int>();

        public Level(WadArchive archive, int lump, WadArchive.MapFormat format, int gllabel)
        {
            this.archive = archive;
            this.label = lump;
            this.name = archive.Lumps[label].Name;
            this.format = format;
            this.gllabel = gllabel;
            
            ReadSectors();
            ReadVertexes();
            ReadGlVertexes();
            ReadSidedef();
            ReadLinedef();
            ReadGlSegments();
            ReadSubSectors();
            ReadNodes();
        }

        private void ReadSectors()
        {
            using (Stream stream = archive.OpenLump(label + WadArchive.ML_SECTORS))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                while (stream.Position < stream.Length)
                {
                    var sector = new Sector
                    {
                        FloorHeight = reader.ReadInt16(),
                        CeilingHeight = reader.ReadInt16(),
                        FloorTexture = WadArchive.ReadLumpName(reader),
                        CeilingTexture = WadArchive.ReadLumpName(reader),
                        LightLevel = reader.ReadInt16(),
                        Special = reader.ReadInt16(),
                        Tag = reader.ReadInt16()
                    };

                    SectorLines[sector] = new List<Linedef>();
                    SectorSides[sector] = new List<Sidedef>();
                    SectorSubsectors[sector] = new List<SubSector>();

                    sectors.Add(sector);
                }
            }
        }

        private void ReadVertexes()
        {
            using (Stream stream = archive.OpenLump(label + WadArchive.ML_VERTEXES))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                while (stream.Position < stream.Length)
                {
                    vertexes.Add(new Vertex(reader.ReadInt16(), reader.ReadInt16()));
                }
            }
        }

        private void ReadGlVertexes()
        {
            using (Stream stream = archive.OpenLump(gllabel + WadArchive.GL_VERT))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                stream.Position += 4;
                while (stream.Position < stream.Length)
                {
                    float x0 = reader.ReadInt16();
                    float x1 = reader.ReadInt16();
                    float x = x1 + x0 / 65536.0f;
                    float y0 = reader.ReadInt16();
                    float y1 = reader.ReadInt16();
                    float y = y1 + y0 / 65536.0f;
                    if (firstGlVertex == -1) firstGlVertex = vertexes.Count;
                    vertexes.Add(new Vertex(x, y));
                }
            }
        }

        private void ReadSidedef()
        {
            using (Stream stream = archive.OpenLump(label + WadArchive.ML_SIDEDEFS))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                while (stream.Position < stream.Length)
                {
                    var sidedef = new Sidedef
                    {
                        XOffset = reader.ReadInt16(),
                        YOffset = reader.ReadInt16(),
                        UpperTexture = WadArchive.ReadLumpName(reader),
                        LowerTexture = WadArchive.ReadLumpName(reader),
                        MiddleTexture = WadArchive.ReadLumpName(reader),
                        Sector = reader.ReadInt16()
                    };

                    sidedefs.Add(sidedef);

                    SectorSides[sectors[sidedef.Sector]].Add(sidedef);
                    SidedefLinedef[sidedef] = new List<Linedef>();
                }
            }
        }

        private void ReadLinedef()
        {
            using (Stream stream = archive.OpenLump(label + WadArchive.ML_LINEDEFS))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                while (stream.Position < stream.Length)
                {
                    var linedef = new Linedef();

                    if (format == WadArchive.MapFormat.Doom)
                    {
                        linedef = new Linedef
                        {
                            StartVertex = reader.ReadInt16(),
                            EndVertex = reader.ReadInt16(),
                            Flags = (Linedef.LinedefFlags)reader.ReadInt16(),
                            Special = reader.ReadInt16(),
                            SectorTag = reader.ReadInt16(),
                            FrontSidedef = reader.ReadInt16(),
                            BackSidedef = reader.ReadInt16()
                        };
                    }
                    else
                    {
                        linedef = new Linedef();
                        linedef.StartVertex = reader.ReadInt16();
                        linedef.EndVertex = reader.ReadInt16();
                        linedef.Flags = (Linedef.LinedefFlags)reader.ReadInt16();
                        linedef.Special = reader.ReadByte();
                        reader.ReadByte();
                        reader.ReadByte();
                        reader.ReadByte();
                        reader.ReadByte();
                        reader.ReadByte();
                        linedef.FrontSidedef = reader.ReadInt16();
                        linedef.BackSidedef = reader.ReadInt16();
                    }

                    float dx = vertexes[linedef.EndVertex].X - vertexes[linedef.StartVertex].X;
                    float dy = vertexes[linedef.EndVertex].Y - vertexes[linedef.StartVertex].Y;
                    linedef.Length = (float)Math.Sqrt(dx * dx + dy * dy);

                    linedefs.Add(linedef);

                    if (linedef.FrontSidedef != -1)
                    {
                        SectorLines[sectors[sidedefs[linedef.FrontSidedef].Sector]].Add(linedef);
                        SidedefLinedef[sidedefs[linedef.FrontSidedef]].Add(linedef);

                        if (linedef.BackSidedef != -1)
                        {
                            SidedefOtherSide[linedef.BackSidedef] = linedef.FrontSidedef;
                        }
                    }

                    if (linedef.BackSidedef != -1)
                    {
                        SectorLines[sectors[sidedefs[linedef.BackSidedef].Sector]].Add(linedef);
                        SidedefLinedef[sidedefs[linedef.BackSidedef]].Add(linedef);

                        if (linedef.FrontSidedef != -1)
                        {
                            SidedefOtherSide[linedef.FrontSidedef] = linedef.BackSidedef;
                        }
                    }
                }
            }
        }

        private void ReadSegments()
        {
            using (Stream stream = archive.OpenLump(label + WadArchive.ML_SEGS))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                while (stream.Position < stream.Length)
                {
                    var segment = new Segment()
                    {
                        Line = new Line(
                            vertexes[reader.ReadInt16()],
                            vertexes[reader.ReadInt16()]
                        ),
                        Angle = reader.ReadInt16(),
                        Linedef = reader.ReadInt16(),
                        Direction = reader.ReadInt16(),
                        Offset = reader.ReadInt16()
                    };

                    segments.Add(segment);

                    if (segment.Linedef != -1)
                    {
                        var linedef = linedefs[segment.Linedef];

                        if (segment.Direction == 0 && linedef.FrontSidedef != -1)
                        {
                            SegmentSidedef[segment] = sidedefs[linedef.FrontSidedef];
                        }
                        else if (segment.Direction == 1 && linedef.BackSidedef != -1)
                        {
                            SegmentSidedef[segment] = sidedefs[linedef.BackSidedef];
                        }
                    }
                }
            }
        }

        private int ReadGlVertexIndex(BinaryReader reader)
        {
            short i = reader.ReadInt16();
            if ((i & VERT_IS_GL) == VERT_IS_GL)
            {
                i = (short)((i & ~VERT_IS_GL) + (short)firstGlVertex);
            }

            return (int)i;
        }

        private void ReadGlSegments()
        {
            using (Stream stream = archive.OpenLump(gllabel + WadArchive.GL_SEGS))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                while (stream.Position < stream.Length)
                {
                    var segment = new Segment()
                    {
                        Line = new Line(
                            vertexes[ReadGlVertexIndex(reader)],
                            vertexes[ReadGlVertexIndex(reader)]
                        ),
                        Linedef = reader.ReadInt16(),
                        Direction = reader.ReadInt16()
                    };

                    reader.ReadInt16();

                    segments.Add(segment);

                    if (segment.Linedef != -1)
                    {
                        var linedef = linedefs[segment.Linedef];

                        if (segment.Direction == 0 && linedef.FrontSidedef != -1)
                        {
                            SegmentSidedef[segment] = sidedefs[linedef.FrontSidedef];
                        }
                        else if (segment.Direction == 1 && linedef.BackSidedef != -1)
                        {
                            SegmentSidedef[segment] = sidedefs[linedef.BackSidedef];
                        }
                    }
                }
            }
        }

        private void ReadSubSectors()
        {
            using (Stream stream = archive.OpenLump(gllabel + WadArchive.GL_SSECT))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                while (stream.Position < stream.Length)
                {
                    var subsector = new SubSector()
                    {
                        SegmentCount = reader.ReadInt16(),
                        FirstSegment = reader.ReadInt16()
                    };

                    subsectors.Add(subsector);
                    SubsectorSegments[subsector] = new Segment[subsector.SegmentCount];
                    
                    for (int i = 0; i < subsector.SegmentCount; i++)
                    {
                        var seg = segments[subsector.FirstSegment + i];
                        SubsectorSegments[subsector][i] = seg;

                        if (SegmentSidedef.ContainsKey(seg))
                        {
                            var sector = sectors[SegmentSidedef[seg].Sector];
                            SubsectorSector[subsector] = sector;
                            SectorSubsectors[sector].Add(subsector);
                        }
                    }
                }
            }
        }
        
        private void ReadNodes()
        {
            using (Stream stream = archive.OpenLump(label + WadArchive.ML_NODES))
            using (BinaryReader reader = new BinaryReader(stream))
            {
                while (stream.Position < stream.Length)
                {
                    int x0 = reader.ReadInt16();
                    int y0 = reader.ReadInt16();
                    int x1 = x0 + reader.ReadInt16();
                    int y1 = y0 + reader.ReadInt16();
                    var node = new Node()
                    {
                        Line = new Line(x0, y0, x1, y1),
                        RightBoundingBoxTop = reader.ReadInt16(),
                        RightBoundingBoxBottom = reader.ReadInt16(),
                        RightBoundingBoxLeft = reader.ReadInt16(),
                        RightBoundingBoxRight = reader.ReadInt16(),
                        LeftBoundingBoxTop = reader.ReadInt16(),
                        LeftBoundingBoxBottom = reader.ReadInt16(),
                        LeftBoundingBoxLeft = reader.ReadInt16(),
                        LeftBoundingBoxRight = reader.ReadInt16(),
                        RightChild = reader.ReadInt16(),
                        LeftChild = reader.ReadInt16()
                    };

                    nodes.Add(node);
                }
            }
        }
    }
}
