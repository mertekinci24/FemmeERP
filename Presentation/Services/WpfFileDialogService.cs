using InventoryERP.Presentation.Abstractions;
using Microsoft.Win32;

namespace InventoryERP.Presentation.Services
{
    /// <summary>
    /// R-008: WPF implementation of file dialog service
    /// </summary>
    public class WpfFileDialogService : IFileDialogService
    {
        public string? ShowSaveFileDialog(string defaultFileName, string filter, string title)
        {
            var dialog = new SaveFileDialog
            {
                FileName = defaultFileName,
                Filter = filter,
                Title = title,
                DefaultExt = ".pdf"
            };

            bool? result = dialog.ShowDialog();
            return result == true ? dialog.FileName : null;
        }

        /// <summary>
        /// R-093: Opens file dialog for selecting files to import
        /// </summary>
        public string? ShowOpenFileDialog(string filter, string title)
        {
            var dialog = new OpenFileDialog
            {
                Filter = filter,
                Title = title,
                Multiselect = false
            };

            bool? result = dialog.ShowDialog();
            return result == true ? dialog.FileName : null;
        }
    }
}
