using UnityEngine.SceneManagement;

namespace AIEditorWindowTool.Windows {

  using System.Collections.Generic;
  using UnityEditor;
  using UnityEditor.SceneManagement;
  using UnityEngine;

  public class SceneMaterialViewer : AIEditorWindow {
    Vector2 scrollPos;
    Dictionary<Material, List<GameObject>> materialUsage = new Dictionary<Material, List<GameObject>>();
    Dictionary<Material, bool> foldouts = new Dictionary<Material, bool>();

    [MenuItem("GPTGenerated/" + nameof(SceneMaterialViewer))]
    public static void ShowWindow() {
      GetWindow<SceneMaterialViewer>("Scene Material Viewer");
    }

    protected override void OnEnable() {
      base.OnEnable();
      CollectMaterials();
      EditorSceneManager.sceneOpened += OnEditorSceneManagerOnsceneOpened;
      EditorSceneManager.sceneClosed += OnEditorSceneManagerOnsceneClosed;

      EditorApplication.hierarchyChanged += CollectMaterials;
    }

    void OnEditorSceneManagerOnsceneOpened(Scene scene, OpenSceneMode openSceneMode) {
      CollectMaterials();
    }

    void OnEditorSceneManagerOnsceneClosed(Scene _) {
      CollectMaterials();
    }

    protected override void OnDisable() {
      base.OnDisable();
      EditorSceneManager.sceneOpened -= OnEditorSceneManagerOnsceneOpened;
      EditorSceneManager.sceneClosed -= OnEditorSceneManagerOnsceneClosed;

      EditorApplication.hierarchyChanged -= CollectMaterials;
    }

    void CollectMaterials() {
      materialUsage.Clear();
      foldouts.Clear();

      Renderer[] allRenderers = FindObjectsByType<Renderer>(FindObjectsInactive.Include, FindObjectsSortMode.InstanceID);

      foreach (var renderer in allRenderers) {
        foreach (var mat in renderer.sharedMaterials) {
          if (mat == null) continue;
          if (!materialUsage.ContainsKey(mat))
            materialUsage[mat] = new List<GameObject>();
          materialUsage[mat].Add(renderer.gameObject);
        }
      }

      foreach (var mat in materialUsage.Keys) {
        if (!foldouts.ContainsKey(mat))
          foldouts.Add(mat, false);
      }

      Repaint();
    }

    public override void OnGUI() {
      base.OnGUI();

      EditorGUILayout.LabelField("Scene Materials", EditorStyles.boldLabel);
      scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

      foreach (var kvp in materialUsage) {
        var mat = kvp.Key;
        var users = kvp.Value;

        foldouts[mat] = EditorGUILayout.Foldout(foldouts[mat], mat != null ? mat.name : "Missing Material", true);
        if (foldouts[mat]) {
          EditorGUI.indentLevel++;

          EditorGUILayout.ObjectField("Material", mat, typeof(Material), false);
          Shader shader = mat != null ? mat.shader : null;
          EditorGUILayout.LabelField("Shader", shader != null ? shader.name : "(null)");
          string shaderPath = shader != null ? AssetDatabase.GetAssetPath(shader) : null;
          EditorGUILayout.LabelField("Shader Path", !string.IsNullOrEmpty(shaderPath) ? shaderPath : "(Builtin/Unknown)");

          EditorGUILayout.Space();

          foreach (var userObj in users) {
            EditorGUILayout.ObjectField("Used by", userObj, typeof(GameObject), true);
          }

          EditorGUILayout.Space();
          EditorGUI.indentLevel--;
        }
      }

      EditorGUILayout.EndScrollView();

      if (GUILayout.Button("Refresh materials")) {
        CollectMaterials();
      }
    }
  }
}
