namespace AIEditorWindowTool.Windows {

  using System.Collections.Generic;
  using UnityEditor;
  using UnityEngine;

  public class HierarchyProject : AIEditorWindow {
    const string PrefKey = "HierarchyProject.LastFolderPath";
    Object folderObject;
    TreeNode rootNode;

    [MenuItem("GPTGenerated/" + nameof(HierarchyProject))]
    public static void ShowWindow() {
      GetWindow<HierarchyProject>("HierarchyProject");
    }

    protected override void OnEnable() {
      base.OnEnable();
      var lastFolderPath = EditorPrefs.GetString(PrefKey, string.Empty);
      if (!string.IsNullOrEmpty(lastFolderPath)) {
        var obj = AssetDatabase.LoadAssetAtPath<Object>(lastFolderPath);
        if (obj != null && AssetDatabase.IsValidFolder(lastFolderPath)) {
          folderObject = obj;
          rootNode = new TreeNode(lastFolderPath);
        }
      }
    }

    protected override void OnDisable() {
      base.OnDisable();
    }

    public override void OnGUI() {
      base.OnGUI();

      if (GUILayout.Button("Copy Hierarchy")) {
        if (rootNode != null) {
          EnsureAllLoaded(rootNode);
          var hierarchyText = BuildHierarchyString(rootNode);
          EditorGUIUtility.systemCopyBuffer = hierarchyText;
        }
      }

      DrawDropArea();
      if (rootNode != null) {
        EditorGUILayout.Space();
        DrawNode(rootNode, 0);
      }
    }

    void DrawDropArea() {
      var dropArea = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));
      GUI.Box(dropArea, "Drag folder here", EditorStyles.helpBox);

      var evt = Event.current;
      if (!(evt.type == EventType.DragUpdated || evt.type == EventType.DragPerform))
        return;

      if (!dropArea.Contains(evt.mousePosition))
        return;

      DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
      if (evt.type == EventType.DragPerform) {
        DragAndDrop.AcceptDrag();
        foreach (var dragged in DragAndDrop.objectReferences) {
          var path = AssetDatabase.GetAssetPath(dragged);
          if (AssetDatabase.IsValidFolder(path)) {
            folderObject = dragged;
            EditorPrefs.SetString(PrefKey, path);
            rootNode = new TreeNode(path);
            GUI.FocusControl(null);
            break;
          }
        }
      }
      Event.current.Use();
    }

    void DrawNode(TreeNode node, int indent) {
      EditorGUI.indentLevel = indent;
      if (node.IsFolder) {
        var expanded = EditorGUILayout.Foldout(node.isExpanded, node.NodeName, true);
        if (expanded != node.isExpanded) {
          node.isExpanded = expanded;
          if (node.isExpanded && !node.childrenLoaded)
            node.LoadChildren();
        }

        if (Event.current.type == EventType.MouseDown && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition)) {
          PingAndSelect(node.Path);
        }

        if (node.isExpanded && node.children != null) {
          foreach (var child in node.children) {
            DrawNode(child, indent + 1);
          }
        }
      }
      else {
        EditorGUILayout.LabelField(node.NodeName);
        if (Event.current.type == EventType.MouseDown && GUILayoutUtility.GetLastRect().Contains(Event.current.mousePosition)) {
          PingAndSelect(node.Path);
        }
      }
    }

    void PingAndSelect(string assetPath) {
      var obj = AssetDatabase.LoadAssetAtPath<Object>(assetPath);
      if (obj != null) {
        EditorGUIUtility.PingObject(obj);
        Selection.activeObject = obj;
      }
    }

    void EnsureAllLoaded(TreeNode node) {
      if (!node.childrenLoaded && node.IsFolder)
        node.LoadChildren();

      if (node.children != null) {
        foreach (var child in node.children) {
          EnsureAllLoaded(child);
        }
      }
    }

    string BuildHierarchyString(TreeNode node, int indent = 0) {
      var indentString = new string(' ', indent * 2);
      var result = indentString + node.NodeName + "\n";

      if (node.children != null) {
        foreach (var child in node.children) {
          result += BuildHierarchyString(child, indent + 1);
        }
      }
      return result;
    }

    class TreeNode {
      public string Path;
      public string NodeName;
      public bool isExpanded;
      public bool childrenLoaded;
      public bool IsFolder;
      public List<TreeNode> children;

      public TreeNode(string path) {
        Path = path;
        NodeName = System.IO.Path.GetFileName(path);
        IsFolder = AssetDatabase.IsValidFolder(path);
        isExpanded = false;
        childrenLoaded = false;
      }

      public void LoadChildren() {
        childrenLoaded = true;
        children = new List<TreeNode>();
        if (!IsFolder) return;

        var subFolders = AssetDatabase.GetSubFolders(Path);
        foreach (var sf in subFolders) {
          children.Add(new TreeNode(sf));
        }

        var guids = AssetDatabase.FindAssets("", new[] { Path });
        foreach (var guid in guids) {
          var assetPath = AssetDatabase.GUIDToAssetPath(guid);
          if (assetPath == Path) continue;
          if (!AssetDatabase.IsValidFolder(assetPath)) {
            var parentPath = System.IO.Path.GetDirectoryName(assetPath)?.Replace('\\', '/');
            var thisPath = Path.Replace('\\', '/');
            if (parentPath == thisPath) {
              children.Add(new TreeNode(assetPath));
            }
          }
        }
      }
    }
  }
}
