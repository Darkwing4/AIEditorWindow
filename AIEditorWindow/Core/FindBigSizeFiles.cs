using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace AIEditorWindowTool.Core {
  public class FindBigSizeFiles : AIEditorWindow {
    float sizeThresholdMB = 100f;
    Vector2 scroll;
    Dictionary<string, List<FileEntry>> groupedFiles = new Dictionary<string, List<FileEntry>>();
    Dictionary<string, bool> groupFoldouts = new Dictionary<string, bool>();
    bool listFoldout = true;

    [MenuItem("GPTGenerated/" + nameof(FindBigSizeFiles))]
    public static void ShowWindow() {
      FindBigSizeFiles window = GetWindow<FindBigSizeFiles>(false, "FindBigSizeFiles");
      window.Show();
    }

    protected override void OnEnable() {
      base.OnEnable();
    }

    protected override void OnDisable() {
      base.OnDisable();
    }

    public override void OnGUI() {
      base.OnGUI();

      EditorGUILayout.LabelField("Find big files in Assets/ by threshold (MB)", EditorStyles.boldLabel);
      EditorGUILayout.BeginHorizontal();
      EditorGUILayout.LabelField("Size Threshold (MB):", GUILayout.Width(140));
      sizeThresholdMB = EditorGUILayout.FloatField(sizeThresholdMB, GUILayout.Width(80));
      if (GUILayout.Button("Search", GUILayout.Width(70))) {
        SearchBigFiles();
      }
      EditorGUILayout.EndHorizontal();
      EditorGUILayout.Space();

      listFoldout = EditorGUILayout.Foldout(listFoldout, $"Found files: {groupedFiles.Sum(g => g.Value.Count)}");
      if (listFoldout) {
        scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.MaxHeight(600));
        foreach (var group in groupedFiles.OrderByDescending(g => g.Value.Count)) {
          string groupLabel = $"{group.Key.ToUpperInvariant()} ({group.Value.Count})";
          if (!groupFoldouts.ContainsKey(group.Key))
            groupFoldouts[group.Key] = true;

          groupFoldouts[group.Key] = EditorGUILayout.Foldout(groupFoldouts[group.Key], groupLabel, true);

          if (groupFoldouts[group.Key]) {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            foreach (var file in group.Value) {
              EditorGUILayout.BeginHorizontal();

              EditorGUILayout.LabelField($"{file.SizeMB:N2} MB", GUILayout.Width(80));
              GUI.enabled = true;
              if (GUILayout.Button(file.Path, EditorStyles.linkLabel, GUILayout.ExpandWidth(true))) {
                var obj = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(file.Path);
                if (obj != null) {
                  EditorGUIUtility.PingObject(obj);
                  Selection.activeObject = obj;
                }
                else {
                  Debug.LogWarning($"Not found: {file.Path}");
                }
              }

              EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndVertical();
          }
        }
        EditorGUILayout.EndScrollView();
      }
    }

    void SearchBigFiles() {
      groupedFiles.Clear();
      groupFoldouts.Clear();
      string assetsPath = Application.dataPath;
      double thresholdBytes = sizeThresholdMB * 1024 * 1024;

      try {
        var files = Directory.GetFiles(assetsPath, "*", SearchOption.AllDirectories);

        foreach (var file in files) {
          try {
            FileInfo fi = new FileInfo(file);
            if (fi.Length > thresholdBytes) {
              string relPath = "Assets" + file.Replace(Application.dataPath, "").Replace('\\', '/');
              // Only include valid asset files
              if (!File.Exists(relPath)) continue;
              string ext = Path.GetExtension(file).ToLowerInvariant();
              if (string.IsNullOrEmpty(ext)) ext = "(no ext)";
              if (!groupedFiles.ContainsKey(ext))
                groupedFiles[ext] = new List<FileEntry>();

              groupedFiles[ext].Add(new FileEntry {
                      SizeMB = fi.Length / (1024f * 1024f),
                      Path = relPath
              });
            }
          }
          catch { }
        }

        foreach (var key in groupedFiles.Keys.ToList()) {
          groupedFiles[key] = groupedFiles[key].OrderByDescending(f => f.SizeMB).ToList();
        }
      }
      catch (Exception e) {
        Debug.LogError("Error searching files: " + e);
      }
    }

    [Serializable]
    class FileEntry {
      public float SizeMB;
      public string Path;
    }
  }
}
