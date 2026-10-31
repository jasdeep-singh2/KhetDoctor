using KhetDoctor.Api.Inference;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();
builder.Services.AddOpenApi();
builder.Services.AddSingleton<DiseaseClassifier>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

app.UseHttpsRedirection();
app.MapDefaultEndpoints();

string[] allowedTypes = ["image/jpeg", "image/png", "image/webp"];

app.MapPost("/api/diagnoses", async (IFormFile image, DiseaseClassifier classifier, CancellationToken ct) =>
{
    if (image.Length is 0 or > 5_000_000)
        return Results.BadRequest("Image must be between 1 byte and 5 MB.");
    if (!allowedTypes.Contains(image.ContentType))
        return Results.BadRequest("Only JPEG, PNG or WebP images are allowed.");

    await using var stream = image.OpenReadStream();
    var predictions = await classifier.PredictAsync(stream, ct: ct);
    return Results.Ok(predictions);
})
.DisableAntiforgery();

app.Run();