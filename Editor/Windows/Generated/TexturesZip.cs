namespace AIEditorWindowTool.Windows {

  using System.Collections.Generic;
  using UnityEditor;
  using UnityEngine;

  public class TexturesZip : AIEditorWindow {
    class TextureGroup {
      public string GroupName;
      public bool Foldout;
      public List<TextureEntry> Textures = new List<TextureEntry>();
      public int ScrollIndex = 0;
      public int PageSize = 20;
    }

    class TextureEntry {
      public Texture2D Texture;
      public string AssetPath;
      public TextureImporter Importer;
      public TextureImporterType TextureType;
      public string Compression;
      public Dictionary<string, object> ImporterSettings;
      public int Width;
      public int Height;
      public string SizeTag;
      public bool ShowSettings = false;
    }

    enum GroupingMode {
      Compression,
      TextureType,
      Size
    }

    Vector2 _scroll;
    readonly List<TextureGroup> _compressionGroups = new();
    readonly List<TextureGroup> _typeGroups = new();
    readonly List<TextureGroup> _sizeGroups = new();

    [MenuItem("GPTGenerated/" + nameof(TexturesZip))]
    public static void ShowWindow() {
      GetWindow<TexturesZip>(nameof(TexturesZip));
    }

    protected override void OnEnable() {
      base.OnEnable();
      RefreshTextures();
    }

    void RefreshTextures() {
      _compressionGroups.Clear();
      _typeGroups.Clear();
      _sizeGroups.Clear();

      string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D");
      var compressionMap = new Dictionary<string, TextureGroup>();
      var typeMap = new Dictionary<string, TextureGroup>();
      var sizeMap = new Dictionary<string, TextureGroup>();

      foreach (string guid in textureGuids) {
        string assetPath = AssetDatabase.GUIDToAssetPath(guid);
        if (assetPath.IndexOf("/Editor/", System.StringComparison.OrdinalIgnoreCase) >= 0)
          continue;

        if (AssetImporter.GetAtPath(assetPath) is not TextureImporter importer)
          continue;

#if UNITY_2022_1_OR_NEWER
        var platformSettings = importer.GetPlatformTextureSettings("Default");
        string compression = platformSettings.textureCompression.ToString();
#else
                TextureImporterPlatformSettings platformSettings = importer.GetPlatformTextureSettings("Default");
                string compression = platformSettings.textureCompression.ToString();
#endif
        string typeName = importer.textureType.ToString();

        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
        if (tex == null) continue;

        int width = 0, height = 0;
        try {
          width = tex.width;
          height = tex.height;
        }
        catch { /* ignored */
        }

        string sizeTag = GetSizeCategory(width, height);

        if (!compressionMap.TryGetValue(compression, out var compressionGroup)) {
          compressionGroup = new TextureGroup { GroupName = compression, Foldout = false };
          compressionMap.Add(compression, compressionGroup);
        }

        if (!typeMap.TryGetValue(typeName, out var typeGroup)) {
          typeGroup = new TextureGroup { GroupName = typeName, Foldout = false };
          typeMap.Add(typeName, typeGroup);
        }

        if (!sizeMap.TryGetValue(sizeTag, out var sizeGroup)) {
          sizeGroup = new TextureGroup { GroupName = sizeTag, Foldout = false };
          sizeMap.Add(sizeTag, sizeGroup);
        }

        var entry = new TextureEntry {
                Texture = tex,
                AssetPath = assetPath,
                Importer = importer,
                Compression = compression,
                TextureType = importer.textureType,
                ImporterSettings = GetEditableImporterSettings(importer),
                Width = width,
                Height = height,
                SizeTag = sizeTag
        };
        compressionGroup.Textures.Add(entry);
        typeGroup.Textures.Add(entry);
        sizeGroup.Textures.Add(entry);
      }

      foreach (var kv in compressionMap) {
        kv.Value.Textures.Sort((a, b) => string.Compare(a.Texture.name, b.Texture.name, System.StringComparison.Ordinal));
        _compressionGroups.Add(kv.Value);
      }
      _compressionGroups.Sort((a, b) => string.Compare(a.GroupName, b.GroupName, System.StringComparison.Ordinal));

      foreach (var kv in typeMap) {
        kv.Value.Textures.Sort((a, b) => string.Compare(a.Texture.name, b.Texture.name, System.StringComparison.Ordinal));
        _typeGroups.Add(kv.Value);
      }
      _typeGroups.Sort((a, b) => string.Compare(a.GroupName, b.GroupName, System.StringComparison.Ordinal));

      foreach (var kv in sizeMap) {
        kv.Value.Textures.Sort((a, b) => string.Compare(a.Texture.name, b.Texture.name, System.StringComparison.Ordinal));
        _sizeGroups.Add(kv.Value);
      }
      _sizeGroups.Sort((a, b) => CompareSizeTag(a.GroupName, b.GroupName));
    }

    static string GetSizeCategory(int width, int height) {
      int size = Mathf.Max(width, height);
      if (size <= 64) return "1) <=64";
      if (size <= 128) return "2) 65–128";
      if (size <= 256) return "3) 129–256";
      if (size <= 512) return "4) 257–512";
      if (size <= 1024) return "5) 513–1024";
      if (size <= 2048) return "6) 1025–2048";
      if (size <= 4096) return "7) 2049–4096";
      return "8) >4096";
    }

    static int CompareSizeTag(string a, string b) {
      int ai = a.IndexOf(')');
      int bi = b.IndexOf(')');
      if (ai > 0 && bi > 0 &&
              int.TryParse(a[..ai], out int an) &&
              int.TryParse(b[..bi], out int bn)) {
        return an.CompareTo(bn);
      }
      return string.Compare(a, b, System.StringComparison.Ordinal);
    }

    static Dictionary<string, object> GetEditableImporterSettings(TextureImporter importer) {
      var dict = new Dictionary<string, object> {
              ["Texture Type"] = importer.textureType,
              ["sRGB"] = importer.sRGBTexture,
              ["Alpha Source"] = importer.alphaSource,
              ["Alpha Is Transparency"] = importer.alphaIsTransparency,
              ["Mipmaps"] = importer.mipmapEnabled,
              ["Wrap Mode"] = importer.wrapMode,
              ["Filter Mode"] = importer.filterMode,
              ["Aniso Level"] = importer.anisoLevel,
              ["NPOT Scale"] = importer.npotScale
      };

#if UNITY_2022_1_OR_NEWER
      var defaultSettings = importer.GetPlatformTextureSettings("Default");
#else
            var defaultSettings = importer.GetPlatformTextureSettings("Default");
#endif
      dict["Compression"] = defaultSettings.textureCompression;
      dict["Max Size"] = defaultSettings.maxTextureSize;
      dict["Format"] = defaultSettings.format;
      dict["Crunched Compression"] = defaultSettings.crunchedCompression;
      dict["Quality"] = defaultSettings.compressionQuality;
      return dict;
    }

    void SetTextureImporterSetting(TextureEntry entry, string key, object value) {
      var importer = entry.Importer;

      try {
        switch (key) {
          case "Texture Type": importer.textureType = (TextureImporterType)value; break;
          case "sRGB": importer.sRGBTexture = (bool)value; break;
          case "Alpha Source": importer.alphaSource = (TextureImporterAlphaSource)value; break;
          case "Alpha Is Transparency": importer.alphaIsTransparency = (bool)value; break;
          case "Mipmaps": importer.mipmapEnabled = (bool)value; break;
          case "Wrap Mode": importer.wrapMode = (TextureWrapMode)value; break;
          case "Filter Mode": importer.filterMode = (FilterMode)value; break;
          case "Aniso Level": importer.anisoLevel = (int)value; break;
          case "NPOT Scale": importer.npotScale = (TextureImporterNPOTScale)value; break;

          case "Compression":
          case "Max Size":
          case "Format":
          case "Crunched Compression":
          case "Quality": {
#if UNITY_2022_1_OR_NEWER
            var platformSettings = importer.GetPlatformTextureSettings("Default");
#else
                        var platformSettings = importer.GetPlatformTextureSettings("Default");
#endif
            switch (key) {
              case "Compression": platformSettings.textureCompression = (TextureImporterCompression)value; break;
              case "Max Size": platformSettings.maxTextureSize = (int)value; break;
              case "Format": platformSettings.format = (TextureImporterFormat)value; break;
              case "Crunched Compression": platformSettings.crunchedCompression = (bool)value; break;
              case "Quality": platformSettings.compressionQuality = (int)value; break;
            }
            importer.SetPlatformTextureSettings(platformSettings);
            break;
          }
        }
      }
      catch { /* ignored */
      }
    }

    void DrawEditableSettings(TextureEntry entry) {
      var dict = entry.ImporterSettings;
      EditorGUI.BeginChangeCheck();

      foreach (var kv in dict) {
        object newValue = kv.Value;
        string key = kv.Key;

        switch (key) {
          case "Texture Type": newValue = (TextureImporterType)EditorGUILayout.EnumPopup("Texture Type", (TextureImporterType)kv.Value); break;
          case "sRGB": newValue = EditorGUILayout.Toggle("sRGB", (bool)kv.Value); break;
          case "Alpha Source": newValue = (TextureImporterAlphaSource)EditorGUILayout.EnumPopup("Alpha Source", (TextureImporterAlphaSource)kv.Value); break;
          case "Alpha Is Transparency": newValue = EditorGUILayout.Toggle("Alpha Is Transparency", (bool)kv.Value); break;
          case "Mipmaps": newValue = EditorGUILayout.Toggle("Mipmaps", (bool)kv.Value); break;
          case "Wrap Mode": newValue = (TextureWrapMode)EditorGUILayout.EnumPopup("Wrap Mode", (TextureWrapMode)kv.Value); break;
          case "Filter Mode": newValue = (FilterMode)EditorGUILayout.EnumPopup("Filter Mode", (FilterMode)kv.Value); break;
          case "Aniso Level": newValue = EditorGUILayout.IntSlider("Aniso Level", (int)kv.Value, 0, 16); break;
          case "NPOT Scale": newValue = (TextureImporterNPOTScale)EditorGUILayout.EnumPopup("NPOT Scale", (TextureImporterNPOTScale)kv.Value); break;
          case "Compression": newValue = (TextureImporterCompression)EditorGUILayout.EnumPopup("Compression", (TextureImporterCompression)kv.Value); break;
          case "Max Size":
            newValue = EditorGUILayout.IntPopup("Max Size", (int)kv.Value,
                    new[] { "32", "64", "128", "256", "512", "1024", "2048", "4096", "8192" },
                    new[] { 32, 64, 128, 256, 512, 1024, 2048, 4096, 8192 }); break;
          case "Format": newValue = (TextureImporterFormat)EditorGUILayout.EnumPopup("Format", (TextureImporterFormat)kv.Value); break;
          case "Crunched Compression": newValue = EditorGUILayout.Toggle("Crunched Compression", (bool)kv.Value); break;
          case "Quality": newValue = EditorGUILayout.IntSlider("Quality", (int)kv.Value, 0, 100); break;
        }

        if (!Equals(newValue, kv.Value)) {
          dict[key] = newValue;
          SetTextureImporterSetting(entry, key, newValue);
        }
      }

      if (EditorGUI.EndChangeCheck()) {
        AssetDatabase.ImportAsset(entry.AssetPath, ImportAssetOptions.ForceUpdate);
        entry.ImporterSettings = GetEditableImporterSettings(entry.Importer);
      }
    }

    void DrawGroupWithPaging(TextureGroup group, bool showSize = false) {
      EditorGUILayout.BeginHorizontal();
      group.Foldout = EditorGUILayout.Foldout(group.Foldout, $"{group.GroupName} ({group.Textures.Count})", true);

      if (group.Foldout && group.Textures.Count > group.PageSize) {
        GUILayout.FlexibleSpace();

        int pageCount = Mathf.CeilToInt(group.Textures.Count / (float)group.PageSize);
        int currPage = group.ScrollIndex + 1;

        if (GUILayout.Button("<", GUILayout.Width(24)))
          group.ScrollIndex = Mathf.Max(0, group.ScrollIndex - 1);

        GUILayout.Label($"Page {currPage}/{pageCount}", GUILayout.Width(80));

        if (GUILayout.Button(">", GUILayout.Width(24)))
          group.ScrollIndex = Mathf.Min(pageCount - 1, group.ScrollIndex + 1);
      }
      EditorGUILayout.EndHorizontal();

      if (!group.Foldout) return;

      using (new EditorGUI.IndentLevelScope()) {
        int from = group.ScrollIndex * group.PageSize;
        int to = Mathf.Min(from + group.PageSize, group.Textures.Count);

        for (int i = from; i < to; i++) {
          var entry = group.Textures[i];

          EditorGUILayout.BeginHorizontal();
          EditorGUILayout.ObjectField(entry.Texture, typeof(Texture2D), false, GUILayout.MinWidth(100));
          EditorGUILayout.LabelField(entry.AssetPath, EditorStyles.miniLabel);
          if (showSize)
            EditorGUILayout.LabelField($"{entry.Width}x{entry.Height}", GUILayout.Width(65));
          EditorGUILayout.EndHorizontal();

          entry.ShowSettings = EditorGUILayout.Foldout(entry.ShowSettings, "Settings", true);
          if (entry.ShowSettings) {
            using (new EditorGUI.IndentLevelScope())
              DrawEditableSettings(entry);
          }
          GUILayout.Space(4);
        }
      }
    }

    void OnGUI() {
      base.OnGUI();

      EditorGUILayout.LabelField(
              "Textures are displayed in three different list views:\n" +
              "1. Grouped by Compression: textures are grouped by the Compression value. Inside each group they are sorted by name. The list is paginated (20 per page).\n" +
              "2. Grouped by Texture Type (Sprite, Lightmap, etc.): textures are grouped by type and then sorted by name. Also paginated.\n" +
              "3. Grouped by Size (largest side): textures are grouped into size ranges. Inside each group they are sorted by name. Also paginated (20 per page).\n" +
              "Textures located in folders whose path contains '/Editor/' are excluded.\n" +
              "You can change import settings for each texture directly in this window.",
              EditorStyles.wordWrappedMiniLabel);
      EditorGUILayout.Space();

      EditorGUILayout.BeginHorizontal();
      if (GUILayout.Button("Refresh", GUILayout.Width(100)))
        RefreshTextures();

      EditorGUILayout.Space();
      GUILayout.Label("Grouping by Compression", EditorStyles.boldLabel);
      EditorGUILayout.EndHorizontal();

      EditorGUILayout.LabelField(
              "Textures are grouped by their Compression value. Inside each group they are sorted by name. Use the arrows to navigate between pages.",
              EditorStyles.wordWrappedMiniLabel);
      EditorGUILayout.Space();

      _scroll = EditorGUILayout.BeginScrollView(_scroll);

      foreach (var group in _compressionGroups)
        DrawGroupWithPaging(group);

      EditorGUILayout.Space();
      GUI.backgroundColor = new Color(0.95f, 0.95f, 1f, 1f);
      EditorGUILayout.LabelField("Grouping by Texture Type", EditorStyles.boldLabel);
      EditorGUILayout.LabelField(
              "Textures are grouped by type: Sprite, Default, Lightmap, etc. Inside each group they are sorted by name. Use the arrows to navigate between pages.",
              EditorStyles.wordWrappedMiniLabel);
      GUI.backgroundColor = Color.white;
      EditorGUILayout.Space();

      foreach (var group in _typeGroups)
        DrawGroupWithPaging(group);

      EditorGUILayout.Space();
      GUI.backgroundColor = new Color(0.9f, 1f, 0.95f, 1f);
      EditorGUILayout.LabelField("Grouping by Texture Size", EditorStyles.boldLabel);
      EditorGUILayout.LabelField(
              "Textures are grouped into ranges based on their largest dimension (width or height). Inside each group they are sorted by name. Use the arrows to navigate between pages.",
              EditorStyles.wordWrappedMiniLabel);
      GUI.backgroundColor = Color.white;
      EditorGUILayout.Space();

      foreach (var group in _sizeGroups)
        DrawGroupWithPaging(group, true);

      EditorGUILayout.EndScrollView();
    }
  }
}
