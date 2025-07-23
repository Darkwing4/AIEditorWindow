using System;
using System.IO;
using System.Linq;
using AIEditorWindowTool.Core.Extensions;
using AIEditorWindowTool.LLM;
using UnityEditor;
using UnityEngine;

namespace AIEditorWindowTool.Core {

  using Debug = UnityEngine.Debug;

  public abstract class AIEditorWindow : EditorWindow {
    const double PROMPT_AUTO_SAVE_INTERVAL = 2.5;

    protected bool showGPTPanel;

    int providerIndex;
    int modelIndex;

    string providerPrefsKey;
    string modelPrefsKey;

    bool fileModifiedCached;
    double lastCheckTime;
    double lastAutoSaveTime;
    (string csPath, string promptPath) pathTuple;
    bool isGenerating;

    ContextScriptsToolbar contextScriptsToolbar;
    Vector2 promptScrollPos;
    string ContextPrefsKey { get; set; }

    [SerializeField] PromptField promptField;
    GitFilesToolbar gitFilesToolbar;

    protected virtual void OnEnable() {
      ContextPrefsKey = $"{GetType().Name}/ContextScripts_List";
      contextScriptsToolbar = new ContextScriptsToolbar(ContextPrefsKey);

      pathTuple = GetScriptAndPromptPaths();
      lastAutoSaveTime = EditorApplication.timeSinceStartup;

      providerPrefsKey = $"AIEditorWindow/LastProvider";
      modelPrefsKey = $"AIEditorWindow/LastModel";
      if (LLMRegistry.All.Count > 0) {
        var savedProvider = EditorPrefs.GetString(providerPrefsKey, null);
        if (!string.IsNullOrEmpty(savedProvider)) {
          providerIndex = LLMRegistry.All.FindIndex(p => p.Name == savedProvider);
          if (providerIndex < 0) {
            providerIndex = 0;
          }
        }

        var savedModel = EditorPrefs.GetString(modelPrefsKey, null);
        var modelList = LLMRegistry.All[providerIndex].GetModels().ToArray();
        var modelIdx = Array.IndexOf(modelList, savedModel);
        if (modelIdx >= 0) {
          modelIndex = modelIdx;
        }
      }

      promptField = new PromptField(pathTuple.promptPath);

      var gitFiles = new[] {
              pathTuple.csPath, pathTuple.csPath + ".meta",
              pathTuple.promptPath, pathTuple.promptPath + ".meta",
      };
      gitFilesToolbar = new(gitFiles, () => $"commit {GetFileName()}.cs, {GetFileName()}.cs.prompt");

      Undo.undoRedoPerformed -= Repaint;
      Undo.undoRedoPerformed += Repaint;

      return;

      (string csPath, string promptPath) GetScriptAndPromptPaths() {
        var scriptAsset = MonoScript.FromScriptableObject(this);
        var scriptFullPath = AssetDatabase.GetAssetPath(scriptAsset);
        var directory = Path.GetDirectoryName(scriptFullPath);
        var fileName = GetFileName();
        var csP = Path.Combine(directory!, fileName + ".cs");
        var promptP = Path.Combine(directory, fileName + ".cs.prompt");
        return (csP, promptP);
      }
    }

    protected virtual void OnDisable() {
      Undo.undoRedoPerformed -= Repaint;
      promptField.Save();
      contextScriptsToolbar.Save();
    }

    public virtual void OnGUI() {
      showGPTPanel = EditorGUILayout.Foldout(showGPTPanel, "AI Editor Tools");
      if (!showGPTPanel) {
        return;
      }

      if (LLMRegistry.All.Count == 0) {
        EditorGUILayout.HelpBox("No LLM providers found.", MessageType.Warning);
        return;
      }

      var providerNames = LLMRegistry.All.Select(p => p.Name).ToArray();

      var prevProvider = providerIndex;
      providerIndex = EditorGUILayout.Popup("Provider", providerIndex, providerNames);
      providerIndex = Mathf.Clamp(providerIndex, 0, providerNames.Length - 1);

      if (providerIndex != prevProvider) {
        EditorPrefs.SetString(providerPrefsKey, providerNames[providerIndex]);
        modelIndex = 0;
      }

      var provider = LLMRegistry.All[providerIndex];
      var models = provider.GetModels().ToArray();

      var prevModel = modelIndex;
      modelIndex = EditorGUILayout.Popup("Model", modelIndex, models);
      modelIndex = Mathf.Clamp(modelIndex, 0, models.Length - 1);

      if (modelIndex != prevModel) {
        EditorPrefs.SetString(modelPrefsKey, models[modelIndex]);
      }

      promptField.Draw(this);

      EditorGUILayout.Space(10);

      contextScriptsToolbar.Draw();

      EditorGUILayout.Space(10);

      EditorGUI.BeginDisabledGroup(isGenerating);
      EditorGUILayout.BeginHorizontal();

      if (GUILayout.Button("Write code")) {
        GenerateFromScratch(provider, models[modelIndex]);
      }

      if (GUILayout.Button("Rework code")) {
        GenerateWithCurrentCode(provider, models[modelIndex]);
      }

      if (GUILayout.Button("Save prompt")) { 
        promptField.Save();
      }

      EditorGUILayout.EndHorizontal();
      EditorGUI.EndDisabledGroup();

      var time = EditorApplication.timeSinceStartup;

      gitFilesToolbar.Draw();

      if (time - lastAutoSaveTime >= PROMPT_AUTO_SAVE_INTERVAL) {
        promptField.Save();
        lastAutoSaveTime = time;
      }
    }

    async void GenerateFromScratch(ILLMProvider provider, string modelName) {
      try {
        isGenerating = true;

        var (csPath, _) = pathTuple;
        if (!File.Exists(csPath)) {
          Debug.LogError("FILE NOT FOUND: " + csPath);
          return;
        }

        var messages = PromptWrapper.WithoutCode(GetFileName(), promptField.Prompt);

        if (contextScriptsToolbar.Count > 0) {
          messages.Add(new("user",
                  $"Here are additional files for context, NOT modify them:\n{contextScriptsToolbar.GetCombinedContextText()}"));
        }

        Debug.Log(string.Join("\n---\n", messages.Select(m => $"{m.role}: {m.content}")));

        var generatedCode = await provider.RequestAsync(messages, modelName);

        if (string.IsNullOrEmpty(generatedCode)) {
          Debug.LogError("Generation error");
          return;
        }

        await File.WriteAllTextAsync(csPath, generatedCode);

        AssetDatabase.Refresh();
      }
      catch (Exception e) {
        Debug.Log(e.Message);
      }
      finally {
        isGenerating = false;
      }
    }

    async void GenerateWithCurrentCode(ILLMProvider provider, string modelName) {
      try {
        isGenerating = true;

        var (csPath, _) = pathTuple;
        if (!File.Exists(csPath)) {
          Debug.LogError("File not found: " + csPath);
          return;
        }

        var currentCode = await File.ReadAllTextAsync(csPath);

        var messages = PromptWrapper.WithCode(GetFileName(), promptField.Prompt, currentCode);

        if (contextScriptsToolbar.Count > 0) {
          messages.Add(new("user",
                  $"Here are additional files for context, NOT modify them:\n{contextScriptsToolbar.GetCombinedContextText()}"));
        }

        Debug.Log(string.Join("\n---\n", messages.Select(m => $"{m.role}: {m.content}")));

        var generatedCode = await provider.RequestAsync(messages, modelName);

        if (string.IsNullOrEmpty(generatedCode)) {
          Debug.LogError("Source generation error");
          return;
        }

        await File.WriteAllTextAsync(csPath, generatedCode);

        AssetDatabase.Refresh();
      }
      catch (Exception e) {
        Debug.LogError(e);
      }
      finally {
        isGenerating = false;
      }
    }

    string GetFileName() => GetType().Name;
  }
}
