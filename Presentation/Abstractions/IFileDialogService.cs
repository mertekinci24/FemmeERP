namespace InventoryERP.Presentation.Abstractions
{
    /// <summary>
    /// R-008: Service for showing file save/open dialogs
    /// </summary>
    public interface IFileDialogService
    {
        /// <summary>
        /// Shows a save file dialog and returns the selected file path, or null if cancelled
        /// </summary>
        /// <param name="defaultFileName">Default file name</param>
        /// <param name="filter">File filter (e.g., "PDF files (*.pdf)|*.pdf")</param>
        /// <param name="title">Dialog title</param>
        /// <returns>Selected file path or null if cancelled</returns>
        string? ShowSaveFileDialog(string defaultFileName, string filter, string title);

        /// <summary>
        /// R-093: Shows an open file dialog and returns the selected file path, or null if cancelled
        /// </summary>
        /// <param name="filter">File filter (e.g., "Excel files (*.xlsx)|*.xlsx")</param>
        /// <param name="title">Dialog title</param>
        /// <returns>Selected file path or null if cancelled</returns>
        string? ShowOpenFileDialog(string filter, string title);
    }
}
