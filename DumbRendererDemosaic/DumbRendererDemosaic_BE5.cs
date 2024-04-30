using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DumbRendererDemosaic;
using BepInEx;
using BepInEx.Logging;
using UnityEngine;
using DemosaicCommon;

namespace DumbRendererDemosaic_BE5
{
    [BepInPlugin(Metadata.PLUGIN_GUID, Metadata.PLUGIN_NAME, Metadata.Version)]
    public class loader : BaseUnityPlugin
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

        internal static ManualLogSource llog;

        public void Awake()
        {
            llog = this.Logger;
            DumbRendererDemosaic.DumbRendererDemosaic.Setup();
        }

    }
}
