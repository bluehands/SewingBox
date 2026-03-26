global using FunicularSwitch.Generic;
using System.Text.Json;
using FunicularSwitch;

namespace Interceptors;

public record Todo(Guid Id, string Title, bool IsCompleted, DateTime CreatedAt);

public record CreateTodoDto(string Title);

public record UpdateTodoDto(string? Title, bool? IsCompleted);

public interface ITodoRepository
{
    Task<Writer<GenericResult<IEnumerable<Todo>, Error>, string>> GetAll();
    Task<Writer<GenericResult<Todo, Error>, string>> GetById(Guid id);
    Task<Writer<GenericResult<Todo, Error>, string>> Add(CreateTodoDto dto);
    Task<Writer<GenericResult<Todo, Error>, string>> Update(Guid id, UpdateTodoDto dto);
    Task<Writer<GenericResult<Unit, Error>, string>> Delete(Guid id);
}

public class JsonTodoRepository : ITodoRepository
{
    private readonly string _filePath;
    private readonly JsonSerializerOptions _options = new() { WriteIndented = true };
    private readonly SemaphoreSlim _lock = new(1, 1);

    public JsonTodoRepository(string filePath = "todos.json")
    {
        _filePath = Path.GetFullPath(filePath);
    }

    private async Task<List<Todo>> LoadTodos()
    {
        if (!File.Exists(_filePath)) return new List<Todo>();
        var json = await File.ReadAllTextAsync(_filePath);
        return JsonSerializer.Deserialize<List<Todo>>(json) ?? new List<Todo>();
    }

    private async Task SaveTodos(List<Todo> todos)
    {
        var json = JsonSerializer.Serialize(todos, _options);
        await File.WriteAllTextAsync(_filePath, json);
    }

    public async Task<Writer<GenericResult<IEnumerable<Todo>, Error>, string>> GetAll()
    {
        try
        {
            await _lock.WaitAsync();
            var todos = await LoadTodos();
            return Writer.Unit<string>().Map(_ => GenericResult.Ok<IEnumerable<Todo>, Error>(todos));
        }
        catch (Exception ex)
        {
            return Writer.Unit<string>().Map(_ =>
                GenericResult.Error<IEnumerable<Todo>, Error>(
                    new Error.UnexpectedException(ex, "Failed to load todos")));
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<Writer<GenericResult<Todo, Error>, string>> GetById(Guid id)
    {
        try
        {
            await _lock.WaitAsync();
            var todos = await LoadTodos();
            var todo = todos.FirstOrDefault(t => t.Id == id);
            return todo != null
                ? Writer.Unit<string>().Map(_ => GenericResult.Ok<Todo, Error>(todo))
                : Writer.Unit<string>().Map(_ =>
                    GenericResult.Error<Todo, Error>(new Error.NotFound($"Todo with id {id} not found")));
        }
        catch (Exception ex)
        {
            return Writer.Unit<string>().Map(_ =>
                GenericResult.Error<Todo, Error>(new Error.UnexpectedException(ex, "Failed to get todo")));
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<Writer<GenericResult<Todo, Error>, string>> Add(CreateTodoDto dto)
    {
        try
        {
            await _lock.WaitAsync();
            var todos = await LoadTodos();
            var todo = new Todo(Guid.NewGuid(), dto.Title, false, DateTime.UtcNow);
            todos.Add(todo);
            await SaveTodos(todos);
            return GenericResult.Ok<Todo, Error>(todo).Write(_ => $"Added todo: {todo.Title} ({todo.Id})");
        }
        catch (Exception ex)
        {
            return Writer.Unit<string>().Map(_ =>
                GenericResult.Error<Todo, Error>(new Error.UnexpectedException(ex, "Failed to add todo")));
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<Writer<GenericResult<Todo, Error>, string>> Update(Guid id, UpdateTodoDto dto)
    {
        try
        {
            await _lock.WaitAsync();
            var todos = await LoadTodos();
            var index = todos.FindIndex(t => t.Id == id);
            if (index == -1)
                return Writer.Unit<string>().Map(_ =>
                    GenericResult.Error<Todo, Error>(new Error.NotFound($"Todo with id {id} not found")));

            var existing = todos[index];
            var updated = existing with
            {
                Title = dto.Title ?? existing.Title,
                IsCompleted = dto.IsCompleted ?? existing.IsCompleted
            };

            todos[index] = updated;
            await SaveTodos(todos);
            return GenericResult.Ok<Todo, Error>(updated).Write(_ => $"Updated todo: {id}");
        }
        catch (Exception ex)
        {
            return Writer.Unit<string>().Map(_ =>
                GenericResult.Error<Todo, Error>(new Error.UnexpectedException(ex, "Failed to update todo")));
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<Writer<GenericResult<Unit, Error>, string>> Delete(Guid id)
    {
        try
        {
            await _lock.WaitAsync();
            var todos = await LoadTodos();
            var todo = todos.FirstOrDefault(t => t.Id == id);
            if (todo == null)
                return Writer.Unit<string>().Map(_ =>
                    GenericResult.Error<Unit, Error>(new Error.NotFound($"Todo with id {id} not found")));

            todos.Remove(todo);
            await SaveTodos(todos);
            return GenericResult.Ok<Unit, Error>(Unit.Instance).Write(_ => $"Deleted todo: {id}");
        }
        catch (Exception ex)
        {
            return Writer.Unit<string>().Map(_ =>
                GenericResult.Error<Unit, Error>(new Error.UnexpectedException(ex, "Failed to delete todo")));
        }
        finally
        {
            _lock.Release();
        }
    }
}