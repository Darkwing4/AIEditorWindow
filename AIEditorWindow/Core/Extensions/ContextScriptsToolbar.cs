// ReSharper disable EnforceIfStatementBraces
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace AIEditorWindowTool.Core.Extensions {

  using static UnityEditor.EditorGUIUtility;

  [System.Serializable]
  public class ContextScriptsToolbar {
    public int Count => contextScripts.Count;

    [SerializeField] List<MonoScript> contextScripts = new();

    Vector2 scrollPos;
    bool contextFilesFoldout;

    readonly string prefsKey;
    ReorderableList reorderableList;

    public ContextScriptsToolbar(string prefsKey) {
      this.prefsKey = prefsKey;

      Load();
      InitReorderableList();
    }

    public void Add(MonoScript ms) {
      if (!ms) {
        return;
      }

      if (contextScripts.Contains(ms)) {
        return;
      }

      contextScripts.Add(ms);

      Save();
      InitReorderableList();
    }

    public void RemoveAt(int idx) {
      if (idx < 0) {
        return;
      }

      if (idx >= contextScripts.Count) {
        return;
      }

      contextScripts.RemoveAt(idx);
      Save();
      InitReorderableList();
    }

    public void Remove(MonoScript ms) {
      if (contextScripts.Remove(ms)) {
        Save();
        InitReorderableList();
      }
    }

    public void Clear() {
      contextScripts.Clear();
      Save();
      InitReorderableList();
    }

    public void Save() {
      if (contextScripts.Count == 0) {
        EditorPrefs.DeleteKey(prefsKey);
        return;
      }

      var paths = contextScripts
              .Where(ms => ms)
              .Select(AssetDatabase.GetAssetPath)
              .Where(p => !string.IsNullOrEmpty(p) && p.EndsWith(".cs"))
              .ToArray();

      EditorPrefs.SetString(prefsKey, string.Join("|", paths));
    }

    public void Load() {
      contextScripts.Clear();

      if (!EditorPrefs.HasKey(prefsKey)) {
        return;
      }

      var joined = EditorPrefs.GetString(prefsKey);

      if (string.IsNullOrEmpty(joined)) {
        return;
      }

      foreach (var p in joined.Split('|')) {

        if (string.IsNullOrEmpty(p)) {
          continue;
        }

        var ms = AssetDatabase.LoadAssetAtPath<MonoScript>(p);

        if (!ms) {
          continue;
        }

        if (contextScripts.Contains(ms)) {
          continue;
        }

        contextScripts.Add(ms);
      }
    }

    public string GetCombinedContextText() {
      var sb = new StringBuilder();
      foreach (var ms in contextScripts) {
        if (!ms) {
          continue;
        }

        var path = AssetDatabase.GetAssetPath(ms);

        if (string.IsNullOrEmpty(path)) {
          continue;
        }

        if (!File.Exists(path)) {
          continue;
        }

        sb.AppendLine($"File: {Path.GetFileName(path)}");
        sb.AppendLine(File.ReadAllText(path));
      }

      return sb.ToString();
    }

    public void Draw() {
      contextFilesFoldout = EditorGUILayout.Foldout(contextFilesFoldout, "Context Scripts (drag .cs files)", true);

      if (!contextFilesFoldout) {
        return;
      }

      if (Count > 0) {
        if (GUILayout.Button("Reset List")) {
          Clear();
        }
      }

      HandleDragAndDrop();
      EditorGUILayout.Space(5);
      DrawListGUI();
      EditorGUILayout.Space(5);
    }

    void InitReorderableList() {
      reorderableList = new ReorderableList(contextScripts, typeof(MonoScript), true, false, false, false);

      reorderableList.drawElementCallback = (rect, index, active, focused) => {
        var width = rect.width;
        var fieldWidth = width - 210;
        var rObj = new Rect(rect.x, rect.y, fieldWidth, EditorGUIUtility.singleLineHeight);

        contextScripts[index] = (MonoScript)EditorGUI.ObjectField(rObj, contextScripts[index], typeof(MonoScript), false);

        var rCopyName = new Rect(rect.x + fieldWidth + 4, rect.y, 70, EditorGUIUtility.singleLineHeight);

        if (GUI.Button(rCopyName, "Copy name")) {
          systemCopyBuffer = contextScripts[index] ? contextScripts[index].name : "";
        }

        var rCopyFile = new Rect(rect.x + fieldWidth + 4 + 70 + 4, rect.y, 65, EditorGUIUtility.singleLineHeight);

        if (GUI.Button(rCopyFile, "Copy file")) {
          systemCopyBuffer = contextScripts[index] ? contextScripts[index].text : "";
        }

        var rRemove = new Rect(rect.x + fieldWidth + 4 + 70 + 4 + 65 + 4, rect.y, 65, EditorGUIUtility.singleLineHeight);

        if (GUI.Button(rRemove, "Remove")) {
          RemoveAt(index);
        }
      };

      reorderableList.onReorderCallback = list => Save();
      reorderableList.elementHeight = EditorGUIUtility.singleLineHeight + 2;
    }

    void DrawListGUI() {
      if (reorderableList == null || reorderableList.list != contextScripts) {
        InitReorderableList();
      }

      var listHeight = Mathf.Clamp(contextScripts.Count * (EditorGUIUtility.singleLineHeight + 4) + 20f, 60f, 400f);

      scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(listHeight));

      reorderableList.DoLayoutList();

      EditorGUILayout.EndScrollView();
    }

    void HandleDragAndDrop() {
      var dropArea = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));
      GUI.Box(dropArea, "Drag C# scripts here", EditorStyles.helpBox);

      var evt = Event.current;

      if (evt.type is not (EventType.DragUpdated or EventType.DragPerform)) {
        return;
      }

      if (!dropArea.Contains(evt.mousePosition)) {
        return;
      }

      DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

      if (evt.type != EventType.DragPerform) {
        return;
      }

      DragAndDrop.AcceptDrag();

      foreach (var obj in DragAndDrop.objectReferences) {
        var ms = obj as MonoScript;

        if (!ms) {
          var p = AssetDatabase.GetAssetPath(obj);

          if (!string.IsNullOrEmpty(p) && p.EndsWith(".cs")) {
            ms = AssetDatabase.LoadAssetAtPath<MonoScript>(p);
          }
        }

        if (ms) {
          Add(ms);
        }
      }

      Event.current.Use();
    }
  }
}
