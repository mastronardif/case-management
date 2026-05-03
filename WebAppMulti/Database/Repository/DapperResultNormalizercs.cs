public static class DapperResultNormalizer
{
    public static object Normalize(IEnumerable<dynamic> rows)
    {
        var list = rows.ToList();

        return new
        {
            Count = list.Count,
            Rows = list
        };
    }
}
