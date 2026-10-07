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

        private const string TargetModId = "aedenthorn.LightSwitches";

        // 既覆盖 dll 中直接出现的英文字符串，也覆盖 i18n 翻译键返回的英文值
        private static readonly Dictionary<string, string> Translations = new()
        {
            // ==== 如果 dll 里是字符串字面量 ====
            ["Enabled"] = "启用",
            ["Debug"] = "调试模式",
            ["Indoors Only"] = "仅限室内",
            ["Shop Price"] = "商店价格",
            ["On Sound"] = "开启音效",
            ["Off Sound"] = "关闭音效",
            ["Color Button"] = "按钮颜色",

            ["Enables or disables the mod"] = "启用或禁用此模组",
            ["Show debug information"] = "显示调试信息",
            ["Only allow switches indoors"] = "仅允许在室内放置开关",
            ["The price of the light switch in the shop"] = "商店中电灯开关的价格",
            ["The sound played when turning on"] = "打开时播放的声音",
            ["The sound played when turning off"] = "关闭时播放的声音",
            ["The color of the button"] = "按钮的颜色",

            // ==== 如果走的是 i18n 的翻译键 ====
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

            // aedenthorn 常见命名风格（带 config. 前缀 / 大写键）
            ["config.enabled.name"] = "启用",
            ["config.enabled.description"] = "启用或禁用此模组",
            ["config.debug.name"] = "调试模式",
            ["config.debug.description"] = "显示调试信息",
            ["config.indoors-only.name"] = "仅限室内",
            ["config.indoors-only.description"] = "仅允许在室内放置开关",
            ["config.shop-price.name"] = "商店价格",
            ["config.shop-price.description"] = "商店中电灯开关的价格",
            ["config.on-sound.name"] = "开启音效",
            ["config.on-sound.description"] = "打开时播放的声音",
            ["config.off-sound.name"] = "关闭音效",
            ["config.off-sound.description"] = "关闭时播放的声音",
            ["config.color-button.name"] = "按钮颜色",
            ["config.color-button.description"] = "按钮的颜色",
        };

        public override void Entry(IModHelper helper)
        {
            _harmony = new Harmony(ModManifest.UniqueID);

            // ==== 1. 修补 dll 中直接出现的英文字符串字面量 ====
            Assembly? targetMod = AppDomain.CurrentDomain.GetAssemblies()
                .FirstOrDefault(a => a.GetName().Name == "LightSwitches");

            if (targetMod == null)
            {
                Monitor.Log("未找到 LightSwitches 程序集，跳过字符串字面量汉化。", LogLevel.Warn);
            }
            else
            {
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
                Monitor.Log($"LightSwitches 字符串字面量修补：{patchedCount} 个方法。", LogLevel.Info);
            }

            // ==== 2. 拦截 TranslationHelper.Get，替换 i18n 返回的英文 ====
            try
            {
                var postfix = new HarmonyMethod(
                    typeof(ModEntry).GetMethod(
                        nameof(TranslationGetPostfix),
                        BindingFlags.Static | BindingFlags.NonPublic)!);

                var getMethod1 = AccessTools.Method(
                    typeof(TranslationHelper),
                    nameof(TranslationHelper.Get),
                    new[] { typeof(string) });

                var getMethod2 = AccessTools.Method(
                    typeof(TranslationHelper),
                    nameof(TranslationHelper.Get),
                    new[] { typeof(string), typeof(object) });

                int cnt = 0;
                if (getMethod1 != null) { _harmony.Patch(getMethod1, postfix: postfix); cnt++; }
                if (getMethod2 != null) { _harmony.Patch(getMethod2, postfix: postfix); cnt++; }

                Monitor.Log($"已拦截 TranslationHelper.Get 的 {cnt} 个重载。", LogLevel.Info);
            }
            catch (Exception ex)
            {
                Monitor.Log($"拦截 TranslationHelper.Get 失败：{ex}", LogLevel.Error);
            }
        }

        private static void TranslationGetPostfix(TranslationHelper __instance, ref string __result)
        {
            if (string.IsNullOrEmpty(__result)) return;
            if (!IsTargetHelper(__instance)) return;

            if (Translations.TryGetValue(__result, out string? translated))
            {
                __result = translated;
            }
        }

        private static bool IsTargetHelper(TranslationHelper helper)
        {
            try
            {
                // TranslationHelper 内部持有 IManifest，字段名可能不固定，直接按类型找
                FieldInfo? manifestField = AccessTools
                    .GetDeclaredFields(typeof(TranslationHelper))
                    .FirstOrDefault(f => typeof(IManifest).IsAssignableFrom(f.FieldType));

                if (manifestField == null) return false;

                var manifest = manifestField.GetValue(helper) as IManifest;
                return manifest?.UniqueID == TargetModId;
            }
            catch
            {
                return false;
            }
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
