namespace System
{
    using System.Text.RegularExpressions;

    internal static class StringExtensionMethods
    {
        private static readonly Regex backslashCleanup = new Regex(@"\\+", RegexOptions.Compiled | RegexOptions.CultureInvariant | RegexOptions.IgnoreCase);

        public static bool IsNullOrWhiteSpace(this string argThis)
        {
            return String.IsNullOrWhiteSpace(argThis);
        }

        public static bool IsNullOrEmpty(this string argThis)
        {
            return String.IsNullOrEmpty(argThis);
        }

        public static string ToWinPathString(this string pathString)
        {
            return backslashCleanup.Replace(pathString.Replace('/', '\\'), @"\");
        }
    }
}

namespace System.Collections
{
    internal static class EnumerableExtensionMethods
    {
        public static bool IsNullOrEmpty(this IEnumerable argThis)
        {
            return null == argThis || false == argThis.GetEnumerator().MoveNext();
        }
    }
}

namespace System.Collections.Generic
{
    using Linq;

    internal static class EnumerableExtensionMethods
    {
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> argThis)
        {
            return null == argThis || false == argThis.Any();
        }

        public static IList<T> ToListOrNull<T>(this IEnumerable<T> argThis)
        {
            if (null == argThis)
                return null;

            return argThis.ToList();
        }

        public static T[] ToArrayOrNull<T>(this IEnumerable<T> argThis)
        {
            if (null == argThis)
                return null;

            return argThis.ToArray();
        }

        public static IEnumerable<T> Compact<T>(this IEnumerable<T> argThis)
        {
            if (null == argThis)
                return null;

            return argThis.Where<T>(t => t != null);
        }
    }
}

namespace System.IO
{
    using System.Security.Cryptography;

    internal static class FileInfoExtensionMethods
    {
        // http://blogs.msdn.com/b/blambert/archive/2009/02/22/blambert-codesnip-fast-byte-array-to-hex-string-conversion.aspx
        public static string Hexdigest(this FileInfo argThis)
        {
            using (FileStream fs = File.OpenRead(argThis.FullName))
            {
                using (var sha1 = SHA1.Create())
                {
                    return BitConverter.ToString(sha1.ComputeHash(fs)).Replace("-", String.Empty).ToLowerInvariant();
                }
            }
        }
    }
}

namespace System.Text
{
    internal static class StringBuilderExtensionMethods
    {
        public static StringBuilder SmartAppendLine(this StringBuilder argThis, string toAppend)
        {
            if (!toAppend.IsNullOrWhiteSpace())
            {
                argThis.AppendLine(toAppend);
            }
            return argThis;
        }
    }
}

namespace System.Text.RegularExpressions
{
    internal static class RegexExtensionMethods
    {
        public static string Postmatch(this Match match, string target)
        {
            int unmatchedIdx = match.Index + match.Length;
            return target.Substring(unmatchedIdx);
        }
    }
}

namespace System.Net.Sockets
{
    internal static class TcpClientExtensionMethods
    {
        public static int Read(this TcpClient client, byte[] buffer)
        {
            NetworkStream stream = client.GetStream();
            return stream.Read(buffer, 0, buffer.Length);
        }

        public static void Write(this TcpClient client, byte[] data)
        {
            NetworkStream stream = client.GetStream();
            stream.Write(data, 0, data.Length);
        }

        public static bool DataAvailable(this TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            return stream.DataAvailable;
        }

        public static void CloseStream(this TcpClient client)
        {
            NetworkStream stream = client.GetStream();
            stream.Close();
            stream.Dispose();
        }
    }
}

namespace System.Threading
{
    using System;

    public static class TimerExtensionMethods
    {
        public static void Stop(this Timer argThis)
        {
            argThis.Change(Timeout.Infinite, Timeout.Infinite);
        }

        public static void Restart(this Timer argThis, TimeSpan argInterval)
        {
            argThis.Change(argInterval, argInterval);
        }
    }
}