namespace DemosaicCommon
{
    internal static class Metadata
    {
#if interop
        public const string PLUGIN_GUID = "dumb.renderer.demosaic.Il2Cpp";
        public const string PLUGIN_NAME = "DumbRendererDemosaicIl2Cpp";
#else
        public const string PLUGIN_GUID = "dumb.renderer.demosaic";
        public const string PLUGIN_NAME = "DumbRendererDemosaic";
#endif
        public const string Version = "1.4.5.0";
    }
}