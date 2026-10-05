using System.Collections;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace TimeFold.Wpf.Shared.Controls
{
    /// <summary>
    /// Dropdown of the Terminal: the closed box has the frame of the other fields and the list opens in a card.
    /// GroupMemberPath groups the list under small headers (the naming templates).
    /// </summary>
    public partial class SelectField : UserControl
    {
        public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
            nameof(ItemsSource), typeof(IEnumerable), typeof(SelectField), new PropertyMetadata(null, OnLookChanged));

        public static readonly DependencyProperty SelectedItemProperty = DependencyProperty.Register(
            nameof(SelectedItem),
            typeof(object),
            typeof(SelectField),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedItemChanged));

        public static readonly DependencyProperty DisplayMemberPathProperty = DependencyProperty.Register(
            nameof(DisplayMemberPath), typeof(string), typeof(SelectField), new PropertyMetadata(string.Empty, OnLookChanged));

        public static readonly DependencyProperty GroupMemberPathProperty = DependencyProperty.Register(
            nameof(GroupMemberPath), typeof(string), typeof(SelectField), new PropertyMetadata(string.Empty, OnLookChanged));

        public static readonly DependencyProperty PlaceholderProperty = DependencyProperty.Register(
            nameof(Placeholder), typeof(string), typeof(SelectField), new PropertyMetadata(string.Empty, OnLookChanged));

        public SelectField()
        {
            InitializeComponent();
            Show();
        }

        public IEnumerable? ItemsSource
        {
            get => (IEnumerable?)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public object? SelectedItem
        {
            get => GetValue(SelectedItemProperty);
            set => SetValue(SelectedItemProperty, value);
        }

        public string DisplayMemberPath
        {
            get => (string)GetValue(DisplayMemberPathProperty);
            set => SetValue(DisplayMemberPathProperty, value);
        }

        public string GroupMemberPath
        {
            get => (string)GetValue(GroupMemberPathProperty);
            set => SetValue(GroupMemberPathProperty, value);
        }

        public string Placeholder
        {
            get => (string)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }

        private static void OnSelectedItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e) =>
            ((SelectField)d).Show();

        private static void OnLookChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var field = (SelectField)d;
            field.Items.DisplayMemberPath = field.DisplayMemberPath;

            if (field.ItemsSource is null)
            {
                field.Items.ItemsSource = null;
            }
            else if (string.IsNullOrEmpty(field.GroupMemberPath))
            {
                field.Items.ItemsSource = field.ItemsSource;
            }
            else
            {
                var view = new ListCollectionView(field.ItemsSource.Cast<object>().ToList());
                view.GroupDescriptions.Add(new PropertyGroupDescription(field.GroupMemberPath));
                field.Items.ItemsSource = view;
            }

            field.Show();
        }

        private void Show()
        {
            if (!Equals(Items.SelectedItem, SelectedItem))
                Items.SelectedItem = SelectedItem;

            var chosen = SelectedItem is not null;
            Label.Text = chosen ? Text(SelectedItem!) : Placeholder;
            Label.SetResourceReference(TextBlock.ForegroundProperty, chosen ? "PrimaryTextBrush" : "ThirdlyTextBrush");
        }

        private string Text(object item) =>
            (string.IsNullOrEmpty(DisplayMemberPath)
                ? item.ToString()
                : TypeDescriptor.GetProperties(item)[DisplayMemberPath]?.GetValue(item)?.ToString()) ?? string.Empty;

        private void Items_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (Items.SelectedItem is { } item && !Equals(item, SelectedItem))
                SelectedItem = item;

            Toggle.IsChecked = false;
        }
    }
}
