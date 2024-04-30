using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using DemosaicCommon;
using UnityEngine;

namespace DumbRendererDemosaic_BE_il2cpp
{
    [BepInPlugin(Metadata.PLUGIN_GUID, Metadata.PLUGIN_NAME, Metadata.Version)]
    public class loader : BasePlugin
    {
        public static void log(LogLevel lv, object data, bool force = false)
        {
#if DEBUG
            llog?.Log(lv, data);
#else
            if (lv == LogLevel.Error || lv == LogLevel.Warning || force)
                llog?.Log(lv, data);
#endif
            return;
        }

        internal static ManualLogSource? llog;

        public override void Load()
        {
            llog = base.Log;
            DumbRendererDemosaic.DumbRendererDemosaic.Setup();
        }

    }
}
