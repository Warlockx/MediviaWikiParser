using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace MediviaWikiParser.Services
{
    public static class EnumConvertor
    {
        public static object GetEnum<T>(this string s) where T : struct
        {
            try
            {
                T result;
                Enum.TryParse(Regex.Replace(s, @"\s+", ""), out result);
                return result;
            }
            catch (Exception e)
            {
                Console.WriteLine($"Exception raised at the function {nameof(GetEnum)} : {e.Message}");
            }

            return null;
        }
    }
}
