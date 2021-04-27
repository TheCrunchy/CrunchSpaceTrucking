﻿using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Blocks;
using Sandbox.Game.GameSystems.BankingAndCurrency;
using Sandbox.Game.SessionComponents;
using Sandbox.Game.World;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Torch.Managers.PatchManager;
using Torch.Mod;
using Torch.Mod.Messages;
using VRage.Game;
using VRageMath;

namespace CrunchSpaceTrucking
{
    [PatchShim]
    public static class MyStorePatch
    {

        internal static readonly MethodInfo update =
            typeof(MyStoreBlock).GetMethod("BuyFromPlayer", BindingFlags.Instance | BindingFlags.NonPublic) ??
            throw new Exception("Failed to find patch method");

        internal static readonly MethodInfo storePatch =
            typeof(MyStorePatch).GetMethod(nameof(StorePatchMethod), BindingFlags.Static | BindingFlags.Public) ??
            throw new Exception("Failed to find patch method");

        public static void Patch(PatchContext ctx)
        {

            ctx.GetPattern(update).Prefixes.Add(storePatch);
        }

        public static Boolean StorePatchMethod(MyStoreBlock __instance, long id, int amount, long targetEntityId, MyPlayer player, MyAccountInfo playerAccountInfo)
        {

            MyStoreItem storeItem = (MyStoreItem)null;
            bool proceed = false;
            foreach (MyStoreItem playerItem in __instance.PlayerItems)
            {

                MyCubeGrid grid = __instance.CubeGrid;
                if (FacUtils.GetFactionTag(FacUtils.GetOwner(grid)) != null && FacUtils.GetFactionTag(FacUtils.GetOwner(grid)).Length > 3 && TruckingPlugin.config.NPCGridContracts)
                {
                    proceed = true;
                }
                if (!grid.Editable || !grid.DestructibleBlocks)
                {
                    proceed = true;
                }

                if (__instance.DisplayNameText != null && __instance.DisplayNameText.ToLower().Contains("hauling contracts") && proceed)
                {

                    if (playerItem.Id == id)
                    {
                        storeItem = playerItem;
                        break;
                    }

                }
            }
            if (storeItem != null && proceed)
            {
                if (MyBankingSystem.GetBalance(player.Identity.IdentityId) >= storeItem.PricePerUnit)
                {
                    if (!TruckingPlugin.GenerateContract(player.Id.SteamId, player.Identity.IdentityId))
                    {
                        return false;
                    }
                    else
                    {
                        MyBankingSystem.ChangeBalance(player.Identity.IdentityId, (storeItem.PricePerUnit * -1));
                    }
                }
            }
            return false;
        }
    }
}