using System;
using System.IO;
using System.Drawing;

using EasterLib;
using SixLabors.ImageSharp;
using K4os.Compression.LZ4;
using System.Diagnostics;
using SixLabors.ImageSharp.PixelFormats;

namespace Easter
{
    class Program
    {
        static void Main(string[] args)
        {
            //Package.Unpack(args[0]);

            /*string[] archives = Directory.GetFiles(".", "*.g");
            for (int i = 0; i < archives.Length; ++i)
            {
                Package.Unpack(archives[i]);
            }*/

            using (FileStream stream = new FileStream(args[0], FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    UInt32 value = reader.ReadUInt32();
                    UInt32 magic = value & 0x00FFFFFF;
                    byte unk08Size = (byte)(value >> 24);

                    Debug.Assert(magic == 0x00464750);

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

                    //NOTE(adm244): skip garbage at 0x18
                    reader.BaseStream.Seek(unk16Size, SeekOrigin.Current);

                    byte[] buffer = reader.ReadBytes((int)compressedSize);
                    byte[] pixels = new byte[width * height * 4];

                    int written = LZ4Codec.Decode(buffer, pixels);
                    Debug.Assert(written > 0);

                    Image image = Image.LoadPixelData<Rgba32>(pixels, (int)width, (int)height);
                    image.SaveAsPng(args[0] + ".png");
                }
            }
        }
    }
}
