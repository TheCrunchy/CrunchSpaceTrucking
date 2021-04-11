using Sandbox.Game.Screens.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRageMath;

namespace CrunchSpaceTrucking
{
  public class Contract
    {
        private ulong PlayerSteamId;
        private Guid ContractId;
        private List<ContractItems> items;
        private double GpsX;
        private double GpsY;
        private double GpsZ;
        private int Reputation;

        public Contract(ulong SteamId, List<ContractItems> newItems, double x, double y, double z, int reputation)
        {
            this.Reputation = reputation;
            this.PlayerSteamId = SteamId;
            this.items = newItems;
            this.GpsX = x;
            this.GpsY = y;
            this.GpsZ = z;
            this.ContractId = System.Guid.NewGuid();
        }
        public Contract(Guid contractId, ulong SteamId, List<ContractItems> newItems, double x, double y, double z, int reputation)
        {
            this.Reputation = reputation;
            this.PlayerSteamId = SteamId;
            this.items = newItems;
            this.GpsX = x;
            this.GpsY = y;
            this.GpsZ = z;
            this.ContractId = contractId;
        }
        public List<ContractItems> getItemsInContract()
        {
            return this.items;
        }
        public Guid GetContractId()
        {
            return this.ContractId;
        }
        public MyGps GetDeliveryLocation()
        {
            MyGps gps = new MyGps();
            gps.Name = "Delivery Location within 1km - !contract deliver";
            Vector3D newCoords = new Vector3D(GpsX, GpsY, GpsZ);
            gps.Coords = newCoords;
            return gps;
        }
        public void SetDeliveryLocation(double x, double y, double z)
        {
            this.GpsX = x;
            this.GpsY = y;
            this.GpsZ = z;
        }
        public int GetReputation()
        {
            return this.Reputation;
        }
    }
}
