namespace AIEditorWindowTool.Editor.Extensions {

  using Object = UnityEngine.Object;
  using System;
  using System.IO;
  using UnityEditor;
  using UnityEngine;

  /// <summary>
  /// TextArea for editing the LLM prompt inside EditorWindow.
  /// /// </summary>
  [Serializable]
  sealed class PromptField {
    const float MIN_HEIGHT = 50f;
    const float MAX_HEIGHT = 400f;

    Vector2 scroll;

    [SerializeField]
    string prompt;

    public string Prompt => prompt;

    string promptFilePath;

    public PromptField(string file) {
      promptFilePath = file;
      LoadIfExist();
    }

    public void Draw(Object undoTarget) {
      EditorGUILayout.LabelField("Prompt:", EditorStyles.boldLabel);
      var style = new GUIStyle(EditorStyles.textArea) { wordWrap = true };

      var width = EditorGUIUtility.currentViewWidth - 20f;
      var desiredHeight = style.CalcHeight(new GUIContent(prompt), width);
      var height = Mathf.Clamp(desiredHeight, MIN_HEIGHT, MAX_HEIGHT);

      EditorGUI.BeginChangeCheck();

      string newPrompt;
      using (var sv = new EditorGUILayout.ScrollViewScope(scroll, GUILayout.Height(height))) {
        newPrompt = EditorGUILayout.TextArea(prompt, style, GUILayout.ExpandHeight(true));
        scroll = sv.scrollPosition;
      }

      var isDirty = EditorGUI.EndChangeCheck();
      if (isDirty) {
        Undo.RecordObject(undoTarget, "Prompt Change");
        prompt = newPrompt;
        EditorUtility.SetDirty(undoTarget);

        Save();
      }
    }

    public void Save() {
      File.WriteAllText(promptFilePath, prompt);
    }

    void LoadIfExist() {
      if (!File.Exists(promptFilePath)) return;

      prompt = File.ReadAllText(promptFilePath);
    }
  }
}
