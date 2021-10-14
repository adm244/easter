using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;

using EasterLib;

using K4os.Compression.LZ4;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Easter
{
    class Program
    {
        static void Main(string[] args)
        {
            //PGFDecode(args[0], args[0] + ".png");
            //PGFEncode(args[0], args[0] + ".pgf");

            //NOTE(adm244): packed json is just a "message pack" thing:
            // https://github.com/msgpack/msgpack/blob/master/spec.md

            string filepath = Path.GetFullPath(args[0]);
            string outputDirectory = Path.GetFullPath(args[1]);

            using (FileStream stream = new FileStream(filepath, FileMode.Open, FileAccess.Read))
            {
                using (GArchive archive = new GArchive(stream, GArchiveMode.Read))
                {
                    foreach (GArchiveEntry entry in archive.Entries)
                    {
                        string destinationPath = Path.GetFullPath(Path.Combine(outputDirectory, entry.FullName));
                        entry.ExtractToFile(destinationPath);
                    }
                }
            }
        }

        private static byte[] _pgfMagic = new byte[] { 0x50, 0x47, 0x46 };

        public static void PGFDecode(string source, string target)
        {
            using (FileStream stream = new FileStream(source, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    byte[] magic = reader.ReadBytes(_pgfMagic.Length);
                    Debug.Assert(magic == _pgfMagic);

                    byte unk08Size = reader.ReadByte();

                    UInt32 fileSize = reader.ReadUInt32();

                    //NOTE(adm244): skip garbage at 0x8
                    reader.BaseStream.Seek(unk08Size, SeekOrigin.Current);

                    UInt32 compressedSize = reader.ReadUInt32();
                    UInt32 width = reader.ReadUInt32();
                    UInt32 height = reader.ReadUInt32();

                    //NOTE(adm244): skip two bytes that are unused by PGF parser
                    // these are probably meant to be bpp and isCompressed?
                    reader.BaseStream.Seek(0x2, SeekOrigin.Current);

                    byte unk16Size = reader.ReadByte();
                    bool hasPallete = reader.ReadByte() != 0;

                    //TODO(adm244): handle a palette (256 entries) that's appended to [width * height] buffer
                    //NOTE(adm244): game's PGF parser ignores this palette by the way
                    Debug.Assert(hasPallete == false);

                    //NOTE(adm244): skip garbage at 0x18
                    reader.BaseStream.Seek(unk16Size, SeekOrigin.Current);

                    byte[] buffer = reader.ReadBytes((int)compressedSize);
                    byte[] pixels = new byte[width * height * 4];

                    int written = LZ4Codec.Decode(buffer, pixels);
                    Debug.Assert(written > 0);

                    Image image = Image.LoadPixelData<Rgba32>(pixels, (int)width, (int)height);
                    image.SaveAsPng(target);
                }
            }
        }

        public static void PGFEncode(string source, string target)
        {
            Image<Rgba32> image = Image.Load<Rgba32>(source);

            image.TryGetSinglePixelSpan(out var span);
            byte[] pixels = MemoryMarshal.AsBytes(span).ToArray();
            byte[] pixelsCompressed = new byte[pixels.Length + (int)(pixels.Length * 0.04 + 0.5)];

            int compressedSize = LZ4Codec.Encode(pixels, pixelsCompressed);

            using (FileStream stream = new FileStream(target, FileMode.Create, FileAccess.Write))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    writer.Write(_pgfMagic);

                    writer.Write((byte)0x0);

                    writer.Write((UInt32)0xDEADBEEF);
                    writer.Write((UInt32)compressedSize);

                    writer.Write((UInt32)image.Width);
                    writer.Write((UInt32)image.Height);

                    writer.Write((UInt16)0x44A2);

                    writer.Write((byte)0x0);

                    writer.Write((byte)0x0);

                    writer.Write(pixelsCompressed, 0, compressedSize);

                    UInt32 fileSize = (UInt32)writer.BaseStream.Position;
                    writer.BaseStream.Seek(0x4, SeekOrigin.Begin);
                    writer.Write((UInt32)fileSize);
                }
            }
        }
    }
}
