using System.IO;
using UnityEditor;
using UnityEngine;

namespace ThirdLamp.EditorTools
{
    /// <summary>
    /// Import settings for Resources/ThirdLamp/Textures, driven by Art/asset_manifest.json:
    /// tiling slots repeat, everything else clamps; decals and sprites keep their alpha; cookies become
    /// light cookies; "pixel" slots use point filtering. Max size follows the slot size.
    /// </summary>
    public class ThirdLampTextureImporter : AssetPostprocessor
    {
        const string Folder = "Assets/_Project/Resources/ThirdLamp/Textures/";
        const string ManifestPath = "Assets/_Project/Art/asset_manifest.json";

        [System.Serializable] class Slot { public string name; public int[] size; public bool tiling; public string kind; public bool pixel; }
        [System.Serializable] class Manifest { public Slot[] slots; }

        static Manifest manifest;

        static Slot Find(string name)
        {
            if (manifest == null)
            {
                if (!File.Exists(ManifestPath)) return null;
                manifest = JsonUtility.FromJson<Manifest>(File.ReadAllText(ManifestPath));
            }
            if (manifest?.slots == null) return null;
            foreach (var s in manifest.slots) if (s.name == name) return s;
            return null;
        }

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Folder) || !assetPath.EndsWith(".png")) return;
            var slot = Find(Path.GetFileNameWithoutExtension(assetPath));
            if (slot == null) return;
            var ti = (TextureImporter)assetImporter;
            string kind = string.IsNullOrEmpty(slot.kind) ? "tex" : slot.kind;

            ti.isReadable = false;
            ti.mipmapEnabled = kind != "sprite";
            ti.wrapMode = slot.tiling ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
            ti.filterMode = slot.pixel ? FilterMode.Point : FilterMode.Bilinear;
            ti.anisoLevel = slot.tiling ? 4 : 1;
            if (slot.size != null && slot.size.Length == 2)
            {
                int max = Mathf.Max(slot.size[0], slot.size[1]);
                ti.maxTextureSize = Mathf.Max(32, Mathf.NextPowerOfTwo(max));
            }

            if (kind == "cookie")
            {
                ti.textureType = TextureImporterType.Cookie;
                ti.alphaSource = TextureImporterAlphaSource.FromGrayScale;
                ti.wrapMode = TextureWrapMode.Clamp;
                ti.mipmapEnabled = false;
            }
            else
            {
                ti.textureType = TextureImporterType.Default;
                bool alpha = kind == "decal" || kind == "sprite";
                ti.alphaSource = alpha ? TextureImporterAlphaSource.FromInput : TextureImporterAlphaSource.None;
                ti.alphaIsTransparency = alpha;
            }
        }

        [MenuItem("Tools/The Third Lamp/Reimport Textures", priority = 21)]
        static void ReimportAll()
        {
            manifest = null;
            AssetDatabase.ImportAsset(Folder.TrimEnd('/'), ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceUpdate);
        }
    }
}
