// This program has been developed by students from the bachelor Computer Science at Utrecht
// University within the Software Project course.
// 
// © Copyright Utrecht University (Department of Information and Computing Sciences)

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using Microsoft.ML.Tokenizers; // ★ new
using Pgvector;

namespace Conflux.Domain.Logic.Services;

/// <summary>
/// Generates sentence embeddings locally via ONNX Runtime using all-MiniLM-L12-v2.
/// </summary>
public sealed class OnnxEmbeddingService : IEmbeddingService, IDisposable
{
    private readonly ILogger<OnnxEmbeddingService> _logger;
    private readonly InferenceSession _session;
    private readonly Tokenizer _tokenizer; // ★ new

    private readonly int _maxTokens;
    public int EmbeddingDimension { get; }

    public OnnxEmbeddingService(ILogger<OnnxEmbeddingService> logger,
        IConfiguration cfg)
    {
        _logger = logger;

        string modelPath = cfg["EmbeddingModel:Path"]
            ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "Models", "all-MiniLM-L12-v2.onnx");
        string tokenizerPath = cfg["EmbeddingModel:TokenizerPath"]
            ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory,
                "Models", "vocab.txt");

        if (!File.Exists(modelPath))
            throw new FileNotFoundException($"ONNX model not found: {modelPath}");
        if (!File.Exists(tokenizerPath))
            throw new FileNotFoundException($"Tokenizer not found: {tokenizerPath}");

        var opts = new SessionOptions();
        opts.EnableCpuMemArena = true;
        opts.EnableMemoryPattern = true;
        _session = new InferenceSession(modelPath, opts);

        using FileStream fs = File.OpenRead(tokenizerPath);
        _tokenizer = WordPieceTokenizer.Create(tokenizerPath, new WordPieceOptions()
        {
            UnknownToken = "[UNK]",
        });

        _maxTokens = cfg.GetValue("EmbeddingModel:MaxTokens", 512);
        EmbeddingDimension = cfg.GetValue("EmbeddingModel:Dimension", 384);

        _logger.LogInformation("Loaded ONNX model ({Model}) and tokenizer ({Tok})",
            Path.GetFileName(modelPath),
            Path.GetFileName(tokenizerPath));
    }

    public async Task<Vector> GenerateEmbeddingAsync(string text) =>
        (await GenerateEmbeddingsAsync([text]))[0];

    public async Task<Vector[]> GenerateEmbeddingsAsync(string[] texts)
    {
        var vectors = new Vector[texts.Length];

        for (int k = 0; k < texts.Length; k++)
        {
            string processed = texts[k];

            var
                ids = _tokenizer
                    .EncodeToIds(
                        processed); 
            int len = Math.Min(ids.Count, _maxTokens);

            var inputIds = new DenseTensor<long>(new[] { 1, _maxTokens });
            var attentionMask = new DenseTensor<long>(new[] { 1, _maxTokens });
            var tokenTypeIds = new DenseTensor<long>(new[] { 1, _maxTokens });

            for (int i = 0; i < len; i++)
            {
                inputIds[0, i] = ids[i];
                attentionMask[0, i] = 1;
                tokenTypeIds[0, i] = 0;
            }

            var inputs = new List<NamedOnnxValue>
            {
                NamedOnnxValue.CreateFromTensor("input_ids", inputIds),
                NamedOnnxValue.CreateFromTensor("attention_mask", attentionMask),
                NamedOnnxValue.CreateFromTensor("token_type_ids", tokenTypeIds)
            };

            using IDisposableReadOnlyCollection<DisposableNamedOnnxValue> results =
                _session.Run(inputs);

            var output = results.First().AsTensor<float>(); 
            float[] pooled = MeanPool(output, attentionMask);

            vectors[k] = new Vector(pooled);
        }

        return vectors;
    }

    private static float[] MeanPool(Tensor<float> embed, DenseTensor<long> mask)
    {
        int seqLen = embed.Dimensions[1], hidden = embed.Dimensions[2];
        var sum = new float[hidden];
        int tokens = 0;

        for (int i = 0; i < seqLen; i++)
            if (mask[0, i] == 1)
            {
                for (int j = 0; j < hidden; j++)
                    sum[j] += embed[0, i, j];
                tokens++;
            }

        for (int j = 0; j < hidden; j++)
            sum[j] /= tokens;

        return sum;
    }

    public void Dispose()
    {
        _session.Dispose();
    }
}
