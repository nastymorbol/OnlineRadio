using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OnlineRadio.Console
{
    public static class StringExtensions
    {
        public static string TruncateMid(this string s, int length)
        {
            if(s.Length < length)
                return s;

            return s.Substring(0, length / 3) + " .. " + s.Substring(s.Length - length / 3);
        }

        public static void Deconstruct(this string[] data, out string first, out string second, out string third, out string last)
        {
            if (data == null)
                throw new ArgumentNullException($"Parameter {nameof(data)} shouldn't be null");
            first = string.Empty;
            second = string.Empty;
            last = string.Empty;
            third = string.Empty;

            if(data.Length > 0)
                first = data[0];
            if (data.Length > 1)
                second = data[1];
            if (data.Length > 2)
                third = data[2];
            if (data.Length == 4)   
                last = data[3];
            if (data.Length > 4)
                last = data.Skip(3).Aggregate( (s1, s2) => s1 + " " + s2);

        }
    }

}
