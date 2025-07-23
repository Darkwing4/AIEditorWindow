using UnityEditor;
using UnityEngine;

namespace AIEditorWindowTool.Core {
  public class ReserializeAssetTool : AIEditorWindow {
    [MenuItem("GPTGenerated/" + nameof(ReserializeAssetTool))]
    public static void ShowWindow() {
      var window = GetWindow<ReserializeAssetTool>();
      window.titleContent = new GUIContent("ReserializeAssetTool");
      window.Show();
    }

    void OnEnable() {
      base.OnEnable();
    }

    void OnDisable() {
      base.OnDisable();
    }

    void OnGUI() {
      base.OnGUI();

      if (GUILayout.Button("Reserialize Selected Assets")) {
        var selectedObjects = Selection.objects;
        foreach (var obj in selectedObjects) {
          if (obj != null) {
            EditorUtility.SetDirty(obj);
          }
        }
        AssetDatabase.SaveAssets();
        Debug.Log($"Reserialized {selectedObjects.Length} assets.");
      }
    }
  }
}
