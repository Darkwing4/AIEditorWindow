using System.Collections.Generic;
using AIEditorWindowTool.Core.Extensions;
using UnityEditor;
using UnityEngine;

namespace AIEditorWindowTool.Core {
  public class FilesToContext : AIEditorWindow {
    const string PREFS_KEY = "AITools.AIEditorWindowTool.FilesToContext.SelectedScripts";
    List<ContextWrapper> contexts;
    Vector2 scrollPos;

    [MenuItem("GPTGenerated/" + nameof(FilesToContext))]
    public static void ShowWindow() {
      var wnd = GetWindow<FilesToContext>();
      wnd.titleContent = new GUIContent("FilesToContext");
      wnd.Show();
    }

    protected override void OnEnable() {
      base.OnEnable();
      if (contexts == null) {
        contexts = new List<ContextWrapper>();
        int savedCount = EditorPrefs.GetInt(PREFS_KEY + "_contextCount", 1);
        if (savedCount < 1) savedCount = 1;
        for (int i = 0; i < savedCount; i++) {
          string key = i == 0 ? PREFS_KEY : PREFS_KEY + i;
          var toolbar = new ContextScriptsToolbar(key);
          string desc = EditorPrefs.GetString(key + "_description", "");
          contexts.Add(new ContextWrapper(toolbar, desc, key));
        }
      }
    }

    protected override void OnDisable() {
      base.OnDisable();
      EditorPrefs.SetInt(PREFS_KEY + "_contextCount", contexts.Count);
      for (int i = 0; i < contexts.Count; i++) {
        EditorPrefs.SetString(contexts[i].PrefsKey + "_description", contexts[i].Description);
      }
    }

    public override void OnGUI() {
      base.OnGUI();

      EditorGUILayout.BeginHorizontal();
      if (GUILayout.Button("Add Context")) {
        int i = contexts.Count;
        string key = i == 0 ? PREFS_KEY : PREFS_KEY + i;
        contexts.Add(new ContextWrapper(new ContextScriptsToolbar(key), "", key));
        EditorPrefs.SetInt(PREFS_KEY + "_contextCount", contexts.Count);
      }
      GUI.enabled = contexts.Count > 1;
      if (GUILayout.Button("Remove Last")) {
        if (contexts.Count > 1) {
          int idx = contexts.Count - 1;
          string key = idx == 0 ? PREFS_KEY : PREFS_KEY + idx;
          EditorPrefs.DeleteKey(key + "_description");
          contexts.RemoveAt(idx);
          EditorPrefs.SetInt(PREFS_KEY + "_contextCount", contexts.Count);
        }
      }
      GUI.enabled = true;
      if (GUILayout.Button("Copy All")) {
        List<string> allContexts = new List<string>();
        for (int i = 0; i < contexts.Count; i++) {
          var c = contexts[i];
          string txt = "";
          if (!string.IsNullOrEmpty(c.Description))
            txt += $" - {c.Description}:\n";
          txt += $"```{c.Toolbar.GetCombinedContextText()}```";
          allContexts.Add(txt);
        }
        EditorGUIUtility.systemCopyBuffer = string.Join("\n\n", allContexts);
      }
      EditorGUILayout.EndHorizontal();

      scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
      for (int i = 0; i < contexts.Count; i++) {
        var context = contexts[i];
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.LabelField($"Context {i + 1}", EditorStyles.boldLabel);
        context.Description = EditorGUILayout.TextField("Description", context.Description);
        context.Toolbar.Draw();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("CopyToBuffer")) {
          string combinedText = "";
          if (!string.IsNullOrEmpty(context.Description))
            combinedText += $" - {context.Description}:\n";
          combinedText += $"```{context.Toolbar.GetCombinedContextText()}```";
          EditorGUIUtility.systemCopyBuffer = combinedText;
          EditorPrefs.SetString(context.PrefsKey + "_description", context.Description);
        }
        if (GUILayout.Button("ResetSelected")) {
          context.Toolbar.Clear();
          context.Description = string.Empty;
          EditorPrefs.SetString(context.PrefsKey + "_description", "");
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space();
      }
      EditorGUILayout.EndScrollView();
    }

    class ContextWrapper {
      public ContextScriptsToolbar Toolbar;
      public string Description;
      public string PrefsKey;

      public ContextWrapper(ContextScriptsToolbar toolbar, string description, string prefsKey) {
        Toolbar = toolbar;
        Description = description;
        PrefsKey = prefsKey;
      }
    }
  }
}
