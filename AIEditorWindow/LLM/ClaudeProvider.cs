using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace AIEditorWindowTool.LLM {

  public sealed class ClaudeProvider : ILLMProvider {
    const string URL = "https://api.anthropic.com/v1/messages";
    const string API_VERSION = "2023-06-01";

    static readonly HttpClient client = new();

    public enum ClaudeModel {
      claude_3_sonnet_20240229,
      claude_3_opus_20240229,
      claude_sonnet_4_20250514,
      claude_opus_4_20250514,
    }

    public string Name => "Anthropic";

    public IEnumerable<string> GetModels() {
      foreach (var m in (ClaudeModel[])Enum.GetValues(typeof(ClaudeModel))) {
        yield return m.ToString().Replace('_', '-');
      }
    }

    public async Task<string> RequestAsync(List<LLMMessage> msgs, string model) {
      var key = AIToolsSettings.instance.anthropicAPIKey;
      if (string.IsNullOrEmpty(key)) {
        return string.Empty;
      }

      var reqBody = new {
              model,
              max_tokens = 1024,
              messages = msgs.ConvertAll(m => new { role = m.role, content = m.content })
      };

      var content = new StringContent(JsonConvert.SerializeObject(reqBody), Encoding.UTF8, "application/json");

      client.DefaultRequestHeaders.Clear();
      client.DefaultRequestHeaders.Add("x-api-key", key);
      client.DefaultRequestHeaders.Add("anthropic-version", API_VERSION);

      try {
        var resp = await client.PostAsync(URL, content);
        if (!resp.IsSuccessStatusCode) {
          return string.Empty;
        }

        var json = await resp.Content.ReadAsStringAsync();
        dynamic d = JsonConvert.DeserializeObject(json);
        return (string)d.content[0].text;
      }
      catch (Exception e) {
        Debug.LogError(e.Message);
        return string.Empty;
      }
    }

    static ClaudeProvider() => LLMRegistry.Register(new ClaudeProvider());
  }
}
