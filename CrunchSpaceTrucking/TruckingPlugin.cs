﻿using NLog;
using Sandbox.Definitions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Torch;
using Torch.API;
using Torch.API.Session;
using Torch.Session;
using VRage.Game;
using Torch.API.Managers;
using Sandbox.Game.Screens.Helpers;
using VRageMath;
using System.Text.RegularExpressions;
using System.Globalization;
using Sandbox.Game.Entities.Character;
using VRage.Game.ModAPI;

namespace CrunchSpaceTrucking
{
    public class TruckingPlugin : TorchPluginBase
    {
        private static Dictionary<String, ContractItems> easyItems = new Dictionary<string, ContractItems>();
        private static Dictionary<String, ContractItems> mediumItems = new Dictionary<string, ContractItems>();
        private static Dictionary<String, ContractItems> hardItems = new Dictionary<string, ContractItems>();
        public static Logger Log = LogManager.GetCurrentClassLogger();
        public static string path;
        public static bool UsingDatabase = false;
        private static List<MyGps> DeliveryLocations = new List<MyGps>();
        public static Boolean getChance(int minimalChance)
        {
            Random random = new Random();
            return random.Next(99) + 1 <= minimalChance;
        }
        // 
        public static MyGps getDeliveryLocation()
        {
            //List<MyGps> possibleLocations = new List<MyGps>();
            //TruckingPlugin.Log.Info(DeliveryLocations.Count);
            //foreach (MyGps gps in DeliveryLocations)
            //{
            //    Vector3D coords = gps.Coords;
            //    float distance = Vector3.Distance(coords, character.PositionComp.GetPosition());
            //    TruckingPlugin.Log.Info(distance + "");
            //    if (distance <= minDistance)
            //    {
            //        possibleLocations.Add(gps);
            //    }
            //}
            //if (possibleLocations.Count == 0)
            //{
            //    foreach (MyGps gps in DeliveryLocations)
            //    {
            //        Vector3D coords = gps.Coords;
            //      //  minDistance = 10000000;
            //       // float distance = Vector3.Distance(coords, character.PositionComp.GetPosition());
            //        TruckingPlugin.Log.Info(distance + "");
            //        if (distance <= minDistance)
            //        {
            //            possibleLocations.Add(gps);
            //        }
            //    }
            //}
            Random random = new Random();
            int r = random.Next(DeliveryLocations.Count);
            return DeliveryLocations[r];
        }
        private static List<ContractItems> getItems(List<ContractItems> items, int AmountToPick)
        {
            List<ContractItems> returnList = new List<ContractItems>();
            int lowestPossible = 1000;
            List<ContractItems> SortedList = items.OrderByDescending(o => o.chance).ToList();
            SortedList.Reverse();
            int amountPicked = 0;
            lowestPossible = items[0].chance;
            Random random = new Random();
            foreach (ContractItems item in SortedList)
            {
                int chance = random.Next(101);
                if (chance <= item.chance && amountPicked < AmountToPick)
                {
                    item.SetAmountToDeliver();
                    returnList.Add(item);
                    amountPicked++;
                }
            }
            if (returnList.Count == 0)
            {
                int index = random.Next(items.Count);
                ContractItems temp = items.ElementAt(index);

                returnList.Add(temp);
            }

            return returnList;
        }
        public static List<ContractItems> getRandomContractItem(string type)
        {
            Random random = new Random();
            int index;

            List<ContractItems> list = new List<ContractItems>();
            List<ContractItems> temp = new List<ContractItems>();
            int maxAmount = 1;
            int added = 0;
            int chance = random.Next(101);
            switch (type)
            {
                case "easy":
                    foreach (ContractItems fuck in easyItems.Values)
                    {
                        temp.Add(fuck);
                    }
                    list = getItems(temp, 1);
                    break;
                case "medium":
                    foreach (ContractItems fuck in mediumItems.Values)
                    {
                        temp.Add(fuck);
                    }
                    list = getItems(temp, 2);
                    break;
                case "hard":
                    foreach (ContractItems fuck in mediumItems.Values)
                    {
                        temp.Add(fuck);
                    }
                    list = getItems(temp, 3);
                    break;
                default:
                    return null;
            }
            return list;

        }
        private TorchSessionManager sessionManager;
        public override void Init(ITorchBase torch)
        {
            base.Init(torch);
            Log.Info("Loading Space Trucking");
            path = StoragePath;

            sessionManager = Torch.Managers.GetManager<TorchSessionManager>();
            if (sessionManager != null)
            {
                sessionManager.SessionStateChanged += SessionChanged;
            }
        }

        public static Dictionary<ulong, Contract> activeContracts = new Dictionary<ulong, Contract>();
        public static void AddToEasyContractItems(ContractItems item)
        {
            if (easyItems.ContainsKey(item.ContractItemId))
            {
                easyItems.Remove(item.ContractItemId);
                easyItems.Add(item.ContractItemId, item);
            }
            else
            {
                easyItems.Add(item.ContractItemId, item);
            }
        }
        public static void AddToMediumContractItems(ContractItems item)
        {
            if (mediumItems.ContainsKey(item.ContractItemId))
            {
                mediumItems.Remove(item.ContractItemId);
                mediumItems.Add(item.ContractItemId, item);
            }
            else
            {
                mediumItems.Add(item.ContractItemId, item);
            }
        }
        public static void AddToHardContractItems(ContractItems item)
        {
            if (hardItems.ContainsKey(item.ContractItemId))
            {
                hardItems.Remove(item.ContractItemId);
                hardItems.Add(item.ContractItemId, item);
            }
            else
            {
                hardItems.Add(item.ContractItemId, item);
            }
        }

        public ContractItems ReadContractItem(String[] split)
        {
           foreach (String s in split)
            {
                s.Replace(" ", "");
            }
            ContractItems temp = new ContractItems();
            temp.ContractItemId = split[0];
            temp.ItemType = split[1];
            temp.SubType = split[2]; 
            temp.MinToDeliver = int.Parse(split[3]);
            temp.MaxToDeliver = int.Parse(split[4]);
            temp.MinPrice = int.Parse(split[5]);
            temp.MaxPrice = int.Parse(split[6]);
            temp.chance = int.Parse(split[7]);
            return temp;
        }

        private void SessionChanged(ITorchSession session, TorchSessionState state)
        {

            if (state == TorchSessionState.Loaded)
            {
                if (!System.IO.File.Exists(TruckingPlugin.path + "//SpaceTrucking"))
                {
                    System.IO.Directory.CreateDirectory(TruckingPlugin.path + "//SpaceTrucking");
                }
                if (!System.IO.File.Exists(TruckingPlugin.path + "//SpaceTrucking//deliveryLocations.txt"))
                {
                    MyGps bob = new MyGps();
                    Vector3D alan = new Vector3D(100, 100, 100);
                    bob.Coords = alan;
                    bob.Name = "Example GPS";
                    bob.DisplayName = "Example GPS";

                    File.WriteAllText(TruckingPlugin.path + "//SpaceTrucking//deliveryLocations.txt", bob.ToString());
                }
                if (!System.IO.File.Exists(TruckingPlugin.path + "//SpaceTrucking//definitions.csv"))
                {
                    StringBuilder ingots = new StringBuilder();
                    StringBuilder components = new StringBuilder();
                    StringBuilder ore = new StringBuilder();
                    StringBuilder ammo = new StringBuilder();
                    foreach (MyDefinitionBase def in MyDefinitionManager.Static.GetAllDefinitions())
                    {
                        if (def.Id.TypeId.ToString().Equals("MyObjectBuilder_Ingot"))
                        {

                            ingots.AppendLine(def.Id.TypeId.ToString().Replace("MyObjectBuilder_", "") + "," + def.Id.SubtypeId + ",1 " + ",10 " + ",20 " + ",50 " + ",100 ");
                        }
                        if (def.Id.TypeId.ToString().Equals("MyObjectBuilder_Component"))
                        {

                            components.AppendLine(def.Id.TypeId.ToString().Replace("MyObjectBuilder_", "") + "," + def.Id.SubtypeId + ",1 " + ",10 " + ",20 " + ", 50 " + ",100 ");
                        }
                        if (def.Id.TypeId.ToString().Equals("MyObjectBuilder_Ore"))
                        {
                            ore.AppendLine(def.Id.TypeId.ToString().Replace("MyObjectBuilder_", "") + "," + def.Id.SubtypeId + ",1 " + ",10 " + ",20 " + ",50 " + ",100 ");
                        }
                        if (def.Id.TypeId.ToString().Equals("MyObjectBuilder_AmmoMagazine"))
                        {
                            ammo.AppendLine(def.Id.TypeId.ToString().Replace("MyObjectBuilder_", "") + "," + def.Id.SubtypeId + ",1 " + ",10 " + ",20 " + ",50 " + ",100 ");
                        }

                    }
                    StringBuilder output = new StringBuilder();
                    output.AppendLine("TypeId, SubtypeId, minAmount, maxAmount, minPrice, maxPrice, percentageChance");
                    output.AppendLine(ingots.ToString());
                    output.AppendLine(components.ToString());
                    output.AppendLine(ore.ToString());
                    output.AppendLine(ammo.ToString());
                    File.WriteAllText(TruckingPlugin.path + "//SpaceTrucking//definitions.csv", output.ToString());
                }

                if (System.IO.File.Exists(TruckingPlugin.path + "//SpaceTrucking//deliveryLocations.txt"))
                {
                    String[] line = File.ReadAllLines(TruckingPlugin.path + "//SpaceTrucking//deliveryLocations.txt");
                    for (int i = 1; i < line.Length; i++)
                    {
                        if (ScanChat(line[i]) != null)
                        {
                            MyGps gpsRef = ScanChat(line[i]);
                            DeliveryLocations.Add(gpsRef);
                        }
                    }
                }
                if (System.IO.File.Exists(TruckingPlugin.path + "//SpaceTrucking//easy.csv"))
                {
                    String[] line = File.ReadAllLines(TruckingPlugin.path + "//SpaceTrucking//easy.csv");

                    for (int i = 1; i < line.Length; i++)
                    {
                    
                        String[] split = line[i].Split(',');
                        TruckingPlugin.AddToEasyContractItems(ReadContractItem(split));
                    }
                }
                else
                {
                    StringBuilder easy = new StringBuilder();
                    easy.AppendLine("ConractItemId,TypeId,SubtypeId,minAmount,maxAmount,minPrice,maxPrice,percentageChance");
                    easy.AppendLine("Easy Ingot1,Ingot,Iron,1,10,20,50,50");
                    easy.AppendLine("Easy Component1,Component, SteelPlate, 1,10,20,50,50");
                    easy.AppendLine("Easy Ore1,Ore,Iron,1,10,20,50,50");



                    if (!System.IO.File.Exists(TruckingPlugin.path + "//SpaceTrucking//easy.csv"))
                    {
                        File.WriteAllText(TruckingPlugin.path + "//SpaceTrucking//easy.csv", easy.ToString());
                    }

                }
                if (System.IO.File.Exists(TruckingPlugin.path + "//SpaceTrucking//medium.csv"))
                {
                    String[] line = File.ReadAllLines(TruckingPlugin.path + "//SpaceTrucking//medium.csv");

                    for (int i = 1; i < line.Length; i++)
                    {

                        String[] split = line[i].Split(',');
                        TruckingPlugin.AddToMediumContractItems(ReadContractItem(split));
                    }
                }
                else
                {
                    StringBuilder medium = new StringBuilder();
                    medium.AppendLine("ConractItemId,TypeId,SubtypeId,minAmount,maxAmount,minPrice,maxPrice,percentageChance");
                    medium.AppendLine("MediumIngot1,Ingot,Iron,1,10,20,50,50");
                    medium.AppendLine("MediumComponent1,Component,SteelPlate,1,10,20,50,50");
                    medium.AppendLine("MediumOre1,Ore,Iron,1,10,20,50,50");

                    if (!System.IO.File.Exists(TruckingPlugin.path + "//SpaceTrucking//medium.csv"))
                    {
                        File.WriteAllText(TruckingPlugin.path + "//SpaceTrucking//medium.csv", medium.ToString());
                    }

                }
                if (System.IO.File.Exists(TruckingPlugin.path + "//SpaceTrucking//hard.csv"))
                {
                    String[] line = File.ReadAllLines(TruckingPlugin.path + "//SpaceTrucking//hard.csv");

                    for (int i = 1; i < line.Length; i++)
                    {

                        String[] split = line[i].Split(',');
                        TruckingPlugin.AddToHardContractItems(ReadContractItem(split));
                    }
                }
                else
                {
                    if (!System.IO.File.Exists(TruckingPlugin.path + "//SpaceTrucking//hard.csv"))
                    {
                        StringBuilder hard = new StringBuilder();
                        hard.AppendLine("ConractItemId,TypeId,SubtypeId,minAmount,maxAmount,minPrice,maxPrice,percentageChance");
                        hard.AppendLine("HardIngot1,Ingot,Iron,1,10,20,50,50");
                        hard.AppendLine("HardComponent1,Component,SteelPlate,1,10,20,50,50");
                        hard.AppendLine("HardOre1,Ore,Iron,1,10,20,50,50");
                        File.WriteAllText(TruckingPlugin.path + "//SpaceTrucking//hard.csv", hard.ToString());
                    }
                }

            }


        }


        public static MyGps ScanChat(string input, string desc = null)
        {

            int num = 0;
            bool flag = true;
            MatchCollection matchCollection = Regex.Matches(input, "GPS:([^:]{0,32}):([\\d\\.-]*):([\\d\\.-]*):([\\d\\.-]*):");

            Color color = new Color(117, 201, 241);
            foreach (Match match in matchCollection)
            {
                string str = match.Groups[1].Value;
                double x;
                double y;
                double z;
                try
                {
                    x = Math.Round(double.Parse(match.Groups[2].Value, (IFormatProvider)CultureInfo.InvariantCulture), 2);
                    y = Math.Round(double.Parse(match.Groups[3].Value, (IFormatProvider)CultureInfo.InvariantCulture), 2);
                    z = Math.Round(double.Parse(match.Groups[4].Value, (IFormatProvider)CultureInfo.InvariantCulture), 2);
                    if (flag)
                        color = (Color)new ColorDefinitionRGBA(match.Groups[5].Value);
                }
                catch (SystemException ex)
                {
                    continue;
                }
                MyGps gps = new MyGps()
                {
                    Name = str,
                    Description = desc,
                    Coords = new Vector3D(x, y, z),
                    GPSColor = color,
                    ShowOnHud = false
                };
                gps.UpdateHash();

                return gps;
            }
            return null;
        }
    }
}
