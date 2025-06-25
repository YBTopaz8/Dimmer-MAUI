using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerSearch;
public static class SemanticQueryHelpers
{
    // --- Helper methods to get property values using Reflection ---
    private static object GetPropValue(object src, string propName)
    {
        if (src == null)
            return null;
        if (propName.Contains('.'))
        {
            var temp = propName.Split(new char[] { '.' }, 2);
            return GetPropValue(GetPropValue(src, temp[0]), temp[1]);
        }
        else
        {
            var prop = src.GetType().GetProperty(propName);
            return prop?.GetValue(src, null);
        }
    }

    public static string GetStringProp(SongModelView song, string name) => GetPropValue(song, name) as string ?? "";
    public static double GetNumericProp(SongModelView song, string name) => Convert.ToDouble(GetPropValue(song, name) ?? 0);
    public static bool GetBoolProp(SongModelView song, string name) => Convert.ToBoolean(GetPropValue(song, name) ?? false);

    // Levenshtein Distance implementation for fuzzy search
    public static int LevenshteinDistance(string s, string t)
    {
        if (string.IsNullOrEmpty(s))
            return string.IsNullOrEmpty(t) ? 0 : t.Length;
        if (string.IsNullOrEmpty(t))
            return s.Length;

        int n = s.Length;
        int m = t.Length;
        int[,] d = new int[n + 1, m + 1];

        for (int i = 0; i <= n; d[i, 0] = i++)
            ;
        for (int j = 0; j <= m; d[0, j] = j++)
            ;

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1),
                    d[i - 1, j - 1] + cost);
            }
        }
        return d[n, m];
    }
}