using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace DeathRoll.Data;

public static class Uploader
{
    private const string BaseUrl = "https://xzwnvwjxgmaqtrxewngh.supabase.co/rest/v1/";
    private const string SupabaseAnonKey = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6Inh6d252d2p4Z21hcXRyeGV3bmdoIiwicm9sZSI6ImFub24iLCJpYXQiOjE2ODk3NzcwMDIsImV4cCI6MjAwNTM1MzAwMn0.aNYTnhY_Sagi9DyH5Q9tCz9lwaRCYzMC12SZ7q7jZBc";
    private static readonly HttpClient Client = new();

    static Uploader()
    {
        Client.DefaultRequestHeaders.Add("apikey", SupabaseAnonKey);
        Client.DefaultRequestHeaders.Add("Authorization", $"Bearer {SupabaseAnonKey}");
        Client.DefaultRequestHeaders.Add("Prefer", "return=representation");
    }

    public class Upload
    {
        [JsonIgnore]
        public string Table;

        public Upload(string table)
        {
            Table = table;
        }
    }

    public static async Task<string> UploadNewEntry(Upload entry)
    {
        try
        {
            var content = new StringContent(JsonConvert.SerializeObject(entry), Encoding.UTF8, "application/json");
            var response = await Client.PostAsync($"{BaseUrl}{entry.Table}", content);

            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e, "Upload new entry failed");
            return string.Empty;
        }
    }

    public static async Task<string> GetEmptyCount()
    {
        try
        {
            var response = await Client.GetAsync($"{BaseUrl}TripleT?player2=eq.Empty&full=is.false&done=is.false&select=id");
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e, "Get rooms failed");
            return string.Empty;
        }
    }

    public static async Task<string> FindEmptyRoom()
    {
        try
        {
            var response = await Client.GetAsync($"{BaseUrl}TripleT?player2=eq.Empty&full=is.false&done=is.false&select=*");
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e, "Get rooms failed");
            return string.Empty;
        }
    }

    public static async Task<string> FindPrivateRoom(string identifier)
    {
        try
        {
            var response = await Client.GetAsync($"{BaseUrl}TripleT?room=eq.{identifier}&player2=eq.Private&full=is.false&done=is.false&select=*");
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e, "Find private room failed");
            return string.Empty;
        }
    }

    public static async Task<string> GetCurrentRoom(OnlineRoom room)
    {
        try
        {
            var response = await Client.GetAsync($"{BaseUrl}TripleT?id=eq.{room.ID}&select=*");
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e, "Get room failed");
            return string.Empty;
        }
    }

    public static async Task<string> GetMoves(OnlineRoom room, long lastMove)
    {
        try
        {
            var response = await Client.GetAsync($"{BaseUrl}TripleTMoves?room=eq.{room.Identifier}&id=gt.{lastMove}&select=*");
            return await response.Content.ReadAsStringAsync();
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e, "Get moves failed");
            return string.Empty;
        }
    }

    public static async Task UpdateRoom(OnlineRoom entry)
    {
        try
        {
            var content = new StringContent(JsonConvert.SerializeObject(entry), Encoding.UTF8, "application/json");
            await Client.PatchAsync($"{BaseUrl}{entry.Table}?id=eq.{entry.ID}", content);
        }
        catch (Exception e)
        {
            Plugin.Log.Error(e, "Update room failed");
        }
    }
}