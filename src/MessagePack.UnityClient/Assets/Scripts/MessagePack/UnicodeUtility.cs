namespace MessagePack.Internal
{
    using System;
    using System.Text;
    /// <summary>
    /// unicode manupilation utilities
    /// </summary>
    internal static class UnicodeUtility
    {
        /// <summary>
        /// convert utf8 bytearray to string instance
        /// </summary>
        /// <param name="data">UTF-8 byte array(without BOM)</param>
        /// <param name="offset">start position in data</param>
        /// <param name="count">number of byte length</param>
        /// <remarks>for unity, this function passes arguments to Encoding.UTF8.GetBytes<remarks>
        /// <returns>converted string instance</returns>
        public static unsafe string GetStringFromUtf8(byte[] data, int offset, int count)
        {
            return Encoding.UTF8.GetString(data, offset, count);
        }
    }
}
