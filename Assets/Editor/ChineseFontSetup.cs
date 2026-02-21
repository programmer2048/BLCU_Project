using UnityEngine;
using UnityEditor;
using TMPro;
using System.IO;
using System.Linq;

/// <summary>
/// 自动查找系统中文字体并创建 TMP 字体资产 + 全局设置为默认
/// </summary>
public class ChineseFontSetup
{
    [MenuItem("Tools/Setup Chinese Font")]
    public static void Setup()
    {
        // 尝试查找系统中文字体
        string[] fontPaths = new string[]
        {
            "C:/Windows/Fonts/msyh.ttc",      // 微软雅黑
            "C:/Windows/Fonts/msyhbd.ttc",     // 微软雅黑 Bold
            "C:/Windows/Fonts/simhei.ttf",     // 黑体
            "C:/Windows/Fonts/simsun.ttc",     // 宋体
            "C:/Windows/Fonts/STKAITI.TTF",    // 华文楷体
            "C:/Windows/Fonts/STXIHEI.TTF",    // 华文细黑
        };

        string selectedFontPath = null;
        foreach (var path in fontPaths)
        {
            if (File.Exists(path))
            {
                selectedFontPath = path;
                break;
            }
        }

        if (selectedFontPath == null)
        {
            Debug.LogError("[ChineseFontSetup] 未找到任何系统中文字体!");
            return;
        }

        Debug.Log($"[ChineseFontSetup] 找到字体: {selectedFontPath}");

        // 复制字体到项目内
        string destDir = "Assets/03_Resources/Fonts";
        if (!Directory.Exists(destDir))
            Directory.CreateDirectory(destDir);

        string ext = Path.GetExtension(selectedFontPath);
        string destPath = $"{destDir}/ChineseFont{ext}";
        
        if (!File.Exists(destPath))
        {
            File.Copy(selectedFontPath, destPath, true);
            AssetDatabase.Refresh();
            Debug.Log($"[ChineseFontSetup] 字体已复制到: {destPath}");
        }

        // 加载字体
        Font font = AssetDatabase.LoadAssetAtPath<Font>(destPath);
        if (font == null)
        {
            Debug.LogError($"[ChineseFontSetup] 无法加载字体: {destPath}");
            return;
        }

        // 创建 TMP 字体资产
        string tmpFontPath = $"{destDir}/ChineseFont_SDF.asset";
        
        // 删除旧的字体资产
        if (File.Exists(tmpFontPath))
        {
            AssetDatabase.DeleteAsset(tmpFontPath);
        }

        // 使用 TMP 的字体资产创建器
        TMP_FontAsset tmpFont = TMP_FontAsset.CreateFontAsset(font, 36, 4, 
            UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA, 1024, 1024);
        if (tmpFont == null)
        {
            Debug.LogError("[ChineseFontSetup] 无法创建 TMP 字体资产!");
            return;
        }

        // 设置动态字体 - 运行时按需生成字符
        tmpFont.atlasPopulationMode = AtlasPopulationMode.Dynamic;
        
        // 保存字体资产及其子资产（atlas texture + material）
        AssetDatabase.CreateAsset(tmpFont, tmpFontPath);
        
        // 保存材质和纹理作为子资产
        if (tmpFont.material != null)
        {
            tmpFont.material.name = "ChineseFont_SDF Material";
            AssetDatabase.AddObjectToAsset(tmpFont.material, tmpFontPath);
        }
        if (tmpFont.atlasTexture != null)
        {
            tmpFont.atlasTexture.name = "ChineseFont_SDF Atlas";
            AssetDatabase.AddObjectToAsset(tmpFont.atlasTexture, tmpFontPath);
        }
        
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[ChineseFontSetup] TMP字体资产已创建: {tmpFontPath}");

        // 设置为 TMP 默认字体
        TMP_Settings tmpSettings = Resources.Load<TMP_Settings>("TMP Settings");
        if (tmpSettings != null)
        {
            var so = new SerializedObject(tmpSettings);
            var prop = so.FindProperty("m_defaultFontAsset");
            if (prop != null)
            {
                prop.objectReferenceValue = tmpFont;
                so.ApplyModifiedProperties();
                Debug.Log("[ChineseFontSetup] 已设置为 TMP 默认字体");
            }
            else
            {
                Debug.LogWarning("[ChineseFontSetup] 未找到 m_defaultFontAsset 属性");
            }
        }
        else
        {
            Debug.LogWarning("[ChineseFontSetup] 未找到 TMP Settings，请手动设置默认字体");
        }

        // 重新搭建所有场景以使用新字体
        Debug.Log("[ChineseFontSetup] 中文字体设置完成!");
        Debug.Log("[ChineseFontSetup] 下一步: 运行 Tools/Rebuild All Scenes With Font");
    }

    [MenuItem("Tools/Rebuild All Scenes With Font")]
    public static void RebuildScenesWithFont()
    {
        // 加载字体
        TMP_FontAsset chineseFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/03_Resources/Fonts/ChineseFont_SDF.asset");
        if (chineseFont == null)
        {
            Debug.LogError("[RebuildScenes] 请先运行 Tools/Setup Chinese Font");
            return;
        }

        // 对每个场景中的所有 TMP 组件设置字体
        string[] scenePaths = new string[]
        {
            "Assets/04_Scenes/00_Boot.unity",
            "Assets/04_Scenes/01_MainUI.unity",
            "Assets/04_Scenes/02_PartTimeJobs.unity",
            "Assets/04_Scenes/03_MiniGames.unity",
            "Assets/04_Scenes/04_StoryScenes.unity",
            "Assets/04_Scenes/05_Album.unity",
        };

        foreach (var scenePath in scenePaths)
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath);
            var roots = scene.GetRootGameObjects();
            int count = 0;

            foreach (var root in roots)
            {
                var tmps = root.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var tmp in tmps)
                {
                    tmp.font = chineseFont;
                    count++;
                    EditorUtility.SetDirty(tmp);
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
            Debug.Log($"[RebuildScenes] {scene.name}: 更新了 {count} 个TMP组件字体");
        }

        Debug.Log("[RebuildScenes] 所有场景字体更新完成!");
    }
}
