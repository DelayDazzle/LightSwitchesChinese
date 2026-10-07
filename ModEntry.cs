using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using StardewModdingAPI;

namespace LightSwitchesChinese
{
    public class ModEntry : Mod
    {
        internal static IMonitor ModMonitor;

        public override void Entry(IModHelper helper)
        {
            ModMonitor = Monitor;
            var harmony = new Harmony(ModManifest.UniqueID);
            harmony.PatchAll();

            var targetType = AccessTools.TypeByName("LightSwitches.ModEntry");
            if (targetType == null)
            {
                ModMonitor.Log("找不到目标类 LightSwitches.ModEntry，汉化补丁无法生效。", LogLevel.Error);
            }
            else
            {
                ModMonitor.Log("LightSwitches 汉化补丁已加载。", LogLevel.Info);
            }
        }
    }

    [HarmonyPatch]
    public static class LightSwitchesPatcher
    {
        public static IEnumerable<MethodBase> TargetMethods()
        {
            var type = AccessTools.TypeByName("LightSwitches.ModEntry");
            if (type == null) yield break;

            // 尝试补丁所有可能注册 GMCM 的方法
            var methodNames = new[]
            {
                "GameLoop_GameLaunched",
                "RegisterControlsGenericModConfigMenu",
                "RegisterControls"
            };

            foreach (var name in methodNames)
            {
                var m = AccessTools.Method(type, name);
                if (m != null) yield return m;
            }
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var translations = new Dictionary<string, string>
            {
                // 选项名称
                ["Mod Enabled"] = "启用模组",
                ["Debug"] = "调试模式",
                ["Indoors Only"] = "仅室内",
                ["Shop Price"] = "商店价格",
                ["On Sound"] = "开启音效",
                ["Off Sound"] = "关闭音效",
                ["Color Button"] = "按钮颜色",
                ["Options"] = "选项",

                // 工具提示描述
                ["Enables or disables the mod"] = "启用或禁用此模组。",
                ["Enable debug logging"] = "启用调试日志输出。",
                ["Only allow light switches indoors"] = "仅在室内允许使用电灯开关。",
                ["The price of the light switch in the shop"] = "电灯开关在商店中的售价。",
                ["The sound played when turning the switch on"] = "打开开关时播放的音效。",
                ["The sound played when turning the switch off"] = "关闭开关时播放的音效。",
                ["The color of the button"] = "选择开关按钮的颜色。",
            };

            foreach (var instr in instructions)
            {
                if (instr.opcode == OpCodes.Ldstr && instr.operand is string s)
                {
                    if (translations.TryGetValue(s, out var cn))
                    {
                        instr.operand = cn;
                    }
                }
                yield return instr;
            }
        }
    }
}
