using System.IO;
using System.Text;

namespace EasterLib.Extensions
{
    public static class BinaryReaderExtension
    {
        public static bool EOF(this BinaryReader reader)
        {
            return (reader.BaseStream.Position >= reader.BaseStream.Length);
        }

        public static string ReadCString(this BinaryReader reader)
        {
            if (reader.EOF())
                return string.Empty;

            char character;
            StringBuilder stringBuilder = new StringBuilder();

            while ((character = reader.ReadChar()) != 0)
            {
                stringBuilder.Append(character);
            }

            return stringBuilder.ToString();
        }
    }
}
