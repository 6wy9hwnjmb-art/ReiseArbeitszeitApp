using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using ReiseArbeitszeitApp.Models;
using ReiseArbeitszeitApp.Services;

namespace ReiseArbeitszeitApp;

public partial class HolidayRulesWindow : Window
{
    private readonly DatabaseService _database;
    private readonly HolidayService _holidayService;
    private readonly AppSettings _settings;
    private readonly HolidayRegion _currentRegion;

    private sealed record ScopeOption(HolidayRuleScope Value, string Name)
    {
        public override string ToString() => Name;
    }

    public HolidayRulesWindow(
        DatabaseService database,
        HolidayService holidayService,
        AppSettings settings)
    {
        _database = database;
        _holidayService = holidayService;
        _settings = settings;
        _currentRegion = HolidayRegionCatalog.Resolve(settings);

        InitializeComponent();

        RegionHeaderText.Text =
            $"Automatische Grundlage: {_currentRegion.DisplayName}. Eigene Regeln werden lokal in der App gespeichert.";

        RuleScopeCombo.ItemsSource = new[]
        {
            new ScopeOption(HolidayRuleScope.HolidayRegion, "Bundesland / Kanton"),
            new ScopeOption(HolidayRuleScope.Country, "Land"),
            new ScopeOption(HolidayRuleScope.WorkLocation, "Einsatzort")
        };
        RuleScopeCombo.DisplayMemberPath = nameof(ScopeOption.Name);
        RuleScopeCombo.SelectedValuePath = nameof(ScopeOption.Value);

        RuleRegionCombo.ItemsSource = HolidayRegionCatalog.GetAll();
        RuleRegionCombo.DisplayMemberPath = nameof(HolidayRegion.DisplayName);
        RuleRegionCombo.SelectedValuePath = nameof(HolidayRegion.Code);

        RuleCountryCombo.ItemsSource = CountryCatalog.GetAll()
            .Where(country => !string.IsNullOrWhiteSpace(country.Code))
            .ToList();
        RuleCountryCombo.DisplayMemberPath = nameof(CountryOption.DisplayName);
        RuleCountryCombo.SelectedValuePath = nameof(CountryOption.Code);

        RuleDatePicker.SelectedDate = DateTime.Today;
        RuleScopeCombo.SelectedValue = HolidayRuleScope.HolidayRegion;
        RuleRegionCombo.SelectedValue = _currentRegion.Code;
        RuleCountryCombo.SelectedValue =
            _currentRegion.Country == HolidayCountry.Switzerland ? "CH" : "DE";
        AutomaticHolidayYearBox.Text =
            DateTime.Today.Year.ToString(CultureInfo.InvariantCulture);

        UpdateScopeControls();
        RefreshRules();
    }

    public bool RulesChanged { get; private set; }

    private void RuleScopeCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (IsInitialized)
            UpdateScopeControls();
    }

    private void UpdateScopeControls()
    {
        var scope = GetSelectedScope();
        var usesRegion = scope == HolidayRuleScope.HolidayRegion;
        var usesCountry = scope is HolidayRuleScope.Country or HolidayRuleScope.WorkLocation;
        var usesLocation = scope == HolidayRuleScope.WorkLocation;

        RuleRegionLabel.Visibility = usesRegion ? Visibility.Visible : Visibility.Collapsed;
        RuleRegionCombo.Visibility = usesRegion ? Visibility.Visible : Visibility.Collapsed;
        RuleCountryLabel.Visibility = usesCountry ? Visibility.Visible : Visibility.Collapsed;
        RuleCountryCombo.Visibility = usesCountry ? Visibility.Visible : Visibility.Collapsed;
        RuleLocationLabel.Visibility = usesLocation ? Visibility.Visible : Visibility.Collapsed;
        RuleLocationBox.Visibility = usesLocation ? Visibility.Visible : Visibility.Collapsed;
    }

    private void AddHolidayRule_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var name = RuleNameBox.Text.Trim();
            if (string.IsNullOrWhiteSpace(name))
                throw new InvalidOperationException("Bitte eine Bezeichnung eingeben.");
            if (RuleDatePicker.SelectedDate is not DateTime date)
                throw new InvalidOperationException("Bitte ein Datum auswählen.");

            var scope = GetSelectedScope();
            var regionCode = scope == HolidayRuleScope.HolidayRegion
                ? Convert.ToString(RuleRegionCombo.SelectedValue) ?? string.Empty
                : string.Empty;
            var countryCode = scope is HolidayRuleScope.Country or HolidayRuleScope.WorkLocation
                ? Convert.ToString(RuleCountryCombo.SelectedValue) ?? string.Empty
                : string.Empty;
            var location = scope == HolidayRuleScope.WorkLocation
                ? RuleLocationBox.Text.Trim()
                : string.Empty;

            if (scope == HolidayRuleScope.HolidayRegion && string.IsNullOrWhiteSpace(regionCode))
                throw new InvalidOperationException("Bitte ein Bundesland oder einen Kanton auswählen.");
            if (scope is HolidayRuleScope.Country or HolidayRuleScope.WorkLocation
                && string.IsNullOrWhiteSpace(countryCode))
            {
                throw new InvalidOperationException("Bitte ein Land auswählen.");
            }
            if (scope == HolidayRuleScope.WorkLocation && string.IsNullOrWhiteSpace(location))
                throw new InvalidOperationException("Bitte einen Einsatzort eingeben.");

            _database.SaveHolidayRule(new HolidayRule
            {
                Kind = HolidayRuleKind.CustomHoliday,
                Name = name,
                Date = date.Date,
                IsRecurring = RuleRecurringCheck.IsChecked == true,
                Scope = scope,
                RegionCode = regionCode,
                CountryCode = countryCode,
                Location = location
            });

            RulesChanged = true;
            RuleNameBox.Clear();
            RefreshRules();
        }
        catch (Exception ex)
        {
            ShowError(ex.Message);
        }
    }

    private void RefreshAutomaticHolidays_Click(object sender, RoutedEventArgs e)
    {
        RefreshAutomaticHolidays();
    }

    private void DisableAutomaticHoliday_Click(object sender, RoutedEventArgs e)
    {
        if (AutomaticHolidaysGrid.SelectedItem is not HolidayOccurrence occurrence)
        {
            ShowError("Bitte zuerst einen automatischen Feiertag auswählen.");
            return;
        }
        if (occurrence.IsDisabled)
        {
            ShowError("Dieser automatische Feiertag ist bereits deaktiviert. Zum Reaktivieren die entsprechende Regel unten löschen.");
            return;
        }

        var duplicateExists = _database.GetHolidayRules().Any(rule =>
            rule.Kind == HolidayRuleKind.DisabledAutomaticHoliday
            && rule.Scope == HolidayRuleScope.HolidayRegion
            && string.Equals(rule.RegionCode, _currentRegion.Code, StringComparison.OrdinalIgnoreCase)
            && rule.MatchesDate(occurrence.Date)
            && string.Equals(rule.Name, occurrence.Name, StringComparison.CurrentCultureIgnoreCase));
        if (duplicateExists)
        {
            ShowError("Für diesen Feiertag besteht bereits eine Deaktivierung.");
            return;
        }

        _database.SaveHolidayRule(new HolidayRule
        {
            Kind = HolidayRuleKind.DisabledAutomaticHoliday,
            Name = occurrence.Name,
            Date = occurrence.Date,
            IsRecurring = true,
            Scope = HolidayRuleScope.HolidayRegion,
            RegionCode = _currentRegion.Code
        });

        RulesChanged = true;
        RefreshRules();
    }

    private void DeleteHolidayRule_Click(object sender, RoutedEventArgs e)
    {
        if (HolidayRulesGrid.SelectedItem is not HolidayRule rule)
        {
            ShowError("Bitte zuerst eine Regel auswählen.");
            return;
        }

        _database.DeleteHolidayRule(rule.Id);
        RulesChanged = true;
        RefreshRules();
    }

    private void RefreshRules()
    {
        var rules = _database.GetHolidayRules();
        HolidayRulesGrid.ItemsSource = rules;
        RulesSummaryText.Text = rules.Count switch
        {
            0 => "Noch keine eigenen Regeln.",
            1 => "1 eigene Regel",
            _ => $"{rules.Count} eigene Regeln"
        };
        RefreshAutomaticHolidays();
    }

    private void RefreshAutomaticHolidays()
    {
        if (!int.TryParse(AutomaticHolidayYearBox.Text.Trim(), out var year)
            || year is < 2000 or > 2100)
        {
            ShowError("Bitte ein gültiges Jahr zwischen 2000 und 2100 eingeben.");
            return;
        }

        var disabledRules = _database.GetHolidayRules()
            .Where(rule => rule.Kind == HolidayRuleKind.DisabledAutomaticHoliday
                           && rule.Scope == HolidayRuleScope.HolidayRegion
                           && string.Equals(
                               rule.RegionCode,
                               _currentRegion.Code,
                               StringComparison.OrdinalIgnoreCase))
            .ToList();

        AutomaticHolidaysGrid.ItemsSource = _holidayService
            .GetAutomaticHolidays(year, _settings)
            .OrderBy(entry => entry.Key)
            .Select(entry => new HolidayOccurrence
            {
                Date = entry.Key,
                Name = entry.Value,
                IsDisabled = disabledRules.Any(rule =>
                    rule.MatchesDate(entry.Key)
                    && string.Equals(
                        rule.Name,
                        entry.Value,
                        StringComparison.CurrentCultureIgnoreCase))
            })
            .ToList();
    }

    private HolidayRuleScope GetSelectedScope()
    {
        if (RuleScopeCombo.SelectedValue is HolidayRuleScope scope)
            return scope;
        if (RuleScopeCombo.SelectedItem is ScopeOption option)
            return option.Value;
        return HolidayRuleScope.HolidayRegion;
    }

    private void Close_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = RulesChanged;
        Close();
    }

    private static void ShowError(string message)
    {
        MessageBox.Show(
            message,
            "Feiertagsverwaltung",
            MessageBoxButton.OK,
            MessageBoxImage.Warning);
    }
}
