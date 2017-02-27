using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace MediviaWikiParser.Models
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum DamageType
    {
        Fire,
        Energy,
        Poison,
        LifeDrain,
        Physical,
        Death,
        Invisibility
    }
}
