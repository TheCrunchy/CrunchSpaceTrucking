using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrunchSpaceTrucking
{
    public class ContractItems
    {
        public string ContractItemId;
        public string ItemType;
        public string SubType;
        public int MinToDeliver;
        public int MaxToDeliver;
        public int MinPrice;
        public int MaxPrice;
        public int AmountToDeliver;
        public int chance;
        public void SetAmountToDeliver()
        {
            Random random = new Random();
            AmountToDeliver = random.Next(MinToDeliver + 1, MaxToDeliver);
            
        }
        public int GetAmountToDeliver()
        {
            return AmountToDeliver;
        }
        public int GetPrice()
        {
            Random random = new Random();

            return random.Next(MinPrice, MaxPrice);
        }
    }
}
