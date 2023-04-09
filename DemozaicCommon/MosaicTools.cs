using System.Linq;

namespace DemosaicCommon
{
    public static class MozaicTools
    {
        public const string MozaicNameParts_Default = "mozaic,mosaic,mozaik,mosaik,pixelate,censor,cenzor,masaike";
        public const string MozaicNameParts_Shader_Default = "FX/Censor,FX/Censor (Masked Smooth),FX/Censor (Masked Cutout),FX/CensorMask,Custom\\Pixelate";
        public static string[] MozaicNameParts { get; set; }
        public static string[] MozaicNameParts_Shader { get; set; }
        public static string[] MozaicNameParts_ForceRemove { get; set; }
        private static bool bInited = false;

        public static bool IsMozaicName(string str, bool bCmpCase = false)
        {
            if (string.IsNullOrEmpty(str)) return false;
            if (!bInited)
                Init_MosaicNameParts();
            if (!bCmpCase)
                str = str.ToLower();
            return MozaicNameParts.Any(x => str.Contains(x));
        }
        public static bool IsMozaicName_Shader(string str, bool bCmpCase = false)
        {
            if (string.IsNullOrEmpty(str)) return false;
            if (!bInited)
                Init_MosaicNameParts();
            if (!bCmpCase)
                str = str.ToLower();
            return MozaicNameParts_Shader.Any(x => str.Contains(x));
        }
        public static bool IsMozaicName_ForceRemove(string str, bool bCmpCase = false)
        {
            if (string.IsNullOrEmpty(str)) return false;
            if (!bCmpCase)
                str = str.ToLower();
            return MozaicNameParts_ForceRemove.Any(x => str.Contains(x));
        }
        public static void Init_MosaicNameParts_ForceRemove(string str = "", bool bCmpCase = false)
        {
            if (string.IsNullOrEmpty(str))
            {
                MozaicNameParts_ForceRemove = new string[] { };
                return;
            }
            if (!bCmpCase)
            {
                MozaicNameParts_ForceRemove = str.ToLower().Split(',').Select(p => p.Trim()).ToArray();
            }
            else
            {
                MozaicNameParts_ForceRemove = str.Split(',').Select(p => p.Trim()).ToArray();
            }
        }

        public static void Init_MosaicNameParts(string str = "", bool bCmpCase = false)
        {
            if (string.IsNullOrEmpty(str)) str = MozaicNameParts_Default;
            if (!bCmpCase)
            {
                MozaicNameParts = str.ToLower().Split(',').Select(p => p.Trim()).ToArray();
                MozaicNameParts_Shader = MozaicNameParts_Shader_Default.ToLower().Split(',').Select(p => p.Trim()).ToArray();
            }
            else
            {
                MozaicNameParts = str.Split(',').Select(p => p.Trim()).ToArray();
                MozaicNameParts_Shader = MozaicNameParts_Shader_Default.Split(',').Select(p => p.Trim()).ToArray();
            }
            bInited = true;
        }
    }
}
