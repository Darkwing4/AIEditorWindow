namespace AIEditorWindowTool.Editor.LLM {

  public struct LLMMessage {
    public string role;
    public string content;

    public LLMMessage(string r, string c) {
      role = r;
      content = c;
    }
  }
}
