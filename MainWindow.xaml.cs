using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using ReiseArbeitszeitApp.Helpers;
using ReiseArbeitszeitApp.Models;
using ReiseArbeitszeitApp.Services;

namespace ReiseArbeitszeitApp;

public partial class MainWindow : Window
{
    private static readonly Version CurrentAppVersion = typeof(MainWindow).Assembly.GetName().Version ?? new Version(0, 1, 0);
    private static readonly string AppVersion = CurrentAppVersion.ToString(3);

    private static readonly Brush PositiveOvertimeBrush = new SolidColorBrush(Color.FromRgb(52, 199, 89));
    private static readonly Brush NegativeOvertimeBrush = new SolidColorBrush(Color.FromRgb(255, 69, 58));
    private static readonly Brush NeutralOvertimeBrush = new SolidColorBrush(Color.FromRgb(166, 175, 190));
    private static readonly Brush CalendarWorkBrush = new SolidColorBrush(Color.FromRgb(24, 43, 65));
    private static readonly Brush CalendarHomeOfficeBrush = new SolidColorBrush(Color.FromRgb(24, 53, 43));
    private static readonly Brush CalendarVacationBrush = new SolidColorBrush(Color.FromRgb(59, 45, 24));
    private static readonly Brush CalendarSickBrush = new SolidColorBrush(Color.FromRgb(58, 32, 37));
    private static readonly Brush CalendarHolidayBrush = new SolidColorBrush(Color.FromRgb(44, 36, 64));
    private static readonly Brush CalendarTravelBrush = new SolidColorBrush(Color.FromRgb(23, 52, 58));
    private static readonly Brush CalendarWorkAccentBrush = new SolidColorBrush(Color.FromRgb(79, 163, 255));
    private static readonly Brush CalendarHomeOfficeAccentBrush = new SolidColorBrush(Color.FromRgb(85, 201, 138));
    private static readonly Brush CalendarVacationAccentBrush = new SolidColorBrush(Color.FromRgb(240, 181, 90));
    private static readonly Brush CalendarSickAccentBrush = new SolidColorBrush(Color.FromRgb(255, 107, 118));
    private static readonly Brush CalendarHolidayAccentBrush = new SolidColorBrush(Color.FromRgb(167, 139, 250));
    private static readonly Brush CalendarTravelAccentBrush = new SolidColorBrush(Color.FromRgb(94, 208, 223));
    private static readonly Brush CalendarEmptyBrush = new SolidColorBrush(Color.FromRgb(18, 22, 32));
    private static readonly Brush CalendarWeekendBrush = new SolidColorBrush(Color.FromRgb(21, 25, 35));
    private static readonly Brush CalendarDefaultBorderBrush = new SolidColorBrush(Color.FromRgb(48, 55, 71));
    private static readonly Brush CalendarTodayBorderBrush = new SolidColorBrush(Color.FromRgb(10, 132, 255));

    private DatabaseService _database = new();
    private readonly TimeCalculationService _timeCalculation = new();
    private readonly CsvExportService _csvExport = new();
    private readonly TimeZoneLookupService _timeZoneLookup = new();
    private readonly SettingsService _settingsService = new();
    private readonly WorkLocationReportService _workLocationReportService = new();
    private readonly DatabaseBackupService _backupService = new();
    private readonly ValidationService _validation = new();
    private readonly UpdateService _updateService = new();
    private readonly HolidayService _holidayService = new();
    private AppSettings _settings = new();
    private TripEntry? _lastTripCalculation;
    private int _editingTripId;
    private int _editingWorkDayId;
    private bool _isUpdatingWorkDayForm;
    private DateTime _workCalendarMonth = new(DateTime.Today.Year, DateTime.Today.Month, 1);

    private sealed record MonthOption(int Number, string Name)
    {
        public override string ToString() => Name;
    }

    private sealed record WorkDayTypeOption(WorkDayType Value, string Name)
    {
        public override string ToString() => Name;
    }

    private sealed class WorkCalendarDay
    {
        public DateTime Date { get; init; }
        public string DayNumber { get; init; } = string.Empty;
        public string TypeLabel { get; init; } = string.Empty;
        public string DetailLabel { get; init; } = string.Empty;
        public string ToolTip { get; init; } = string.Empty;
        public Brush Background { get; init; } = CalendarEmptyBrush;
        public Brush Accent { get; init; } = Brushes.Transparent;
        public Brush Border { get; init; } = CalendarDefaultBorderBrush;
        public Thickness BorderThickness { get; init; } = new(1);
        public double Opacity { get; init; } = 1;
    }

    public MainWindow()
    {
        InitializeComponent();
        InitializeDefaults();
        LoadLists();
        RefreshReport();
        Loaded += MainWindow_Loaded;
    }

    private void InitializeDefaults()
    {
        _settings = _settingsService.Load();

        Title = $"Reise- & Arbeitszeitrechner v{AppVersion}";
        AppVersionText.Text = $"Version {AppVersion}";

        WorkDayTypeCombo.ItemsSource = Enum.GetValues<WorkDayType>()
            .Select(type => new WorkDayTypeOption(type, WorkDayTypeInfo.GetDisplayName(type)))
            .ToList();
        WorkDayTypeCombo.DisplayMemberPath = nameof(WorkDayTypeOption.Name);
        WorkDayTypeCombo.SelectedValuePath = nameof(WorkDayTypeOption.Value);
        WorkDayTypeCombo.SelectedValue = WorkDayType.Work;

        WorkCountryCombo.ItemsSource = CountryCatalog.GetAll();
        WorkCountryCombo.DisplayMemberPath = nameof(CountryOption.DisplayName);
        WorkCountryCombo.SelectedValuePath = nameof(CountryOption.Code);

        SettingsHolidayRegionCombo.ItemsSource = HolidayRegionCatalog.GetAll();
        SettingsHolidayRegionCombo.DisplayMemberPath = nameof(HolidayRegion.DisplayName);
        SettingsHolidayRegionCombo.SelectedValuePath = nameof(HolidayRegion.Code);

        DepartureDatePicker.SelectedDate = DateTime.Today;
        ArrivalDatePicker.SelectedDate = DateTime.Today;
        WorkDatePicker.SelectedDate = DateTime.Today;
        ReportYearBox.Text = DateTime.Today.Year.ToString(CultureInfo.InvariantCulture);
        ReportMonthCombo.ItemsSource = Enumerable.Range(1, 12)
            .Select(month => new MonthOption(month, CultureInfo.GetCultureInfo("de-DE").DateTimeFormat.GetMonthName(month)))
            .ToList();
        ReportMonthCombo.DisplayMemberPath = nameof(MonthOption.Name);
        ReportMonthCombo.SelectedValuePath = nameof(MonthOption.Number);
        ReportMonthCombo.SelectedValue = DateTime.Today.Month;

        var zones = TimeZoneInfo.GetSystemTimeZones().OrderBy(x => x.DisplayName).ToList();
        DepartureTimeZoneCombo.ItemsSource = zones;
        ArrivalTimeZoneCombo.ItemsSource = zones;
        DepartureTimeZoneCombo.DisplayMemberPath = "DisplayName";
        ArrivalTimeZoneCombo.DisplayMemberPath = "DisplayName";
        DepartureTimeZoneCombo.SelectedItem = zones.FirstOrDefault(x => x.Id == TimeZoneInfo.Local.Id) ?? zones.FirstOrDefault();
        LoadSettingsIntoForm();
        ApplySettingsDefaultsToTrip();
        ApplySettingsDefaultsToWorkDay();
        UpdateHolidayHint(selectHoliday: true);
        ApplyDepartureTimeZoneFromLocation(showStatus: false);
        ApplyArrivalTimeZoneFromLocation(showStatus: false);

        StatusText.Text = _database.LastMigrationBackupPath is null
            ? $"Datenbank bereit: Schema v{_database.SchemaVersion}"
            : $"Datenbank auf Schema v{_database.SchemaVersion} aktualisiert und gesichert.";
        SettingsInfoText.Text = BuildSettingsInfo();
        RefreshHolidayRulesSummary();
        RefreshBackupList();
    }

    private void LoadLists()
    {
        var workDays = _database.GetWorkDays();
        WorkDaysGrid.ItemsSource = workDays.Take(100).ToList();
        TripsGrid.ItemsSource = _database.GetTrips().Take(100).ToList();
        RefreshWorkLocationOptions();
        RefreshWorkCalendar(workDays);
        UpdateWorkDashboard();
    }

    private void DepartureLocationBox_LostFocus(object sender, RoutedEventArgs e)
    {
        ApplyDepartureTimeZoneFromLocation(showStatus: true);
    }

    private void ArrivalLocationBox_LostFocus(object sender, RoutedEventArgs e)
    {
        ApplyArrivalTimeZoneFromLocation(showStatus: true);
    }

    private bool ApplyDepartureTimeZoneFromLocation(bool showStatus)
    {
        var zone = _timeZoneLookup.FindByLocation(DepartureLocationBox.Text);
        if (zone is null)
        {
            DepartureTimeZoneHintText.Text = "Zeitzone konnte nicht automatisch erkannt werden.";
            return false;
        }

        DepartureTimeZoneCombo.SelectedItem = DepartureTimeZoneCombo.Items
            .OfType<TimeZoneInfo>()
            .FirstOrDefault(x => x.Id == zone.Id);

        DepartureTimeZoneHintText.Text = $"Automatisch erkannt: {zone.DisplayName}";

        if (showStatus)
            StatusText.Text = $"Start-Zeitzone automatisch erkannt: {zone.DisplayName}";

        return true;
    }

    private bool ApplyArrivalTimeZoneFromLocation(bool showStatus)
    {
        var zone = _timeZoneLookup.FindByLocation(ArrivalLocationBox.Text);
        if (zone is null)
        {
            ArrivalTimeZoneHintText.Text = "Zeitzone konnte nicht automatisch erkannt werden.";
            return false;
        }

        ArrivalTimeZoneCombo.SelectedItem = ArrivalTimeZoneCombo.Items
            .OfType<TimeZoneInfo>()
            .FirstOrDefault(x => x.Id == zone.Id);

        ArrivalTimeZoneHintText.Text = $"Automatisch erkannt: {zone.DisplayName}";

        if (showStatus)
            StatusText.Text = $"Ziel-Zeitzone automatisch erkannt: {zone.DisplayName}";

        return true;
    }

    private void CalculateTrip_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _lastTripCalculation = ReadTripFromInput();
            if (!ConfirmWarnings("Reise prüfen", _validation.GetTripWarnings(_lastTripCalculation)))
                return;

            ShowTripResult(_lastTripCalculation);
            StatusText.Text = "Reisezeit berechnet. Noch nicht gespeichert.";
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void SaveTrip_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _lastTripCalculation = ReadTripFromInput();
            if (!ConfirmWarnings("Reise speichern", _validation.GetTripWarnings(_lastTripCalculation)))
                return;

            var wasEditing = _editingTripId > 0;
            _database.SaveTrip(_lastTripCalculation);
            ShowTripResult(_lastTripCalculation);
            LoadLists();
            RefreshReport();
            ResetTripEditState();
            StatusText.Text = wasEditing ? "Reise aktualisiert." : "Reise gespeichert.";
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void NewTrip_Click(object sender, RoutedEventArgs e)
    {
        ResetTripForm();
        StatusText.Text = "Neue Reise kann erfasst werden.";
    }

    private void EditSelectedTrip_Click(object sender, RoutedEventArgs e)
    {
        if (TripsGrid.SelectedItem is not TripEntry trip)
        {
            ShowError("Bitte zuerst eine Reise in der Tabelle auswählen.");
            return;
        }

        LoadTripIntoForm(trip);
        StatusText.Text = "Reise zum Bearbeiten geladen.";
    }

    private void TripsGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (TripsGrid.SelectedItem is TripEntry trip)
        {
            LoadTripIntoForm(trip);
            StatusText.Text = "Reise per Doppelklick geladen.";
        }
    }

    private void DeleteSelectedTrip_Click(object sender, RoutedEventArgs e)
    {
        if (TripsGrid.SelectedItem is not TripEntry trip)
        {
            ShowError("Bitte zuerst eine Reise in der Tabelle auswählen.");
            return;
        }

        if (MessageBox.Show($"Reise von {trip.DepartureLocation} nach {trip.ArrivalLocation} löschen?",
                "Reise löschen", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        _database.DeleteTrip(trip.Id);
        LoadLists();
        RefreshReport();
        if (_editingTripId == trip.Id)
            ResetTripForm();
        StatusText.Text = "Reise gelöscht.";
    }

    private void UseTripAsWorkDay_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var trip = TripsGrid.SelectedItem as TripEntry ?? _lastTripCalculation ?? ReadTripFromInput();
            FillWorkDayFromTrip(trip);
            MainTabs.SelectedIndex = 1;
            StatusText.Text = "Reisezeit wurde als Arbeitstag vorbereitet. Bitte prüfen und speichern.";
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private TripEntry ReadTripFromInput()
    {
        if (DepartureDatePicker.SelectedDate is null)
            throw new InvalidOperationException("Bitte ein Abflugdatum auswählen.");
        if (ArrivalDatePicker.SelectedDate is null)
            throw new InvalidOperationException("Bitte ein Ankunftsdatum auswählen.");

        ApplyDepartureTimeZoneFromLocation(showStatus: false);
        ApplyArrivalTimeZoneFromLocation(showStatus: false);

        if (DepartureTimeZoneCombo.SelectedItem is not TimeZoneInfo departureZone)
            throw new InvalidOperationException("Bitte eine Start-Zeitzone auswählen.");
        if (ArrivalTimeZoneCombo.SelectedItem is not TimeZoneInfo arrivalZone)
            throw new InvalidOperationException("Bitte eine Ziel-Zeitzone auswählen.");

        var departureTime = ParseTime(DepartureTimeBox.Text, "Abflugzeit");
        var arrivalTime = ParseTime(ArrivalTimeBox.Text, "Ankunftszeit");

        var departureDateTime = DepartureDatePicker.SelectedDate.Value.Date + departureTime;
        var arrivalDateTime = ArrivalDatePicker.SelectedDate.Value.Date + arrivalTime;

        var trip = _timeCalculation.CalculateTrip(
            DepartureLocationBox.Text,
            ArrivalLocationBox.Text,
            departureDateTime,
            arrivalDateTime,
            departureZone.Id,
            arrivalZone.Id,
            TripNoteBox.Text);
        trip.Id = _editingTripId;

        if (trip.TravelTime < TimeSpan.Zero)
            throw new InvalidOperationException("Die berechnete Reisezeit ist negativ. Prüfe Datum, Uhrzeit und Zeitzonen.");

        return trip;
    }

    private void ShowTripResult(TripEntry trip)
    {
        var departureZone = TimeZoneInfo.FindSystemTimeZoneById(trip.DepartureTimeZoneId);
        var arrivalZone = TimeZoneInfo.FindSystemTimeZoneById(trip.ArrivalTimeZoneId);
        var departureUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(trip.DepartureLocalDateTime, DateTimeKind.Unspecified), departureZone);
        var arrivalUtc = TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(trip.ArrivalLocalDateTime, DateTimeKind.Unspecified), arrivalZone);

        TripResultText.Text =
            $"Reisezeit: {TimeFormatter.Format(trip.TravelTime)}\n" +
            $"Zeitverschiebung Ziel gegenüber Start: {TimeFormatter.FormatSigned(trip.TimeDifference)}\n" +
            $"Von {trip.DepartureLocation} nach {trip.ArrivalLocation}";

        TripUtcText.Text =
            $"Abflug UTC: {departureUtc:dd.MM.yyyy HH:mm}\n" +
            $"Ankunft UTC: {arrivalUtc:dd.MM.yyyy HH:mm}";
    }

    private void LoadTripIntoForm(TripEntry trip)
    {
        _editingTripId = trip.Id;
        _lastTripCalculation = trip;

        DepartureLocationBox.Text = trip.DepartureLocation;
        ArrivalLocationBox.Text = trip.ArrivalLocation;
        DepartureDatePicker.SelectedDate = trip.DepartureLocalDateTime.Date;
        ArrivalDatePicker.SelectedDate = trip.ArrivalLocalDateTime.Date;
        DepartureTimeBox.Text = FormatClockInput(trip.DepartureLocalDateTime.TimeOfDay);
        ArrivalTimeBox.Text = FormatClockInput(trip.ArrivalLocalDateTime.TimeOfDay);
        TripNoteBox.Text = trip.Note;
        DepartureTimeZoneCombo.SelectedItem = FindComboTimeZone(DepartureTimeZoneCombo, trip.DepartureTimeZoneId);
        ArrivalTimeZoneCombo.SelectedItem = FindComboTimeZone(ArrivalTimeZoneCombo, trip.ArrivalTimeZoneId);
        DepartureTimeZoneHintText.Text = "Zeitzone aus gespeicherter Reise geladen.";
        ArrivalTimeZoneHintText.Text = "Zeitzone aus gespeicherter Reise geladen.";
        TripFormTitleText.Text = "Reise bearbeiten";
        SaveTripButton.Content = "Änderungen speichern";
        ShowTripResult(trip);
    }

    private void ResetTripForm()
    {
        _editingTripId = 0;
        _lastTripCalculation = null;
        DepartureLocationBox.Text = _settings.DefaultDepartureLocation;
        ArrivalLocationBox.Text = "New York";
        DepartureDatePicker.SelectedDate = DateTime.Today;
        ArrivalDatePicker.SelectedDate = DateTime.Today;
        DepartureTimeBox.Text = "14:30";
        ArrivalTimeBox.Text = "18:00";
        TripNoteBox.Text = string.Empty;
        TripFormTitleText.Text = "Reise erfassen";
        SaveTripButton.Content = "Berechnen & speichern";
        ApplyDepartureTimeZoneFromLocation(showStatus: false);
        ApplyArrivalTimeZoneFromLocation(showStatus: false);
        TripResultText.Text = "Noch keine Berechnung durchgeführt.";
        TripUtcText.Text = string.Empty;
    }

    private void ResetTripEditState()
    {
        _editingTripId = 0;
        TripFormTitleText.Text = "Reise erfassen";
        SaveTripButton.Content = "Berechnen & speichern";
    }

    private void FillWorkDayFromTrip(TripEntry trip)
    {
        _isUpdatingWorkDayForm = true;
        _editingWorkDayId = 0;
        WorkDatePicker.SelectedDate = trip.DepartureLocalDateTime.Date;
        WorkDayTypeCombo.SelectedValue = WorkDayType.Work;
        WorkStartBox.Text = "00:00";
        WorkEndBox.Text = "00:00";
        BreakBox.Text = "00:00";
        TargetBox.Text = _settings.DefaultTarget;
        IsTravelDayCheck.IsChecked = true;
        TravelWorkTimeBox.Text = FormatDurationInput(trip.TravelTime);
        if (string.IsNullOrWhiteSpace(GetSelectedWorkCountryCode()))
            WorkCountryCombo.SelectedValue = GetDefaultWorkCountryCode();
        WorkLocationBox.Text = $"{trip.DepartureLocation} -> {trip.ArrivalLocation}";
        WorkNoteBox.Text = string.IsNullOrWhiteSpace(trip.Note)
            ? $"Reisezeit übernommen: {TimeFormatter.Format(trip.TravelTime)}"
            : $"Reisezeit übernommen: {TimeFormatter.Format(trip.TravelTime)}\n{trip.Note}";
        _isUpdatingWorkDayForm = false;
        ApplyWorkDayTypeToForm(resetValues: false);
        UpdateHolidayHint(selectHoliday: false);
        WorkFormTitleText.Text = "Arbeitstag erfassen";
        SaveWorkDayButton.Content = "Speichern";
        WorkPreviewText.Text =
            $"Vorbereitet als Reisetag.\nAngerechnete Reisezeit: {TimeFormatter.Format(trip.TravelTime)}";
    }

    private void PreviewWorkDay_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var day = ReadWorkDayFromInput();
            if (!ConfirmWorkDayWarnings("Arbeitszeit prüfen", day))
                return;

            WorkPreviewText.Text =
                $"Tagesart: {day.DayTypeText}\n" +
                $"Istzeit: {TimeFormatter.Format(day.ActualWorkTime)}\n" +
                $"Sollzeit: {TimeFormatter.Format(day.TargetTime)}\n" +
                $"Überstunden: {TimeFormatter.FormatSigned(day.Overtime)}";
            StatusText.Text = "Arbeitszeit berechnet. Noch nicht gespeichert.";
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void SaveWorkDay_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var wasEditing = _editingWorkDayId > 0;
            var day = ReadWorkDayFromInput();
            if (!ConfirmWorkDayWarnings("Arbeitstag speichern", day))
                return;

            _database.SaveWorkDay(day);
            WorkPreviewText.Text =
                $"Gespeichert. Istzeit: {TimeFormatter.Format(day.ActualWorkTime)}, Überstunden: {TimeFormatter.FormatSigned(day.Overtime)}";
            LoadLists();
            RefreshReport();
            ResetWorkDayEditState();
            BreakBox.Text = _settings.DefaultBreak;
            TargetBox.Text = _settings.DefaultTarget;
            StatusText.Text = wasEditing ? "Arbeitstag aktualisiert." : "Arbeitstag gespeichert.";
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void NewWorkDay_Click(object sender, RoutedEventArgs e)
    {
        ResetWorkDayForm();
        StatusText.Text = "Neuer Arbeitstag kann erfasst werden.";
    }

    private void SetWorkDateToday_Click(object sender, RoutedEventArgs e)
    {
        SetWorkDateAndDefaults(DateTime.Today);
        StatusText.Text = "Heute wurde vorbereitet.";
    }

    private void SetWorkDateYesterday_Click(object sender, RoutedEventArgs e)
    {
        SetWorkDateAndDefaults(DateTime.Today.AddDays(-1));
        StatusText.Text = "Gestern wurde vorbereitet.";
    }

    private void SaveStandardWorkDay_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            if (WorkDatePicker.SelectedDate is null)
                WorkDatePicker.SelectedDate = DateTime.Today;

            _editingWorkDayId = 0;
            SetStandardWorkInputs();
            var day = ReadWorkDayFromInput();
            if (!ConfirmWorkDayWarnings("Standardtag speichern", day))
                return;

            _database.SaveWorkDay(day);
            WorkPreviewText.Text =
                $"Standardtag gespeichert. Istzeit: {TimeFormatter.Format(day.ActualWorkTime)}, Überstunden: {TimeFormatter.FormatSigned(day.Overtime)}";
            LoadLists();
            RefreshReport();
            ResetWorkDayEditState();
            StatusText.Text = "Standardtag gespeichert.";
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void OpenBulkWorkDay_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new BulkWorkDayWindow(_database, _holidayService, _settings)
        {
            Owner = this
        };

        if (dialog.ShowDialog() != true)
            return;

        LoadLists();
        RefreshReport();
        StatusText.Text = $"{dialog.SavedCount} Tage aus dem Zeitraum gespeichert.";
    }

    private void EditSelectedWorkDay_Click(object sender, RoutedEventArgs e)
    {
        if (WorkDaysGrid.SelectedItem is not WorkDay day)
        {
            ShowError("Bitte zuerst einen Arbeitstag in der Tabelle auswählen.");
            return;
        }

        LoadWorkDayIntoForm(day);
        StatusText.Text = "Arbeitstag zum Bearbeiten geladen.";
    }

    private void WorkDaysGrid_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (WorkDaysGrid.SelectedItem is WorkDay day)
        {
            LoadWorkDayIntoForm(day);
            StatusText.Text = "Arbeitstag per Doppelklick geladen.";
        }
    }

    private void DeleteSelectedWorkDay_Click(object sender, RoutedEventArgs e)
    {
        if (WorkDaysGrid.SelectedItem is not WorkDay day)
        {
            ShowError("Bitte zuerst einen Arbeitstag in der Tabelle auswählen.");
            return;
        }

        if (MessageBox.Show($"Arbeitstag vom {day.Date:dd.MM.yyyy} löschen?",
                "Arbeitstag löschen", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
            return;

        _database.DeleteWorkDay(day.Id);
        LoadLists();
        RefreshReport();
        if (_editingWorkDayId == day.Id)
            ResetWorkDayForm();
        StatusText.Text = "Arbeitstag gelöscht.";
    }

    private WorkDay ReadWorkDayFromInput()
    {
        if (WorkDatePicker.SelectedDate is null)
            throw new InvalidOperationException("Bitte ein Datum auswählen.");

        var dayType = GetSelectedWorkDayType();
        var isAbsence = dayType is WorkDayType.Vacation or WorkDayType.Sick or WorkDayType.Holiday;
        var isTravelDay = !isAbsence && IsTravelDayCheck.IsChecked == true;

        return new WorkDay
        {
            Id = _editingWorkDayId,
            Date = WorkDatePicker.SelectedDate.Value.Date,
            DayType = dayType,
            StartTime = isAbsence ? TimeSpan.Zero : ParseTime(WorkStartBox.Text, "Beginn"),
            EndTime = isAbsence ? TimeSpan.Zero : ParseTime(WorkEndBox.Text, "Ende"),
            BreakTime = isAbsence ? TimeSpan.Zero : ParseTime(BreakBox.Text, "Pause"),
            TargetTime = ParseTime(TargetBox.Text, "Sollzeit"),
            IsTravelDay = isTravelDay,
            TravelWorkTime = isTravelDay
                ? ParseTime(TravelWorkTimeBox.Text, "angerechnete Reisezeit")
                : TimeSpan.Zero,
            CountryCode = isAbsence ? string.Empty : GetSelectedWorkCountryCode(),
            Location = isAbsence ? string.Empty : WorkLocationBox.Text.Trim(),
            Note = WorkNoteBox.Text.Trim()
        };
    }

    private void LoadWorkDayIntoForm(WorkDay day)
    {
        _isUpdatingWorkDayForm = true;
        _editingWorkDayId = day.Id;
        WorkDatePicker.SelectedDate = day.Date;
        _workCalendarMonth = new DateTime(day.Date.Year, day.Date.Month, 1);
        WorkDayTypeCombo.SelectedValue = day.DayType;
        WorkStartBox.Text = FormatClockInput(day.StartTime);
        WorkEndBox.Text = FormatClockInput(day.EndTime);
        BreakBox.Text = FormatDurationInput(day.BreakTime);
        TargetBox.Text = FormatDurationInput(day.TargetTime);
        IsTravelDayCheck.IsChecked = day.IsTravelDay;
        TravelWorkTimeBox.Text = FormatDurationInput(day.TravelWorkTime);
        WorkCountryCombo.SelectedValue = day.CountryCode;
        WorkLocationBox.Text = day.Location;
        WorkNoteBox.Text = day.Note;
        _isUpdatingWorkDayForm = false;
        RefreshWorkLocationOptions();
        ApplyWorkDayTypeToForm(resetValues: false);
        UpdateHolidayHint(selectHoliday: false);
        RefreshWorkCalendar();
        UpdateWorkDashboard();
        WorkFormTitleText.Text = "Arbeitstag bearbeiten";
        SaveWorkDayButton.Content = "Änderungen speichern";
        WorkPreviewText.Text =
            $"Geladen. Istzeit: {TimeFormatter.Format(day.ActualWorkTime)}, Überstunden: {TimeFormatter.FormatSigned(day.Overtime)}";
    }

    private void ResetWorkDayForm()
    {
        _isUpdatingWorkDayForm = true;
        _editingWorkDayId = 0;
        WorkDatePicker.SelectedDate = DateTime.Today;
        _workCalendarMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        WorkDayTypeCombo.SelectedValue = WorkDayType.Work;
        WorkStartBox.Text = _settings.DefaultWorkStart;
        BreakBox.Text = _settings.DefaultBreak;
        TargetBox.Text = _settings.DefaultTarget;
        UpdateCalculatedWorkEnd();
        IsTravelDayCheck.IsChecked = false;
        TravelWorkTimeBox.Text = "00:00";
        WorkCountryCombo.SelectedValue = GetDefaultWorkCountryCode();
        WorkLocationBox.Text = string.Empty;
        WorkNoteBox.Text = string.Empty;
        _isUpdatingWorkDayForm = false;
        RefreshWorkLocationOptions();
        UpdateHolidayHint(selectHoliday: true);
        ApplyWorkDayTypeToForm(resetValues: false);
        RefreshWorkCalendar();
        UpdateWorkDashboard();
        WorkFormTitleText.Text = "Arbeitstag erfassen";
        SaveWorkDayButton.Content = "Speichern";
        WorkPreviewText.Text = "Vorschau: noch keine Berechnung.";
    }

    private void ResetWorkDayEditState()
    {
        _editingWorkDayId = 0;
        WorkFormTitleText.Text = "Arbeitstag erfassen";
        SaveWorkDayButton.Content = "Speichern";
    }

    private void RefreshReport_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            RefreshReport();
            StatusText.Text = "Auswertung aktualisiert.";
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void RefreshReport()
    {
        var year = ParseYear();
        var month = ParseMonth();
        var yearDays = _database.GetWorkDays(year);
        var report = _database.GetYearReport(year);
        var trips = _database.GetTrips(year);
        var monthDays = yearDays.Where(x => x.Date.Month == month).ToList();
        var monthTrips = trips.Where(x => x.DepartureLocalDateTime.Month == month).ToList();

        var monthActual = monthDays.Aggregate(TimeSpan.Zero, (sum, x) => sum + x.ActualWorkTime);
        var monthTarget = monthDays.Aggregate(TimeSpan.Zero, (sum, x) => sum + x.TargetTime);
        var monthTravelWork = monthDays.Aggregate(TimeSpan.Zero, (sum, x) => sum + x.TravelWorkTime);
        var monthPureTravel = monthTrips.Aggregate(TimeSpan.Zero, (sum, x) => sum + x.TravelTime);
        var monthOvertime = monthActual - monthTarget;

        MonthWorkDaysText.Text = monthDays.Count.ToString(CultureInfo.InvariantCulture);
        MonthActualText.Text = TimeFormatter.Format(monthActual);
        MonthTargetText.Text = TimeFormatter.Format(monthTarget);
        MonthOvertimeText.Text = TimeFormatter.FormatSigned(monthOvertime);
        SetOvertimeBrush(MonthOvertimeText, monthOvertime);
        MonthDetailsText.Text =
            $"Homeoffice: {monthDays.Count(x => x.DayType == WorkDayType.HomeOffice)} | " +
            $"Urlaub: {monthDays.Count(x => x.DayType == WorkDayType.Vacation)} | " +
            $"Krank: {monthDays.Count(x => x.DayType == WorkDayType.Sick)} | " +
            $"Feiertage: {monthDays.Count(x => x.DayType == WorkDayType.Holiday)}\n" +
            $"Reisetage: {monthDays.Count(x => x.IsTravelDay)} | " +
            $"Angerechnete Reisezeit: {TimeFormatter.Format(monthTravelWork)} | " +
            $"Gespeicherte Reisen: {monthTrips.Count} | " +
            $"Reine Reisezeit: {TimeFormatter.Format(monthPureTravel)}";

        ReportWorkDaysText.Text = report.WorkDayCount.ToString(CultureInfo.InvariantCulture);
        ReportActualText.Text = TimeFormatter.Format(report.TotalActualWorkTime);
        ReportTargetText.Text = TimeFormatter.Format(report.TotalTargetWorkTime);
        ReportOvertimeText.Text = TimeFormatter.FormatSigned(report.TotalOvertime);
        SetOvertimeBrush(ReportOvertimeText, report.TotalOvertime);
        ReportDetailsText.Text =
            $"Homeoffice: {report.HomeOfficeDayCount} | Urlaub: {report.VacationDayCount} | " +
            $"Krank: {report.SickDayCount} | Feiertage: {report.HolidayCount}\n" +
            $"Reisetage in Arbeitszeit: {report.TravelDayCount} | " +
            $"Angerechnete Reisezeit: {TimeFormatter.Format(report.TotalTravelWorkTime)} | " +
            $"Gespeicherte Reisen: {trips.Count} | " +
            $"Gesamte reine Reisezeit: {TimeFormatter.Format(trips.Aggregate(TimeSpan.Zero, (sum, x) => sum + x.TravelTime))}";

        var countryRows = _workLocationReportService.CreateCountryRows(monthDays, yearDays);
        var locationRows = _workLocationReportService.CreateLocationRows(monthDays, yearDays);
        CountryReportGrid.ItemsSource = countryRows;
        LocationReportGrid.ItemsSource = locationRows;
        CountryReportSummaryText.Text = CreateWorkLocationSummary(countryRows.Count, "Land", "Länder");
        LocationReportSummaryText.Text = CreateWorkLocationSummary(locationRows.Count, "Einsatzort", "Einsatzorte");
    }

    private static string CreateWorkLocationSummary(int count, string singular, string plural)
    {
        return count switch
        {
            0 => "Noch keine regulären Arbeitstage mit Arbeitsort vorhanden.",
            1 => $"1 {singular} im ausgewählten Jahr",
            _ => $"{count} {plural} im ausgewählten Jahr"
        };
    }

    private void UpdateWorkDashboard()
    {
        var days = _database.GetWorkDays(_workCalendarMonth.Year)
            .Where(x => x.Date.Month == _workCalendarMonth.Month)
            .ToList();

        var actual = days.Aggregate(TimeSpan.Zero, (sum, x) => sum + x.ActualWorkTime);
        var target = days.Aggregate(TimeSpan.Zero, (sum, x) => sum + x.TargetTime);
        var overtime = actual - target;

        WorkDashboardTitleText.Text = FormatMonthTitle(_workCalendarMonth);
        WorkDashboardDaysText.Text = days.Count.ToString(CultureInfo.InvariantCulture);
        WorkDashboardActualText.Text = TimeFormatter.Format(actual);
        WorkDashboardTargetText.Text = TimeFormatter.Format(target);
        WorkDashboardOvertimeText.Text = TimeFormatter.FormatSigned(overtime);
        SetOvertimeBrush(WorkDashboardOvertimeText, overtime);
    }

    private bool ConfirmWorkDayWarnings(string title, WorkDay day)
    {
        var hasDuplicateDate = _database.HasWorkDayOnDate(day.Date, day.Id);
        return ConfirmWarnings(title, _validation.GetWorkDayWarnings(day, hasDuplicateDate));
    }

    private static bool ConfirmWarnings(string title, IReadOnlyCollection<string> warnings)
    {
        if (warnings.Count == 0)
            return true;

        var message = "Bitte prüfe die folgenden Punkte:\n\n"
            + string.Join("\n", warnings.Select(x => $"• {x}"))
            + "\n\nTrotzdem fortfahren?";

        return MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes;
    }

    private void ExportCsv_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var year = ParseYear();
            var days = _database.GetWorkDays(year);
            var path = _csvExport.ExportWorkDays(year, days, _settings.CsvExportFolder);
            StatusText.Text = $"Jahres-CSV exportiert: {path}";
            MessageBox.Show($"Export erfolgreich:\n{path}", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void ExportMonthCsv_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var year = ParseYear();
            var month = ParseMonth();
            var days = _database.GetWorkDays(year).Where(x => x.Date.Month == month).ToList();
            var path = _csvExport.ExportWorkDays(year, month, days, _settings.CsvExportFolder);
            StatusText.Text = $"Monats-CSV exportiert: {path}";
            MessageBox.Show($"Export erfolgreich:\n{path}", "Export", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void SaveSettings_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var settings = ReadSettingsFromForm();
            _settingsService.Save(settings);
            _settings = settings;
            if (_editingWorkDayId == 0)
            {
                BreakBox.Text = _settings.DefaultBreak;
                TargetBox.Text = _settings.DefaultTarget;
                UpdateCalculatedWorkEnd();
            }
            UpdateHolidayHint(selectHoliday: _editingWorkDayId == 0);
            RefreshWorkCalendar();
            SettingsInfoText.Text = BuildSettingsInfo("Einstellungen gespeichert.");
            StatusText.Text = "Einstellungen gespeichert.";
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void SelectCsvExportFolder_Click(object sender, RoutedEventArgs e)
    {
        var currentFolder = SettingsCsvExportFolderBox.Text.Trim();
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = "CSV-Exportordner auswählen",
            Multiselect = false
        };

        if (Directory.Exists(currentFolder))
        {
            dialog.InitialDirectory = currentFolder;
        }

        if (dialog.ShowDialog(this) != true)
        {
            return;
        }

        SettingsCsvExportFolderBox.Text = dialog.FolderName;
        StatusText.Text = "CSV-Exportordner ausgewählt. Bitte Einstellungen speichern.";
    }

    private void ApplySettingsDefaults_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _settings = ReadSettingsFromForm();
            ApplySettingsDefaultsToTrip();
            ApplySettingsDefaultsToWorkDay();
            ApplyDepartureTimeZoneFromLocation(showStatus: false);
            UpdateHolidayHint(selectHoliday: _editingWorkDayId == 0);
            RefreshWorkCalendar();
            SettingsInfoText.Text = "Standardwerte wurden auf die Eingabemasken angewendet.";
            StatusText.Text = "Standardwerte angewendet.";
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void ManageHolidayRules_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new HolidayRulesWindow(_database, _holidayService, _settings)
        {
            Owner = this
        };

        if (dialog.ShowDialog() != true)
            return;

        RefreshHolidayRulesSummary();
        UpdateHolidayHint(selectHoliday: _editingWorkDayId == 0);
        RefreshWorkCalendar();
        StatusText.Text = "Feiertagsregeln aktualisiert.";
    }

    private void RefreshHolidayRulesSummary()
    {
        var rules = _database.GetHolidayRules();
        var customCount = rules.Count(rule => rule.Kind == HolidayRuleKind.CustomHoliday);
        var disabledCount = rules.Count(rule =>
            rule.Kind == HolidayRuleKind.DisabledAutomaticHoliday);
        HolidayRulesSummaryText.Text =
            $"{customCount} eigene Feiertage · {disabledCount} automatische Feiertage deaktiviert";
    }

    private void BackupDatabase_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var backup = _backupService.CreateManualBackup(_database.DatabasePath);
            RefreshBackupList(backup.FilePath);
            SettingsInfoText.Text = BuildSettingsInfo($"Datenbank gesichert:\n{backup.FilePath}");
            StatusText.Text = "Datenbank gesichert.";
            MessageBox.Show(
                $"Die Sicherung wurde geprüft und gespeichert:\n{backup.FilePath}",
                "Datenbank sichern",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void RefreshBackups_Click(object sender, RoutedEventArgs e)
    {
        RefreshBackupList();
        StatusText.Text = "Sicherungsliste aktualisiert.";
    }

    private void BackupsGrid_SelectionChanged(
        object sender,
        System.Windows.Controls.SelectionChangedEventArgs e)
    {
        var hasSelection = BackupsGrid.SelectedItem is DatabaseBackupInfo;
        RestoreBackupButton.IsEnabled = hasSelection;
        DeleteBackupButton.IsEnabled = hasSelection;
    }

    private void RestoreBackup_Click(object sender, RoutedEventArgs e)
    {
        if (BackupsGrid.SelectedItem is not DatabaseBackupInfo backup)
        {
            ShowError("Bitte zuerst eine Sicherung auswählen.");
            return;
        }

        var result = MessageBox.Show(
            $"Soll die Sicherung vom {backup.CreatedText} wiederhergestellt werden?\n\n" +
            "Der aktuelle Datenstand wird vorher automatisch gesichert.",
            "Datenbank wiederherstellen",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);
        if (result != MessageBoxResult.Yes)
            return;

        try
        {
            var safetyBackupPath = _backupService.RestoreDatabase(
                _database.DatabasePath,
                backup.FilePath);
            _database = new DatabaseService();

            ResetTripForm();
            ResetWorkDayForm();
            LoadLists();
            RefreshReport();
            RefreshHolidayRulesSummary();
            RefreshBackupList();
            SettingsInfoText.Text = BuildSettingsInfo(
                $"Sicherung wiederhergestellt.\nRettungskopie: {safetyBackupPath}");
            StatusText.Text = "Datenbank erfolgreich wiederhergestellt.";

            MessageBox.Show(
                $"Die Sicherung wurde wiederhergestellt.\n\nRettungskopie des vorherigen Datenstands:\n{safetyBackupPath}",
                "Wiederherstellung abgeschlossen",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ShowError($"Wiederherstellung nicht möglich: {ex.Message}");
        }
    }

    private void DeleteBackup_Click(object sender, RoutedEventArgs e)
    {
        if (BackupsGrid.SelectedItem is not DatabaseBackupInfo backup)
        {
            ShowError("Bitte zuerst eine Sicherung auswählen.");
            return;
        }

        if (MessageBox.Show(
                $"Soll diese Sicherung endgültig gelöscht werden?\n\n{backup.FileName}",
                "Sicherung löschen",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question) != MessageBoxResult.Yes)
        {
            return;
        }

        try
        {
            _backupService.DeleteBackup(backup.FilePath);
            RefreshBackupList();
            StatusText.Text = "Sicherung gelöscht.";
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (_settings.AutomaticBackupsEnabled)
        {
            try
            {
                var backup = _backupService.CreateAutomaticBackupIfDue(
                    _database.DatabasePath,
                    TimeSpan.FromDays(1));
                if (backup is not null)
                {
                    RefreshBackupList(backup.FilePath);
                    StatusText.Text = "Automatische Datensicherung erstellt.";
                }
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Automatische Sicherung fehlgeschlagen: {ex.Message}";
            }
        }

        if (_settings.CheckForUpdatesOnStartup && UpdateConfiguration.IsConfigured)
            await CheckForUpdatesAsync(showNoUpdateMessage: false);
    }

    private async void CheckForUpdates_Click(object sender, RoutedEventArgs e)
    {
        await CheckForUpdatesAsync(showNoUpdateMessage: true);
    }

    private async Task CheckForUpdatesAsync(bool showNoUpdateMessage)
    {
        if (!UpdateConfiguration.IsConfigured)
        {
            if (showNoUpdateMessage)
                ShowError("Die GitHub-Updatequelle ist noch nicht eingerichtet.");
            return;
        }

        try
        {
            StatusText.Text = "Suche nach Updates...";
            var update = await _updateService.CheckForUpdateAsync(CurrentAppVersion);

            if (update is null)
            {
                StatusText.Text = $"Version {AppVersion} ist aktuell.";
                if (showNoUpdateMessage)
                {
                    MessageBox.Show(
                        $"Du verwendest bereits die aktuelle Version {AppVersion}.",
                        "Keine Updates",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                return;
            }

            var notes = string.IsNullOrWhiteSpace(update.Notes)
                ? "Für diese Version sind keine weiteren Hinweise hinterlegt."
                : update.Notes.Trim();
            if (notes.Length > 900)
                notes = notes[..900] + "...";

            var result = MessageBox.Show(
                $"Version {update.Version} ist verfügbar.\n\n{notes}\n\nUpdate jetzt herunterladen und installieren?",
                update.Name,
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);
            if (result != MessageBoxResult.Yes)
            {
                StatusText.Text = $"Update {update.Version} wurde zurückgestellt.";
                return;
            }

            StatusText.Text = $"Update {update.Version} wird heruntergeladen...";
            var installerPath = await _updateService.DownloadInstallerAsync(update);
            _updateService.LaunchInstallerAfterApplicationExit(installerPath);
            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            StatusText.Text = "Update-Prüfung fehlgeschlagen.";
            if (showNoUpdateMessage)
                ShowError($"Update-Prüfung nicht möglich: {ex.Message}");
        }
    }

    private void LoadSettingsIntoForm()
    {
        SettingsDefaultDepartureLocationBox.Text = _settings.DefaultDepartureLocation;
        var region = HolidayRegionCatalog.Resolve(_settings);
        SettingsHolidayRegionCombo.SelectedValue = region.Code;
        UpdateHolidayRegionSettingsHint(region);
        SettingsDefaultWorkStartBox.Text = _settings.DefaultWorkStart;
        SettingsDefaultBreakBox.Text = _settings.DefaultBreak;
        SettingsDefaultTargetBox.Text = _settings.DefaultTarget;
        SettingsCsvExportFolderBox.Text = _settings.CsvExportFolder;
        SettingsCheckUpdatesBox.IsChecked = _settings.CheckForUpdatesOnStartup;
        SettingsAutomaticBackupsBox.IsChecked = _settings.AutomaticBackupsEnabled;
    }

    private string BuildSettingsInfo(string? message = null)
    {
        var lines = new List<string>();
        if (!string.IsNullOrWhiteSpace(message))
            lines.Add(message);

        lines.Add($"App-Version: {AppVersion}");
        lines.Add($"Datenbank-Schema: v{_database.SchemaVersion}");
        lines.Add($"Feiertagsregion: {_holidayService.GetRegion(_settings).DisplayName}");
        lines.Add($"Einstellungen: {_settingsService.SettingsPath}");
        lines.Add($"Datenbank: {_database.DatabasePath}");
        lines.Add($"Sicherungen: {_backupService.BackupFolder}");

        if (_database.LastMigrationBackupPath is not null)
            lines.Add($"Migrationssicherung: {_database.LastMigrationBackupPath}");

        return string.Join(Environment.NewLine, lines);
    }

    private void RefreshBackupList(string? selectFilePath = null)
    {
        var backups = _backupService.GetBackups();
        BackupsGrid.ItemsSource = backups;

        DatabaseBackupInfo? selectedBackup = null;
        if (!string.IsNullOrWhiteSpace(selectFilePath))
        {
            selectedBackup = backups.FirstOrDefault(
                backup => string.Equals(
                    backup.FilePath,
                    selectFilePath,
                    StringComparison.OrdinalIgnoreCase));
        }

        BackupsGrid.SelectedItem = selectedBackup;
        RestoreBackupButton.IsEnabled = selectedBackup is not null;
        DeleteBackupButton.IsEnabled = selectedBackup is not null;

        if (backups.Count == 0)
        {
            BackupSummaryText.Text =
                "Noch keine Sicherungen vorhanden. Eine neue Sicherung kann jederzeit manuell erstellt werden.";
            return;
        }

        var latest = backups[0];
        BackupSummaryText.Text =
            $"{backups.Count} Sicherungen · zuletzt {latest.CreatedText} ({latest.KindText})\n" +
            _backupService.BackupFolder;
    }

    private AppSettings ReadSettingsFromForm()
    {
        var holidayRegion = GetSelectedHolidayRegion();
        var defaultWorkStart = NormalizeTimeSetting(
            SettingsDefaultWorkStartBox.Text,
            "Standard-Arbeitsbeginn");
        var defaultBreak = NormalizeTimeSetting(
            SettingsDefaultBreakBox.Text,
            "Standard-Pause");
        var defaultTarget = NormalizeTimeSetting(
            SettingsDefaultTargetBox.Text,
            "Standard-Sollzeit");
        var settings = new AppSettings
        {
            DefaultDepartureLocation = SettingsDefaultDepartureLocationBox.Text.Trim(),
            DefaultWorkStart = defaultWorkStart,
            DefaultWorkEnd = CalculateWorkEndText(defaultWorkStart, defaultBreak, defaultTarget),
            DefaultBreak = defaultBreak,
            DefaultTarget = defaultTarget,
            HolidayRegionCode = holidayRegion.Code,
            FederalState = holidayRegion.GermanState?.ToString() ?? _settings.FederalState,
            CsvExportFolder = SettingsCsvExportFolderBox.Text.Trim(),
            CheckForUpdatesOnStartup = SettingsCheckUpdatesBox.IsChecked == true,
            AutomaticBackupsEnabled = SettingsAutomaticBackupsBox.IsChecked == true
        };

        if (string.IsNullOrWhiteSpace(settings.DefaultDepartureLocation))
            throw new InvalidOperationException("Bitte einen Standard-Startort eingeben.");
        if (string.IsNullOrWhiteSpace(settings.CsvExportFolder))
            throw new InvalidOperationException("Bitte einen CSV-Exportordner eingeben.");

        return settings;
    }

    private void ApplySettingsDefaultsToTrip()
    {
        DepartureLocationBox.Text = _settings.DefaultDepartureLocation;
    }

    private void ApplySettingsDefaultsToWorkDay()
    {
        WorkStartBox.Text = _settings.DefaultWorkStart;
        BreakBox.Text = _settings.DefaultBreak;
        TargetBox.Text = _settings.DefaultTarget;
        UpdateCalculatedWorkEnd();
        if (_editingWorkDayId == 0)
            WorkCountryCombo.SelectedValue = GetDefaultWorkCountryCode();
    }

    private void SetWorkDateAndDefaults(DateTime date)
    {
        _isUpdatingWorkDayForm = true;
        _editingWorkDayId = 0;
        WorkDatePicker.SelectedDate = date.Date;
        _workCalendarMonth = new DateTime(date.Year, date.Month, 1);
        SetStandardWorkInputs();
        _isUpdatingWorkDayForm = false;
        UpdateHolidayHint(selectHoliday: true);
        ApplyWorkDayTypeToForm(resetValues: false);
        RefreshWorkCalendar();
        UpdateWorkDashboard();
        WorkFormTitleText.Text = "Arbeitstag erfassen";
        SaveWorkDayButton.Content = "Speichern";
        WorkPreviewText.Text = "Standardtag vorbereitet.";
    }

    private void SetStandardWorkInputs()
    {
        WorkDayTypeCombo.SelectedValue = WorkDayType.Work;
        WorkStartBox.Text = _settings.DefaultWorkStart;
        BreakBox.Text = _settings.DefaultBreak;
        TargetBox.Text = _settings.DefaultTarget;
        UpdateCalculatedWorkEnd();
        IsTravelDayCheck.IsChecked = false;
        TravelWorkTimeBox.Text = "00:00";
        WorkCountryCombo.SelectedValue = GetDefaultWorkCountryCode();

        if (string.IsNullOrWhiteSpace(WorkLocationBox.Text))
            WorkLocationBox.Text = _settings.DefaultDepartureLocation;
    }

    private void WorkDatePicker_SelectedDateChanged(object? sender, EventArgs e)
    {
        if (_isUpdatingWorkDayForm)
            return;

        if (WorkDatePicker.SelectedDate is DateTime date)
            _workCalendarMonth = new DateTime(date.Year, date.Month, 1);

        UpdateHolidayHint(selectHoliday: _editingWorkDayId == 0);
        RefreshWorkCalendar();
        UpdateWorkDashboard();
    }

    private void WorkLocationContext_Changed(object sender, RoutedEventArgs e)
    {
        if (ReferenceEquals(sender, WorkCountryCombo))
            RefreshWorkLocationOptions();

        if (_isUpdatingWorkDayForm || WorkDatePicker.SelectedDate is null)
            return;

        UpdateHolidayHint(selectHoliday: _editingWorkDayId == 0);
    }

    private void WorkStartBox_TextChanged(object? sender, EventArgs e)
    {
        if (_isUpdatingWorkDayForm)
            return;

        UpdateCalculatedWorkEnd();
    }

    private void IsTravelDayCheck_Changed(object sender, RoutedEventArgs e)
    {
        if (_isUpdatingWorkDayForm)
            return;

        UpdateTravelTimeVisibility();
    }

    private void UpdateCalculatedWorkEnd()
    {
        if (WorkDayTypeCombo is null
            || WorkStartBox is null
            || WorkEndBox is null
            || BreakBox is null
            || TargetBox is null)
        {
            return;
        }

        if (GetSelectedWorkDayType() is WorkDayType.Vacation or WorkDayType.Sick or WorkDayType.Holiday)
            return;

        if (!TryParseTimeInput(WorkStartBox.Text, out var start)
            || !TryParseTimeInput(BreakBox.Text, out var breakTime)
            || !TryParseTimeInput(TargetBox.Text, out var targetTime))
        {
            return;
        }

        var end = start + breakTime + targetTime;
        WorkEndBox.Text = $"{(int)end.TotalHours % 24:00}:{end.Minutes:00}";
    }

    private static string CalculateWorkEndText(
        string startText,
        string breakText,
        string targetText)
    {
        var end = ParseTime(startText, "Standard-Arbeitsbeginn")
                  + ParseTime(breakText, "Standard-Pause")
                  + ParseTime(targetText, "Standard-Sollzeit");
        return $"{(int)end.TotalHours % 24:00}:{end.Minutes:00}";
    }

    private void UpdateTravelTimeVisibility()
    {
        var isAbsence = GetSelectedWorkDayType()
            is WorkDayType.Vacation or WorkDayType.Sick or WorkDayType.Holiday;
        var visibility = !isAbsence && IsTravelDayCheck.IsChecked == true
            ? Visibility.Visible
            : Visibility.Collapsed;

        TravelWorkTimeLabel.Visibility = visibility;
        TravelWorkTimeBox.Visibility = visibility;
    }

    private void RefreshWorkLocationOptions()
    {
        var currentLocation = WorkLocationBox.Text;
        WorkLocationBox.ItemsSource = _database.GetWorkLocations(GetSelectedWorkCountryCode());
        WorkLocationBox.Text = currentLocation;
    }

    private void PreviousWorkCalendarMonth_Click(object sender, RoutedEventArgs e)
    {
        _workCalendarMonth = _workCalendarMonth.AddMonths(-1);
        RefreshWorkCalendar();
        UpdateWorkDashboard();
    }

    private void CurrentWorkCalendarMonth_Click(object sender, RoutedEventArgs e)
    {
        _workCalendarMonth = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
        RefreshWorkCalendar();
        UpdateWorkDashboard();
    }

    private void NextWorkCalendarMonth_Click(object sender, RoutedEventArgs e)
    {
        _workCalendarMonth = _workCalendarMonth.AddMonths(1);
        RefreshWorkCalendar();
        UpdateWorkDashboard();
    }

    private void ShowWorkCalendarView_Click(object sender, RoutedEventArgs e)
    {
        SetWorkView(showCalendar: true);
    }

    private void ShowWorkListView_Click(object sender, RoutedEventArgs e)
    {
        SetWorkView(showCalendar: false);
    }

    private void SetWorkView(bool showCalendar)
    {
        WorkCalendarPanel.Visibility = showCalendar ? Visibility.Visible : Visibility.Collapsed;
        WorkListPanel.Visibility = showCalendar ? Visibility.Collapsed : Visibility.Visible;

        WorkCalendarViewButton.Background = showCalendar ? FindBrush("AccentBrush") : Brushes.Transparent;
        WorkCalendarViewButton.BorderBrush = showCalendar ? FindBrush("AccentDarkBrush") : Brushes.Transparent;
        WorkCalendarViewButton.Foreground = showCalendar ? Brushes.White : FindBrush("MutedBrush");

        WorkListViewButton.Background = showCalendar ? Brushes.Transparent : FindBrush("AccentBrush");
        WorkListViewButton.BorderBrush = showCalendar ? Brushes.Transparent : FindBrush("AccentDarkBrush");
        WorkListViewButton.Foreground = showCalendar ? FindBrush("MutedBrush") : Brushes.White;
    }

    private static Brush FindBrush(string resourceName)
    {
        return Application.Current.Resources[resourceName] as Brush ?? Brushes.Transparent;
    }

    private void WorkCalendarDay_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not System.Windows.Controls.Button { Tag: DateTime date })
            return;

        _workCalendarMonth = new DateTime(date.Year, date.Month, 1);
        var day = _database.GetWorkDays(date.Year)
            .FirstOrDefault(x => x.Date.Date == date.Date);

        if (day is null)
        {
            SetWorkDateAndDefaults(date);
            StatusText.Text = $"{date:dd.MM.yyyy} kann erfasst werden.";
        }
        else
        {
            LoadWorkDayIntoForm(day);
            WorkDaysGrid.SelectedItem = WorkDaysGrid.Items
                .OfType<WorkDay>()
                .FirstOrDefault(x => x.Id == day.Id);
            StatusText.Text = $"{day.DayTypeText} vom {date:dd.MM.yyyy} wurde geladen.";
        }
    }

    private void RefreshWorkCalendar(IReadOnlyCollection<WorkDay>? allWorkDays = null)
    {
        if (WorkCalendarItems is null || WorkCalendarMonthTitleText is null)
            return;

        WorkCalendarMonthTitleText.Text = FormatMonthTitle(_workCalendarMonth);

        allWorkDays ??= _database.GetWorkDays();
        var daysByDate = allWorkDays
            .GroupBy(x => x.Date.Date)
            .ToDictionary(group => group.Key, group => group.First());
        var monthEntries = allWorkDays.Count(x =>
            x.Date.Year == _workCalendarMonth.Year && x.Date.Month == _workCalendarMonth.Month);
        var holidayRules = _database.GetHolidayRules();
        var monthHolidays = _holidayService.GetHolidays(
                _workCalendarMonth.Year,
                _settings,
                holidayRules)
            .Count(x => x.Key.Month == _workCalendarMonth.Month);
        WorkCalendarSummaryText.Text =
            $"{monthEntries} {(monthEntries == 1 ? "Eintrag" : "Einträge")} · " +
            $"{monthHolidays} {(monthHolidays == 1 ? "Feiertag" : "Feiertage")}";
        var selectedDate = WorkDatePicker.SelectedDate?.Date;

        var firstOfMonth = new DateTime(_workCalendarMonth.Year, _workCalendarMonth.Month, 1);
        var offset = ((int)firstOfMonth.DayOfWeek + 6) % 7;
        var firstVisibleDate = firstOfMonth.AddDays(-offset);
        var calendarDays = new List<WorkCalendarDay>(42);

        for (var index = 0; index < 42; index++)
        {
            var date = firstVisibleDate.AddDays(index);
            daysByDate.TryGetValue(date.Date, out var workDay);
            var holidayName = _holidayService.GetHolidayName(
                date,
                _settings,
                holidayRules,
                workDay?.CountryCode,
                workDay?.Location);
            calendarDays.Add(CreateWorkCalendarDay(date, workDay, holidayName, selectedDate));
        }

        WorkCalendarItems.ItemsSource = calendarDays;
    }

    private WorkCalendarDay CreateWorkCalendarDay(
        DateTime date,
        WorkDay? workDay,
        string? holidayName,
        DateTime? selectedDate)
    {
        var isCurrentMonth = date.Month == _workCalendarMonth.Month
                             && date.Year == _workCalendarMonth.Year;
        var background = date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday
            ? CalendarWeekendBrush
            : CalendarEmptyBrush;
        Brush accent = Brushes.Transparent;
        var typeLabel = string.Empty;
        var detailLabel = string.Empty;
        var toolTip = date.ToString("dddd, dd. MMMM yyyy", CultureInfo.GetCultureInfo("de-DE"));

        if (workDay is not null)
        {
            background = workDay.IsTravelDay
                ? CalendarTravelBrush
                : workDay.DayType switch
                {
                    WorkDayType.HomeOffice => CalendarHomeOfficeBrush,
                    WorkDayType.Vacation => CalendarVacationBrush,
                    WorkDayType.Sick => CalendarSickBrush,
                    WorkDayType.Holiday => CalendarHolidayBrush,
                    _ => CalendarWorkBrush
                };
            accent = workDay.IsTravelDay
                ? CalendarTravelAccentBrush
                : workDay.DayType switch
                {
                    WorkDayType.HomeOffice => CalendarHomeOfficeAccentBrush,
                    WorkDayType.Vacation => CalendarVacationAccentBrush,
                    WorkDayType.Sick => CalendarSickAccentBrush,
                    WorkDayType.Holiday => CalendarHolidayAccentBrush,
                    _ => CalendarWorkAccentBrush
                };
            var calendarType = workDay.IsTravelDay ? "Reise" : workDay.DayTypeText;
            var calendarTime = workDay.IsAbsence
                ? TimeFormatter.FormatDuration(workDay.TargetTime)
                : TimeFormatter.FormatDuration(workDay.ActualWorkTime);
            typeLabel = $"{calendarType} · {calendarTime}";
            toolTip +=
                $"\n{typeLabel}" +
                $"\nIst: {TimeFormatter.FormatDuration(workDay.ActualWorkTime)}" +
                $"\nSoll: {TimeFormatter.FormatDuration(workDay.TargetTime)}" +
                $"\nSaldo: {TimeFormatter.FormatSignedDuration(workDay.Overtime)}";
            if (!workDay.IsAbsence && !string.IsNullOrWhiteSpace(workDay.Location))
                toolTip += $"\nArbeitsort: {workDay.Location}, {workDay.CountryName}";
        }
        else if (holidayName is not null)
        {
            background = CalendarHolidayBrush;
            accent = CalendarHolidayAccentBrush;
            typeLabel = $"Feiertag · {holidayName}";
            toolTip += $"\n{holidayName}\nNoch nicht gespeichert";
        }

        var isSelected = selectedDate == date.Date;
        var isToday = date.Date == DateTime.Today;

        return new WorkCalendarDay
        {
            Date = date.Date,
            DayNumber = date.Day.ToString(CultureInfo.InvariantCulture),
            TypeLabel = typeLabel,
            DetailLabel = detailLabel,
            ToolTip = toolTip,
            Background = background,
            Accent = accent,
            Border = isSelected || isToday ? CalendarTodayBorderBrush : CalendarDefaultBorderBrush,
            BorderThickness = isSelected ? new Thickness(2) : new Thickness(1),
            Opacity = isCurrentMonth ? 1 : 0.45
        };
    }

    private static string FormatMonthTitle(DateTime date)
    {
        var culture = CultureInfo.GetCultureInfo("de-DE");
        return culture.TextInfo.ToTitleCase(date.ToString("MMMM yyyy", culture));
    }

    private void WorkDayTypeCombo_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (_isUpdatingWorkDayForm)
            return;

        ApplyWorkDayTypeToForm(resetValues: true);
    }

    private void UpdateHolidayHint(bool selectHoliday)
    {
        if (WorkDatePicker.SelectedDate is not DateTime date)
        {
            WorkHolidayHintText.Text = string.Empty;
            return;
        }

        var region = _holidayService.GetRegion(_settings);
        var holidayName = _holidayService.GetHolidayName(
            date,
            _settings,
            _database.GetHolidayRules(),
            GetSelectedWorkCountryCode(),
            WorkLocationBox.Text);
        var regionNotice = _holidayService.GetRegionNotice(_settings);
        WorkHolidayHintText.Text = holidayName is null
            ? $"Kein erfasster Feiertag in {region.ShortDisplayName}."
            : $"{holidayName} · automatisch erkannt für {region.ShortDisplayName}";
        if (!string.IsNullOrWhiteSpace(regionNotice))
            WorkHolidayHintText.Text += $"{Environment.NewLine}{regionNotice}";

        if (!selectHoliday)
            return;

        _isUpdatingWorkDayForm = true;
        if (holidayName is not null)
            WorkDayTypeCombo.SelectedValue = WorkDayType.Holiday;
        else if (GetSelectedWorkDayType() == WorkDayType.Holiday)
            WorkDayTypeCombo.SelectedValue = WorkDayType.Work;
        _isUpdatingWorkDayForm = false;
        ApplyWorkDayTypeToForm(resetValues: true);
    }

    private void ApplyWorkDayTypeToForm(bool resetValues)
    {
        var type = GetSelectedWorkDayType();
        var isAbsence = type is WorkDayType.Vacation or WorkDayType.Sick or WorkDayType.Holiday;
        var showLocation = !isAbsence && type != WorkDayType.HomeOffice;
        var workVisibility = isAbsence ? Visibility.Collapsed : Visibility.Visible;

        WorkStartLabel.Visibility = workVisibility;
        WorkStartBox.Visibility = workVisibility;
        WorkEndLabel.Visibility = workVisibility;
        WorkEndBox.Visibility = workVisibility;
        IsTravelDayCheck.Visibility = workVisibility;
        WorkCountryLabel.Visibility = workVisibility;
        WorkCountryCombo.Visibility = workVisibility;
        WorkLocationLabel.Visibility = showLocation ? Visibility.Visible : Visibility.Collapsed;
        WorkLocationBox.Visibility = showLocation ? Visibility.Visible : Visibility.Collapsed;
        UpdateTravelTimeVisibility();

        if (!resetValues)
            return;

        if (isAbsence)
        {
            WorkStartBox.Text = "00:00";
            WorkEndBox.Text = "00:00";
            BreakBox.Text = "00:00";
            IsTravelDayCheck.IsChecked = false;
            TravelWorkTimeBox.Text = "00:00";
            WorkCountryCombo.SelectedValue = string.Empty;
            WorkLocationBox.Text = string.Empty;
            UpdateTravelTimeVisibility();
            return;
        }

        if (WorkStartBox.Text == "00:00" && WorkEndBox.Text == "00:00")
        {
            WorkStartBox.Text = _settings.DefaultWorkStart;
            BreakBox.Text = _settings.DefaultBreak;
            TargetBox.Text = _settings.DefaultTarget;
            UpdateCalculatedWorkEnd();
        }

        if (type == WorkDayType.HomeOffice)
        {
            if (string.IsNullOrWhiteSpace(GetSelectedWorkCountryCode()))
                WorkCountryCombo.SelectedValue = GetDefaultWorkCountryCode();
            WorkLocationBox.Text = "Homeoffice";
        }
        else if (string.IsNullOrWhiteSpace(WorkLocationBox.Text)
                 || string.Equals(WorkLocationBox.Text.Trim(), "Homeoffice", StringComparison.OrdinalIgnoreCase))
        {
            if (string.IsNullOrWhiteSpace(GetSelectedWorkCountryCode()))
                WorkCountryCombo.SelectedValue = GetDefaultWorkCountryCode();
            WorkLocationBox.Text = _settings.DefaultDepartureLocation;
        }

        UpdateTravelTimeVisibility();
    }

    private WorkDayType GetSelectedWorkDayType()
    {
        if (WorkDayTypeCombo.SelectedValue is WorkDayType type)
            return type;
        if (WorkDayTypeCombo.SelectedItem is WorkDayTypeOption option)
            return option.Value;
        return WorkDayType.Work;
    }

    private string GetSelectedWorkCountryCode()
    {
        if (WorkCountryCombo.SelectedValue is string code)
            return code;
        if (WorkCountryCombo.SelectedItem is CountryOption country)
            return country.Code;
        return string.Empty;
    }

    private string GetDefaultWorkCountryCode()
    {
        return HolidayRegionCatalog.Resolve(_settings).Country == HolidayCountry.Switzerland
            ? "CH"
            : "DE";
    }

    private HolidayRegion GetSelectedHolidayRegion()
    {
        if (SettingsHolidayRegionCombo.SelectedItem is HolidayRegion region)
            return region;
        if (SettingsHolidayRegionCombo.SelectedValue is string code)
        {
            var selected = HolidayRegionCatalog.GetAll().FirstOrDefault(
                item => string.Equals(item.Code, code, StringComparison.OrdinalIgnoreCase));
            if (selected is not null)
                return selected;
        }

        return HolidayRegionCatalog.Resolve(_settings);
    }

    private void SettingsHolidayRegionCombo_SelectionChanged(
        object sender,
        SelectionChangedEventArgs e)
    {
        if (SettingsHolidayRegionCombo.SelectedItem is HolidayRegion region)
            UpdateHolidayRegionSettingsHint(region);
    }

    private void UpdateHolidayRegionSettingsHint(HolidayRegion region)
    {
        if (region.Country == HolidayCountry.Germany)
        {
            SettingsHolidayRegionHintText.Text =
                $"Gesetzliche Feiertage für {region.RegionName}.";
            return;
        }

        SettingsHolidayRegionHintText.Text = region.HasLocalVariations
            ? "Kantonsweit geltende Feiertage werden automatisch berücksichtigt. Je nach Bezirk oder Gemeinde können weitere Feiertage gelten."
            : "Kantonale Feiertage werden automatisch berücksichtigt. Lokale oder vertragliche Sonderregelungen können zusätzlich gelten.";
    }

    private int ParseYear()
    {
        if (!int.TryParse(ReportYearBox.Text.Trim(), out var year) || year < 2000 || year > 2100)
            throw new InvalidOperationException("Bitte ein gültiges Jahr zwischen 2000 und 2100 eingeben.");
        return year;
    }

    private int ParseMonth()
    {
        if (ReportMonthCombo.SelectedValue is int month && month is >= 1 and <= 12)
            return month;

        if (ReportMonthCombo.SelectedItem is MonthOption option)
            return option.Number;

        throw new InvalidOperationException("Bitte einen gültigen Monat auswählen.");
    }

    private static void SetOvertimeBrush(System.Windows.Controls.TextBlock textBlock, TimeSpan overtime)
    {
        if (overtime > TimeSpan.Zero)
            textBlock.Foreground = PositiveOvertimeBrush;
        else if (overtime < TimeSpan.Zero)
            textBlock.Foreground = NegativeOvertimeBrush;
        else
            textBlock.Foreground = NeutralOvertimeBrush;
    }

    private static TimeZoneInfo? FindComboTimeZone(System.Windows.Controls.ComboBox comboBox, string timeZoneId)
    {
        return comboBox.Items.OfType<TimeZoneInfo>().FirstOrDefault(x => x.Id == timeZoneId);
    }

    private static string FormatClockInput(TimeSpan value)
    {
        return TimeFormatter.FormatClock(value);
    }

    private static string FormatDurationInput(TimeSpan value)
    {
        return TimeFormatter.FormatDuration(value);
    }

    private static string NormalizeTimeSetting(string text, string fieldName)
    {
        return FormatDurationInput(ParseTime(text, fieldName));
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

        var formats = new[] { @"h\:m", @"hh\:mm", @"h\:mm", @"hh\:m" };
        if (TimeSpan.TryParseExact(text, formats, CultureInfo.InvariantCulture, out var value))
            return value;
        if (TimeSpan.TryParse(text, CultureInfo.CurrentCulture, out value))
            return value;
        throw new InvalidOperationException($"Ungültige Zeitangabe bei '{fieldName}'. Bitte im Format HH:mm eingeben, z. B. 07:30.");
    }

    private static bool TryParseTimeInput(string text, out TimeSpan value)
    {
        try
        {
            value = ParseTime(text, string.Empty);
            return true;
        }
        catch (InvalidOperationException)
        {
            value = TimeSpan.Zero;
            return false;
        }
    }

    private static void ShowError(string message)
    {
        MessageBox.Show(message, "Hinweis", MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}
