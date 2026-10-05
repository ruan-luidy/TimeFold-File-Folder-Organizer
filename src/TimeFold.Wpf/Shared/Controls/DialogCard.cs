using System.Windows.Controls;
using System.Windows.Input;

namespace TimeFold.Wpf.Shared.Controls
{
    /// <summary>
    /// Content of an in-window dialog. It finishes with a result; DialogService shows it and closes it.
    /// Esc finishes with <see cref="CancelResult"/>.
    /// </summary>
    public class DialogCard : UserControl
    {
        private readonly TaskCompletionSource<object?> _result = new();

        public Task<object?> Result => _result.Task;

        protected object? CancelResult { get; set; }

        protected void Finish(object? result) => _result.TrySetResult(result);

        protected override void OnPreviewKeyDown(KeyEventArgs e)
        {
            base.OnPreviewKeyDown(e);
            if (e.Key == Key.Escape)
            {
                Finish(CancelResult);
                e.Handled = true;
            }
        }
    }
}
