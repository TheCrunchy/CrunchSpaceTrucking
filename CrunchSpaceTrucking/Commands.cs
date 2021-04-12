using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.GameSystems.BankingAndCurrency;
using Sandbox.Game.Multiplayer;
using Sandbox.Game.Screens.Helpers;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch.Commands;
using Torch.Commands.Permissions;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage;
using VRage.Game;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Ingame;
using VRageMath;

namespace CrunchSpaceTrucking
{
    public class Commands : CommandModule
    {

        [Command("truck", "output definitions")]
        [Permission(MyPromoteLevel.Admin)]
        public void OutputDefinitions()
        {
            StringBuilder ingots = new StringBuilder();
            StringBuilder components = new StringBuilder();
            StringBuilder ore = new StringBuilder();
            StringBuilder ammo = new StringBuilder();

            foreach (MyDefinitionBase def in MyDefinitionManager.Static.GetAllDefinitions())
            {
                if (def.Id.TypeId.ToString().Equals("MyObjectBuilder_Ingot"))
                {

                    ingots.AppendLine(def.Id.TypeId.ToString().Replace("MyObjectBuilder_", "") + "," + def.Id.SubtypeId + ", 1 " + ", 10 " + ", 20 " + ", 50 ");
                }
                if (def.Id.TypeId.ToString().Equals("MyObjectBuilder_Component"))
                {

                    components.AppendLine(def.Id.TypeId.ToString().Replace("MyObjectBuilder_", "") + "," + def.Id.SubtypeId + ", 1 " + ", 10 " + ", 20 " + ", 50 ");
                }
                if (def.Id.TypeId.ToString().Equals("MyObjectBuilder_Ore"))
                {
                    ore.AppendLine(def.Id.TypeId.ToString().Replace("MyObjectBuilder_", "") + "," + def.Id.SubtypeId + ", 1 " + ", 10 " + ", 20 " + ", 50 ");
                }
                if (def.Id.TypeId.ToString().Equals("MyObjectBuilder_AmmoMagazine"))
                {
                    ammo.AppendLine(def.Id.TypeId.ToString().Replace("MyObjectBuilder_", "") + "," + def.Id.SubtypeId + ", 1 " + ", 10 " + ", 20 " + ", 50 ");
                }

            }
            StringBuilder output = new StringBuilder();
            output.AppendLine("TypeId, SubtypeId, minAmount, maxAmount, minPrice, maxPrice");
            output.AppendLine(ingots.ToString());
            output.AppendLine(components.ToString());
            output.AppendLine(ore.ToString());
            output.AppendLine(ammo.ToString());



            if (!System.IO.File.Exists(TruckingPlugin.path + "//SpaceTrucking"))
            {
                System.IO.Directory.CreateDirectory(TruckingPlugin.path + "//SpaceTrucking");
            }
            File.WriteAllText(TruckingPlugin.path + "//SpaceTrucking//definitions.csv", output.ToString());
        }

        public Boolean getChance(int minimalChance)
        {
            Random random = new Random();
            return random.Next(99) + 1 <= minimalChance;
        }
        public void AddContractToStorage(ulong steamid, Contract contract)
        {
            if (TruckingPlugin.UsingDatabase)
            {
            }
            else
            {
                TruckingPlugin.activeContracts.Add(steamid, contract);
                Database.addNewContract(steamid, contract);
            }
        }
        [Command("contract quit", "quit a contract")]
        [Permission(MyPromoteLevel.None)]
        public void QuitContract()
        {
            if (TruckingPlugin.getActiveContract(Context.Player.SteamUserId) != null)
            {
                TruckingPlugin.SendMessage("The Boss", "Contract quit, reputation lowered", Color.Purple, Context.Player.SteamUserId);
                Database.RemoveContract(Context.Player.SteamUserId, false, TruckingPlugin.getActiveContract(Context.Player.SteamUserId), Context.Player.IdentityId);
                List<IMyGps> playerList = new List<IMyGps>();
                MySession.Static.Gpss.GetGpsList(Context.Player.IdentityId, playerList);
                foreach (IMyGps gps in playerList)
                {
                    if (gps.Name.Contains("Delivery Location, bring hauling vehicle within 300m"))
                    {
                        MyAPIGateway.Session?.GPS.RemoveGps(Context.Player.Identity.IdentityId, gps);
                    }
                }
            }

        }
        [Command("setupDatabase", "show contract details")]
        [Permission(MyPromoteLevel.None)]
        public void SetupDatabase()
        {

        }
        [Command("contract details", "show contract details")]
        [Permission(MyPromoteLevel.None)]
        public void ContractDetails()
        {
            Contract contract = TruckingPlugin.getActiveContract(Context.Player.SteamUserId);
            StringBuilder contractDetails = new StringBuilder();
            if (contract != null)
            {
                int pay = TruckingPlugin.GetMinimumPay(contract.getItemsInContract());
                contractDetails = TruckingPlugin.MakeContractDetails(contract.getItemsInContract());
                MyGps gps = TruckingPlugin.getDeliveryLocation();

                MyGpsCollection gpscol = (MyGpsCollection)MyAPIGateway.Session?.GPS;

                gpscol.SendAddGps(Context.Player.Identity.IdentityId, ref gps);
                DialogMessage m = new DialogMessage("Contract Details", "Obtain and deliver these items", contractDetails.ToString());
                ModCommunication.SendMessageTo(m, Context.Player.SteamUserId);
            }
            else
            {
                Context.Respond("You dont currently have a contract", "The Boss");
            }
        }




        [Command("contract deliver", "deliver a contract")]
        [Permission(MyPromoteLevel.None)]
        public void DeliverContract()
        {
            Contract contract = TruckingPlugin.getActiveContract(Context.Player.SteamUserId);
            StringBuilder contractDetails = new StringBuilder();
            if (contract != null)
            {
                Vector3D coords = contract.GetDeliveryLocation().Coords;
                float distance = Vector3.Distance(coords, Context.Player.Character.PositionComp.GetPosition());
                  if (distance <= 1000)
                   {

                List<VRage.ModAPI.IMyEntity> l = new List<VRage.ModAPI.IMyEntity>();

                BoundingSphereD sphere = new BoundingSphereD(Context.Player.Character.PositionComp.GetPosition(), 1000);
                l = MyAPIGateway.Entities.GetEntitiesInSphere(ref sphere);

                Dictionary<MyDefinitionId, int> itemsToRemove = new Dictionary<MyDefinitionId, int>();
                int pay = 0;
                foreach (ContractItems item in contract.getItemsInContract())
                {
                    if (MyDefinitionId.TryParse("MyObjectBuilder_" + item.ItemType, item.SubType, out MyDefinitionId id))
                    {
                        itemsToRemove.Add(id, item.AmountToDeliver);
                        pay += item.AmountToDeliver * item.GetPrice();
                    } else
                    {
                        Context.Respond("Theres an error in the config for this item, report in a ticket " + id.SubtypeName + " " + id.TypeId);
                    }
                }

                foreach (IMyEntity entity in l)
                {
                    if (entity is MyCubeGrid grid)
                    {
                        List<VRage.Game.ModAPI.IMyInventory> inventories = TakeTheItems.GetInventories(grid);
                        if (FacUtils.IsOwnerOrFactionOwned(grid, Context.Player.IdentityId, true))
                        {
                            if (TakeTheItems.ConsumeComponents(inventories, itemsToRemove, Context.Player.SteamUserId))
                            {
                                MyBankingSystem.ChangeBalance(Context.Player.Identity.IdentityId, pay);
                                Database.RemoveContract(Context.Player.SteamUserId, true , contract, Context.Player.IdentityId);
                                    TruckingPlugin.SendMessage("The Boss", "Contract Complete, Payment delivered to bank account.", Color.Purple, Context.Player.SteamUserId);
                                    return;
                            }
                        }
                    }
                }
                Context.Respond("Could not find owned grid in vicinity with the required items.", "The Boss");


                  }
                 else {
                     Context.Respond("You arent close enough to the delivery point! Must be within 1km of signal", "The Boss");
                 }
            }
            else
            {
                Context.Respond("You dont have an active contract.", "The Boss");
            }
        }
        [Command("contract take", "take a contract")]
        [Permission(MyPromoteLevel.Admin)]
        public void TakeContract()
        {

            if (TruckingPlugin.getActiveContract(Context.Player.SteamUserId) != null)
            {
                Context.Respond("You cannot take another contract while you have an active one. To quit a contract use !contract quit", "The Boss");
            }
            else
            {
                TruckingPlugin.GenerateContract(Context.Player.SteamUserId, Context.Player.IdentityId);
              
            }

        }
    }
}


