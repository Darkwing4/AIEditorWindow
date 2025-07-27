namespace AIEditorWindowTool.Editor.Settings {

  using System.Collections.Generic;
  using LLM;
  using UnityEditor;
  using UnityEngine;

  static class PromptWrapper {
    static PromptWrapperConfig cfg;

    static PromptWrapperConfig Cfg => cfg ??= LoadOrDefault();

    static PromptWrapperConfig LoadOrDefault() {

      var guids = AssetDatabase.FindAssets("t:PromptWrapperConfig");

      if (guids.Length > 0) {
        var path = AssetDatabase.GUIDToAssetPath(guids[0]);
        return AssetDatabase.LoadAssetAtPath<PromptWrapperConfig>(path);
      }

      return ScriptableObject.CreateInstance<PromptWrapperConfig>();
    }

    public static List<LLMMessage> WithoutCode(string scriptName, string prompt) {
      var systemMsg = Cfg.systemWithoutCode
              .Replace("{scriptName}", scriptName);

      var userMsg = Cfg.userWithoutCode
              .Replace("{prompt}", prompt);

      return new() { new("system", systemMsg), new("user", userMsg) };
    }

    public static List<LLMMessage> WithCode(string scriptName, string prompt, string currentCode) {
      var systemMsg = Cfg.systemWithCode
              .Replace("{scriptName}", scriptName);

      var userMsg = Cfg.userWithCode
              .Replace("{prompt}", prompt)
              .Replace("{currentCode}", currentCode);

      return new() { new("system", systemMsg), new("user", userMsg) };
    }
  }
}
