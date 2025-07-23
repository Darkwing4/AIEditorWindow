using System;
using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AIEditorWindowTool.Core {
  public class HierarchyToJson : AIEditorWindow {
    [MenuItem("GPTGenerated/" + nameof(HierarchyToJson))]
    public static void ShowWindow() {
      GetWindow<HierarchyToJson>(false, "HierarchyToJson");
    }

    public override void OnGUI() {
      base.OnGUI();

      EditorGUILayout.LabelField("Hierarchy To Json Converter", EditorStyles.boldLabel);

      if (Selection.activeGameObject == null) {
        EditorGUILayout.HelpBox("Select a GameObject in the Hierarchy.", MessageType.Info);
        return;
      }

      if (GUILayout.Button("ToJson")) {
        var root = Selection.activeGameObject;
        var json = BuildJson(root);
        EditorGUIUtility.systemCopyBuffer = json;
        Debug.Log("Hierarchy JSON copied to clipboard.");
      }
    }

    string BuildJson(GameObject go) {
      var dict = HierarchyGoToDict(go);
      return MiniJSON.Serialize(dict);
    }

    static Dictionary<string, object> HierarchyGoToDict(GameObject go) {
      var node = new Dictionary<string, object> {
              ["name"] = go.name,
      };
      // Components
      var components = go.GetComponents<Component>();
      var componentNames = new List<string>();
      foreach (var comp in components) {
        if (comp == null) continue;
        componentNames.Add(comp.GetType().Name);
      }
      node["components"] = componentNames;
      // Children
      var children = new List<object>();
      for (int i = 0; i < go.transform.childCount; i++) {
        var child = go.transform.GetChild(i).gameObject;
        children.Add(HierarchyGoToDict(child));
      }
      node["children"] = children;
      return node;
    }

    // MiniJSON implementation in place, no dependency
    internal static class MiniJSON {
      public static string Serialize(object obj) {
        var sb = new StringBuilder();
        SerializeValue(obj, sb);
        return sb.ToString();
      }

      static void SerializeValue(object value, StringBuilder sb) {
        if (value == null) {
          sb.Append("null");
        }
        else if (value is string s) {
          SerializeString(s, sb);
        }
        else if (value is bool b) {
          sb.Append(b ? "true" : "false");
        }
        else if (value is IDictionary<string, object> dict) {
          SerializeObject(dict, sb);
        }
        else if (value is IList<object> list) {
          SerializeArray(list, sb);
        }
        else if (value is IList<string> listString) {
          SerializeArrayString(listString, sb);
        }
        else if (value is double || value is float || value is int || value is long) {
          sb.Append(Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture));
        }
        else {
          SerializeString(value.ToString(), sb);
        }
      }

      static void SerializeObject(IDictionary<string, object> obj, StringBuilder sb) {
        sb.Append('{');
        bool first = true;
        foreach (var kvp in obj) {
          if (!first) sb.Append(',');
          SerializeString(kvp.Key, sb);
          sb.Append(':');
          SerializeValue(kvp.Value, sb);
          first = false;
        }
        sb.Append('}');
      }

      static void SerializeArray(IList<object> anArray, StringBuilder sb) {
        sb.Append('[');
        bool first = true;
        foreach (object obj in anArray) {
          if (!first) sb.Append(',');
          SerializeValue(obj, sb);
          first = false;
        }
        sb.Append(']');
      }

      static void SerializeArrayString(IList<string> anArray, StringBuilder sb) {
        sb.Append('[');
        bool first = true;
        foreach (string str in anArray) {
          if (!first) sb.Append(',');
          SerializeString(str, sb);
          first = false;
        }
        sb.Append(']');
      }

      static void SerializeString(string str, StringBuilder sb) {
        sb.Append('\"');
        foreach (var c in str) {
          switch (c) {
            case '\"': sb.Append("\\\""); break;
            case '\\': sb.Append("\\\\"); break;
            case '\b': sb.Append("\\b"); break;
            case '\f': sb.Append("\\f"); break;
            case '\n': sb.Append("\\n"); break;
            case '\r': sb.Append("\\r"); break;
            case '\t': sb.Append("\\t"); break;
            default:
              if (c < ' ')
                sb.AppendFormat("\\u{0:X4}", (int)c);
              else
                sb.Append(c);
              break;
          }
        }
        sb.Append('\"');
      }
    }

    protected override void OnEnable() {
      base.OnEnable();
    }

    protected override void OnDisable() {
      base.OnDisable();
    }
  }
}
