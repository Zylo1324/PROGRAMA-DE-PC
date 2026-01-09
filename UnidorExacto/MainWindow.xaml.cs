using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using Microsoft.Win32;

namespace UnidorExacto;

public partial class MainWindow : Window
{
    public ObservableCollection<string> SelectedFiles { get; } = new();

    public MainWindow()
    {
        InitializeComponent();
        DataContext = this;
    }

    private void AddFiles_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Multiselect = true,
            Title = "Seleccionar archivos"
        };

        if (dialog.ShowDialog() == true)
        {
            foreach (var file in dialog.FileNames)
            {
                if (!SelectedFiles.Contains(file))
                {
                    SelectedFiles.Add(file);
                }
            }
        }
    }

    private void MergeAndSave_Click(object sender, RoutedEventArgs e)
    {
        if (SelectedFiles.Count == 0)
        {
            MessageBox.Show("Selecciona al menos un archivo.", "UnidorExacto",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var saveDialog = new SaveFileDialog
        {
            Title = "Guardar archivo unido",
            FileName = "unido.txt",
            DefaultExt = ".txt",
            AddExtension = true,
            Filter = "Archivo de texto (*.txt)|*.txt|Todos los archivos (*.*)|*.*"
        };

        if (saveDialog.ShowDialog() != true)
        {
            return;
        }

        try
        {
            MergeFiles(saveDialog.FileName, SelectedFiles);
            MessageBox.Show("Archivos unidos correctamente.", "UnidorExacto",
                MessageBoxButton.OK, MessageBoxImage.Information);
            SelectedFiles.Clear();
        }
        catch (IOException ex)
        {
            MessageBox.Show($"Error al unir archivos: {ex.Message}", "UnidorExacto",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ClearFiles_Click(object sender, RoutedEventArgs e)
    {
        SelectedFiles.Clear();
    }

    private static void MergeFiles(string destinationPath, IReadOnlyList<string> files)
    {
        using var destination = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None);

        for (var index = 0; index < files.Count; index++)
        {
            if (index > 0 && NeedsSeparator(files[index - 1]))
            {
                var separator = new byte[] { (byte)'\r', (byte)'\n' };
                destination.Write(separator, 0, separator.Length);
            }

            using var source = new FileStream(files[index], FileMode.Open, FileAccess.Read, FileShare.Read);
            source.CopyTo(destination);
        }
    }

    private static bool NeedsSeparator(string previousFilePath)
    {
        using var previous = new FileStream(previousFilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        if (previous.Length == 0)
        {
            return true;
        }

        previous.Seek(-1, SeekOrigin.End);
        var lastByte = previous.ReadByte();
        return lastByte != '\n';
    }
}
