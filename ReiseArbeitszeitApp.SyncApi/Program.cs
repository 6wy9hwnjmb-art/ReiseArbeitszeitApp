using ReiseArbeitszeitApp.Sync;
using ReiseArbeitszeitApp.SyncApi;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<InMemorySyncStore>();

var app = builder.Build();

app.MapGet("/health", () => Results.Ok(new
{
    status = "ok",
    apiVersion = 1,
    serverTimeUtc = DateTimeOffset.UtcNow
}));

app.MapPost("/api/sync/push", (
    SyncPushRequest request,
    InMemorySyncStore store) =>
{
    if (string.IsNullOrWhiteSpace(request.DeviceId))
        return Results.BadRequest(new { error = "DeviceId darf nicht leer sein." });

    return Results.Ok(store.Push(request));
});

app.MapGet("/api/sync/pull", (
    long? cursor,
    InMemorySyncStore store) =>
{
    return Results.Ok(store.Pull(Math.Max(0, cursor ?? 0)));
});

app.Run();
