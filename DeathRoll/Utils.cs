using System.Security.Cryptography;
using System.Text;

namespace DeathRoll;

public static class Utils
{
    public static string GenerateHashedName(string name)
    {
        var hash = SHA1.HashData(Encoding.UTF8.GetBytes(name));
        var sb = new StringBuilder(hash.Length * 2);

        foreach (var b in hash)
            sb.Append(b.ToString("X2"));

        return $"Player {sb.ToString()[..10]}";
    }
}

static class ListExtension
{
    public static T PopAt<T>(this List<T> list, int index)
    {
        var r = list[index];
        list.RemoveAt(index);

        return r;
    }
}