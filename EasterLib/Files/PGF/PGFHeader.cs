using System;
using System.Diagnostics;
using System.IO;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace EasterLib.Files.PGF
{
    internal struct PGFHeader
    {
        private static byte[] _pgfMagic = new byte[] { 0x50, 0x47, 0x46 };

        private static readonly string InvalidHeader = "PGF file header is invalid or not supported.";

        public int Width;
        public int Height;
        public int BytesPerPixel;
        public bool HasPalette;

        public int FileSize;
        public int CompressedLength;

        public int Length => Width * Height * BytesPerPixel;

        public static PGFHeader Create(Image<Rgba32> image, out byte[] pixelsCompressed)
        {
            pixelsCompressed = LZ4Helper.EncodeImage(image);

            return new PGFHeader()
            {
                Width = image.Width,
                Height = image.Height,
                BytesPerPixel = 4,
                HasPalette = false,
                FileSize = 0,
                CompressedLength = pixelsCompressed.Length
            };
        }

        public static PGFHeader Read(BinaryReader reader)
        {
            try
            {
                byte[] magic = reader.ReadBytes(_pgfMagic.Length);
                Debug.Assert(magic.Equals(_pgfMagic));

                byte unk08Size = reader.ReadByte();

                Int32 fileSize = reader.ReadInt32();

                //NOTE(adm244): skip garbage at 0x8
                reader.BaseStream.Seek(unk08Size, SeekOrigin.Current);

                Int32 compressedSize = reader.ReadInt32();
                Int32 width = reader.ReadInt32();
                Int32 height = reader.ReadInt32();

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

                return new PGFHeader()
                {
                    Width = width,
                    Height = height,
                    
                    //NOTE(adm244): see Write method
                    BytesPerPixel = 4,

                    HasPalette = hasPallete,
                    FileSize = fileSize,
                    CompressedLength = compressedSize
                };
            }
            catch (IOException ex)
            {
                throw new InvalidDataException(InvalidHeader, ex);
            }
        }

        public void Write(BinaryWriter writer, out long fileSizePosition)
        {
            writer.Write(_pgfMagic);

            writer.Write((byte)0x0);

            fileSizePosition = writer.BaseStream.Position;

            writer.Write((UInt32)0xDEADBEEF);
            writer.Write((Int32)CompressedLength);

            writer.Write((Int32)Width);
            writer.Write((Int32)Height);

            //NOTE(adm244): currently ignores bpp(?) and unk, since it's not used by eastward's parser
            writer.Write((UInt16)0x44A2);

            writer.Write((byte)0x0);

            writer.Write((byte)(HasPalette ? 1 : 0));
        }
    }
}
