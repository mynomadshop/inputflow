using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using InputFlow.Shared;
using Microsoft.Data.Sqlite;

namespace InputFlow.Core.Storage;

/// <summary>Persistent storage for AppSettings.</summary>
public class SettingsStore : IDisposable
{
    private readonly string _dbPath;
    private bool _disposed;

    public static readonly AppSettings DefaultSettings = new(
        DistanceUnit: CursorDisplayUnit.Auto,
        KeyboardTrackingEnabled: false,
        PointerTrackingEnabled: false,
        Theme: "dark",
        AnimationsEnabled: true,
        StartWithWindows: false,
        CloseBehavior: CloseBehavior.QuitImmediately,
        RetentionDays30SecBuckets: 7,
        RetentionMonthsDailySummaries: 12,
        ConsentLevel: PrivacyConsentLevel.NotGiven,
        LastConsentUpdate: null);

    public SettingsStore(string dbPath) => _dbPath = dbPath;

    public AppSettings Load()
    {
        using var conn = OpenConnection();
        var dict = new Dictionary<string, string>();

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = "SELECT key, value FROM settings";
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
                dict[reader.GetString(0)] = reader.GetString(1);
        }

        return new AppSettings(
            DistanceUnit: ParseEnum<CursorDisplayUnit>(dict.GetValueOrDefault("DistanceUnit"), CursorDisplayUnit.Auto),
            KeyboardTrackingEnabled: bool.Parse(dict.GetValueOrDefault("KeyboardTracking") ?? "false"),
            PointerTrackingEnabled: bool.Parse(dict.GetValueOrDefault("PointerTracking") ?? "false"),
            Theme: dict.GetValueOrDefault("Theme") ?? "dark",
            AnimationsEnabled: bool.Parse(dict.GetValueOrDefault("Animations") ?? "true"),
            StartWithWindows: bool.Parse(dict.GetValueOrDefault("StartWithWindows") ?? "false"),
            CloseBehavior: ParseEnum<CloseBehavior>(dict.GetValueOrDefault("CloseBehavior"), CloseBehavior.QuitImmediately),
            RetentionDays30SecBuckets: int.Parse(dict.GetValueOrDefault("RetentionDays30Sec") ?? "7"),
            RetentionMonthsDailySummaries: int.Parse(dict.GetValueOrDefault("RetentionMonthsDaily") ?? "12"),
            ConsentLevel: ParseEnum<PrivacyConsentLevel>(dict.GetValueOrDefault("ConsentLevel"), PrivacyConsentLevel.NotGiven),
            LastConsentUpdate: dict.ContainsKey("LastConsentUpdate") 
                ? DateTimeOffset.TryParse(dict["LastConsentUpdate"], out var dt) ? dt : (DateTimeOffset?)null 
                : null);
    }

    public async Task SaveAsync(AppSettings settings)
    {
        using var conn = OpenConnection();
        await conn.ExecuteAsync(@"
            INSERT OR REPLACE INTO settings (key, value) VALUES 
            ('DistanceUnit', ?), ('KeyboardTracking', ?), ('PointerTracking', ?),
            ('Theme', ?), ('Animations', ?), ('StartWithWindows', ?),
            ('CloseBehavior', ?), ('RetentionDays30Sec', ?), ('RetentionMonthsDaily', ?),
            ('ConsentLevel', ?), ('LastConsentUpdate', ?)",
            settings.DistanceUnit.ToString(),
            settings.KeyboardTrackingEnabled,
            settings.PointerTrackingEnabled,
            settings.Theme,
            settings.AnimationsEnabled,
            settings.StartWithWindows,
            settings.CloseBehavior.ToString(),
            settings.RetentionDays30SecBuckets,
            settings.RetentionMonthsDailySummaries,
            settings.ConsentLevel.ToString(),
            settings.LastConsentUpdate?.ToUnixTimeSeconds() ?? (object)DBNull.Value);
    }

    private static SqliteConnection OpenConnection()
    {
        var conn = new SqliteConnection($"Data Source={_dbPath}");
        conn.Open();
        return conn;
    }

    private static T ParseEnum<T>(string value, T defaultValue) where T : struct =>
        Enum.TryParse(value, out T result) ? result : defaultValue;

    public void Dispose() { _disposed = true; GC.SuppressFinalize(this); }
}
