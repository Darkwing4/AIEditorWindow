using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using UnityEngine;

namespace AIEditorWindowTool.LLM {

  public sealed class OpenAIProvider : ILLMProvider {

    public enum OpenAIModel {
      gpt_4_1,
      gpt_4o,
      gpt_4o_mini,
      o1,
      gpt_4_5_preview,
    }

    const string url = "https://api.openai.com/v1/chat/completions";
    static readonly HttpClient client = new();

    public string Name => "OpenAI";

    public IEnumerable<string> GetModels() {
      foreach (var v in (OpenAIModel[])Enum.GetValues(typeof(OpenAIModel))) {
        yield return v.ToString().Replace('_', '-');
      }
    }

    public async Task<string> RequestAsync(List<LLMMessage> msgs, string model) {
      var key = AIToolsSettings.instance.openAIAPIKey;
      if (string.IsNullOrEmpty(key)) {
        Debug.LogError("OpenAIProvider: API key is missing");
        return string.Empty;
      }

      var req = new { model, messages = msgs };
      var content = new StringContent(JsonConvert.SerializeObject(req), Encoding.UTF8, "application/json");

      client.DefaultRequestHeaders.Clear();
      client.DefaultRequestHeaders.Add("Authorization", $"Bearer {key}");

      try {
        var resp = await client.PostAsync(url, content);
        if (!resp.IsSuccessStatusCode) {
          var errBody = await resp.Content.ReadAsStringAsync();
          Debug.LogError($"OpenAIProvider: Request failed - {(int)resp.StatusCode} {resp.ReasonPhrase}\n{errBody}");
          return string.Empty;
        }

        var json = await resp.Content.ReadAsStringAsync();
        dynamic data = JsonConvert.DeserializeObject(json);
        if (data == null) {
          Debug.LogError("OpenAIProvider: Failed to parse JSON response");
          return string.Empty;
        }

        return (string)data.choices[0].message.content;
      }
      catch (Exception e) {
        Debug.LogException(e);
        return string.Empty;
      }
    }

    static OpenAIProvider() { LLMRegistry.Register(new OpenAIProvider()); }
  }
}
