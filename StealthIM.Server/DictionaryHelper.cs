using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StealthIM.Server;

public static class DictionaryHelper
{
    public static string StringDescriptor<TKey, TValue>(this Dictionary<TKey, TValue> dict)
    {
        StringBuilder sb = new();
        sb.Append('{');
        foreach (var item in dict)
        {
            sb.Append($"{item.Key}: {item.Value};");
        }
        sb.Append('}');
        return sb.ToString();
    }
}
