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

                if (gmcmType == null)
                {
                    ModMonitor.Log("找不到 GenericModConfigMenu 的目标类，汉化补丁无法生效。请确认 GMCM 已安装。", LogLevel.Warn);
                    return;
                }

                var methods = new[] { "AddBoolOption", "AddNumberOption", "AddTextOption", "AddColorOption" };
                foreach (var methodName in methods)
                {
                    var method = AccessTools.Method(gmcmType, methodName);
                    if (method == null) continue;

                    // 判断参数类型，使用对应的 Prefix 打补丁
                    var parameters = method.GetParameters();
                    bool hasFuncName = false;
                    bool hasStringName = false;

                    foreach (var p in parameters)
                    {
                        if (p.Name == "name")
                        {
                            if (p.ParameterType == typeof(Func<string>)) hasFuncName = true;
                            else if (p.ParameterType == typeof(string)) hasStringName = true;
                        }
                    }

                    if (hasFuncName)
                    {
                        harmony.Patch(method, prefix: new HarmonyMethod(typeof(ModEntry), nameof(Prefix_Func)));
                    }
                    else if (hasStringName)
                    {
                        harmony.Patch(method, prefix: new HarmonyMethod(typeof(ModEntry), nameof(Prefix_String)));
                    }
                }

                ModMonitor.Log("GMCM 补丁加载成功。", LogLevel.Info);
            }
            catch (Exception ex)
            {
                ModMonitor.Log($"应用 Harmony 补丁时发生错误: {ex}", LogLevel.Error);
            }
        }

        // 处理 Func<string> 类型的 name 和 tooltip（使用 ref 引用原参数）
        public static void Prefix_Func(IManifest mod, ref Func<string> name, ref Func<string> tooltip)
        {
            if (mod.UniqueID != "aedenthorn.LightSwitches") return;

            if (name != null)
            {
                try
                {
                    string originalName = name();
                    if (NameDict.TryGetValue(originalName, out var cnName))
                        name = () => cnName;
                }
                catch { }
            }

            if (tooltip != null)
            {
                try
                {
                    string originalTooltip = tooltip();
                    if (TooltipDict.TryGetValue(originalTooltip, out var cnTooltip))
                        tooltip = () => cnTooltip;
                }
                catch { }
            }
        }

        // 处理 string 类型的 name 和 tooltip（使用 ref 引用原参数）
        public static void Prefix_String(IManifest mod, ref string name, ref string tooltip)
        {
            if (mod.UniqueID != "aedenthorn.LightSwitches") return;

            if (name != null && NameDict.TryGetValue(name, out var cnName))
                name = cnName;

            if (tooltip != null && TooltipDict.TryGetValue(tooltip, out var cnTooltip))
                tooltip = cnTooltip;
        }
    }
}
