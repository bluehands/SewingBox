using Interceptors;
using FunicularSwitch.Generic;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddLogging(config => config.AddConsole());
builder.Services.AddSingleton<ITodoRepository, JsonTodoRepository>();
builder.Services.AddScoped<ITodoValidationService, TodoValidationService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var todosApi = app.MapGroup("/todos");

todosApi.MapGet("/", async (ITodoRepository repo, ILogger<Program> logger) =>
    (await repo.GetAll()).ToHttpResult(logger));

todosApi.MapGet("/{id:guid}", async (Guid id, ITodoRepository repo, ILogger<Program> logger) =>
    (await repo.GetById(id)).ToHttpResult(logger));

todosApi.MapPost("/", async (CreateTodoDto dto, ITodoRepository repo, ITodoValidationService validator, ILogger<Program> logger) =>
    await validator.ValidateCreate(dto)
        .Bind(validDto => repo.Add(validDto))
        .ToHttpResult(logger, t => Results.Created($"/todos/{t.Id}", t)));

todosApi.MapPut("/{id:guid}", async (Guid id, UpdateTodoDto dto, ITodoRepository repo, ITodoValidationService validator, ILogger<Program> logger) =>
    await validator.ValidateUpdate(dto)
        .Bind(validDto => repo.Update(id, validDto))
        .ToHttpResult(logger));

todosApi.MapDelete("/{id:guid}", async (Guid id, ITodoRepository repo, ILogger<Program> logger) =>
    (await repo.Delete(id)).ToHttpResult(logger, _ => Results.NoContent()));

app.Run();

public static class ResultExtensions
{
    public static IResult ToHttpResult<T>(
        this Writer<GenericResult<T, Error>, string> writer,
        ILogger logger,
        Func<T, IResult>? successOverride = null)
    {
        var (result, logElements) = writer;
        var log = string.Join(Environment.NewLine, logElements);
        if (!string.IsNullOrEmpty(log))
        {
            logger.LogInformation("Workflow Log: {Log}", log);
        }

        return result.Match(
            ok => successOverride?.Invoke(ok) ?? Results.Ok(ok),
            error => error.Match(
                (Error.NotFound notFound) => Results.NotFound(new { notFound.Message }),
                (Error.Validation validation) => Results.BadRequest(new { validation.Message }),
                (Error.UnexpectedException unexpected) =>
                    Results.InternalServerError(new { unexpected.Details, unexpected.Exception.Message })
            )
        );
    }

    public static async Task<IResult> ToHttpResult<T>(
        this Task<Writer<GenericResult<T, Error>, string>> writerTask,
        ILogger logger,
        Func<T, IResult>? successOverride = null) =>
        (await writerTask).ToHttpResult(logger, successOverride);
}