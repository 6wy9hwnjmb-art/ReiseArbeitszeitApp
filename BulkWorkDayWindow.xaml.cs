using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using ReiseArbeitszeitApp.Models;
using ReiseArbeitszeitApp.Services;

namespace ReiseArbeitszeitApp;

public partial class BulkWorkDayWindow : Window
{
    private static readonly CultureInfo GermanCulture = CultureInfo.GetCultureInfo("de-DE");

    private readonly DatabaseService _database;
    private readonly HolidayService _holidayService;
    private readonly AppSettings _settings;
    private readonly HolidayRegion _holidayRegion;
    private readonly IReadOnlyCollection<HolidayRule> _holidayRules;
    private List<BulkDayPreview> _preview = [];
    private bool _isInitialized;

    private sealed record DayTypeOption(WorkDayType Value, string Name)
    {
        public override string ToString() => Name;
    }

    private sealed class BulkDayPreview
    {
        public DateTime Date { get; init; }
        public string DateText { get; init; } = string.Empty;
        public string WeekdayText { get; init; } = string.Empty;
        public WorkDayType DayType { get; init; }
        public string DayTypeText { get; init; } = string.Empty;
        public string StatusText { get; init; } = string.Empty;
        public string? HolidayName { get; init; }
        public bool IsSaveable { get; init; }
    }

    public BulkWorkDayWindow(
        DatabaseService database,
        HolidayService holidayService,
        AppSettings settings)
    {
        _database = database;
        _holidayService = holidayService;
        _settings = settings;
        _holidayRegion = holidayService.GetRegion(settings);
        _holidayRules = database.GetHolidayRules();

        InitializeComponent();

        DayTypeCombo.ItemsSource = new[]
        {
            new DayTypeOption(WorkDayType.Vacation, WorkDayTypeInfo.GetDisplayName(WorkDayType.Vacation)),
            new DayTypeOption(WorkDayType.Sick, WorkDayTypeInfo.GetDisplayName(WorkDayType.Sick)),
            new DayTypeOption(WorkDayType.HomeOffice, WorkDayTypeInfo.GetDisplayName(WorkDayType.HomeOffice)),
            new DayTypeOption(WorkDayType.Holiday, WorkDayTypeInfo.GetDisplayName(WorkDayType.Holiday))
        };
        DayTypeCombo.DisplayMemberPath = nameof(DayTypeOption.Name);
        DayTypeCombo.SelectedValuePath = nameof(DayTypeOption.Value);

        StartDatePicker.SelectedDate = DateTime.Today;
        EndDatePicker.SelectedDate = DateTime.Today;
        DayTypeCombo.SelectedValue = WorkDayType.Vacation;
        var regionNotice = _holidayService.GetRegionNotice(_settings);
        HolidayRegionText.Text =
            $"Feiertage werden für {_holidayRegion.ShortDisplayName} berücksichtigt." +
            (string.IsNullOrWhiteSpace(regionNotice) ? string.Empty : $" {regionNotice}");

        _isInitialized = true;
        RefreshPreview();
    }

    public int SavedCount { get; private set; }

    private void PreviewInput_Changed(object? sender, EventArgs e)
    {
        if (_isInitialized)
            RefreshPreview();
    }

    private void RefreshPreview()
    {
        if (StartDatePicker.SelectedDate is not DateTime start
            || EndDatePicker.SelectedDate is not DateTime end)
        {
            SetInvalidPreview("Bitte Start- und Enddatum auswählen.");
            return;
        }

        start = start.Date;
        end = end.Date;

        if (end < start)
        {
            SetInvalidPreview("Das Enddatum liegt vor dem Startdatum.");
            return;
        }

        var totalDays = (end - start).Days + 1;
        if (totalDays > 732)
        {
            SetInvalidPreview("Der Zeitraum darf höchstens zwei Jahre umfassen.");
            return;
        }

        var dayType = GetSelectedDayType();
        var existingDates = _database.GetWorkDays()
            .Select(day => day.Date.Date)
            .ToHashSet();
        var rows = new List<BulkDayPreview>(totalDays);

        for (var date = start; date <= end; date = date.AddDays(1))
            rows.Add(CreatePreviewRow(date, dayType, existingDates));

        _preview = rows;
        PreviewGrid.ItemsSource = _preview;

        var saveable = rows.Count(row => row.IsSaveable);
        var skipped = rows.Count - saveable;
        SaveableCountText.Text = saveable == 1
            ? "1 Tag speicherbar"
            : $"{saveable} Tage speicherbar";
        SaveButton.Content = saveable == 1
            ? "1 Tag speichern"
            : $"{saveable} Tage speichern";
        PreviewSummaryText.Text = skipped == 0
            ? $"{rows.Count} Tage geprüft."
            : $"{rows.Count} Tage geprüft · {skipped} werden übersprungen.";
        SaveButton.IsEnabled = saveable > 0;
    }

    private BulkDayPreview CreatePreviewRow(
        DateTime date,
        WorkDayType dayType,
        HashSet<DateTime> existingDates)
    {
        var holidayName = _holidayService.GetHolidayName(
            date,
            _settings,
            _holidayRules);
        var isWeekend = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday;
        var isSaveable = true;
        var status = "Wird gespeichert";

        if (existingDates.Contains(date.Date))
        {
            isSaveable = false;
            status = "Bereits erfasst";
        }
        else if (SkipWeekendsCheck.IsChecked == true && isWeekend)
        {
            isSaveable = false;
            status = "Wochenende";
        }
        else if (dayType == WorkDayType.Holiday && holidayName is null)
        {
            isSaveable = false;
            status = "Kein gesetzlicher Feiertag";
        }
        else if (dayType != WorkDayType.Holiday && holidayName is not null)
        {
            isSaveable = false;
            status = $"Feiertag: {holidayName}";
        }
        else if (holidayName is not null)
        {
            status = $"Wird gespeichert · {holidayName}";
        }

        return new BulkDayPreview
        {
            Date = date,
            DateText = date.ToString("dd.MM.yyyy", GermanCulture),
            WeekdayText = date.ToString("dddd", GermanCulture),
            DayType = dayType,
            DayTypeText = WorkDayTypeInfo.GetDisplayName(dayType),
            StatusText = status,
            HolidayName = holidayName,
            IsSaveable = isSaveable
        };
    }

    private void SetInvalidPreview(string message)
    {
        _preview = [];
        PreviewGrid.ItemsSource = _preview;
        PreviewSummaryText.Text = message;
        SaveableCountText.Text = "0 Tage speicherbar";
        SaveButton.Content = "Tage speichern";
        SaveButton.IsEnabled = false;
    }

    private WorkDayType GetSelectedDayType()
    {
        if (DayTypeCombo.SelectedValue is WorkDayType selectedValue)
            return selectedValue;
        if (DayTypeCombo.SelectedItem is DayTypeOption option)
            return option.Value;
        return WorkDayType.Vacation;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            RefreshPreview();
            var rows = _preview.Where(row => row.IsSaveable).ToList();
            if (rows.Count == 0)
                return;

            var note = NoteBox.Text.Trim();
            var days = rows.Select(row => CreateWorkDay(row, note)).ToList();
            _database.SaveWorkDays(days);

            SavedCount = days.Count;
            DialogResult = true;
            Close();
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                ex.Message,
                "Zeitraum konnte nicht gespeichert werden",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }
    }

    private WorkDay CreateWorkDay(BulkDayPreview row, string commonNote)
    {
        var isAbsence = row.DayType is WorkDayType.Vacation or WorkDayType.Sick or WorkDayType.Holiday;
        var note = commonNote;
        if (row.DayType == WorkDayType.Holiday
            && row.HolidayName is not null
            && string.IsNullOrWhiteSpace(note))
        {
            note = row.HolidayName;
        }

        return new WorkDay
        {
            Date = row.Date,
            DayType = row.DayType,
            StartTime = isAbsence ? TimeSpan.Zero : ParseTime(_settings.DefaultWorkStart, "Standard-Arbeitsbeginn"),
            EndTime = isAbsence ? TimeSpan.Zero : ParseTime(_settings.DefaultWorkEnd, "Standard-Arbeitsende"),
            BreakTime = isAbsence ? TimeSpan.Zero : ParseTime(_settings.DefaultBreak, "Standard-Pause"),
            TargetTime = ParseTime(_settings.DefaultTarget, "Standard-Sollzeit"),
            CountryCode = row.DayType == WorkDayType.HomeOffice
                ? (_holidayRegion.Country == HolidayCountry.Switzerland ? "CH" : "DE")
                : string.Empty,
            Location = row.DayType == WorkDayType.HomeOffice ? "Homeoffice" : string.Empty,
            Note = note,
            IsTravelDay = false,
            TravelWorkTime = TimeSpan.Zero
        };
    }

    private static TimeSpan ParseTime(string text, string fieldName)
    {
        text = text.Trim();
        var parts = text.Split(':');

        if (parts.Length == 2
            && int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out var hours)
            && int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out var minutes)
            && hours >= 0
            && minutes is >= 0 and < 60)
        {
            return TimeSpan.FromHours(hours) + TimeSpan.FromMinutes(minutes);
        }

        throw new InvalidOperationException(
            $"Ungültige Zeitangabe bei '{fieldName}'. Bitte die Standardzeiten in den Einstellungen prüfen.");
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
