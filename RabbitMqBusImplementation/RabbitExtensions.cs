using System.IO;
using System.IO.Compression;
using System.Text;
using Newtonsoft.Json;

namespace RabbitMqBusImplementation
{
    public static class RabbitExtensions
    {
        public static T ToObject<T>(this byte[] source)
        {
            return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(source)); //TODO add decompress
        }

        public static byte[] ToRabbitMessageByteArray<T>(this T source)
        {
            if (source is string)
                return Encoding.UTF8.GetBytes(source as string);

            if (source is byte[])
                return source as byte[];

            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(source));
        }

        public static byte[] ToGzipRabbitMessageByteArray<T>(this T source)
        {
            if (source is string)
                return (source as string).CompressString();

            if (source is byte[])
                return (source as byte[]).CompressBytes();

            return JsonConvert.SerializeObject(source).CompressString();
        }

        private static byte[] CompressString(this string source)
        {
            return Encoding.UTF8.GetBytes(source).CompressBytes();
        }

        private static byte[] CompressBytes(this byte[] source)
        {
            byte[] result;

            using (MemoryStream outputStream = new MemoryStream())
            {
                using (GZipStream gZipStream = new GZipStream(outputStream, CompressionMode.Compress))
                {
                    gZipStream.Write(source, 0, source.Length);
                    gZipStream.Close();
                    result = outputStream.ToArray();
                }
            }

            return result;
        }
    }
}