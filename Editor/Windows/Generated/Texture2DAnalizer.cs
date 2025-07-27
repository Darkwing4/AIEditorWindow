namespace AIEditorWindowTool.Windows {

  using System.Collections.Generic;
  using System.Linq;
  using UnityEditor;
  using UnityEditor.IMGUI.Controls;
  using UnityEngine;

  public class Texture2DAnalizer : AIEditorWindow {
    Texture2DTreeView treeView;
    TreeViewState treeViewState;
    MultiColumnHeaderState multiColumnHeaderState;

    [MenuItem("GPTGenerated/" + nameof(Texture2DAnalizer))]
    public static void ShowWindow() {
      Texture2DAnalizer window = GetWindow<Texture2DAnalizer>();
      window.titleContent = new GUIContent(nameof(Texture2DAnalizer));
      window.Show();
    }

    protected override void OnEnable() {
      base.OnEnable();
      if (treeViewState == null)
        treeViewState = new TreeViewState();

      var headerState = Texture2DTreeView.CreateDefaultMultiColumnHeaderState();
      if (MultiColumnHeaderState.CanOverwriteSerializedFields(multiColumnHeaderState, headerState))
        MultiColumnHeaderState.OverwriteSerializedFields(multiColumnHeaderState, headerState);
      multiColumnHeaderState = headerState;

      var multiColumnHeader = new MultiColumnHeader(headerState);
      treeView = new Texture2DTreeView(treeViewState, multiColumnHeader);
      RefreshTreeView();
    }

    protected override void OnDisable() {
      base.OnDisable();
    }

    public override void OnGUI() {
      base.OnGUI();

      if (GUILayout.Button("Refresh", GUILayout.Height(25f))) {
        RefreshTreeView();
      }

      if (treeView != null) {
        Rect treeRect = GUILayoutUtility.GetRect(0, 100000, 0, 100000);
        treeView.OnGUI(treeRect);
      }
    }

    void RefreshTreeView() {
      List<TextureEntry> textures = FindAllTexture2Ds();
      if (treeView != null)
        treeView.SetTextures(textures);
    }

    List<TextureEntry> FindAllTexture2Ds() {
      var results = new List<TextureEntry>();
      string[] guids = AssetDatabase.FindAssets("t:Texture2D");

      foreach (string guid in guids) {
        string path = AssetDatabase.GUIDToAssetPath(guid);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (importer != null && texture != null) {
          results.Add(new TextureEntry() {
                  Name = texture.name,
                  Path = path,
                  Size = $"{texture.width}x{texture.height}",
                  Compression = importer.textureCompression.ToString(),
                  TextureType = importer.textureType.ToString()
          });
        }
      }
      return results;
    }

    internal class TextureEntry {
      public string Name;
      public string Path;
      public string Size;
      public string Compression;
      public string TextureType;
    }

    class Texture2DTreeView : TreeView {
      enum Column {
        Name,
        Path,
        Size,
        Compression,
        TextureType
      }

      List<TextureEntry> textures;

      public Texture2DTreeView(TreeViewState state, MultiColumnHeader multiColumnHeader)
              : base(state, multiColumnHeader) {
        showAlternatingRowBackgrounds = true;
        multiColumnHeader.sortingChanged += OnSortingChanged;
        textures = new List<TextureEntry>();
        Reload();
      }

      public void SetTextures(List<TextureEntry> textureList) {
        textures = textureList;
        OnSortingChanged(multiColumnHeader);
        Reload();
      }

      protected override TreeViewItem BuildRoot() {
        var root = new TreeViewItem(-1, -1, "Root");
        var items = new List<TreeViewItem>();

        for (int i = 0; i < textures.Count; i++) {
          items.Add(new TextureEntryTreeViewItem(i, textures[i]));
        }

        SetupParentsAndChildrenFromDepths(root, items);
        return root;
      }

      protected override void RowGUI(RowGUIArgs args) {
        var item = (TextureEntryTreeViewItem)args.item;

        for (int i = 0; i < args.GetNumVisibleColumns(); i++) {
          Rect cellRect = args.GetCellRect(i);
          Column column = (Column)args.GetColumn(i);
          string content = GetCellContent(item.entry, column);
          DefaultGUI.Label(cellRect, content, args.selected, args.focused);
        }
      }

      string GetCellContent(TextureEntry entry, Column column) {
        switch (column) {
          case Column.Name: return entry.Name;
          case Column.Path: return entry.Path;
          case Column.Size: return entry.Size;
          case Column.Compression: return entry.Compression;
          case Column.TextureType: return entry.TextureType;
          default: return "";
        }
      }

      void OnSortingChanged(MultiColumnHeader header) {
        if (textures == null || textures.Count <= 0 || header.sortedColumnIndex < 0)
          return;

        bool ascending = header.IsSortedAscending(header.sortedColumnIndex);

        switch ((Column)header.sortedColumnIndex) {
          case Column.Name:
            textures = ascending ? textures.OrderBy(e => e.Name).ToList()
                    : textures.OrderByDescending(e => e.Name).ToList();
            break;
          case Column.Path:
            textures = ascending ? textures.OrderBy(e => e.Path).ToList()
                    : textures.OrderByDescending(e => e.Path).ToList();
            break;
          case Column.Size:
            textures = ascending ?
                    textures.OrderBy(e => ParseSize(e.Size)).ToList() :
                    textures.OrderByDescending(e => ParseSize(e.Size)).ToList();
            break;
          case Column.Compression:
            textures = ascending ? textures.OrderBy(e => e.Compression).ToList()
                    : textures.OrderByDescending(e => e.Compression).ToList();
            break;
          case Column.TextureType:
            textures = ascending ? textures.OrderBy(e => e.TextureType).ToList()
                    : textures.OrderByDescending(e => e.TextureType).ToList();
            break;
          default:
            break;
        }

        Reload();
      }

      static int ParseSize(string size) {
        if (string.IsNullOrEmpty(size))
          return 0;
        // parse "NxM" to N*M as total pixels (or max(N,M) for single direction sort)
        var split = size.Split('x');
        if (split.Length == 2 && int.TryParse(split[0], out int w) && int.TryParse(split[1], out int h)) {
          return w * h;
        }
        return 0;
      }

      public static MultiColumnHeaderState CreateDefaultMultiColumnHeaderState() {
        var columns = new[] {
                new MultiColumnHeaderState.Column {
                        headerContent = new GUIContent("Name"),
                        width = 150,
                        autoResize = true,
                        allowToggleVisibility = false,
                        canSort = true,
                },
                new MultiColumnHeaderState.Column {
                        headerContent = new GUIContent("Path"),
                        width = 250,
                        autoResize = true,
                        allowToggleVisibility = false,
                        canSort = true,
                },
                new MultiColumnHeaderState.Column {
                        headerContent = new GUIContent("Size"),
                        width = 75,
                        autoResize = true,
                        allowToggleVisibility = false,
                        canSort = true,
                },
                new MultiColumnHeaderState.Column {
                        headerContent = new GUIContent("Compression"),
                        width = 100,
                        autoResize = true,
                        allowToggleVisibility = false,
                        canSort = true,
                },
                new MultiColumnHeaderState.Column {
                        headerContent = new GUIContent("TextureType"),
                        width = 100,
                        autoResize = true,
                        allowToggleVisibility = false,
                        canSort = true,
                },
        };

        return new MultiColumnHeaderState(columns);
      }

      class TextureEntryTreeViewItem : TreeViewItem {
        public TextureEntry entry;

        public TextureEntryTreeViewItem(int id, TextureEntry entry)
                : base(id, 0, entry.Name) {
          this.entry = entry;
        }
      }
    }
  }
}
