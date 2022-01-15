using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using OpenTK.Graphics.ES11;

namespace FieryGunHand
{
    public class WadArchive : IDisposable
    {
        private static readonly char[] IWAD_MAGIC = {'I', 'W', 'A', 'D' };
        private static readonly char[] PWAD_MAGIC = {'P', 'W', 'A', 'D' };
        private static readonly char[] GLBSP_V2_MAGIC = {'g', 'N', 'd', '2' };

        public const int ML_LABEL = 0;
        public const int ML_THINGS = 1;
        public const int ML_LINEDEFS = 2;
        public const int ML_SIDEDEFS = 3;
        public const int ML_VERTEXES = 4;
        public const int ML_SEGS = 5;
        public const int ML_SSECTORS = 6;
        public const int ML_NODES = 7;
        public const int ML_SECTORS = 8;
        public const int ML_REJECT = 9;
        public const int ML_BLOCKMAP = 10;
        public const int ML_BEHAVIOR = 11;

        public const int GL_LABEL = 0;
        public const int GL_VERT = 1;
        public const int GL_SEGS = 2;
        public const int GL_SSECT = 3;
        public const int GL_NODES = 4;

        public enum MapFormat
        {
            Doom,
            Hexen,
            Udmf
        }

        public enum NodesFormat
        {
            Doom,
            GlBsp2,
            Unknown
        }

        public struct Lump
        {
            public string Name;
            public int Position;
            public int Size;

            public override string ToString()
            {
                return $"Lump {{ Name = {Name}, Position = {Position:x8}, Size = {Size} }}";
            }
        }

        public struct Map
        {
            public string Name;
            public int StartLump;
            public int EndLump;
            public MapFormat Format;
            public NodesFormat NodesFormat;
            public int GlNodesLabel;

            public override string ToString()
            {
                return $"Map {{ Name = {Name}, Format = {Format}, NodesFormat = {NodesFormat} }}";
            }
        }

        public string FilePath { get; private set; }
        public Lump[] Lumps { get; private set; }
        
        public Map[] Maps { get; private set; }

        private FileStream stream;
        private BinaryReader reader;

        private int InfoTableOfs;

        public WadArchive(string path)
        {
            FilePath = Path.GetFullPath(path);
            stream = new FileStream(FilePath, FileMode.Open);
            reader = new BinaryReader(stream);
            
            char[] magic = reader.ReadChars(4);
            if (!magic.SequenceEqual(IWAD_MAGIC) && !magic.SequenceEqual(PWAD_MAGIC))
            {
                throw new InvalidDataException("Magic is not IWAD or PWAD");
            }

            int numLumps = reader.ReadInt32();
            Lumps = new Lump[numLumps];
            InfoTableOfs = reader.ReadInt32();
            ReadLumpTable();
            ScanMaps();
        }

        public void Dispose()
        {
            stream?.Dispose();
            reader?.Dispose();
        }

        private void ReadLumpTable()
        {
            stream.Seek(InfoTableOfs, SeekOrigin.Begin);

            for (int i = 0; i < Lumps.Length; i++)
            {
                Lumps[i] = new Lump();
                Lumps[i].Position = reader.ReadInt32();
                Lumps[i].Size = reader.ReadInt32();
                Lumps[i].Name = ReadLumpName(reader);
            }
        }

        public static string ReadLumpName(BinaryReader reader)
        {
            string buf = string.Empty;
            bool end = false;

            for (int i = 0; i < 8; i++)
            {
                char c = Convert.ToChar(reader.ReadByte());

                if (c != '\0' && !end)
                {
                    buf += c;
                }
                else
                {
                    end = true;
                }
            }

            return buf;
        }

        public int GetLumpIndex(string name, int from = 0)
        {
            for (int i = from; i < Lumps.Length; i++)
            {
                if (string.Equals(Lumps[i].Name, name, StringComparison.InvariantCultureIgnoreCase))
                {
                    return i;
                }
            }

            return -1;
        }

        public bool ContainsLump(string name)
        {
            return (GetLumpIndex(name) != -1);
        }

        /// <summary>
        /// Find a lump.
        /// </summary>
        /// <param name="name">Lump name.</param>
        /// <param name="index">The index of the lump in the <see cref="Lumps"/> array.</param>
        /// <returns>Whether the lump exists in this WAD.</returns>
        public bool FindLump(string name, out int index, int from = 0)
        {
            index = GetLumpIndex(name, from);
            return (index != -1);
        }

        private void ScanMaps()
        {
            List<Map> maps = new List<Map>();

            for (int i = 0; i < Lumps.Length; i++)
            {
                Lump lump = Lumps[i];

                if (lump.Name == "THINGS")
                {
                    int label = i - 1;
                    Lump labelInfo = Lumps[label];
                    int endlump = label;
                    MapFormat format = MapFormat.Doom;
                    NodesFormat nodesFormat = NodesFormat.Doom;

                    if (Lumps[label + ML_THINGS].Name == "THINGS") endlump++;
                    if (Lumps[label + ML_LINEDEFS].Name == "LINEDEFS") endlump++;
                    if (Lumps[label + ML_SIDEDEFS].Name == "SIDEDEFS") endlump++;
                    if (Lumps[label + ML_VERTEXES].Name == "VERTEXES") endlump++;
                    if (Lumps[label + ML_SEGS].Name == "SEGS") endlump++;
                    if (Lumps[label + ML_SSECTORS].Name == "SSECTORS") endlump++;
                    if (Lumps[label + ML_NODES].Name == "NODES") endlump++;
                    if (Lumps[label + ML_SECTORS].Name == "SECTORS") endlump++;
                    if (Lumps[label + ML_REJECT].Name == "REJECT") endlump++;
                    if (Lumps[label + ML_BLOCKMAP].Name == "BLOCKMAP") endlump++;

                    if (label + ML_BEHAVIOR < Lumps.Length && Lumps[label + ML_BEHAVIOR].Name == "BEHAVIOR")
                    {
                        format = MapFormat.Hexen;
                        endlump++;
                    }

                    if (label + ML_BEHAVIOR + 1 < Lumps.Length && Lumps[label + ML_BEHAVIOR + 1].Name == "SCRIPTS") endlump++;

                    int glLabel = -1;
                    bool hasGlLabel = FindLump("GL_" + labelInfo.Name.Substring(0, Math.Min(labelInfo.Name.Length, 5)), out glLabel, label);
                    if (hasGlLabel)
                    {
                        nodesFormat = NodesFormat.Unknown;

                        using (Stream stream = OpenLump(glLabel + GL_VERT))
                        using (BinaryReader reader = new BinaryReader(stream))
                        {
                            char[] magic = reader.ReadChars(4);
                            if (magic.SequenceEqual(GLBSP_V2_MAGIC))
                            {
                                nodesFormat = NodesFormat.GlBsp2;
                            }
                        }
                    }

                    Map map = new Map
                    {
                        StartLump = label,
                        EndLump = endlump,
                        Format = format,
                        NodesFormat = nodesFormat,
                        GlNodesLabel = glLabel,
                        Name = labelInfo.Name
                    };
                    maps.Add(map);
                    Console.WriteLine(map);
                }
                else if (lump.Name == "TEXTMAP")
                {
                    int label = i - 1;
                    Lump labelInfo = Lumps[label];
                    int endmap;

                    for (endmap = i; endmap < Lumps.Length; endmap++)
                    {
                        if (Lumps[endmap].Name == "ENDMAP")
                        {
                            break;
                        }
                    }

                    if (endmap == Lumps.Length)
                    {
                        continue;
                    }

                    Map map = new Map
                    {
                        StartLump = label,
                        EndLump = endmap,
                        Format = MapFormat.Udmf,
                        Name = labelInfo.Name
                    };
                    maps.Add(map);
                    Console.WriteLine(map);
                }
            }

            Maps = maps.ToArray();
        }

        public byte[] ReadLump(int index)
        {
            Lump lump = Lumps[index];
            stream.Seek(lump.Position, SeekOrigin.Begin);

            byte[] data = new byte[lump.Size];
            stream.Read(data, 0, data.Length);

            return data;
        }

        public byte[] ReadLump(string name)
        {
            int index;
            if (!FindLump(name, out index))
            {
                return null;
            }

            return ReadLump(index);
        }

        public Stream OpenLump(int index)
        {
            return new MemoryStream(ReadLump(index));
        }

        public Stream OpenLump(string name)
        {
            int index;
            if (!FindLump(name, out index))
            {
                return null;
            }

            return OpenLump(index);
        }
    }
}
