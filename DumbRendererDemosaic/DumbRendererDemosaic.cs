using System;
using System.Linq;
using BepInEx;
using BepInEx.Logging;
using DemosaicCommon;
using UnityEngine;
using UnityEngine.Rendering;
using BepInEx.Configuration;
using System.IO;
using System.Collections.Generic;



#if interop
using Il2CppInterop.Runtime;
using Il2CppInterop.Runtime.Injection;
using loader = DumbRendererDemosaic_BE_il2cpp.loader;
#endif
#if mono
using loader = DumbRendererDemosaic_BE5.loader;
#endif

namespace DumbRendererDemosaic
{
    /// <summary>
    /// Scans all renderers for materials that could be mozaics and removes the materials
    /// </summary>
    internal class DumbRendererDemosaic : MonoBehaviour
    {
        private Renderer[] Renderers;
        private int RenderIndex = -1;
        private int RendererCount;
        private const string renamed = "[Renamed]";
        private const string SectionName = "Config";
        private int maximumLOD = -2;

        // config
        private static ConfigFile configFile { get; set; }
        public static ConfigEntry<string> FilterStrings { get; set; }
        public static ConfigEntry<string> ForceRemoveFilter { get; set; }
        //public static ConfigEntry<int> maximumLOD { get; set; }
        public static ConfigEntry<bool> UseUnlitShader { get; set; }
        public static ConfigEntry<bool> NameDumpMode { get; set; }

        private static Material _unlitMaterial = null;
        bool bUpdateunlitMaterial = true;
        static bool dumpMode = false;
        private static List<string> dumpKeys;
        static string dumpFilename;

        static int? OldSceneId = null;
        TimeSpan LoopTimeLimit = new TimeSpan(0, 0, 0, 0, 1);
        static DateTime? next_check = null;
        static TimeSpan check_span = new TimeSpan(0, 0, 0, 2);

        internal static DumbRendererDemosaic Instance { get; private set; }
        public static void Setup()
        {
#if interop
            try
            {
                ClassInjector.RegisterTypeInIl2Cpp<DumbRendererDemosaic>();
            }
            catch { }
#endif

            GameObject obj = new GameObject(Metadata.PLUGIN_NAME);
            DontDestroyOnLoad(obj);
            obj.hideFlags = HideFlags.HideAndDontSave;
            try
            {
                Instance = obj.AddComponent<DumbRendererDemosaic>();
            }
            catch (Exception e)
            {
                loader.log(LogLevel.Error, $"instance Init Error {e.Message}");
            }
        }

        private void Awake()
        {
            configFile = new ConfigFile($"{Paths.ConfigPath}\\{Metadata.PLUGIN_NAME}.ini", false);
            dumpFilename = $"{Paths.PluginPath}\\{Metadata.PLUGIN_NAME}.csv";

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

            // Ricca check
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

        }

        void OnGUI()
        {
            //Logger.Log(LogLevel.Info, $"Test RenderIndex {RenderIndex}");
            int scene = Application.loadedLevel;
            if (OldSceneId == null || OldSceneId != scene)
            {
                next_check = null;
                OldSceneId = scene;
                return;
            }

            if (next_check != null && (RendererCount > 0 && RenderIndex >= 0 && RendererCount > RenderIndex))
            {
                DateTime now = DateTime.Now;
                try
                {
                    while (RenderIndex < RendererCount && (!Renderers[RenderIndex].enabled || Renderers[RenderIndex].name == renamed))
                        RenderIndex++;
                }
                catch (Exception)
                {
                    RenderIndex = -1;
                    return;
                }
                if (RenderIndex >= RendererCount)
                {
                    RenderIndex = RendererCount;
                    return;
                }
                do
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

                            //loader.log(LogLevel.Info, $"Removing mozaic material {material.name} shader {material.shader?.name} from renderer {Renderers[RenderIndex].transform.name}");
                            if (bForceRemove ||
                                MozaicTools.IsMozaicName(material.name) ||
                                MozaicTools.IsMozaicName(material.shader?.name))
                            {
                                loader.log(LogLevel.Info, $"material {material.name} shader {material.shader.name} passCount {material.passCount} matCount {Renderers[RenderIndex].materials.Count()} _Color {Renderers[RenderIndex].material.HasProperty("_Color")}");
                                loader.log(LogLevel.Info, $"_unlitMaterial null? {_unlitMaterial == null}");
                                if (!bForceRemove &&
                                    (Renderers[RenderIndex].materials.Count() == 1 && material.passCount > 2
                                    && (material.HasProperty("_Color") || _unlitMaterial != null)))
                                {
                                    if (Renderers[RenderIndex].sharedMaterial != null)
                                    {
                                        loader.log(LogLevel.Info, $"Replacing shared material {Renderers[RenderIndex].sharedMaterial.name} _Color {Renderers[RenderIndex].sharedMaterial.HasProperty("_Color")}");
                                        if (Renderers[RenderIndex].sharedMaterial.HasProperty("_Color") || _unlitMaterial == null)
                                        {
                                            Renderers[RenderIndex].sharedMaterial.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                                            Renderers[RenderIndex].sharedMaterial.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                                            Renderers[RenderIndex].sharedMaterial.SetInt("_ZWrite", 0);
                                            Renderers[RenderIndex].sharedMaterial.DisableKeyword("_ALPHATEST_ON");
                                            Renderers[RenderIndex].sharedMaterial.DisableKeyword("_ALPHABLEND_ON");
                                            Renderers[RenderIndex].sharedMaterial.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                                            //Renderers[RenderIndex].sharedMaterial.renderQueue = (int)RenderQueue.Transparent;
                                            Renderers[RenderIndex].sharedMaterial.color = new Color(material.color.r, material.color.g, material.color.b, 0.00001f);
                                        }
                                        else
                                            Renderers[RenderIndex].sharedMaterial.shader = _unlitMaterial.shader;
                                            //Renderers[RenderIndex].sharedMaterial = _unlitMaterial;
                                        Renderers[RenderIndex].sharedMaterial.name = renamed;
                                    }
                                    else
                                    {
                                        loader.log(LogLevel.Info, $"Removing mozaic material from renderer {Renderers[RenderIndex].transform.name} _Color {Renderers[RenderIndex].material.HasProperty("_Color")}");
                                        if (Renderers[RenderIndex].material.HasProperty("_Color") || _unlitMaterial == null)
                                        {
                                            Renderers[RenderIndex].material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                                            Renderers[RenderIndex].material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                                            Renderers[RenderIndex].material.SetInt("_ZWrite", 0);
                                            Renderers[RenderIndex].material.DisableKeyword("_ALPHATEST_ON");
                                            Renderers[RenderIndex].material.DisableKeyword("_ALPHABLEND_ON");
                                            Renderers[RenderIndex].material.EnableKeyword("_ALPHAPREMULTIPLY_ON");
                                            //Renderers[RenderIndex].material.renderQueue = (int)RenderQueue.Transparent;
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
                                        loader.log(LogLevel.Info, $"Disabled {Renderers[RenderIndex].name}");
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
                                        loader.log(LogLevel.Info, $"maximumLOD set");
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
                                    loader.log(LogLevel.Info, $"Selected shader unlit: {_unlitMaterial.shader.name} {_unlitMaterial.shader.maximumLOD}");
                                }
                            }
                        }
                    }
                    if (!UseUnlitShader.Value && bRenderMustBeDel)
                    {
                        loader.log(LogLevel.Info, $"Removing mozaic renderer {Renderers[RenderIndex].name} material count {Renderers[RenderIndex].materials.Count()} {Renderers[RenderIndex].material.name}");
                        Renderers[RenderIndex].material = null;
                        Renderers[RenderIndex].enabled = false;
                        Renderers[RenderIndex].gameObject.SetActive(false);
                        Renderers[RenderIndex].name = renamed;
                    }
                    RenderIndex++;
                } while (RenderIndex > -1 && RenderIndex < RendererCount && (DateTime.Now - now) < LoopTimeLimit);
            }
            else if (next_check == null || next_check < DateTime.Now)
            {
#if interop
                var array = FindObjectsOfType(Il2CppType.Of<Renderer>());
                Renderers = array.Select(x => x.Cast<Renderer>()).ToArray();
#else
                Renderers = FindObjectsOfType<Renderer>();
#endif
                RendererCount = Renderers.Count();
                if (RendererCount > 0)
                {
                    RenderIndex = 0;
                }
                else
                    RenderIndex = -1;
                next_check = DateTime.Now + check_span;
            }
            //loader.log(LogLevel.Info, $"RenderIndex : {RenderIndex} / {RendererCount}");
        }
    }
}
