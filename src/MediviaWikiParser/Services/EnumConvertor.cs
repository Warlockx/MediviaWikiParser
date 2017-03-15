using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MediviaWikiParser.Services
{
    public static class EnumConvertor
    {
        private static readonly ILogger Logger = ApplicationLogging.CreateLogger<MonstersService>();
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
                Logger.LogCritical($"Exception raised at the function {nameof(GetEnum)} : {e.Message}");
            }

            return null;
        }
    }
}
