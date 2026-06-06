using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace ReiseArbeitszeitApp.Controls;

public class DarkTimePicker : UserControl
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(
            nameof(Text),
            typeof(string),
            typeof(DarkTimePicker),
            new FrameworkPropertyMetadata("00:00", FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnTextChanged));

    public static readonly DependencyProperty MaxHourProperty =
        DependencyProperty.Register(
            nameof(MaxHour),
            typeof(int),
            typeof(DarkTimePicker),
            new PropertyMetadata(23, OnPickerOptionsChanged));

    public static readonly DependencyProperty MinuteStepProperty =
        DependencyProperty.Register(
            nameof(MinuteStep),
            typeof(int),
            typeof(DarkTimePicker),
            new PropertyMetadata(5, OnPickerOptionsChanged));

    private static readonly Brush BackgroundBrush = new SolidColorBrush(Color.FromRgb(16, 19, 26));
    private static readonly Brush CardBrush = new SolidColorBrush(Color.FromRgb(23, 26, 34));
    private static readonly Brush HoverBrush = new SolidColorBrush(Color.FromRgb(42, 49, 64));
    private static readonly Brush AccentBrush = new SolidColorBrush(Color.FromRgb(10, 132, 255));
    private static readonly Brush TextBrush = new SolidColorBrush(Color.FromRgb(245, 247, 250));
    private static readonly Brush MutedBrush = new SolidColorBrush(Color.FromRgb(166, 175, 190));
    private static readonly Brush BorderLineBrush = new SolidColorBrush(Color.FromRgb(48, 55, 71));

    private readonly TextBox _textBox;
    private readonly Popup _popup;
    private readonly ListBox _hourList;
    private readonly ListBox _minuteList;
    private bool _isUpdating;

    public DarkTimePicker()
    {
        Margin = new Thickness(0, 4, 0, 12);
        MinHeight = 38;

        var root = new Grid();
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
        root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(38) });

        var inputBorder = new Border
        {
            Background = BackgroundBrush,
            BorderBrush = BorderLineBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10)
        };
        Grid.SetColumnSpan(inputBorder, 2);
        root.Children.Add(inputBorder);

        _textBox = new TextBox
        {
            Margin = new Thickness(0),
            Padding = new Thickness(12, 0, 8, 0),
            MinHeight = 38,
            BorderThickness = new Thickness(0),
            Background = Brushes.Transparent,
            Foreground = TextBrush,
            CaretBrush = AccentBrush,
            SelectionBrush = AccentBrush,
            FontSize = 15,
            FontWeight = FontWeights.SemiBold,
            VerticalContentAlignment = VerticalAlignment.Center
        };
        _textBox.LostFocus += (_, _) => NormalizeText();
        _textBox.KeyDown += OnTextBoxKeyDown;
        _textBox.PreviewMouseWheel += OnMouseWheel;
        Grid.SetColumn(_textBox, 0);
        root.Children.Add(_textBox);

        var popupButton = new Button
        {
            Content = "00",
            Margin = new Thickness(4, 4, 4, 4),
            Padding = new Thickness(0),
            Width = 30,
            Height = 30,
            MinHeight = 30,
            Background = CardBrush,
            BorderBrush = BorderLineBrush,
            Foreground = TextBrush,
            FontSize = 11,
            FontWeight = FontWeights.SemiBold
        };
        popupButton.Click += (_, _) => TogglePopup();
        Grid.SetColumn(popupButton, 1);
        root.Children.Add(popupButton);

        _hourList = CreateWheelList();
        _minuteList = CreateWheelList();
        _hourList.SelectionChanged += (_, _) => ApplyWheelSelection();
        _minuteList.SelectionChanged += (_, _) => ApplyWheelSelection();

        _popup = new Popup
        {
            AllowsTransparency = true,
            Placement = PlacementMode.Bottom,
            PlacementTarget = this,
            StaysOpen = false,
            Child = BuildPopup()
        };

        Content = root;
        RebuildWheelItems();
        UpdateTextBox();
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public int MaxHour
    {
        get => (int)GetValue(MaxHourProperty);
        set => SetValue(MaxHourProperty, value);
    }

    public int MinuteStep
    {
        get => (int)GetValue(MinuteStepProperty);
        set => SetValue(MinuteStepProperty, value);
    }

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DarkTimePicker picker || picker._isUpdating)
            return;

        picker.UpdateTextBox();
        picker.SyncWheelSelection();
    }

    private static void OnPickerOptionsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not DarkTimePicker picker)
            return;

        picker.RebuildWheelItems();
        picker.SyncWheelSelection();
    }

    private Border BuildPopup()
    {
        var panel = new StackPanel();

        panel.Children.Add(new TextBlock
        {
            Text = "Uhrzeit",
            Foreground = TextBrush,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(4, 0, 4, 10)
        });

        var wheelGrid = new Grid();
        wheelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(92) });
        wheelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });
        wheelGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(92) });

        Grid.SetColumn(_hourList, 0);
        wheelGrid.Children.Add(_hourList);

        var separator = new TextBlock
        {
            Text = ":",
            Foreground = TextBrush,
            FontSize = 22,
            FontWeight = FontWeights.Bold,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center
        };
        Grid.SetColumn(separator, 1);
        wheelGrid.Children.Add(separator);

        Grid.SetColumn(_minuteList, 2);
        wheelGrid.Children.Add(_minuteList);

        panel.Children.Add(wheelGrid);

        return new Border
        {
            Background = BackgroundBrush,
            BorderBrush = BorderLineBrush,
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(12),
            Padding = new Thickness(10),
            Margin = new Thickness(0, 6, 0, 0),
            Child = panel
        };
    }

    private static ListBox CreateWheelList()
    {
        var list = new ListBox
        {
            Width = 92,
            Height = 174,
            Background = CardBrush,
            BorderBrush = BorderLineBrush,
            Foreground = TextBrush,
            BorderThickness = new Thickness(1),
            Padding = new Thickness(4),
            FontSize = 18,
            FontWeight = FontWeights.SemiBold,
            HorizontalContentAlignment = HorizontalAlignment.Stretch
        };
        ScrollViewer.SetVerticalScrollBarVisibility(list, ScrollBarVisibility.Hidden);

        var itemStyle = new Style(typeof(ListBoxItem));
        itemStyle.Setters.Add(new Setter(ForegroundProperty, TextBrush));
        itemStyle.Setters.Add(new Setter(BackgroundProperty, Brushes.Transparent));
        itemStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(0)));
        itemStyle.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
        itemStyle.Setters.Add(new Setter(Control.TemplateProperty, CreateListBoxItemTemplate()));
        list.ItemContainerStyle = itemStyle;

        return list;
    }

    private static ControlTemplate CreateListBoxItemTemplate()
    {
        var border = new FrameworkElementFactory(typeof(Border));
        border.Name = "ItemBorder";
        border.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
        border.SetValue(Border.PaddingProperty, new Thickness(0, 8, 0, 8));
        border.SetValue(Border.BackgroundProperty, Brushes.Transparent);

        var presenter = new FrameworkElementFactory(typeof(ContentPresenter));
        presenter.SetValue(HorizontalAlignmentProperty, HorizontalAlignment.Center);
        presenter.SetValue(VerticalAlignmentProperty, VerticalAlignment.Center);
        border.AppendChild(presenter);

        var template = new ControlTemplate(typeof(ListBoxItem))
        {
            VisualTree = border
        };

        var hoverTrigger = new Trigger { Property = IsMouseOverProperty, Value = true };
        hoverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, HoverBrush, "ItemBorder"));
        template.Triggers.Add(hoverTrigger);

        var selectedTrigger = new Trigger { Property = ListBoxItem.IsSelectedProperty, Value = true };
        selectedTrigger.Setters.Add(new Setter(Border.BackgroundProperty, AccentBrush, "ItemBorder"));
        selectedTrigger.Setters.Add(new Setter(ForegroundProperty, Brushes.White));
        template.Triggers.Add(selectedTrigger);

        return template;
    }

    private void RebuildWheelItems()
    {
        var maxHour = Math.Clamp(MaxHour, 0, 999);
        var step = Math.Clamp(MinuteStep, 1, 30);

        _hourList.ItemsSource = Enumerable.Range(0, maxHour + 1)
            .Select(x => x.ToString("00", CultureInfo.InvariantCulture))
            .ToList();

        _minuteList.ItemsSource = Enumerable.Range(0, 60)
            .Where(x => x % step == 0)
            .Select(x => x.ToString("00", CultureInfo.InvariantCulture))
            .ToList();
    }

    private void TogglePopup()
    {
        NormalizeText();
        SyncWheelSelection();
        _popup.IsOpen = !_popup.IsOpen;
    }

    private void ApplyWheelSelection()
    {
        if (_isUpdating || _hourList.SelectedItem is not string hour || _minuteList.SelectedItem is not string minute)
            return;

        SetText($"{hour}:{minute}");
    }

    private void SyncWheelSelection()
    {
        if (!TryParseTime(Text, out var hours, out var minutes))
            return;

        var maxHour = Math.Clamp(MaxHour, 0, 999);
        hours = Math.Clamp(hours, 0, maxHour);
        var step = Math.Clamp(MinuteStep, 1, 30);
        minutes = (int)Math.Round(minutes / (double)step) * step;
        if (minutes >= 60)
            minutes = 60 - step;

        _isUpdating = true;
        _hourList.SelectedItem = hours.ToString("00", CultureInfo.InvariantCulture);
        _minuteList.SelectedItem = minutes.ToString("00", CultureInfo.InvariantCulture);
        _hourList.ScrollIntoView(_hourList.SelectedItem);
        _minuteList.ScrollIntoView(_minuteList.SelectedItem);
        _isUpdating = false;
    }

    private void OnTextBoxKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            NormalizeText();
            e.Handled = true;
        }
        else if (e.Key == Key.Up)
        {
            AdjustMinutes(5);
            e.Handled = true;
        }
        else if (e.Key == Key.Down)
        {
            AdjustMinutes(-5);
            e.Handled = true;
        }
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        AdjustMinutes(e.Delta > 0 ? 5 : -5);
        e.Handled = true;
    }

    private void AdjustMinutes(int delta)
    {
        if (!TryParseTime(_textBox.Text, out var hours, out var minutes))
            return;

        var totalMinutes = Math.Max(0, hours * 60 + minutes + delta);
        var maxMinutes = Math.Clamp(MaxHour, 0, 999) * 60 + 59;
        totalMinutes = Math.Min(totalMinutes, maxMinutes);

        SetText($"{totalMinutes / 60:00}:{totalMinutes % 60:00}");
    }

    private void NormalizeText()
    {
        if (TryParseTime(_textBox.Text, out var hours, out var minutes))
            SetText($"{hours:00}:{minutes:00}");
        else
            UpdateTextBox();
    }

    private void UpdateTextBox()
    {
        _textBox.Text = Text ?? string.Empty;
    }

    private void SetText(string value)
    {
        _isUpdating = true;
        Text = value;
        _textBox.Text = value;
        _isUpdating = false;
        SyncWheelSelection();
    }

    private static bool TryParseTime(string? text, out int hours, out int minutes)
    {
        hours = 0;
        minutes = 0;

        if (string.IsNullOrWhiteSpace(text))
            return false;

        var parts = text.Trim().Split(':');
        if (parts.Length == 2
            && int.TryParse(parts[0], NumberStyles.None, CultureInfo.InvariantCulture, out hours)
            && int.TryParse(parts[1], NumberStyles.None, CultureInfo.InvariantCulture, out minutes)
            && hours >= 0
            && minutes is >= 0 and < 60)
        {
            return true;
        }

        return false;
    }
}
