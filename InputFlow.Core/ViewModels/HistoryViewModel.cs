using System;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using InputFlow.Core.Storage;
using InputFlow.Shared;

namespace InputFlow.Core.ViewModels;

/// <summary>View model for the History page — date-filtered summaries.</summary>
public partial class HistoryViewModel : ObservableObject
{
    private readonly AggregateStore _store;

    [ObservableProperty] private DateOnly? _startDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-7));
    [ObservableProperty] private DateOnly? _endDate;  // defaults to today
    [ObservableProperty] private ObservableCollection<DailySummaryRow> _summaries = new();

    public HistoryViewModel(AggregateStore store)
    {
        _store = store;
        EndDate = DateOnly.FromDateTime(DateTime.UtcNow);
        LoadSummaries();
    }

    partial void OnStartDateChanged(DateOnly? value) => LoadSummaries();
    partial void OnEndDateChanged(DateOnly? value) => LoadSummaries();

    private async void LoadSummaries()
    {
        if (StartDate == null || EndDate == null) return;

        var summaries = await _store.GetSummariesAsync(StartDate.Value, EndDate.Value);
        
        Summaries.Clear();
        foreach (var s in summaries.OrderByDescending(x => x.Date))
        {
            Summaries.Add(new DailySummaryRow(s));
        }

        OnPropertyChanged(nameof(Summaries));
    }

    /// <summary>Row for the history data grid.</summary>
    public record DailySummaryRow(DailySummary Source)
    {
        public DateOnly Date => Source.Date;
        public int KeyPresses => Source.TotalKeyPresses;
        
        public string DistanceDisplay => Source.TotalDistanceCm switch
        {
            null or 0 => "N/A",
            < 100 => $"{Source.TotalDistanceCm:F1} cm",
            < 10000 => $"{Source.TotalDistanceCm / 100:F2} m",
            _ => $"{Source.TotalDistanceCm / 100000:F3} km"
        };

        public string QualityLabel => Source.DistanceQuality switch
        {
            MeasurementQuality.Calibrated => "Calibrated",
            MeasurementQuality.Estimated => "Estimated",
            MeasurementQuality.PixelsOnly => "Pixels Only",
            _ => "N/A"
        };
    }
}
