using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using StardewModdingAPI;

namespace LightSwitchesChinese
{
    public class ModEntry : Mod
    {
        internal static ModEntry Instance;
        internal static IMonitor ModMonitor;
        private Harmony harmony;

        // 直接config中的英文名称
        internal static readonly Dictionary<string, string> NameDict = new()
        {
            ["Mod Enabled"] = "启用模组",
            ["Debug"] = "调试模式",
            ["Indoors Only"] = "仅室内",
            ["Shop Price"] = "商店价格",
            ["On Sound"] = "开启音效",
            ["Off Sound"] = "关闭音效",
            ["Color Button"] = "按钮颜色",
        };

        internal static readonly Dictionary<string, string> TooltipDict = new()
        {
            ["Enable or disable the mod."] = "启用或禁用此模组。",
            ["Enable debug logging."] = "启用调试日志输出。",
            ["Only allow light switches indoors."] = "仅在室内允许使用电灯开关。",
            ["The price of the light switch in the shop."] = "电灯开关在商店中的售价。",
            ["The sound played when turning the switch on."] = "打开开关时播放的音效。",
            ["The sound played when turning the switch off."] = "关闭开关时播放的音效。",
            ["The color of the button."] = "选择开关按钮的颜色。",
        };

        public override void Entry(IModHelper helper)
        {
            Instance = this;
            ModMonitor = Monitor;

            harmony = new Harmony(ModManifest.UniqueID);

            try
            {
                var gmcmType = AccessTools.TypeByName("GenericModConfigMenu.Framework.ModConfigMenu") 
                               ?? AccessTools.TypeByName("GenericModConfigMenu.ModConfigMenu");

                if (gmcmType != null)
                {
                    var methods = new[] { "AddBoolOption", "AddNumberOption", "AddTextOption", "AddColorOption" };
                    foreach (var name in methods)
                    {
                        var method = AccessTools.Method(gmcmType, name);
                        if (method != null)
                        {
                            harmony.Patch(method, prefix: new HarmonyMethod(typeof(ModEntry), nameof(Prefix)));
                        }
                    }
                    ModMonitor.Log("GMCM 补丁加载成功。", LogLevel.Info);
                }
                else
                {
                    ModMonitor.Log("找不到 GenericModConfigMenu 的目标类，汉化补丁无法生效。请确认 GMCM 已安装。", LogLevel.Warn);
                }
            }
            catch (Exception ex)
            {
                ModMonitor.Log($"应用 Harmony 补丁时发生错误: {ex}", LogLevel.Error);
            }
        }

        public static void Prefix(object[] __args)
        {
            // 参数检查
            if (__args.Length < 4 || __args[0] is not IManifest manifest) return;
            if (manifest.UniqueID != "aedenthorn.LightSwitches") return;

            // 检查 name 参数 (索引 3，类型是 Func<string>)
            if (__args[3] is Func<string> nameFunc)
            {
                try
                {
                    string originalName = nameFunc();
                    if (NameDict.TryGetValue(originalName, out var cnName))
                    {
                        // 替换 name 的委托
                        __args[3] = (Func<string>)(() => cnName);
                    }
                }
                catch { /* 忽略 */ }
            }

            // 检查 tooltip 参数 (索引 4，类型是 Func<string>)
            if (__args.Length > 4 && __args[4] is Func<string> tooltipFunc)
            {
                try
                {
                    string originalTooltip = tooltipFunc();
                    if (TooltipDict.TryGetValue(originalTooltip, out var cnTooltip))
                    {
                        // 替换 tooltip 的委托
                        __args[4] = (Func<string>)(() => cnTooltip);
                    }
                }
                catch { /* 忽略 */ }
            }
        }
    }
}
