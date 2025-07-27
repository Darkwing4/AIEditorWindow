namespace AIEditorWindowTool.Editor.LLM.Providers {

  using System;
  using System.Collections.Generic;
  using System.Threading.Tasks;

  public interface ILLMProvider {
    string Name { get; }
    IEnumerable<string> GetModels();
    Task<string> RequestAsync(List<LLMMessage> messages, string model);
  }

  public static class LLMRegistry {
    public static List<ILLMProvider> All => Providers;

    static readonly List<ILLMProvider> Providers = new();

    static LLMRegistry() {
      foreach (var asm in AppDomain.CurrentDomain.GetAssemblies()) {
        foreach (var t in asm.GetTypes()) {
          if (!typeof(ILLMProvider).IsAssignableFrom(t)) {
            continue;
          }

          if (t.IsInterface || t.IsAbstract) {
            continue;
          }

          if (Activator.CreateInstance(t) is not ILLMProvider p) {
            continue;
          }

          Register(p);
        }
      }
    }

    public static void Register(ILLMProvider p) {
      if (!Providers.Contains(p)) {
        Providers.Add(p);
      }
    }
  }
}
