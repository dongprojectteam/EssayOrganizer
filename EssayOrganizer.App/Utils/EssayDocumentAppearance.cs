using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using Wpf.Ui.Appearance;

namespace EssayOrganizer.App.Utils;

/// <summary>
/// 논술 본문 FlowDocument의 글자색·형광펜 표시 정책 (편집/미리보기/저장/인쇄).
/// DB RTF에 남아 있는 글자색(예: 다크 모드 흰색)은 런타임에 무시합니다.
/// </summary>
public static class EssayDocumentAppearance
{
    public static readonly Color PrintTextColor = Colors.Black;
    public static readonly Color PrintHighlightColor = Color.FromRgb(255, 215, 0);

    /// <summary>다크 모드 편집기에서 흰 글자 위에 쓰기 좋은 형광펜 색.</summary>
    public static readonly Color DarkModeHighlightColor = Color.FromRgb(130, 100, 20);

    private const int ColorTolerance = 28;

    public static bool IsDarkTheme()
        => ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Dark;

    public static Brush DefaultTextBrush(bool isDark)
        => new SolidColorBrush(isDark ? Colors.White : PrintTextColor);

    public static Brush HighlightBrush(bool isDark)
        => new SolidColorBrush(isDark ? DarkModeHighlightColor : PrintHighlightColor);

    /// <summary>화면 표시: 앱 테마 기본 글자색 + 테마별 형광펜. RTF 글자색은 사용하지 않음.</summary>
    public static void ApplyDisplayTheme(FlowDocument document)
    {
        var isDark = IsDarkTheme();
        document.Foreground = DefaultTextBrush(isDark);
        VisitAllTextElements(document, element =>
        {
            StripStoredTextColor(element);
            NormalizeHighlightForDisplay(element, isDark);
        });
    }

    /// <summary>DB 저장용 RTF: 검은 글자 + 노란 형광펜만 남깁니다.</summary>
    public static void PrepareForStorage(FlowDocument document)
    {
        document.Foreground = Brushes.Black;
        VisitAllTextElements(document, element =>
        {
            StripStoredTextColor(element);
            NormalizeHighlightForStorage(element);
        });
    }

    /// <summary>실제 인쇄: 항상 검은 글자 + 노란 형광펜. 레거시 RTF 색상 무시.</summary>
    public static void ApplyPrintOutputTheme(FlowDocument document)
    {
        PrepareForStorage(document);
    }

    /// <summary>RTF 로드 직후 등 — 글자색만 제거하고 표시 테마를 적용합니다.</summary>
    public static void ApplyLegacyFormattedBodyPolicy(FlowDocument document)
    {
        ApplyDisplayTheme(document);
    }

    private static void VisitAllTextElements(FlowDocument document, Action<TextElement> visit)
    {
        var position = document.ContentStart;
        var end = document.ContentEnd;

        while (position.CompareTo(end) < 0)
        {
            if (position.GetPointerContext(LogicalDirection.Backward) == TextPointerContext.ElementStart
                && position.Parent is TextElement element)
            {
                visit(element);
            }

            var next = position.GetNextContextPosition(LogicalDirection.Forward);
            if (next is null)
            {
                break;
            }

            position = next;
        }
    }

    private static void StripStoredTextColor(TextElement element)
    {
        if (element.ReadLocalValue(TextElement.ForegroundProperty) == DependencyProperty.UnsetValue)
        {
            return;
        }

        element.ClearValue(TextElement.ForegroundProperty);
    }

    private static void NormalizeHighlightForStorage(TextElement element)
    {
        if (element.ReadLocalValue(TextElement.BackgroundProperty) == DependencyProperty.UnsetValue)
        {
            return;
        }

        if (TryGetSolidColor(element.Background, out var bg) && IsHighlightColor(bg))
        {
            element.Background = new SolidColorBrush(PrintHighlightColor);
            return;
        }

        if (IsTransparentBackground(element.Background))
        {
            element.ClearValue(TextElement.BackgroundProperty);
        }
    }

    private static void NormalizeHighlightForDisplay(TextElement element, bool isDark)
    {
        if (!TryGetSolidColor(element.Background, out var bg) || IsTransparentBackground(element.Background))
        {
            return;
        }

        if (!IsHighlightColor(bg))
        {
            return;
        }

        element.Background = HighlightBrush(isDark);
    }

    public static bool IsHighlightColor(Color c)
        => ColorsClose(c, PrintHighlightColor)
           || ColorsClose(c, DarkModeHighlightColor)
           || ColorsClose(c, Colors.Gold);

    private static bool ColorsClose(Color a, Color b)
        => Math.Abs(a.R - b.R) <= ColorTolerance
           && Math.Abs(a.G - b.G) <= ColorTolerance
           && Math.Abs(a.B - b.B) <= ColorTolerance;

    private static bool TryGetSolidColor(Brush? brush, out Color color)
    {
        if (brush is SolidColorBrush scb)
        {
            color = scb.Color;
            return true;
        }

        color = default;
        return false;
    }

    private static bool IsTransparentBackground(Brush? brush)
    {
        if (brush is null)
        {
            return true;
        }

        if (brush is SolidColorBrush scb)
        {
            return scb.Color.A == 0;
        }

        return false;
    }
}
