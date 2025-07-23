using System;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace AIEditorWindowTool.Core.Extensions {

  using static GitAsyncHelper;

  public class GitFilesToolbar {
    readonly string[] files;
    readonly Func<string> getCommitMessage;

    public GitFilesToolbar(string[] files, Func<string> getCommitMessage) {
      this.files = files;
      this.getCommitMessage = getCommitMessage;
    }

    public void Draw() {
      EditorGUILayout.BeginHorizontal();

      if (GUILayout.Button("Git Commit")) {
        Commit();
      }

      if (GUILayout.Button("Reset Files")) {
        Reset();
      }

      EditorGUILayout.EndHorizontal();
      EditorGUILayout.Space(20);
    }

    async void Commit() {
      try {
        await AddAsync(files, CancellationToken.None);
        await CommitAsync(getCommitMessage(), CancellationToken.None);
      }
      catch (Exception e) {
        Debug.LogException(e);
      }
    }

    async void Reset() {
      try {
        await RestoreAsync(files, CancellationToken.None);
      }
      catch (Exception e) {
        Debug.LogException(e);
      }
    }
  }
}
