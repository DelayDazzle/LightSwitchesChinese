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

        // 把翻译字典放在类级别，方便 Prefix 访问
        internal static readonly Dictionary<string, string> NameDict = new()
        {
            ["ModEnabled"] = "启用模组",
            ["Debug"] = "调试模式",
            ["IndoorsOnly"] = "仅室内",
            ["ShopPrice"] = "商店价格",
            ["OnSound"] = "开启音效",
            ["OffSound"] = "关闭音效",
            ["ColorButton"] = "按钮颜色",
        };

        internal static readonly Dictionary<string, string> TooltipDict = new()
        {
            ["ModEnabled"] = "启用或禁用此模组。",
            ["Debug"] = "启用调试日志输出。",
            ["IndoorsOnly"] = "仅在室内允许使用电灯开关。",
            ["ShopPrice"] = "电灯开关在商店中的售价。",
            ["OnSound"] = "打开开关时播放的音效。",
            ["OffSound"] = "关闭开关时播放的音效。",
            ["ColorButton"] = "选择开关按钮的颜色。",
        };

        public override void Entry(IModHelper helper)
        {
            Instance = this;
            ModMonitor = Monitor;

            harmony = new Harmony(ModManifest.UniqueID);

            try
            {
                // 动态查找 GMCM 的实现类
                var gmcmType = AccessTools.TypeByName("GenericModConfigMenu.Framework.ModConfigMenu");
                if (gmcmType == null)
                {
                    var apiType = AccessTools.TypeByName("GenericModConfigMenu.IGenericModConfigMenuApi");
                    if (apiType != null)
                    {
                        foreach (var type in AccessTools.AllTypes())
                        {
                            if (apiType.IsAssignableFrom(type) && !type.IsInterface && !type.IsAbstract)
                            {
                                gmcmType = type;
                                break;
                            }
                        }
                    }
                }

                if (gmcmType != null)
                {
                    // 手动精确修补，避免 PatchAll 崩溃
                    var methods = new[] { "AddBoolOption", "AddNumberOption", "AddTextOption", "AddColorOption" };
                    foreach (var name in methods)
                    {
                        var method = AccessTools.Method(gmcmType, name);
                        if (method != null)
                        {
                            harmony.Patch(method, prefix: new HarmonyMethod(typeof(ModEntry), nameof(Prefix)));
                        }
                    }
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

        // 直接作为静态方法供 Harmony 调用
        public static void Prefix(object[] __args)
        {
            // 检查第一个参数是否是 IManifest 且 UniqueID 为 LightSwitches
            if (__args.Length == 0 || __args[0] is not IManifest manifest) return;
            if (manifest.UniqueID != "aedenthorn.LightSwitches") return;

            // 找到 fieldId（string 类型的参数，通常在最后）
            string fieldId = null;
            for (int i = __args.Length - 1; i >= 0; i--)
            {
                if (__args[i] is string s)
                {
                    fieldId = s;
                    break;
                }
            }

            if (fieldId == null) return;

            if (NameDict.TryGetValue(fieldId, out var cnName))
            {
                // 找到 name 参数（索引 3，类型是 Func<string>）
                if (__args.Length > 3 && __args[3] is Func<string>)
                {
                    __args[3] = (Func<string>)(() => cnName);
                }
            }

            if (TooltipDict.TryGetValue(fieldId, out var cnTooltip))
            {
                // 找到 tooltip 参数（索引 4，类型是 Func<string>）
                if (__args.Length > 4 && __args[4] is Func<string>)
                {
                    __args[4] = (Func<string>)(() => cnTooltip);
                }
            }
        }
    }
}
