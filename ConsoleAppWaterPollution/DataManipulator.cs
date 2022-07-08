using ConsoleAppWaterPollution.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleAppWaterPollution
{
    public class DataManipulator
    {
        // start: 31.01.2008
        // end: 31.08.2021
        public static List<Pollution> GetCompletePollution(List<Pollution> data)
        {
            var startDate = new DateTime(2008, 1, 28, 0, 0, 0);
            var endDate = new DateTime(2021, 8, 31, 0, 0, 0);

            var defaultRow = data[0];
            var result = new List<Pollution>();

            while (startDate < endDate)
            {
                var existRow = data.FirstOrDefault(x => x.Period.Year == startDate.Year && x.Period.Month == startDate.Month);
                
                if(existRow == null)
                {
                    existRow = new Pollution
                    {
                        Subject = defaultRow.Subject,
                        River_basin = defaultRow.River_basin,
                        Indicator = defaultRow.Indicator,
                        Hazard_class = defaultRow.Hazard_class,
                        Period = startDate,
                        Cnt_cases = 0,
                        Value_min = 0,
                        Value_max = 0
                    };
                }
                result.Add(existRow);

                startDate = startDate.AddMonths(1);
            }

            return result;
        }
    }
}
