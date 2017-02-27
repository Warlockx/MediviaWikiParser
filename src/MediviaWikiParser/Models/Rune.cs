using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediviaWikiParser.Models
{
    public class Rune
    {
        public string Name { get; set; }
        public string Formula { get; set; }
        public IEnumerable<string> VocationToCast { get; set; }
        public int CastMagicLevel { get; set; }
        public int UseMagicLevel { get; set; } //to use runes
        public int ManaCost { get; set; }
        public int Charges { get; set; }
        public int Price { get; set; }
        public bool NeedsPremium { get; set; }

        public Rune(string name, string formula, IEnumerable<string> vocationToCast, int castMagicLevel, int useMagicLevel, int manaCost, int charges, int price, bool needsPremium)
        {
            Name = name;
            Formula = formula;
            VocationToCast = vocationToCast;
            CastMagicLevel = castMagicLevel;
            UseMagicLevel = useMagicLevel;
            ManaCost = manaCost;
            Charges = charges;
            Price = price;
            NeedsPremium = needsPremium;
        }
    }
}
