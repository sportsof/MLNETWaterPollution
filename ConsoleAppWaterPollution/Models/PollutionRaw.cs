using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleAppWaterPollution.Models
{
    public record PollutionRaw(
        int id,
        string period,
        string okato,
        string subject,
        string river_basin,
        string indicator,
        string hazard_class,
        int cnt_cases,
        string value_min,
        string value_max,
        string unit
     );
}
