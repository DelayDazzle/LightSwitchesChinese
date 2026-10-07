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

        // 字典需要包含 i18n 的 key 和可能的英文字符串
        private static readonly Dictionary<string, string> Translations = new()
        {
            // 英文标签（如果有）
            ["Enabled"] = "启用",
            ["Debug"] = "调试模式",
            ["Indoors Only"] = "仅限室内",
            ["Shop Price"] = "商店价格",
            ["On Sound"] = "开启音效",
            ["Off Sound"] = "关闭音效",
            ["Color Button"] = "按钮颜色",

            // i18n 翻译键（原模组调用的实际 key）
            ["ModEnabled.name"] = "启用",
            ["ModEnabled.description"] = "启用或禁用此模组",
            ["Debug.name"] = "调试模式",
            ["Debug.description"] = "显示调试信息",
            ["IndoorsOnly.name"] = "仅限室内",
            ["IndoorsOnly.description"] = "仅允许在室内放置开关",
            ["ShopPrice.name"] = "商店价格",
            ["ShopPrice.description"] = "商店中电灯开关的价格",
            ["OnSound.name"] = "开启音效",
            ["OnSound.description"] = "打开时播放的声音",
            ["OffSound.name"] = "关闭音效",
            ["OffSound.description"] = "关闭时播放的声音",
            ["ColorButton.name"] = "按钮颜色",
            ["ColorButton.description"] = "按钮的颜色",
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
                    BindingFlags.Public | BindingFlags.NonPublic |
                    BindingFlags.Instance | BindingFlags.Static |
                    BindingFlags.DeclaredOnly))
                {
                    try
                    {
                        if (method.GetMethodBody() == null) continue;
                        _harmony.Patch(method, transpiler: harmonyTranspiler);
                        patchedCount++;
                    }
                    catch { }
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
