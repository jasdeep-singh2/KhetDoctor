using System.Text.Json;
using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace KhetDoctor.Api.Inference;

public sealed record Prediction(string Label, string Crop, string Disease, float Confidence);

public sealed class DiseaseClassifier : IDisposable
{
    private const int Size = 224;
    private static readonly float[] Mean = [0.485f, 0.456f, 0.406f];
    private static readonly float[] Std = [0.229f, 0.224f, 0.225f];

    private readonly InferenceSession _session;
    private readonly string[] _labels;

    public DiseaseClassifier()
    {
        var dir = Path.Combine(AppContext.BaseDirectory, "MLModels");
        _session = new InferenceSession(Path.Combine(dir, "plant_disease.onnx"));
        _labels = JsonSerializer.Deserialize<string[]>(
            File.ReadAllText(Path.Combine(dir, "labels.json")))!;
    }

    public async Task<IReadOnlyList<Prediction>> PredictAsync(
        Stream image, int topK = 3, CancellationToken ct = default)
    {
        using var img = await Image.LoadAsync<Rgb24>(image, ct);
        img.Mutate(x => x
            .AutoOrient()
            .Resize(new ResizeOptions
            {
                Size = new Size(Size, Size),
                Mode = ResizeMode.Stretch,
                Sampler = KnownResamplers.Triangle,
            }));

        var tensor = new DenseTensor<float>([1, 3, Size, Size]);
        img.ProcessPixelRows(rows =>
        {
            for (var y = 0; y < Size; y++)
            {
                var row = rows.GetRowSpan(y);
                for (var x = 0; x < Size; x++)
                {
                    tensor[0, 0, y, x] = (row[x].R / 255f - Mean[0]) / Std[0];
                    tensor[0, 1, y, x] = (row[x].G / 255f - Mean[1]) / Std[1];
                    tensor[0, 2, y, x] = (row[x].B / 255f - Mean[2]) / Std[2];
                }
            }
        });

        using var results = _session.Run([NamedOnnxValue.CreateFromTensor("input", tensor)]);
        var logits = results[0].AsEnumerable<float>().ToArray();

        var max = logits.Max();
        var exp = logits.Select(l => MathF.Exp(l - max)).ToArray();
        var sum = exp.Sum();

        return exp
            .Select((e, i) => ToPrediction(_labels[i], e / sum))
            .OrderByDescending(p => p.Confidence)
            .Take(topK)
            .ToList();
    }

    private static Prediction ToPrediction(string label, float confidence)
    {
        var parts = label.Split("___");
        return new Prediction(label, parts[0], parts[1].Replace('_', ' '), confidence);
    }

    public void Dispose() => _session.Dispose();
}