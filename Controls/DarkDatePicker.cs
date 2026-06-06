using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace ReiseArbeitszeitApp.Controls;

public class DarkDatePicker : UserControl
{
    public static readonly DependencyProperty SelectedDateProperty =
        DependencyProperty.Register(
            nameof(SelectedDate),
            typeof(DateTime?),
            typeof(DarkDatePicker),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedDateChanged));

    private static readonly CultureInfo GermanCulture = CultureInfo.GetCultureInfo("de-DE");
    private static readonly Brush BackgroundBrush = new SolidColorBrush(Color.FromRgb(16, 19, 26));
    private static readonly Brush CardBrush = new SolidColorBrush(Color.FromRgb(23, 26, 34));
    private static readonly Brush HoverBrush = new SolidColorBrush(Color.FromRgb(42, 49, 64));
    private static readonly Brush AccentBrush = new SolidColorBrush(Color.FromRgb(10, 132, 255));
    private static readonly Brush TextBrush = new SolidColorBrush(Color.FromRgb(245, 247, 250));
    private static readonly Brush MutedBrush = new SolidColorBrush(Color.FromRgb(166, 175, 190));
    private static readonly Brush DimBrush = new SolidColorBrush(Color.FromRgb(86, 97, 116));
    private static readonly Brush BorderLineBrush = new SolidColorBrush(Color.FromRgb(48, 55, 71));

    private readonly TextBox _textBox;
    private readonly Popup _popup;
    private readonly TextBlock _headerText;
    private readonly UniformGrid _daysGrid;
    private DateTime _visibleMonth;

    public DarkDatePicker()
    {
        Margin = new Thickness(0, 4, 0, 12);
        MinHeight = 38;
        _visibleMonth = DateTime.Today;

        var root = new Grid();
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(38) });

        _textBox = new TextBox
        {
            Margin = new Thickness(0),
            MinHeight = 38,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        _textBox.LostFocus += (_, _) => TryParseText();
        _textBox.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                TryParseText();
                e.Handled = true;
            }
        };
        Grid.SetColumn(_textBox, 0);
        root.Children.Add(_textBox);

        var button = new Button
        {
            Content = "15",
            Margin = new Thickness(4, 0, 0, 0),
            Padding = new Thickness(0),
            MinHeight = 38,
            Width = 38,
            Background = CardBrush,
            BorderBrush = BorderLineBrush,
            Foreground = TextBrush,
            FontSize = 11,
            FontWeight = FontWeights.SemiBold
        };
        button.Click += (_, _) => TogglePopup();
        Grid.SetColumn(button, 1);
        root.Children.Add(button);

        _headerText = new TextBlock
        {
            Foreground = TextBrush,
            FontWeight = FontWeights.SemiBold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };

        _daysGrid = new UniformGrid { Columns = 7 };

        _popup = new Popup
        {
            AllowsTransparency = true,
            Placement = PlacementMode.Bottom,
            PlacementTarget = this,
            StaysOpen = false,
            Child = BuildCalendarPopup()
        };

        Content = root;
        UpdateText();
        RenderCalendar();
    }

    public DateTime? SelectedDate
    {
        get => (DateTime?)GetValue(SelectedDateProperty);
        set => SetValue(SelectedDateProperty, value);
    }

    public event EventHandler? SelectedDateChanged;

    private static void OnSelectedDateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DarkDatePicker picker)
            return;

        if (e.NewValue is DateTime date)
            picker._visibleMonth = new DateTime(date.Year, date.Month, 1);

        picker.UpdateText();
        picker.RenderCalendar();
        picker.SelectedDateChanged?.Invoke(picker, EventArgs.Empty);
    }

    private Border BuildCalendarPopup()
    {
        var panel = new StackPanel();

        var header = new Grid { Margin = new Thickness(0, 0, 0, 8) };
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(34) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(34) });

        var previousButton = CreateHeaderButton("<");
        previousButton.Click += (_, _) =>
        {
            _visibleMonth = _visibleMonth.AddMonths(-1);
            RenderCalendar();
        };
        Grid.SetColumn(previousButton, 0);
        header.Children.Add(previousButton);

        Grid.SetColumn(_headerText, 1);
        header.Children.Add(_headerText);

        var nextButton = CreateHeaderButton(">");
        nextButton.Click += (_, _) =>
        {
            _visibleMonth = _visibleMonth.AddMonths(1);
            RenderCalendar();
        };
        Grid.SetColumn(nextButton, 2);
        header.Children.Add(nextButton);

        panel.Children.Add(header);

        var weekdays = new UniformGrid { Columns = 7, Margin = new Thickness(0, 0, 0, 4) };
        foreach (var day in new[] { "M", "D", "M", "D", "F", "S", "S" })
        {
            weekdays.Children.Add(new TextBlock
            {
                Text = day,
                Foreground = TextBrush,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                HorizontalAlignment = HorizontalAlignment.Center
            });
        }
        panel.Children.Add(weekdays);
        panel.Children.Add(_daysGrid);

        return new Border
        {
            Background = BackgroundBrush,
            BorderBrush = BorderLineBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(10),
            Margin = new Thickness(0, 6, 0, 0),
            Child = panel,
            Effect = null
        };
    }

    private Button CreateHeaderButton(string content)
    {
        return new Button
        {
            Content = content,
            Margin = new Thickness(0),
            Padding = new Thickness(0),
            Width = 30,
            Height = 30,
            MinHeight = 30,
            Background = Brushes.Transparent,
            BorderBrush = Brushes.Transparent,
            Foreground = TextBrush,
            FontWeight = FontWeights.SemiBold
        };
    }

    private void RenderCalendar()
    {
        _headerText.Text = _visibleMonth.ToString("MMMM yyyy", GermanCulture);
        _daysGrid.Children.Clear();

        var firstOfMonth = new DateTime(_visibleMonth.Year, _visibleMonth.Month, 1);
        var offset = ((int)firstOfMonth.DayOfWeek + 6) % 7;
        var firstVisibleDay = firstOfMonth.AddDays(-offset);

        for (var index = 0; index < 42; index++)
        {
            var date = firstVisibleDay.AddDays(index);
            _daysGrid.Children.Add(CreateDayButton(date));
        }
    }

    private Button CreateDayButton(DateTime date)
    {
        var isCurrentMonth = date.Month == _visibleMonth.Month;
        var isSelected = SelectedDate?.Date == date.Date;
        var isToday = date.Date == DateTime.Today;

        var button = new Button
        {
            Content = date.Day.ToString(CultureInfo.InvariantCulture),
            Width = 30,
            Height = 28,
            MinHeight = 28,
            Margin = new Thickness(1),
            Padding = new Thickness(0),
            Background = isSelected ? AccentBrush : Brushes.Transparent,
            BorderBrush = isToday && !isSelected ? AccentBrush : Brushes.Transparent,
            BorderThickness = isToday && !isSelected ? new Thickness(1) : new Thickness(0),
            Foreground = isSelected ? Brushes.White : isCurrentMonth ? TextBrush : DimBrush,
            FontWeight = isSelected ? FontWeights.SemiBold : FontWeights.Normal
        };

        button.MouseEnter += (_, _) =>
        {
            if (!isSelected)
                button.Background = HoverBrush;
        };
        button.MouseLeave += (_, _) =>
        {
            if (!isSelected)
                button.Background = Brushes.Transparent;
        };
        button.Click += (_, _) =>
        {
            SelectedDate = date.Date;
            _popup.IsOpen = false;
        };

        return button;
    }

    private void TogglePopup()
    {
        if (SelectedDate is DateTime date)
            _visibleMonth = new DateTime(date.Year, date.Month, 1);

        RenderCalendar();
        _popup.IsOpen = !_popup.IsOpen;
    }

    private void TryParseText()
    {
        var text = _textBox.Text.Trim();
        if (DateTime.TryParse(text, GermanCulture, DateTimeStyles.None, out var parsed)
            || DateTime.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out parsed))
        {
            SelectedDate = parsed.Date;
        }
        else
        {
            UpdateText();
        }
    }

    private void UpdateText()
    {
        _textBox.Text = SelectedDate?.ToString("dd.MM.yyyy", GermanCulture) ?? string.Empty;
    }
}
