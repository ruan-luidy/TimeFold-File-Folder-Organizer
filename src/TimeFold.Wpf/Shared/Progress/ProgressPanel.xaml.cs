using System.Collections.Specialized;
using System.Windows.Controls;

namespace TimeFold.Wpf.Shared.Progress
{
    public partial class ProgressPanel : UserControl
    {
        public ProgressPanel()
        {
            InitializeComponent();
            // the log follows the last line, like the old status box
            ((INotifyCollectionChanged)Log.Items).CollectionChanged += (_, e) =>
            {
                if (e.Action == NotifyCollectionChangedAction.Add && Log.Items.Count > 0)
                    Log.ScrollIntoView(Log.Items[^1]);
            };
        }
    }
}
