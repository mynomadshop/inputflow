using System;
using System.Collections.Generic;
using System.Data.Common;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using InputFlow.Shared;
using Microsoft.Data.Sqlite;

namespace InputFlow.Core.Storage;

/// <summary>
/// SQLite-based store for aggregate buckets and daily summaries.
/// 
/// PRIVACY:
///   - Stores ONLY aggregate counts (presses, clicks, scrolls) and distances.
///   - No key names, VK codes, scan codes, character sequences, or timestamps per-event.
///   - No coordinate trails, replayable paths, window titles, clipboard data, etc.
/// 
/// STORAGE:
///   Uses Microsoft.Data.Sqlite with SQLCipher encryption via SQLitePCLRaw.bundle_e_sqlcipher.
///   Encryption key is derived from Windows DPAPI + app GUID for per-user protection.
/// </summary>
public class AggregateStore : IDisposable
{
    private readonly string _dbPath;
    private readonly string? _encryptionKey;
    private bool _disposed;

    // Retention policy defaults
    public const int DefaultRetentionDays30SecBuckets = 7;
    public const int DefaultRetentionMonthsDailySummaries = 12;

    public AggregateStore(string dbPath, string? encryptionKey = null)
    {
        _dbPath = dbPath;
        _encryptionKey = encryptionKey;

        // Ensure parent directory exists.
        Directory.CreateDirectory(Path.GetDirectoryName(_dbPath)!);

        InitializeDatabase();
    }

    /// <summary>Open or create the database schema.</summary>
    private void InitializeDatabase()
    {
        using var conn = OpenConnection();

        conn.Execute(@"
            CREATE TABLE IF NOT EXISTS buckets (
                bucket_id   TEXT PRIMARY KEY,
                start_utc   REAL NOT NULL,     -- Unix timestamp
                end_utc     REAL NOT NULL,
                active_duration_sec REAL NOT NULL DEFAULT 0,
                key_presses INTEGER NOT NULL DEFAULT 0,
                mouse_clicks INTEGER NOT NULL DEFAULT 0,
                scroll_events INTEGER NOT NULL DEFAULT 0,
                pixel_distance BIGINT NOT NULL DEFAULT 0,
                calibrated_cm REAL,
                distance_quality INTEGER NOT NULL DEFAULT 2
            );

            CREATE INDEX IF NOT EXISTS idx_buckets_start ON buckets(start_utc);
            CREATE INDEX IF NOT EXISTS idx_buckets_end ON buckets(end_utc);

            CREATE TABLE IF NOT EXISTS daily_summaries (
                date              TEXT PRIMARY KEY,   -- YYYY-MM-DD
                total_key_presses INTEGER NOT NULL DEFAULT 0,
                total_distance_cm REAL,
                distance_quality  INTEGER NOT NULL DEFAULT 2,
                source_display_ids TEXT
            );

            CREATE TABLE IF NOT EXISTS settings (
                key       TEXT PRIMARY KEY,
                value     TEXT NOT NULL
            );

            CREATE TABLE IF NOT EXISTS calibrations (
                pnp_device_id   TEXT PRIMARY KEY,
                monitor_name    TEXT NOT NULL,
                pixel_width     INTEGER NOT NULL,
                pixel_height    INTEGER NOT NULL,
                width_cm        REAL NOT NULL DEFAULT 0,
                height_cm       REAL NOT NULL DEFAULT 0,
                method          INTEGER NOT NULL DEFAULT 0,
                last_updated    REAL NOT NULL
            );
        ");

        // Save default settings.
        conn.Execute(@"INSERT OR IGNORE INTO settings (key, value) VALUES ('RetentionDays30Sec', ?)",
                     AggregateStore.DefaultRetentionDays30SecBuckets);
        conn.Execute(@"INSERT OR IGNORE INTO settings (key, value) VALUES ('RetentionMonthsDaily', ?)",
                     AggregateStore.DefaultRetentionMonthsDailySummaries);
    }

    private SqliteConnection OpenConnection()
    {
        var builder = new SqliteConnectionStringBuilder { DataSource = _dbPath };
        if (!string.IsNullOrEmpty(_encryptionKey))
        {
            // SQLCipher: PRAGMA key applied after opening.
            var conn = new SqliteConnection(builder.ToString());
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "PRAGMA key = ?";
            cmd.Parameters.AddWithValue("", _encryptionKey);
            cmd.ExecuteNonQuery();

            // Verify the key works by reading a table.
            try
            {
                using var verify = conn.CreateCommand();
                verify.CommandText = "SELECT count(*) FROM buckets";
                verify.ExecuteScalar();
            }
            catch
            {
                throw new InvalidOperationException("Database encryption key is invalid or data is corrupted.");
            }

            return conn;
        }

        var plainConn = new SqliteConnection(builder.ToString());
        plainConn.Open();
        return plainConn;
    }

    // --- Bucket operations ---

    public Task SaveBucketAsync(AggregateBucket bucket)
    {
        using var conn = OpenConnection();
        return conn.ExecuteAsync(@"
            INSERT OR REPLACE INTO buckets 
            (bucket_id, start_utc, end_utc, active_duration_sec, key_presses, mouse_clicks,
             scroll_events, pixel_distance, calibrated_cm, distance_quality)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?)",
            bucket.BucketId,
            bucket.StartUtc.ToUnixTimeSeconds(),
            bucket.EndUtc.ToUnixTimeSeconds(),
            bucket.ActiveDurationSec,
            bucket.KeyPresses,
            bucket.MouseClicks,
            bucket.ScrollEvents,
            bucket.PixelDistance,
            bucket.CalibratedCm,
            (int)bucket.DistanceQuality);
    }

    public async Task<IReadOnlyList<AggregateBucket>> GetBucketsAsync(DateTimeRange range)
    {
        var buckets = new List<AggregateBucket>();
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT bucket_id, start_utc, end_utc, active_duration_sec, key_presses,
                   mouse_clicks, scroll_events, pixel_distance, calibrated_cm, distance_quality
            FROM buckets 
            WHERE start_utc >= ? AND end_utc <= ?
            ORDER BY start_utc ASC";
        cmd.Parameters.AddWithValue("", range.Start.ToUnixTimeSeconds());
        cmd.Parameters.AddWithValue("", range.End.ToUnixTimeSeconds());

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            buckets.Add(new AggregateBucket(
                BucketId: reader.GetString(0),
                StartUtc: UnixTimestampToDateTime(reader.GetInt64(1)),
                EndUtc: UnixTimestampToDateTime(reader.GetInt64(2)),
                ActiveDurationSec: reader.GetDouble(3),
                KeyPresses: reader.GetInt32(4),
                MouseClicks: reader.GetInt32(5),
                ScrollEvents: reader.GetInt32(6),
                PixelDistance: reader.GetInt64(7),
                CalibratedCm: reader.IsDBNull(8) ? (double?)null : reader.GetDouble(8),
                DistanceQuality: (MeasurementQuality)reader.GetInt32(9)));
        }

        return buckets;
    }

    public async Task DeleteBucketsOlderThanDays(int days)
    {
        using var conn = OpenConnection();
        var cutoff = DateTime.UtcNow.AddDays(-days).ToUnixTimeSeconds();
        await conn.ExecuteAsync("DELETE FROM buckets WHERE end_utc < ?", cutoff);
    }

    // --- Daily summary operations ---

    public Task SaveDailySummaryAsync(DailySummary summary)
    {
        using var conn = OpenConnection();
        return conn.ExecuteAsync(@"
            INSERT OR REPLACE INTO daily_summaries (date, total_key_presses, total_distance_cm, 
                                                   distance_quality, source_display_ids)
            VALUES (?, ?, ?, ?, ?)",
            summary.Date.ToString("yyyy-MM-dd"),
            summary.TotalKeyPresses,
            summary.TotalDistanceCm,
            (int)summary.DistanceQuality,
            summary.SourceDisplayIds);
    }

    public async Task<IReadOnlyList<DailySummary>> GetSummariesAsync(DateOnly from, DateOnly to)
    {
        var summaries = new List<DailySummary>();
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT date, total_key_presses, total_distance_cm, distance_quality, source_display_ids
            FROM daily_summaries 
            WHERE date >= ? AND date <= ?
            ORDER BY date ASC";
        cmd.Parameters.AddWithValue("", from.ToString("yyyy-MM-dd"));
        cmd.Parameters.AddWithValue("", to.ToString("yyyy-MM-dd"));

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            summaries.Add(new DailySummary(
                Date: DateOnly.Parse(reader.GetString(0)),
                TotalKeyPresses: reader.GetInt32(1),
                TotalDistanceCm: reader.IsDBNull(2) ? (double?)null : reader.GetDouble(2),
                DistanceQuality: (MeasurementQuality)reader.GetInt32(3),
                SourceDisplayIds: reader.GetString(4)));
        }

        return summaries;
    }

    public async Task DeleteSummariesOlderThanMonths(int months)
    {
        using var conn = OpenConnection();
        var cutoff = DateOnly.FromDateTime(DateTime.UtcNow.AddMonths(-months)).ToString("yyyy-MM-dd");
        await conn.ExecuteAsync("DELETE FROM daily_summaries WHERE date < ?", cutoff);
    }

    // --- Calibration operations ---

    public Task SaveCalibrationAsync(DisplayCalibration cal)
    {
        using var conn = OpenConnection();
        return conn.ExecuteAsync(@"
            INSERT OR REPLACE INTO calibrations 
            (pnp_device_id, monitor_name, pixel_width, pixel_height, width_cm, height_cm, method, last_updated)
            VALUES (?, ?, ?, ?, ?, ?, ?, ?)",
            cal.PnpDeviceId, cal.MonitorName, cal.PixelWidth, cal.PixelHeight,
            cal.ActiveWidthCm, cal.ActiveHeightCm, (int)cal.Method, 
            cal.LastUpdated.ToUnixTimeSeconds());
    }

    public async Task<IReadOnlyList<DisplayCalibration>> GetAllCalibrationsAsync()
    {
        var results = new List<DisplayCalibration>();
        using var conn = OpenConnection();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT * FROM calibrations";

        using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            results.Add(new DisplayCalibration(
                MonitorName: reader.GetString(1),
                PnpDeviceId: reader.GetString(0),
                PixelWidth: reader.GetInt32(2),
                PixelHeight: reader.GetInt32(3),
                ActiveWidthCm: reader.GetDouble(4),
                ActiveHeightCm: reader.GetDouble(5),
                Method: (CalibrationMethod)reader.GetInt32(6),
                LastUpdated: UnixTimestampToDateTime(reader.GetInt64(7))));
        }

        return results;
    }

    // --- Export / Delete ---

    /// <summary>Export buckets as JSON to the given file path.</summary>
    public async Task ExportToJsonAsync(DateTimeRange range, string outputPath)
    {
        var buckets = await GetBucketsAsync(range);
        var json = JsonSerializer.Serialize(buckets, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        await File.WriteAllTextAsync(outputPath, json);
    }

    /// <summary>Export daily summaries as JSON.</summary>
    public async Task ExportSummariesToJsonAsync(DateOnly from, DateOnly to, string outputPath)
    {
        var summaries = await GetSummariesAsync(from, to);
        var json = JsonSerializer.Serialize(summaries, new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        await File.WriteAllTextAsync(outputPath, json);
    }

    /// <summary>Delete ALL app-managed data: buckets, summaries, settings, calibrations.</summary>
    public async Task DeleteAllAsync()
    {
        using var conn = OpenConnection();
        await conn.ExecuteAsync("DELETE FROM buckets");
        await conn.ExecuteAsync("DELETE FROM daily_summaries");
        await conn.ExecuteAsync("DELETE FROM calibrations");

        // Compact to free space.
        await conn.ExecuteAsync("VACUUM");
    }

    /// <summary>Compact the database file.</summary>
    public async Task VacuumAsync()
    {
        using var conn = OpenConnection();
        await conn.ExecuteAsync("VACUUM");
    }

    // --- Helper methods ---

    private static DateTime UnixTimestampToDateTime(long ts) =>
        DateTimeOffset.FromUnixTimeSeconds(ts).UtcDateTime;

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        // Connections auto-close when disposed by the `using` blocks.
        GC.SuppressFinalize(this);
    }
}

public readonly record struct DateTimeRange(DateTime Start, DateTime End);
