using System;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace BreakingClay;

[HarmonyPatch]
  public static class BreakingClayPatches
  {
    private static long prevSecond;

    [HarmonyPostfix]
    [HarmonyPatch(typeof (BlockEntityToolMold), "TryTakeContents")]
    public static void CheckForTakeFromToolMold(
      BlockEntityToolMold __instance,
      IPlayer byPlayer,
      bool __result)
    {
      if (!__result)
        return;
      ((BlockEntity) __instance).GetBehavior<BlockEntityBehaviorBreakingClay>()?.DamageBlock(byPlayer);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof (BlockEntityIngotMold), "TryTakeIngot")]
    public static void CheckForTakeFromIngotMold(
      BlockEntityIngotMold __instance,
      IPlayer byPlayer,
      Vec3d hitPosition,
      bool __result)
    {
      BreakingClayModSystem.Logger.Warning("Ingot Mold Patch!! Chance: " + BreakingClayModSystem.Config.MoldBreakChance);
      if (!__result)
        return;
      ((BlockEntity) __instance).GetBehavior<BlockEntityBehaviorBreakingClay>()?.DamageBlock(byPlayer, hitPosition);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof (BlockMeal), "tryFinishEatMeal")]
    public static void AfterFinishEatingBowlMeal(
      BlockMeal __instance,
      float secondsUsed,
      ItemSlot slot,
      EntityAgent byEntity,
      bool handleAllServingsConsumed,
      bool __result)
    {
      if (((Entity) byEntity).World.Side != EnumAppSide.Server || BreakingClayPatches.prevSecond == ((Entity) byEntity).World.Calendar.ElapsedSeconds)
        return;
      BreakingClayPatches.prevSecond = ((Entity) byEntity).World.Calendar.ElapsedSeconds;
      if (slot.Itemstack.Block is BlockMeal || (double) ((Entity) byEntity).World.Rand.NextSingle() >= BreakingClayModSystem.Config.BowlBreakChance)
        return;
      slot.TakeOutWhole();
      ((Entity) byEntity).World.PlaySoundAt(new AssetLocation("sounds/effect/toolbreak"), ((Entity) byEntity).ServerPos.X, ((Entity) byEntity).ServerPos.Y, ((Entity) byEntity).ServerPos.Z, (IPlayer) null, 1f, 16f, 1f);
      (((EntityPlayer)byEntity).Player as IServerPlayer)?.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("breakingclay:bowl-broke", Array.Empty<object>()), (EnumChatType) 2, (string) null);
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof (BlockSmeltedContainer), "OnHeldInteractStop")]
    public static void OnStopPouringCrucible(
      float secondsUsed,
      ItemSlot slot,
      EntityAgent byEntity,
      BlockSelection blockSel,
      EntitySelection entitySel)
    {
      if (((Entity) byEntity).World.Side != EnumAppSide.Server || BreakingClayPatches.prevSecond == ((Entity) byEntity).World.Calendar.ElapsedSeconds)
        return;
      BreakingClayPatches.prevSecond = ((Entity) byEntity).World.Calendar.ElapsedSeconds;
      if (slot.Itemstack.Block is BlockSmeltedContainer || (double) ((Entity) byEntity).World.Rand.NextSingle() >= (double) BreakingClayModSystem.Config.CrucibleBreakChance)
        return;
      ((Entity) byEntity).Api.World.RegisterCallback((Action<float>) (x => slot.TakeOutWhole()), 0);
      ((Entity) byEntity).World.PlaySoundAt(new AssetLocation("sounds/effect/toolbreak"), ((Entity) byEntity).ServerPos.X, ((Entity) byEntity).ServerPos.Y, ((Entity) byEntity).ServerPos.Z, (IPlayer) null, 1f, 16f, 1f);
      ((IServerPlayer)((EntityPlayer)byEntity).Player).SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("breakingclay:crucible-broke", Array.Empty<object>()), (EnumChatType) 2, (string) null);
    }
  }