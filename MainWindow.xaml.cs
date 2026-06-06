using System.Globalization;
using System.Diagnostics;
using System.Windows;
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

    private readonly DatabaseService _database = new();
    private readonly TimeCalculationService _timeCalculation = new();
    private readonly CsvExportService _csvExport = new();
    private readonly TimeZoneLookupService _timeZoneLookup = new();
    private readonly SettingsService _settingsService = new();
    private readonly ValidationService _validation = new();
    private readonly UpdateService _updateService = new();
    private AppSettings _settings = new();
    private TripEntry? _lastTripCalculation;
    private int _editingTripId;
    private int _editingWorkDayId;

    private sealed record MonthOption(int Number, string Name);

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
        ApplyDepartureTimeZoneFromLocation(showStatus: false);
        ApplyArrivalTimeZoneFromLocation(showStatus: false);

        StatusText.Text = $"Datenbank: {_database.DatabasePath}";
        SettingsInfoText.Text = $"Version {AppVersion}\nGespeichert unter: {_settingsService.SettingsPath}";
    }

    private void LoadLists()
    {
        WorkDaysGrid.ItemsSource = _database.GetWorkDays().Take(100).ToList();
        TripsGrid.ItemsSource = _database.GetTrips().Take(100).ToList();
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
        _editingWorkDayId = 0;
        WorkDatePicker.SelectedDate = trip.DepartureLocalDateTime.Date;
        WorkStartBox.Text = "00:00";
        WorkEndBox.Text = "00:00";
        BreakBox.Text = "00:00";
        TargetBox.Text = _settings.DefaultTarget;
        IsTravelDayCheck.IsChecked = true;
        TravelWorkTimeBox.Text = FormatDurationInput(trip.TravelTime);
        WorkLocationBox.Text = $"{trip.DepartureLocation} -> {trip.ArrivalLocation}";
        WorkNoteBox.Text = string.IsNullOrWhiteSpace(trip.Note)
            ? $"Reisezeit übernommen: {TimeFormatter.Format(trip.TravelTime)}"
            : $"Reisezeit übernommen: {TimeFormatter.Format(trip.TravelTime)}\n{trip.Note}";
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

        return new WorkDay
        {
            Id = _editingWorkDayId,
            Date = WorkDatePicker.SelectedDate.Value.Date,
            StartTime = ParseTime(WorkStartBox.Text, "Beginn"),
            EndTime = ParseTime(WorkEndBox.Text, "Ende"),
            BreakTime = ParseTime(BreakBox.Text, "Pause"),
            TargetTime = ParseTime(TargetBox.Text, "Sollzeit"),
            IsTravelDay = IsTravelDayCheck.IsChecked == true,
            TravelWorkTime = ParseTime(TravelWorkTimeBox.Text, "angerechnete Reisezeit"),
            Location = WorkLocationBox.Text.Trim(),
            Note = WorkNoteBox.Text.Trim()
        };
    }

    private void LoadWorkDayIntoForm(WorkDay day)
    {
        _editingWorkDayId = day.Id;
        WorkDatePicker.SelectedDate = day.Date;
        WorkStartBox.Text = FormatClockInput(day.StartTime);
        WorkEndBox.Text = FormatClockInput(day.EndTime);
        BreakBox.Text = FormatDurationInput(day.BreakTime);
        TargetBox.Text = FormatDurationInput(day.TargetTime);
        IsTravelDayCheck.IsChecked = day.IsTravelDay;
        TravelWorkTimeBox.Text = FormatDurationInput(day.TravelWorkTime);
        WorkLocationBox.Text = day.Location;
        WorkNoteBox.Text = day.Note;
        WorkFormTitleText.Text = "Arbeitstag bearbeiten";
        SaveWorkDayButton.Content = "Änderungen speichern";
        WorkPreviewText.Text =
            $"Geladen. Istzeit: {TimeFormatter.Format(day.ActualWorkTime)}, Überstunden: {TimeFormatter.FormatSigned(day.Overtime)}";
    }

    private void ResetWorkDayForm()
    {
        _editingWorkDayId = 0;
        WorkDatePicker.SelectedDate = DateTime.Today;
        WorkStartBox.Text = _settings.DefaultWorkStart;
        WorkEndBox.Text = _settings.DefaultWorkEnd;
        BreakBox.Text = _settings.DefaultBreak;
        TargetBox.Text = _settings.DefaultTarget;
        IsTravelDayCheck.IsChecked = false;
        TravelWorkTimeBox.Text = "00:00";
        WorkLocationBox.Text = string.Empty;
        WorkNoteBox.Text = string.Empty;
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
        var report = _database.GetYearReport(year);
        var trips = _database.GetTrips(year);
        var monthDays = _database.GetWorkDays(year).Where(x => x.Date.Month == month).ToList();
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
            $"Reisetage in Arbeitszeit: {report.TravelDayCount} | " +
            $"Angerechnete Reisezeit: {TimeFormatter.Format(report.TotalTravelWorkTime)} | " +
            $"Gespeicherte Reisen: {trips.Count} | " +
            $"Gesamte reine Reisezeit: {TimeFormatter.Format(trips.Aggregate(TimeSpan.Zero, (sum, x) => sum + x.TravelTime))}";
    }

    private void UpdateWorkDashboard()
    {
        var today = DateTime.Today;
        var days = _database.GetWorkDays(today.Year)
            .Where(x => x.Date.Month == today.Month)
            .ToList();

        var actual = days.Aggregate(TimeSpan.Zero, (sum, x) => sum + x.ActualWorkTime);
        var target = days.Aggregate(TimeSpan.Zero, (sum, x) => sum + x.TargetTime);
        var overtime = actual - target;

        WorkDashboardTitleText.Text = $"{CultureInfo.GetCultureInfo("de-DE").DateTimeFormat.GetMonthName(today.Month)} {today.Year}";
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
            SettingsInfoText.Text = $"Version {AppVersion}\nEinstellungen gespeichert: {_settingsService.SettingsPath}";
            StatusText.Text = "Einstellungen gespeichert.";
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void ApplySettingsDefaults_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            _settings = ReadSettingsFromForm();
            ApplySettingsDefaultsToTrip();
            ApplySettingsDefaultsToWorkDay();
            ApplyDepartureTimeZoneFromLocation(showStatus: false);
            SettingsInfoText.Text = "Standardwerte wurden auf die Eingabemasken angewendet.";
            StatusText.Text = "Standardwerte angewendet.";
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void BackupDatabase_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var folder = string.IsNullOrWhiteSpace(_settings.CsvExportFolder)
                ? Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)
                : _settings.CsvExportFolder;

            System.IO.Directory.CreateDirectory(folder);
            var fileName = $"reise_arbeitszeit_backup_{DateTime.Now:yyyyMMdd_HHmmss}.db";
            var backupPath = System.IO.Path.Combine(folder, fileName);
            System.IO.File.Copy(_database.DatabasePath, backupPath, overwrite: false);

            SettingsInfoText.Text = $"Datenbank gesichert:\n{backupPath}";
            StatusText.Text = "Datenbank gesichert.";
            MessageBox.Show($"Backup erfolgreich:\n{backupPath}", "Datenbank sichern", MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
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
            Process.Start(new ProcessStartInfo(installerPath) { UseShellExecute = true });
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
        SettingsDefaultWorkStartBox.Text = _settings.DefaultWorkStart;
        SettingsDefaultWorkEndBox.Text = _settings.DefaultWorkEnd;
        SettingsDefaultBreakBox.Text = _settings.DefaultBreak;
        SettingsDefaultTargetBox.Text = _settings.DefaultTarget;
        SettingsCsvExportFolderBox.Text = _settings.CsvExportFolder;
        SettingsCheckUpdatesBox.IsChecked = _settings.CheckForUpdatesOnStartup;
    }

    private AppSettings ReadSettingsFromForm()
    {
        var settings = new AppSettings
        {
            DefaultDepartureLocation = SettingsDefaultDepartureLocationBox.Text.Trim(),
            DefaultWorkStart = NormalizeTimeSetting(SettingsDefaultWorkStartBox.Text, "Standard-Arbeitsbeginn"),
            DefaultWorkEnd = NormalizeTimeSetting(SettingsDefaultWorkEndBox.Text, "Standard-Arbeitsende"),
            DefaultBreak = NormalizeTimeSetting(SettingsDefaultBreakBox.Text, "Standard-Pause"),
            DefaultTarget = NormalizeTimeSetting(SettingsDefaultTargetBox.Text, "Standard-Sollzeit"),
            CsvExportFolder = SettingsCsvExportFolderBox.Text.Trim(),
            CheckForUpdatesOnStartup = SettingsCheckUpdatesBox.IsChecked == true
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
        WorkEndBox.Text = _settings.DefaultWorkEnd;
        BreakBox.Text = _settings.DefaultBreak;
        TargetBox.Text = _settings.DefaultTarget;
    }

    private void SetWorkDateAndDefaults(DateTime date)
    {
        _editingWorkDayId = 0;
        WorkDatePicker.SelectedDate = date.Date;
        SetStandardWorkInputs();
        WorkFormTitleText.Text = "Arbeitstag erfassen";
        SaveWorkDayButton.Content = "Speichern";
        WorkPreviewText.Text = "Standardtag vorbereitet.";
    }

    private void SetStandardWorkInputs()
    {
        WorkStartBox.Text = _settings.DefaultWorkStart;
        WorkEndBox.Text = _settings.DefaultWorkEnd;
        BreakBox.Text = _settings.DefaultBreak;
        TargetBox.Text = _settings.DefaultTarget;
        IsTravelDayCheck.IsChecked = false;
        TravelWorkTimeBox.Text = "00:00";

        if (string.IsNullOrWhiteSpace(WorkLocationBox.Text))
            WorkLocationBox.Text = _settings.DefaultDepartureLocation;
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

    private static void ShowError(string message)
    {
        MessageBox.Show(message, "Hinweis", MessageBoxButton.OK, MessageBoxImage.Warning);
    }
}
