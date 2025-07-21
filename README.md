# AIEditorWindow

**LLM‑powered Unity Editor extension** – create, refactor and version EditorWindows with GPT‑4o, Claude 3 and any other LLM provider you plug in.

## Features
- Base `AIEditorWindow` class for rapid creation of LLM‑driven EditorWindows.
- Provider‑agnostic architecture (OpenAI, Anthropic, extendable).
- Prompt editor with auto‑save, history, and versioning.
- Drag‑and‑drop context scripts (include .cs files in the LLM prompt).
- Built‑in Git helpers (commit & revert selected files).
- Remembers last selected provider / model across sessions.
- Editor‑only assembly definition – zero runtime cost.

## Installation

### Unity Package Manager (Git URL)

1. Open **Window ▸ Package Manager**.
2. Press **+ ▸ Add package from Git URL…**
3. Paste:
   ```
   https://github.com/Darkwing4/AIEditorWindow.git
   ```
4. After the package is installed, go to **Edit ▸ Project Settings ▸ AI Tools** and enter the API key(s) for the provider(s) you plan to use (OpenAI, Anthropic, …).

### Requirements
- Unity **2021.3** or newer
- Newtonsoft.Json package (`com.unity.nuget.newtonsoft-json`) – pulled automatically via dependencies.

## Quick Start
```csharp
using AITools.AIEditorWindowTool;
using UnityEditor;

public class MyWindow : AIEditorWindow
{
    [MenuItem("Window/My LLM Tool")]
    public static void ShowWindow() {
        GetWindow<MyWindow>("My LLM Tool");
    }

    protected override void OnEnable() {
        base.OnEnable();
    }

    protected override void OnDisable() {
        base.OnDisable();
    }
}
```

## Contributing
Pull requests and issues are welcome. For substantial changes, please open a discussion first.

## License
AIEditorWindow is released under the MIT License – see [LICENSE](LICENSE).
