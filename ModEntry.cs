using System;
using System.Collections.Generic;
using System.Linq;
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

            // 找到 LightSwitches 程序集
            var assembly = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "LightSwitches");

            if (assembly == null)
            {
                ModMonitor.Log("找不到 LightSwitches 程序集，汉化补丁无法生效。", LogLevel.Error);
                return;
            }

            int patchedCount = 0;
            foreach (var type in assembly.GetTypes())
            {
                foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
                {
                    try
                    {
                        if (method.GetMethodBody() == null) continue;
                        // 跳过泛型方法，Harmony 不支持
                        if (method.IsGenericMethod) continue;

                        harmony.Patch(method, transpiler: new HarmonyMethod(typeof(ModEntry), nameof(Transpiler)));
                        patchedCount++;
                    }
                    catch
                    {
                        // 某些方法可能无法修补，忽略
                    }
                }
            }

            ModMonitor.Log($"LightSwitches 汉化补丁已加载，共修补 {patchedCount} 个方法。", LogLevel.Info);
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

                // 工具提示
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
