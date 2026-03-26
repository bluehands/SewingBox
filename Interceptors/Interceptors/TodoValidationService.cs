using FunicularSwitch.Generic;

namespace Interceptors;

public interface ITodoValidationService
{
    Writer<GenericResult<CreateTodoDto, Error>, string> ValidateCreate(CreateTodoDto dto);
    Writer<GenericResult<UpdateTodoDto, Error>, string> ValidateUpdate(UpdateTodoDto dto);
}

public class TodoValidationService : ITodoValidationService
{
    public Writer<GenericResult<CreateTodoDto, Error>, string> ValidateCreate(CreateTodoDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Title))
            return GenericResult.Error<CreateTodoDto, Error>(new Error.Validation("Title cannot be empty.")).Write(_ => "Validation failed: Title empty");

        if (dto.Title.Length < 3)
            return GenericResult.Error<CreateTodoDto, Error>(new Error.Validation("Title must be at least 3 characters long.")).Write(_ => $"Validation failed: Title too short ({dto.Title.Length})");

        if (dto.Title.Length > 100)
            return GenericResult.Error<CreateTodoDto, Error>(new Error.Validation("Title must not exceed 100 characters.")).Write(_ => $"Validation failed: Title too long ({dto.Title.Length})");

        return Writer.Unit<string>().Map(_ => GenericResult.Ok<CreateTodoDto, Error>(dto));
    }

    public Writer<GenericResult<UpdateTodoDto, Error>, string> ValidateUpdate(UpdateTodoDto dto)
    {
        if (dto.Title == null && dto.IsCompleted == null)
            return GenericResult.Error<UpdateTodoDto, Error>(new Error.Validation("At least one field (Title or IsCompleted) must be provided.")).Write(_ => "Validation failed: No fields provided");

        if (dto.Title != null)
        {
            if (string.IsNullOrWhiteSpace(dto.Title))
                return GenericResult.Error<UpdateTodoDto, Error>(new Error.Validation("Title cannot be empty if provided.")).Write(_ => "Validation failed: Update Title empty");

            if (dto.Title.Length < 3)
                return GenericResult.Error<UpdateTodoDto, Error>(new Error.Validation("Title must be at least 3 characters long.")).Write(_ => $"Validation failed: Update Title too short ({dto.Title.Length})");

            if (dto.Title.Length > 100)
                return GenericResult.Error<UpdateTodoDto, Error>(new Error.Validation("Title must not exceed 100 characters.")).Write(_ => $"Validation failed: Update Title too long ({dto.Title.Length})");
        }

        return Writer.Unit<string>().Map(_ => GenericResult.Ok<UpdateTodoDto, Error>(dto));
    }
}
