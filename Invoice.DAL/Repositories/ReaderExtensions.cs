using System.Data.Common;

namespace Invoice.DAL.Repositories;

internal static class ReaderExtensions
{
    public static int Int(this DbDataReader r, string column)
        => r.GetInt32(r.GetOrdinal(column));

    public static decimal Num(this DbDataReader r, string column)
        => r.GetDecimal(r.GetOrdinal(column));

    public static bool Flag(this DbDataReader r, string column)
        => r.GetBoolean(r.GetOrdinal(column));

    public static string Text(this DbDataReader r, string column)
        => r.GetString(r.GetOrdinal(column));

    public static string? TextOrNull(this DbDataReader r, string column)
    {
        var i = r.GetOrdinal(column);
        return r.IsDBNull(i) ? null : r.GetString(i);
    }

    public static DateTime Stamp(this DbDataReader r, string column)
        => r.GetDateTime(r.GetOrdinal(column));

    public static DateTime? StampOrNull(this DbDataReader r, string column)
    {
        var i = r.GetOrdinal(column);
        return r.IsDBNull(i) ? null : r.GetDateTime(i);
    }
}
