using UnityEngine;

namespace AIEditorWindowTool {

  [CreateAssetMenu(menuName = "AITools/Prompt Wrapper Config")]
  public class PromptWrapperConfig : ScriptableObject {
    [TextArea(6, 20)] public string systemWithoutCode =
            "Write a Unity C# EditorWindow named \"{scriptName}\".\n" +
            "- I need ready to compile EditorWindow script.\n" +
            "- Do not add any explanations.\n" +
            "- Do not use markdown in response.\n" +
            "- Inheritor MUST call base.OnGUI.\n" +
            "- Inheritor MUST HAVE public override void OnGUI() with base.OnGUI()!!!\n" +
            "- Inheritor MUST be inside namespace AIEditorWindowTool.Core\n" +
            "- Inheritor MUST inherit from AIEditorWindow.\n" +
            "- Inheritor MUST call base.OnEnable if need override it.\n" +
            "- Inheritor MUST call base.OnDisable if need override it.\n" +
            "- Do not define AIEditorWindow, it is already defined.\n" +
            "- Must place ShowWindow method in [MenuItem(\"GPTGenerated/\" + nameof({scriptName}))]\n";

    [TextArea(3, 10)] public string userWithoutCode =
            "It must do the following: {prompt}";

    [TextArea(6, 20)] public string systemWithCode =
            "Write a Unity C# EditorWindow named \"{scriptName}\".\n" +
            "- I need ready to compile EditorWindow script.\n" +
            "- Do not add any explanations.\n" +
            "- Do not use markdown in response.\n" +
            "- Inheritor MUST call base.OnGUI.\n" +
            "- Inheritor MUST be inside namespace AIEditorWindowTool.Core\n" +
            "- Inheritor MUST inherit from AIEditorWindow.\n" +
            "- Inheritor MUST call base.OnEnable if need override it.\n" +
            "- Inheritor MUST call base.OnDisable if need override it.\n" +
            "- Do not define AIEditorWindow, it is already defined.\n" +
            "- Must place ShowWindow method in [MenuItem(\"GPTGenerated/\" + nameof({scriptName}))]\n";

    [TextArea(6, 15)] public string userWithCode =
            "Consider this existing code:\n{currentCode}\n" +
            "It must do the following:\n{prompt}";
  }
}
