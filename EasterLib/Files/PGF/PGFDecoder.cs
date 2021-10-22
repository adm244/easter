using System;
using System.IO;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace EasterLib.Files.PGF
{
    public class PGFDecoder
    {
        public static Image Decode(string source)
        {
            using (FileStream stream = new FileStream(source, FileMode.Open, FileAccess.Read))
            {
                using (BinaryReader reader = new BinaryReader(stream))
                {
                    PGFHeader header = PGFHeader.Read(reader);

                    byte[] buffer = reader.ReadBytes(header.CompressedLength);

                    return LZ4Helper.DecodeImage(buffer, header.Width, header.Height, header.Length);
                }
            }
        }

        public static void Encode(Image<Rgba32> image, string target)
        {
            using (FileStream stream = new FileStream(target, FileMode.Create, FileAccess.Write))
            {
                using (BinaryWriter writer = new BinaryWriter(stream))
                {
                    PGFHeader header = PGFHeader.Create(image, out byte[] pixelsCompressed);
                    header.Write(writer, out long fileSizePosition);

                    writer.Write(pixelsCompressed, 0, header.CompressedLength);

                    Int32 fileSize = (Int32)writer.BaseStream.Position;
                    writer.BaseStream.Seek(fileSizePosition, SeekOrigin.Begin);
                    writer.Write((Int32)fileSize);
                }
            }
        }
    }
}
