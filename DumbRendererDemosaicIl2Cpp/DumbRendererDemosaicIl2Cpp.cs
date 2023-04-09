using System;
using System.Linq;
using BepInEx;
using BepInEx.IL2CPP;
using BepInEx.Logging;
using DemosaicCommon;
using UnhollowerRuntimeLib;
using UnityEngine;
using BepInEx.Configuration;
using UnityEngine.Rendering;
using System.IO;
using System.Collections.Generic;

namespace DumbRendererDemosaicIl2Cpp
{
    public static class PluginInfo_
    {
        public const string PLUGIN_GUID = "dumb.renderer.demosaic.Il2Cpp";
        public const string PLUGIN_NAME = "DumbRendererDemosaicIl2Cpp";
        public const string PLUGIN_VERSION = Metadata.Version;
    }

    [BepInPlugin(PluginInfo_.PLUGIN_GUID, PluginInfo_.PLUGIN_NAME, PluginInfo_.PLUGIN_VERSION)]
    public class DumbRendererDemosaicPlugin : BasePlugin
    {
        internal static ManualLogSource log;

        public override void Load()
        {
            log = base.Log;

            ClassInjector.RegisterTypeInIl2Cpp<DumbRendererDemosaic>();
            AddComponent<DumbRendererDemosaic>();
        }
    }

    public class DumbRendererDemosaic : MonoBehaviour
    {
        private Renderer[] Renderers;
        private Renderer[] RenderersNew;
        private int NewRendererCount = -1;
        private bool bCheckMode = true; // 화면의 모든 renderer를 체크 했다면 변화가 있을 때 까지 비교를 skip
        private bool bRenderIndexSmallerThanLast = false;   // index가 한바퀴 도는지를 체크하기 위해.
        private int RenderIndex = -1;
        private int RenderIndexLast = -1;    // renserer에 변화가 있기 전까지 마지막 처리했던 index
        private int RendererCount;
        private const string renamed = "[Renamed]";
        private const string SectionName = "Config";
        private bool bReGetRenderers = false;   // renderer만 다시 갱신하는 모드. RenderIndex를 웬만하면 안 건드림.
        private TimeSpan looptime;
        private int LoopTimeLimit = 1;
        private int maximumLOD = -2;
        private static ConfigFile configFile { get; set; }
        public static ConfigEntry<string> FilterStrings { get; set; }
        public static ConfigEntry<string> ForceRemoveFilter { get; set; }
        //public static ConfigEntry<int> maximumLOD { get; set; }
        public static ConfigEntry<bool> UseUnlitShader { get; set; }
        public static ConfigEntry<bool> NameDumpMode { get; set; }

        private static bool bOneRendererMode = false;
        private DateTime now = DateTime.Now;
        private static Material _unlitMaterial = null;
        private Renderer FirstRenderer = null;
        private int Indextype = 0;  // 0: renderer 바뀌면 처음부터, 1: RenderIndex 번호 유지
        bool bUpdateunlitMaterial = true;
        static bool dumpMode = false;
        private static List<string> dumpKeys;
        static string dumpFilename;

        public void Log(LogLevel lv, object data)
        {
#if DEBUG
            DumbRendererDemosaicPlugin.log.Log(lv, data);
#endif
            return;
        }

        private void Awake()
        {
            //Log(LogLevel.Info, $"Material_SetColor {Material_SetColor}");
            configFile = new ConfigFile($"{Paths.PluginPath}\\{PluginInfo_.PLUGIN_NAME}.ini", false);
            dumpFilename = $"{Paths.PluginPath}\\{PluginInfo_.PLUGIN_NAME}.csv";

            FilterStrings = configFile.Bind<string>(
                SectionName,
                "FilterStrings",
                "",
                "Parts of strings what compared with. (Ricca needs VColMosaic)"
                );
            UseUnlitShader = configFile.Bind<bool>(
                SectionName,
                "UseUnlitMaterial",
                false,
                "Find Unlit material and replace censored one with unlit shader what found. (Fallen Princess Knight)"
                );
            ForceRemoveFilter = configFile.Bind<string>(
                SectionName,
                "ForceRemoveFilter",
                "",
                "To must remove material or shader's filter (Elf Knight Giselle needs 'MoveMoza')"
                );
            NameDumpMode = configFile.Bind<bool>(
                SectionName,
                "NameDumpMode",
                false,
                "Dumps materials and shaders name. Once runned, changed it to 'false'."
                );

            dumpMode = NameDumpMode.Value;
            NameDumpMode.Value = false;
            dumpKeys = new List<string>();
            if (dumpMode && File.Exists(dumpFilename))
            {
                string key;
                using (StreamReader sr = new StreamReader(dumpFilename))
                {
                    while ((key = sr.ReadLine()) != null)
                        dumpKeys.Add(key);
                }
            }

            // 릿카 체크
            if (Paths.ProcessName == "HolyKnightRicca")
            {
                FilterStrings.Value = "VColMosaic";
            }
            else if (Paths.ProcessName == "RamboHentai")
            {
                FilterStrings.Value = "pixelate";
            }

            if (string.IsNullOrEmpty(FilterStrings.Value))
            {
                FilterStrings.Value = MozaicTools.MozaicNameParts_Default;
            }
            MozaicTools.Init_MosaicNameParts(FilterStrings.Value);
            MozaicTools.Init_MosaicNameParts_ForceRemove(ForceRemoveFilter.Value);
            looptime = new TimeSpan(0, 0, 0, 0, LoopTimeLimit);
            if (LoopTimeLimit == 0)
                bOneRendererMode = true;
        }

        private void Update()
        {
            //Logger.Log(LogLevel.Info, $"Test RenderIndex {RenderIndex}");
            if (RenderIndex == -1 || bReGetRenderers)
            {
                var array = FindObjectsOfType(UnhollowerRuntimeLib.Il2CppType.Of<Renderer>());
                Renderers = array.Select(x => x.Cast<Renderer>()).ToArray();
                RendererCount = Renderers.Count();
                if (RendererCount > 0)
                {
                    if (Indextype == 0)
                    {
                        // 무조건 RenderIndex = 0 부터 시작.
                        RenderIndex = 0;
                        RenderIndexLast = RenderIndex - 1;
                        bRenderIndexSmallerThanLast = false;
                        bCheckMode = false;
                        FirstRenderer = Renderers[0];
                    }
                    else
                    {
                        // 가능하면 RenderIndex를 유지하는 방식. 일부겜에서 중간의 renderer부터 체크하므로 문제 발생
                        if (!bReGetRenderers || RenderIndex >= RendererCount)
                        {
                            RenderIndex = 0;
                        }
                        if (bReGetRenderers || RenderIndex == 0 || RenderIndexLast >= RendererCount)
                        {
                            RenderIndexLast = RenderIndex - 1;
                            bRenderIndexSmallerThanLast = false;
                        }

                        if (RendererCount != NewRendererCount || FirstRenderer != Renderers[0])
                        {
                            bCheckMode = false;
                            FirstRenderer = Renderers[0];   // 첫 renderer 기억. 추후 바뀌었는 지 체크
                        }
                    }
                }
                else
                    RenderIndex = -1;
                if (bReGetRenderers)
                {
                    Log(LogLevel.Info, $"bReGetRenderers check mode : {bCheckMode}  RenderIndex : {RenderIndex} / {RendererCount} {NewRendererCount} {RenderIndexLast} bReGetRenderers {bReGetRenderers} bRenderIndexSmallerThanLast {bRenderIndexSmallerThanLast}");
                    bCheckMode = false;
                    bReGetRenderers = false;
                    //_unlitMaterial = null;
                    bUpdateunlitMaterial = true;
                }
            }
            else if (RendererCount > 0)
            {
                var array = FindObjectsOfType(UnhollowerRuntimeLib.Il2CppType.Of<Renderer>());
                RenderersNew = array.Select(x => x.Cast<Renderer>()).ToArray();
                NewRendererCount = RenderersNew.Count();
                //Logger.Log(LogLevel.Info, $"Test RenderIndex {RenderIndex}  NewRendererCount {NewRendererCount}");
                if (NewRendererCount == 0 || FirstRenderer != RenderersNew[0] || RendererCount != NewRendererCount)
                    bReGetRenderers = true;
                if (RenderIndex >= RendererCount)
                    RenderIndex = -1;
                if (RenderIndex == RenderIndexLast || (bRenderIndexSmallerThanLast && RenderIndex > RenderIndexLast))
                {
                    bCheckMode = true;
                    //Logger.Log(LogLevel.Info, $"Check Mode ON");
                }
                else if (RenderIndex < RenderIndexLast)
                    bRenderIndexSmallerThanLast = true;
                if (!bReGetRenderers && RenderIndex != -1 && !bCheckMode)
                {
                    if (!bOneRendererMode)
                        now = DateTime.Now;
                    while (!bReGetRenderers && RenderIndex < RendererCount && (!Renderers[RenderIndex].enabled || Renderers[RenderIndex].name == renamed))
                        RenderIndex++;
                    do
                    {
                        if (RenderIndex < RendererCount && !bReGetRenderers)
                        {
                            bool bRenderMustBeDel = false;
                            if (MozaicTools.IsMozaicName(Renderers[RenderIndex].name))
                                bRenderMustBeDel = true;    // renderer 이름이 수상하므로 material 중 하나도 처리 안된다면 renderer 자체를 처리
                            foreach (Material material in Renderers[RenderIndex].materials)
                            {
                                if (material.name.StartsWith(renamed) || material.shader.name.StartsWith(renamed))
                                    bRenderMustBeDel = false;
                                else
                                {
                                    // dump materials and shaders name.
                                    if (dumpMode)
                                    {
                                        string key = $"{material.name},{material.shader?.name}";
                                        if (dumpKeys.IndexOf(key) == -1)
                                        {
                                            using (StreamWriter sw = new StreamWriter(dumpFilename, true, System.Text.Encoding.UTF8))
                                            {
                                                sw.WriteLine(key);
                                                dumpKeys.Add(key);
                                            }
                                        }
                                    }
                                    bool bForceRemove = false;
                                    if (MozaicTools.IsMozaicName_ForceRemove(material.name) ||
                                        MozaicTools.IsMozaicName_ForceRemove(material.shader?.name))
                                        bForceRemove = true;

                                    //Log(LogLevel.Info, $"Removing mozaic material {material.name} shader {material.shader?.name} from renderer {Renderers[RenderIndex].transform.name}");
                                    if (bForceRemove ||
                                        MozaicTools.IsMozaicName(material.name) ||
                                        MozaicTools.IsMozaicName(material.shader?.name))
                                    {
                                        Log(LogLevel.Info, $"material {material.name} shader {material.shader.name} passCount {material.passCount} matCount {Renderers[RenderIndex].materials.Count()} _Color {Renderers[RenderIndex].material.HasProperty("_Color")}");
                                        Log(LogLevel.Info, $"_unlitMaterial null? {_unlitMaterial == null}");
                                        if (!bForceRemove &&
                                            (Renderers[RenderIndex].materials.Count() == 1 && material.passCount > 2
                                            && (material.HasProperty("_Color") || _unlitMaterial != null)))
                                        {
                                            if (Renderers[RenderIndex].sharedMaterial != null)
                                            {
                                                Log(LogLevel.Info, $"Replacing shared material {Renderers[RenderIndex].sharedMaterial.name} _Color {Renderers[RenderIndex].sharedMaterial.HasProperty("_Color")}");
                                                if (Renderers[RenderIndex].sharedMaterial.HasProperty("_Color") || _unlitMaterial == null)
                                                {
                                                    Renderers[RenderIndex].sharedMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                                                    Renderers[RenderIndex].sharedMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                                                    Renderers[RenderIndex].sharedMaterial.SetInt("_ZWrite", 0);
                                                    Renderers[RenderIndex].sharedMaterial.DisableKeyword("_ALPHATEST_ON");
                                                    Renderers[RenderIndex].sharedMaterial.DisableKeyword("_ALPHABLEND_ON");
                                                    Renderers[RenderIndex].sharedMaterial.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                                                    Renderers[RenderIndex].sharedMaterial.renderQueue = (int)RenderQueue.Transparent;
                                                    Renderers[RenderIndex].sharedMaterial.color = new Color(material.color.r, material.color.g, material.color.b, 0.00001f);
                                                }
                                                else
                                                    Renderers[RenderIndex].sharedMaterial.shader = _unlitMaterial.shader;
                                                //Renderers[RenderIndex].sharedMaterial = _unlitMaterial;
                                                Renderers[RenderIndex].sharedMaterial.name = renamed;
                                            }
                                            else
                                            {
                                                Log(LogLevel.Info, $"Removing mozaic material from renderer {Renderers[RenderIndex].transform.name} _Color {Renderers[RenderIndex].material.HasProperty("_Color")}");
                                                if (Renderers[RenderIndex].material.HasProperty("_Color") || _unlitMaterial == null)
                                                {
                                                    Renderers[RenderIndex].material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                                                    Renderers[RenderIndex].material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                                                    Renderers[RenderIndex].material.SetInt("_ZWrite", 0);
                                                    Renderers[RenderIndex].material.DisableKeyword("_ALPHATEST_ON");
                                                    Renderers[RenderIndex].material.DisableKeyword("_ALPHABLEND_ON");
                                                    Renderers[RenderIndex].material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                                                    Renderers[RenderIndex].material.renderQueue = (int)RenderQueue.Transparent;
                                                    Renderers[RenderIndex].material.color = new Color(material.color.r, material.color.g, material.color.b, 0.00001f);
                                                }
                                                else
                                                    Renderers[RenderIndex].material.shader = _unlitMaterial.shader;
                                                Renderers[RenderIndex].material.name = renamed;
                                            }
                                            bRenderMustBeDel = false;
                                            break;
                                        }
                                        else
                                        {
                                            bool bSetLOD = false;
                                            //if (material.name.Contains("UniversalMosaic")) bSetLOD = false;
                                            if (MozaicTools.IsMozaicName_Shader(material.shader?.name)) bSetLOD = true;
                                            if (Renderers[RenderIndex].materials.Count() > 1) bSetLOD = true;
                                            if (bForceRemove || !bSetLOD)
                                            {
                                                Log(LogLevel.Info, $"Disabled {Renderers[RenderIndex].name}");
                                                Renderers[RenderIndex].material = null;
                                                Renderers[RenderIndex].enabled = false;
                                                Renderers[RenderIndex].gameObject.SetActive(false);
                                                Renderers[RenderIndex].name = renamed;
                                            }
                                            else
                                            {
                                                material.shader.maximumLOD = maximumLOD;
                                                material.name = renamed;
                                                material.shader.name = renamed;
                                                Log(LogLevel.Info, $"maximumLOD set");
                                            }
                                        }
                                        //material.name = renamed;
                                        bRenderMustBeDel = false;
                                    }
                                    if (UseUnlitShader.Value && bUpdateunlitMaterial && material.name.ToLower().StartsWith("unlit"))
                                    {
                                        if (bUpdateunlitMaterial)
                                        {
                                            _unlitMaterial = material;
                                            bUpdateunlitMaterial = false;
                                            bRenderMustBeDel = false;
                                            Log(LogLevel.Info, $"Selected shader unlit: {_unlitMaterial.shader.name} {_unlitMaterial.shader.maximumLOD}");
                                        }
                                    }
                                }
                            }
                            if (!UseUnlitShader.Value && bRenderMustBeDel)
                            {
                                Log(LogLevel.Info, $"Removing mozaic renderer {Renderers[RenderIndex].name} material count {Renderers[RenderIndex].materials.Count()} {Renderers[RenderIndex].material.name}");
                                Renderers[RenderIndex].material = null;
                                Renderers[RenderIndex].enabled = false;
                                Renderers[RenderIndex].gameObject.SetActive(false);
                                Renderers[RenderIndex].name = renamed;
                            }
                            RenderIndex++;
                        }
                        else if (RenderIndex < RendererCount)
                            RenderIndex++;
                        else
                            RenderIndex = -1;
                    } while (!bOneRendererMode && RenderIndex != -1 && RenderIndex < RendererCount && (DateTime.Now - now) < looptime && !bReGetRenderers);
                    //Logger.Log(LogLevel.Info, $"check mode : {bCheckMode}  RenderIndex : {RenderIndex} / {RendererCount} {NewRendererCount} {RenderIndexLast} bReGetRenderers {bReGetRenderers} bRenderIndexSmallerThanLast {bRenderIndexSmallerThanLast}");
                }
            }
            //Logger.Log(LogLevel.Info, $"check mode : {bCheckMode}  RenderIndex : {RenderIndex} / {RendererCount} {NewRendererCount} {RenderIndexLast}");
        }
    }
}
