using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls.Primitives;

namespace TimeFold.Wpf.Shared.Controls
{
    /// <summary>
    /// Opens a dropdown with the same gap to its field below it or, near the bottom of the screen, above it (from Axiom).
    /// </summary>
    /// <remarks>
    /// The side is picked here from the monitor's work area; left to WPF, the popup stays below and slides up over
    /// the field. ShadowRoom is the margin kept around the card for its shadow: WPF hands the callback the card's size
    /// without it, but the returned point places the margin's corner, so the placement moves back by the margin.
    /// </remarks>
    public static class DropDownPlacement
    {
        public static readonly DependencyProperty GapProperty =
            DependencyProperty.RegisterAttached("Gap", typeof(double), typeof(DropDownPlacement), new PropertyMetadata(double.NaN, OnGapChanged));

        public static readonly DependencyProperty ShadowRoomProperty =
            DependencyProperty.RegisterAttached("ShadowRoom", typeof(Thickness), typeof(DropDownPlacement), new PropertyMetadata(default(Thickness)));

        public static double GetGap(DependencyObject popup) => (double)popup.GetValue(GapProperty);

        public static void SetGap(DependencyObject popup, double value) => popup.SetValue(GapProperty, value);

        public static Thickness GetShadowRoom(DependencyObject popup) => (Thickness)popup.GetValue(ShadowRoomProperty);

        public static void SetShadowRoom(DependencyObject popup, Thickness value) => popup.SetValue(ShadowRoomProperty, value);

        private static void OnGapChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Popup popup)
                return;

            popup.Placement = PlacementMode.Custom;
            popup.CustomPopupPlacementCallback = (popupSize, targetSize, _) =>
            {
                // sizes come in device pixels; gap and shadow room are in DIPs
                var target = popup.PlacementTarget as FrameworkElement;
                var scale = target is { ActualHeight: > 0 } ? targetSize.Height / target.ActualHeight : 1.0;
                var gap = GetGap(popup) * scale;
                var room = GetShadowRoom(popup);
                var (left, top, bottom) = (room.Left * scale, room.Top * scale, room.Bottom * scale);

                var below = new CustomPopupPlacement(new Point(-left, targetSize.Height + gap - top), PopupPrimaryAxis.None);
                var above = new CustomPopupPlacement(new Point(-left, -popupSize.Height - gap - top), PopupPrimaryAxis.None);
                if (target == null || PresentationSource.FromVisual(target) == null)
                    return [below, above];

                var origin = target.PointToScreen(new Point(0, 0));
                var work = WorkArea(origin);
                var fitsBelow = origin.Y + targetSize.Height + gap + popupSize.Height + bottom <= work.Bottom;
                var roomAbove = origin.Y - work.Top;
                var roomBelow = work.Bottom - (origin.Y + targetSize.Height);
                return [fitsBelow || roomBelow >= roomAbove ? below : above];
            };
        }

        // Work area of the monitor under the point, in device pixels (Axiom used WinForms' Screen for this)
        private static Rect WorkArea(Point screenPoint)
        {
            var monitor = MonitorFromPoint(new POINT { X = (int)screenPoint.X, Y = (int)screenPoint.Y }, MONITOR_DEFAULTTONEAREST);
            var info = new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
            if (monitor != IntPtr.Zero && GetMonitorInfo(monitor, ref info))
                return new Rect(info.rcWork.Left, info.rcWork.Top, info.rcWork.Right - info.rcWork.Left, info.rcWork.Bottom - info.rcWork.Top);

            return SystemParameters.WorkArea;
        }

        private const uint MONITOR_DEFAULTTONEAREST = 2;

        [StructLayout(LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct MONITORINFO
        {
            public int cbSize;
            public RECT rcMonitor;
            public RECT rcWork;
            public uint dwFlags;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromPoint(POINT pt, uint dwFlags);

        [DllImport("user32.dll")]
        private static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);
    }
}
