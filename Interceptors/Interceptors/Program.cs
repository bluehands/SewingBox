using Interceptors;
using FunicularSwitch.Generic;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
builder.Services.AddLogging(config => config.AddConsole());
builder.Services.AddSingleton<ITodoRepository, JsonTodoRepository>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

var todosApi = app.MapGroup("/todos");

todosApi.MapGet("/", async (ITodoRepository repo) =>
    (await repo.GetAll()).ToHttpResult());

todosApi.MapGet("/{id:guid}", async (Guid id, ITodoRepository repo) =>
    (await repo.GetById(id)).ToHttpResult());

todosApi.MapPost("/", async (CreateTodoDto dto, ITodoRepository repo) =>
    (await repo.Add(dto)).ToHttpResult(t => Results.Created($"/todos/{t.Id}", t)));

todosApi.MapPut("/{id:guid}", async (Guid id, UpdateTodoDto dto, ITodoRepository repo) =>
    (await repo.Update(id, dto)).ToHttpResult());

todosApi.MapDelete("/{id:guid}", async (Guid id, ITodoRepository repo) =>
    (await repo.Delete(id)).ToHttpResult(_ => Results.NoContent()));

app.Run();

public static class ResultExtensions
{
    public static IResult ToHttpResult<T>(
        this GenericResult<T, Error> result,
        Func<T, IResult>? successOverride = null) =>
        result.Match(
            ok => successOverride?.Invoke(ok) ?? Results.Ok(ok),
            error => error.Match(
                notFound => Results.NotFound(new { notFound.Message }),
                validation => Results.BadRequest(new { validation.Message }),
                unexpected =>
                    Results.InternalServerError(new { unexpected.Details, unexpected.Exception.Message })
            )
        );
}