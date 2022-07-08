using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ConsoleAppWaterPollution.Models
{
    public class Pollution
    {
        public int Id { get; set; }

        public DateTime Period { get; set; }

        public string Okato { get; set; }

        public string Subject { get; set; }

        public string River_basin { get; set; }

        public string Indicator { get; set; }

        public string Hazard_class { get; set; }

        public float Cnt_cases { get; set; }

        public float Value_min { get; set; }

        public float Value_max { get; set; }

        public string Unit { get; set; }
    }
}
