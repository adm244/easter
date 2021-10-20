using System;
using System.IO;

namespace EasterLib.Extensions
{
    public static class BinaryWriterExtension
    {
        public static void WriteCString(this BinaryWriter writer, string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            writer.Write(value.ToCharArray());
            writer.Write((byte)0);
        }
    }
}
