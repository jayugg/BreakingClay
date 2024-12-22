using System;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace BreakingClay;

public class BlockEntityBehaviorBreakingClay : BlockEntityBehavior
  {
    private long prevMs;
    private float breakChance;

    public BlockEntityBehaviorBreakingClay(BlockEntity blockentity)
      : base(blockentity)
    {
    }

    public override void Initialize(ICoreAPI api, JsonObject props)
    {
      base.Initialize(api, props);
      this.breakChance = props["breakChance"].AsFloat();
    }

    public void DamageBlock(IPlayer player, Vec3d hitPos = null)
    {
      if (this.Api.Side != EnumAppSide.Server || this.prevMs == this.Api.World.Calendar.ElapsedSeconds)
        return;
      this.prevMs = this.Api.World.Calendar.ElapsedSeconds;
      float chanceForMoldToBreak = breakChance;
      if (this.Api.World.Rand.NextSingle() > (double) chanceForMoldToBreak)
        return;
      if (this.Blockentity is BlockEntityToolMold)
        this.DamageToolMold();
      else if (this.Blockentity is BlockEntityIngotMold)
        this.DamageIngotMold(hitPos);
      (player as IServerPlayer)?.SendMessage(GlobalConstants.GeneralChatGroup, Lang.Get("breakingclay:mold-broke", Array.Empty<object>()), (EnumChatType) 2);
    }

    private void DamageToolMold()
    {
      this.Api.World.BlockAccessor.SetBlock(0, this.Pos);
      this.Api.World.BlockAccessor.MarkBlockDirty(this.Pos);
      this.Api.World.PlaySoundAt(new AssetLocation("sounds/effect/toolbreak"), this.Pos.X, this.Pos.Y, this.Pos.Z, null, 1f, 16f);
    }

    private void DamageIngotMold(Vec3d hitPos)
    {
      this.Api.World.PlaySoundAt(new AssetLocation("sounds/effect/toolbreak"), this.Pos.X, this.Pos.Y, this.Pos.Z, null, 1f, 16f);
      BlockEntityIngotMold blockentity = this.Blockentity as BlockEntityIngotMold;
      if (blockentity != null)
      {
        --blockentity.quantityMolds;
        IngotMoldRenderer ingotMoldRenderer =
          Traverse.Create(blockentity).Field<IngotMoldRenderer>("ingotRenderer").Value;
        if (ingotMoldRenderer != null)
          ingotMoldRenderer.QuantityMolds = blockentity.quantityMolds;
        if (blockentity.quantityMolds == 0)
        {
          this.Api.World.BlockAccessor.SetBlock(0, this.Pos);
        }
        else
        {
          if (hitPos.X < 0.5)
          {
            blockentity.contentsLeft = blockentity.contentsRight;
            blockentity.fillLevelLeft = blockentity.fillLevelRight;
            blockentity.contentsRight = null;
            blockentity.fillLevelRight = 0;
          }

          blockentity.MarkDirty(true);
        }
      }
    }
  }