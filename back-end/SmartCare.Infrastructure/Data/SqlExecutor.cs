using System.Data;
using System.Data.Common;
using Microsoft.Data.SqlClient;
using SmartCare.Application.Interfaces.Infrastructure;

namespace SmartCare.Infrastructure.Data;

public sealed class SqlExecutor : ISqlExecutor
{
    private const int CommandTimeoutSeconds = 30;

    private readonly string _connectionString;

    public SqlExecutor(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentException("Connection string is required.", nameof(connectionString));
        _connectionString = connectionString;
    }

    public Task<int> ExecuteNonQueryAsync(
        string procedureName,
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default) =>
        RunAsync(procedureName, parameters, cancellationToken,
            cmd => cmd.ExecuteNonQueryAsync(cancellationToken));

    public async Task<T?> ExecuteScalarAsync<T>(
        string procedureName,
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default)
    {
        var result = await RunAsync(procedureName, parameters, cancellationToken,
            cmd => cmd.ExecuteScalarAsync(cancellationToken));

        if (result is null || result is DBNull) return default;
        if (result is T direct) return direct;

        var target = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);
        return (T)Convert.ChangeType(result, target);
    }

    public Task<IReadOnlyList<T>> QueryAsync<T>(
        string procedureName,
        Func<DbDataReader, T> map,
        IReadOnlyDictionary<string, object?>? parameters = null,
        CancellationToken cancellationToken = default) =>
        RunAsync<IReadOnlyList<T>>(procedureName, parameters, cancellationToken, async cmd =>
        {
            await using var reader = await cmd.ExecuteReaderAsync(cancellationToken);
            var rows = new List<T>();
            while (await reader.ReadAsync(cancellationToken))
                rows.Add(map(reader));
            return rows;
        });

    private async Task<TResult> RunAsync<TResult>(
        string procedureName,
        IReadOnlyDictionary<string, object?>? parameters,
        CancellationToken cancellationToken,
        Func<SqlCommand, Task<TResult>> action)
    {
        if (string.IsNullOrWhiteSpace(procedureName) || procedureName.Any(char.IsWhiteSpace))
            throw new ArgumentException(
                "Pass a stored procedure name (e.g. \"dbo.sp_GetUserByUserName\"), not raw SQL.",
                nameof(procedureName));

        await using var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var command = BuildCommand(connection, procedureName, parameters);
        return await action(command);
    }

    private static SqlCommand BuildCommand(
        SqlConnection connection,
        string procedureName,
        IReadOnlyDictionary<string, object?>? parameters)
    {
        var command = connection.CreateCommand();
        command.CommandText = procedureName;
        command.CommandType = CommandType.StoredProcedure;
        command.CommandTimeout = CommandTimeoutSeconds;

        if (parameters is not null)
            foreach (var (name, value) in parameters)
                command.Parameters.Add(CreateParameter(name, value));

        return command;
    }

    private static SqlParameter CreateParameter(string name, object? value)
    {
        var parameterName = name.StartsWith('@') ? name : "@" + name;

        return value switch
        {
            null => new SqlParameter(parameterName, DBNull.Value),
            decimal d => new SqlParameter(parameterName, SqlDbType.Decimal) { Precision = 18, Scale = 2, Value = d },
            DateTime dt => new SqlParameter(parameterName, SqlDbType.DateTime2) { Value = dt },
            _ => new SqlParameter(parameterName, value)
        };
    }
}
