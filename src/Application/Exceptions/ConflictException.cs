using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Application.Exceptions;

public class ConflictException(string message) : Exception(message)
{
    public static bool IsUniqueViolation(DbUpdateException ex) =>
        ex.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };
}

