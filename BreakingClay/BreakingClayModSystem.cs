using System;
using System.Linq;
using ConfigLib;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Util;

namespace BreakingClay;

public class BreakingClayModSystem : ModSystem
{
    public static ClayBreakConfig Config;
    private Harmony HarmonyInstance;
    public static ILogger Logger;
    public static string ConfigName = "BreakingClay.json";
    
    public override void StartPre(ICoreAPI api)
    {
        Logger = Mod.Logger;
        try
        {
            Config = api.LoadModConfig<ClayBreakConfig>(ConfigName);
            if (Config == null) {
                Config = new ClayBreakConfig();
                api.StoreModConfig(Config, ConfigName);
            }
        } catch (Exception e) {
            Logger.Error("Failed to load config, you probably made a typo: {0}", e);
            Config = new ClayBreakConfig();
        }
    }

    public override void Start(ICoreAPI api)
    {
        base.Start(api);
        HarmonyInstance = new Harmony(Mod.Info.ModID);
        HarmonyInstance.PatchAll();
        api.RegisterBlockEntityBehaviorClass($"{Mod.Info.ModID}:BreakableClay", typeof (BlockEntityBehaviorBreakingClay));
    }
    
    public override void AssetsFinalize(ICoreAPI api)
    {
        base.AssetsFinalize(api);
        if (!api.Side.IsServer()) return;
        foreach (var block in api.World.Blocks.Where(b => b?.Code != null))
        {
            if (!WildcardUtil.Match(Config.MoldSelector, block.Code.Path)) continue;
            Logger.Warning($"Adding behavior to {block.Code}");
            block.BlockEntityBehaviors = block.BlockEntityBehaviors.Append(new BlockEntityBehaviorType()
            {
                Name = $"{Mod.Info.ModID}:BreakableClay",
                properties = new JsonObject(JObject.FromObject(new
                {
                    breakChance = Config.MoldBreakChance
                }))
            });
        }
    }

    public override void Dispose()
    {
        this.HarmonyInstance.UnpatchAll();
        base.Dispose();
    }
}