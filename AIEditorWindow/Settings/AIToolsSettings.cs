using UnityEditor;

namespace AIEditorWindowTool {

  [FilePath("UserSettings/" + nameof(AIToolsSettings) + ".asset",
          FilePathAttribute.Location.ProjectFolder)]
  public sealed class AIToolsSettings : ScriptableSingleton<AIToolsSettings> {

    public string openAIAPIKey;
    public string anthropicAPIKey;

    public int timeout;

    public void Save() => Save(true);

    void OnDisable() => Save();
  }

  sealed class AIToolsSettingsProvider : SettingsProvider {
    AIToolsSettingsProvider() : base("Project/AI Tools", SettingsScope.Project) { }

    public override void OnGUI(string search) {
      var settings = AIToolsSettings.instance;

      var openAIKey = settings.openAIAPIKey;
      var anthropicKey = settings.anthropicAPIKey;
      var timeout = settings.timeout;

      EditorGUI.BeginChangeCheck();

      openAIKey = EditorGUILayout.TextField("OpenAI API Key", openAIKey);
      anthropicKey = EditorGUILayout.TextField("Anthropic API Key", anthropicKey);
      timeout = EditorGUILayout.IntField("Timeout (sec)", timeout);

      if (EditorGUI.EndChangeCheck()) {
        settings.openAIAPIKey = openAIKey;
        settings.anthropicAPIKey = anthropicKey;
        settings.timeout = timeout;
        settings.Save();
      }
    }

    [SettingsProvider]
    public static SettingsProvider CreateCustomSettingsProvider() => new AIToolsSettingsProvider();
  }
}
