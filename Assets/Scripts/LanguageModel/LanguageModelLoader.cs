using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

namespace AndroidXR.LanguageModel
{
    public sealed class LoadedLanguageModel
    {
        public Vocabulary Vocabulary { get; }
        public BigramLanguageModel BigramLanguageModel { get; }

        public LoadedLanguageModel(Vocabulary vocabulary, BigramLanguageModel bigramLanguageModel)
        {
            Vocabulary = vocabulary;
            BigramLanguageModel = bigramLanguageModel;
        }
    }

    public static class LanguageModelLoader
    {
        public static IEnumerator Load(string dictionaryFileName, string bigramFileName, System.Action<LoadedLanguageModel> onLoaded)
        {
            string dictionaryText = null;
            string bigramText = null;

            yield return LoadText(dictionaryFileName, value => dictionaryText = value);
            yield return LoadText(bigramFileName, value => bigramText = value);

            if (string.IsNullOrEmpty(dictionaryText) || string.IsNullOrEmpty(bigramText))
            {
                Debug.LogWarning("Language model files could not be loaded from StreamingAssets.");
                onLoaded?.Invoke(null);
                yield break;
            }

            var vocabulary = Vocabulary.Parse(dictionaryText);
            var bigramModel = BigramLanguageModel.Parse(bigramText);
            onLoaded?.Invoke(new LoadedLanguageModel(vocabulary, bigramModel));
        }

        private static IEnumerator LoadText(string fileName, System.Action<string> onLoaded)
        {
            var path = Path.Combine(Application.streamingAssetsPath, fileName);
            if (!path.Contains("://"))
            {
                path = $"file://{path}";
            }

            using (var request = UnityWebRequest.Get(path))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.LogWarning($"Failed to load StreamingAssets file {fileName}: {request.error}");
                    onLoaded?.Invoke(null);
                    yield break;
                }

                onLoaded?.Invoke(request.downloadHandler.text);
            }
        }
    }
}
