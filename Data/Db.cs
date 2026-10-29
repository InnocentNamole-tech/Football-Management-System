using System.Data;
using Microsoft.Data.SqlClient;

namespace FootballClubManagement.Data;

public sealed class Db
{
    private readonly string _connectionString;

    public Db(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");
    }

    public async Task<SqlConnection> OpenAsync()
    {
        var connection = new SqlConnection(_connectionString);
        await connection.OpenAsync();
        return connection;
    }

    public static SqlParameter P(string name, object? value) => new(name, value ?? DBNull.Value);
}

public static class ReaderExtensions
{
    public static int Int(this SqlDataReader r, string column) => r.GetInt32(r.GetOrdinal(column));

    public static int? NInt(this SqlDataReader r, string column)
    {
        var i = r.GetOrdinal(column);
        return r.IsDBNull(i) ? null : r.GetInt32(i);
    }

    public static string Str(this SqlDataReader r, string column) => r.GetString(r.GetOrdinal(column));

    public static string? NStr(this SqlDataReader r, string column)
    {
        var i = r.GetOrdinal(column);
        return r.IsDBNull(i) ? null : r.GetString(i);
    }

    public static bool Bool(this SqlDataReader r, string column) => r.GetBoolean(r.GetOrdinal(column));

    public static DateTime Date(this SqlDataReader r, string column) => r.GetDateTime(r.GetOrdinal(column));

    public static DateTime? NDate(this SqlDataReader r, string column)
    {
        var i = r.GetOrdinal(column);
        return r.IsDBNull(i) ? null : r.GetDateTime(i);
    }

    public static decimal Dec(this SqlDataReader r, string column) => r.GetDecimal(r.GetOrdinal(column));

    public static TimeSpan TimeOfDay(this SqlDataReader r, string column) => r.GetTimeSpan(r.GetOrdinal(column));
}

public static class SqlErrors
{
    public static bool IsUniqueViolation(SqlException ex) => ex.Number is 2601 or 2627;

    public static bool IsForeignKeyViolation(SqlException ex) => ex.Number == 547;
}
