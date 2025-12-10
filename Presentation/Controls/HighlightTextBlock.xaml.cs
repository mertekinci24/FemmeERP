using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;

namespace InventoryERP.Presentation.Controls;

public partial class HighlightTextBlock : UserControl
{
    public static readonly DependencyProperty TextProperty = DependencyProperty.Register(nameof(Text), typeof(string), typeof(HighlightTextBlock), new PropertyMetadata(string.Empty, OnTextChanged));
    public static readonly DependencyProperty HighlightProperty = DependencyProperty.Register(nameof(Highlight), typeof(string), typeof(HighlightTextBlock), new PropertyMetadata(string.Empty, OnTextChanged));

    public string Text { get => (string)GetValue(TextProperty); set => SetValue(TextProperty, value); }
    public string Highlight { get => (string)GetValue(HighlightProperty); set => SetValue(HighlightProperty, value); }

    public HighlightTextBlock()
    {
        InitializeComponent();
    }

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is HighlightTextBlock ctl) ctl.UpdateInlines();
    }

    private void UpdateInlines()
    {
        PART_Text.Inlines.Clear();
        if (string.IsNullOrEmpty(Text)) return;
        if (string.IsNullOrEmpty(Highlight)) { PART_Text.Inlines.Add(new Run(Text)); return; }

        var idx = 0;
        var txt = Text;
        var search = Highlight;
        var comp = StringComparison.InvariantCultureIgnoreCase;
    var highlightBrush = System.Windows.Application.Current?.TryFindResource(System.Windows.SystemColors.HighlightBrushKey) as System.Windows.Media.Brush ?? System.Windows.Media.Brushes.Yellow;
    while (idx < txt.Length)
        {
            var found = txt.IndexOf(search, idx, comp);
            if (found < 0)
            {
                PART_Text.Inlines.Add(new Run(txt.Substring(idx)));
                break;
            }
            if (found > idx) PART_Text.Inlines.Add(new Run(txt.Substring(idx, found - idx)));
            var match = new Run(txt.Substring(found, search.Length)) { FontWeight = FontWeights.Bold, Background = highlightBrush };
            PART_Text.Inlines.Add(match);
            idx = found + search.Length;
        }
    }
}

