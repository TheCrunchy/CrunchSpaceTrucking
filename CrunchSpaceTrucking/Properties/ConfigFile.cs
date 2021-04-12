using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrunchSpaceTrucking
{
    public class ConfigFile
    {
        public string DatabaseIP = "localhost";
        public string DatabaseUser = "fred";
        public int DatabasePort = 3306;
        public string password = "alan";
        public int MaxItemsOnContract = 5;
        public int HardContractRep = 5000;
        public int MediumContractRep = 2000;

        public double HardContractChance = 30;
        public double MediumContractChance = 60;
    }
}
