using KhetDoctor.Api.Inference;
using Microsoft.AspNetCore.Http.HttpResults;

namespace KhetDoctor.Api.Features.Diagnoses;

public static class DiagnosisEndpoints
{
    private const long MaxImageBytes = 5_000_000;
    private static readonly string[] AllowedTypes = ["image/jpeg", "image/png", "image/webp"];

    public static IEndpointRouteBuilder MapDiagnosisEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/diagnoses")
            .WithTags("Diagnoses");

        group.MapPost("/", CreateDiagnosis)
            .WithName("CreateDiagnosis")
            .WithSummary("Upload a leaf photo and get the top disease predictions")
            .DisableAntiforgery();

        return app;
    }

    private static async Task<Results<Ok<IReadOnlyList<Prediction>>, BadRequest<string>>> CreateDiagnosis(
        IFormFile image,
        DiseaseClassifier classifier,
        CancellationToken ct)
    {
        if (image.Length is 0 or > MaxImageBytes)
            return TypedResults.BadRequest("Image must be between 1 byte and 5 MB.");
        if (!AllowedTypes.Contains(image.ContentType))
            return TypedResults.BadRequest("Only JPEG, PNG or WebP images are allowed.");

        await using var stream = image.OpenReadStream();
        var predictions = await classifier.PredictAsync(stream, ct: ct);
        return TypedResults.Ok(predictions);
    }
}