using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;

namespace FlexMapExporter
{
    public partial class ExportDialog : Window
    {
        public string OutputPath { get; private set; } = string.Empty;

        public ExportDialog()
        {
            InitializeComponent();
            
            // Set default output path to user's Documents folder
            string documentsPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            OutputPath = System.IO.Path.Combine(documentsPath, "FlexMapExport");
            OutputPathTextBox.Text = OutputPath;
        }

        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select output folder for Flex-Map export";
                dialog.SelectedPath = OutputPath;
                dialog.ShowNewFolderButton = true;

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    OutputPath = dialog.SelectedPath;
                    OutputPathTextBox.Text = OutputPath;
                }
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(OutputPath))
            {
                System.Windows.MessageBox.Show(
                    "Please select an output folder.", 
                    "Output Folder Required", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Warning);
                return;
            }

            // Create output directory if it doesn't exist
            try
            {
                System.IO.Directory.CreateDirectory(OutputPath);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"Failed to create output folder:\n{ex.Message}", 
                    "Error", 
                    MessageBoxButton.OK, 
                    MessageBoxImage.Error);
                return;
            }

            DialogResult = true;
            // Don't close the window - let the export process update status and close when done
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public void UpdateStatus(string message)
        {
            // Update status on UI thread
            Dispatcher.Invoke(() =>
            {
                string timestamp = DateTime.Now.ToString("HH:mm:ss");
                StatusTextBlock.Text += $"[{timestamp}] {message}\n";
                
                // Auto-scroll to bottom
                var scrollViewer = FindVisualParent<ScrollViewer>(StatusTextBlock);
                scrollViewer?.ScrollToEnd();
            });
        }

        private static T? FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = System.Windows.Media.VisualTreeHelper.GetParent(child);

            if (parentObject == null) return null;

            if (parentObject is T parent)
                return parent;

            return FindVisualParent<T>(parentObject);
        }
    }
}
