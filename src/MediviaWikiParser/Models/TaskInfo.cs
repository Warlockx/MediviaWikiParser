using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MediviaWikiParser.Models
{
    public class TaskInfo
    {
        public int AmountToKill { get; }
        public string Location { get; }

        public TaskInfo(int amountToKill, string location)
        {
            AmountToKill = amountToKill;
            Location = location;
        }
    }
}
