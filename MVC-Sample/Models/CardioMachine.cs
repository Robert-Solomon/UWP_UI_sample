using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MVC_Sample.Models
{
    public class CardioMachine
    {
        //enum Characteristics { NotApplicable = 0; Time, Resistance, RepPerMin, Incline, Calories, Laps }
        public int Id { get; set; }
        public string MachineType { get; set; }
        public string MachineModel { get; set; }
        public int MachineNumber { get; set; }
        public int capabilities1 { get; set; }
        public int capabilities2 { get; set; }
        public int capabilities3 { get; set; }
        public int capabilities4 { get; set; }
        public int capabilities5 { get; set; }
    }
}
