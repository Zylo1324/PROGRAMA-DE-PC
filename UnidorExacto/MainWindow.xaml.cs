using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using Microsoft.Win32;

namespace UnidorExacto;

public enum AdvancedFilterMode
{
    Filter,
    Remove
}

public partial class MainWindow : Window, INotifyPropertyChanged
{
    public ObservableCollection<string> SelectedFiles { get; } = new();
    public ObservableCollection<string> Keywords { get; } = new();

    private bool _advancedEnabled;
    private AdvancedFilterMode _advancedMode = AdvancedFilterMode.Filter;
    private bool _ignoreCase = true;
    private string _keywordInput = string.Empty;

    public bool AdvancedEnabled
    {
        get => _advancedEnabled;
        set
        {
            if (_advancedEnabled == value)
            {
                return;
            }

            _advancedEnabled = value;
            OnPropertyChanged();
        }
    }

    public AdvancedFilterMode AdvancedMode
    {
        get => _advancedMode;
        set
        {
            if (_advancedMode == value)
            {
                return;
            }

            _advancedMode = value;
            OnPropertyChanged();
        }
    }

    public bool IgnoreCase
    {
        get => _ignoreCase;
        set
        {
            if (_ignoreCase == value)
            {
                return;
            }

            _ignoreCase = value;
            OnPropertyChanged();
        }
    }

    public string KeywordInput
    {
        get => _keywordInput;
        set
        {
            if (_keywordInput == value)
            {
                return;
            }

            _keywordInput = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

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

        if (AdvancedEnabled && Keywords.Count == 0)
        {
            MessageBox.Show("Agrega al menos una palabra para el modo avanzado.", "UnidorExacto",
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

    private void AddKeyword_Click(object sender, RoutedEventArgs e)
    {
        var keyword = KeywordInput.Trim();
        if (string.IsNullOrWhiteSpace(keyword))
        {
            MessageBox.Show("Ingresa una palabra válida.", "UnidorExacto",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var comparison = IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        if (Keywords.Any(existing => string.Equals(existing, keyword, comparison)))
        {
            MessageBox.Show("La palabra ya existe en la lista.", "UnidorExacto",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        Keywords.Add(keyword);
        KeywordInput = string.Empty;
    }

    private void RemoveKeyword_Click(object sender, RoutedEventArgs e)
    {
        if (sender is FrameworkElement { DataContext: string keyword })
        {
            Keywords.Remove(keyword);
        }
    }

    private void MergeFiles(string destinationPath, IReadOnlyList<string> files)
    {
        var allLines = new List<string>();
        var treatAsCsv = files.All(file => string.Equals(Path.GetExtension(file), ".csv", StringComparison.OrdinalIgnoreCase));
        string? header = null;

        foreach (var file in files)
        {
            var lines = File.ReadAllLines(file);
            if (lines.Length == 0)
            {
                continue;
            }

            var startIndex = 0;
            if (treatAsCsv)
            {
                if (header == null)
                {
                    header = lines[0];
                    allLines.Add(header);
                }

                startIndex = 1;
            }

            for (var index = startIndex; index < lines.Length; index++)
            {
                var line = lines[index];
                if (ShouldKeepLine(line))
                {
                    allLines.Add(line);
                }
            }
        }

        File.WriteAllLines(destinationPath, allLines);
    }

    private bool ShouldKeepLine(string line)
    {
        if (!AdvancedEnabled)
        {
            return true;
        }

        var comparison = IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var containsKeyword = Keywords.Any(keyword => line.Contains(keyword, comparison));

        return AdvancedMode == AdvancedFilterMode.Filter ? containsKeyword : !containsKeyword;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
