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
        private Harmony? _harmony;

        private static readonly Dictionary<string, string> Translations = new()
        {
            ["Enabled"] = "启用",
            ["Enables or disables the mod"] = "启用或禁用此模组",
            ["Debug Mode"] = "调试模式",
            ["Show debug information"] = "显示调试信息",
            ["Indoors Only"] = "仅限室内",
            ["Only allow switches indoors"] = "仅允许在室内放置开关",
            ["Shop Price"] = "商店价格",
            ["The price of the light switch in the shop"] = "商店中电灯开关的价格",
            ["On Sound"] = "开启音效",
            ["The sound played when turning on"] = "打开时播放的声音",
            ["Off Sound"] = "关闭音效",
            ["The sound played when turning off"] = "关闭时播放的声音",
            ["Color Button"] = "按钮颜色",
            ["The color of the button"] = "按钮的颜色",
        };

        public override void Entry(IModHelper helper)
        {
            _harmony = new Harmony(ModManifest.UniqueID);

            Assembly? targetMod = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "LightSwitches");

            if (targetMod == null)
            {
                Monitor.Log("未找到 LightSwitches 程序集，跳过汉化。", LogLevel.Warn);
                return;
            }

            MethodInfo transpiler = typeof(ModEntry).GetMethod(
                nameof(Transpiler),
                BindingFlags.Static | BindingFlags.NonPublic
            )!;

            var harmonyTranspiler = new HarmonyMethod(transpiler);
            int patchedCount = 0;

            foreach (Type type in targetMod.GetTypes())
            {
                foreach (MethodInfo method in type.GetMethods(
                    BindingFlags.Public |
                    BindingFlags.NonPublic |
                    BindingFlags.Instance |
                    BindingFlags.Static |
                    BindingFlags.DeclaredOnly))
                {
                    if (method.GetMethodBody() == null) continue;

                    try
                    {
                        _harmony.Patch(method, transpiler: harmonyTranspiler);
                        patchedCount++;
                    }
                    catch
                    {
                        // 忽略无法 Patch 的方法
                    }
                }
            }

            Monitor.Log($"LightSwitches 汉化补丁已加载，共修补 {patchedCount} 个方法。", LogLevel.Info);
        }

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Ldstr &&
                    instruction.operand is string original &&
                    Translations.TryGetValue(original, out string? translated))
                {
                    instruction.operand = translated;
                }

                yield return instruction;
            }
        }
    }
}
