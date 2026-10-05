using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace TimeFold.Wpf.Shared.Behaviors
{
    // A list with more rows below fades out its last 20 px instead of showing a row cut in half
    public static class ScrollFade
    {
        private const double FadeHeight = 20;

        public static readonly DependencyProperty IsEnabledProperty =
            DependencyProperty.RegisterAttached("IsEnabled", typeof(bool), typeof(ScrollFade), new PropertyMetadata(false, OnIsEnabledChanged));

        private static readonly DependencyProperty HookedProperty =
            DependencyProperty.RegisterAttached("Hooked", typeof(bool), typeof(ScrollFade), new PropertyMetadata(false));

        public static bool GetIsEnabled(DependencyObject element) => (bool)element.GetValue(IsEnabledProperty);

        public static void SetIsEnabled(DependencyObject element, bool value) => element.SetValue(IsEnabledProperty, value);

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not FrameworkElement element || e.NewValue is not true)
                return;

            element.Loaded += (_, _) => Hook(element);
            if (element.IsLoaded)
                Hook(element);
        }

        private static void Hook(FrameworkElement element)
        {
            if ((bool)element.GetValue(HookedProperty) || (element as ScrollViewer ?? Find<ScrollViewer>(element)) is not { } viewer)
                return;

            element.SetValue(HookedProperty, true);
            viewer.ScrollChanged += (_, _) => Update(viewer);
            viewer.SizeChanged += (_, _) => Update(viewer);
            Update(viewer);
        }

        private static void Update(ScrollViewer viewer)
        {
            var target = viewer.Template?.FindName("PART_ScrollContentPresenter", viewer) as FrameworkElement ?? viewer;
            var height = target.ActualHeight;
            var more = viewer.ScrollableHeight > 0.5 && viewer.VerticalOffset < viewer.ScrollableHeight - 0.5;
            if (!more || height < FadeHeight * 3)
            {
                target.OpacityMask = null;
                return;
            }
            var fade = new LinearGradientBrush
            {
                MappingMode = BrushMappingMode.Absolute,
                StartPoint = new Point(0, height - FadeHeight),
                EndPoint = new Point(0, height),
            };
            fade.GradientStops.Add(new GradientStop(Colors.Black, 0));
            fade.GradientStops.Add(new GradientStop(Colors.Transparent, 1));
            fade.Freeze();
            target.OpacityMask = fade;
        }

        private static T? Find<T>(DependencyObject root)
            where T : DependencyObject
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is T match)
                    return match;
                if (Find<T>(child) is { } found)
                    return found;
            }

            return null;
        }
    }
}
