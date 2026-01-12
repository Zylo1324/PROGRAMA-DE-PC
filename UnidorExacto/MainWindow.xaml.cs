using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
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
    private bool _isBusy;
    private string _statusMessage = string.Empty;
    private CancellationTokenSource? _cancellationTokenSource;

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

    public bool IsBusy
    {
        get => _isBusy;
        set
        {
            if (_isBusy == value)
            {
                return;
            }

            _isBusy = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsNotBusy));
        }
    }

    public bool IsNotBusy => !IsBusy;

    public string StatusMessage
    {
        get => _statusMessage;
        set
        {
            if (_statusMessage == value)
            {
                return;
            }

            _statusMessage = value;
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

    private async void MergeAndSave_Click(object sender, RoutedEventArgs e)
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
            IsBusy = true;
            StatusMessage = "Procesando...";
            _cancellationTokenSource = new CancellationTokenSource();
            var files = SelectedFiles.ToList();
            var keywords = Keywords.ToList();
            var options = new ProcessingOptions(AdvancedEnabled, AdvancedMode, IgnoreCase, keywords);
            await ProcessFilesAsync(saveDialog.FileName, files, options, _cancellationTokenSource.Token);
            MessageBox.Show("Archivos unidos correctamente.", "UnidorExacto",
                MessageBoxButton.OK, MessageBoxImage.Information);
            SelectedFiles.Clear();
        }
        catch (OperationCanceledException)
        {
            MessageBox.Show("Operación cancelada.", "UnidorExacto",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (IOException ex)
        {
            MessageBox.Show($"Error al unir archivos: {ex.Message}", "UnidorExacto",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error al unir archivos: {ex.Message}", "UnidorExacto",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            IsBusy = false;
            StatusMessage = string.Empty;
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

    private void CancelProcessing_Click(object sender, RoutedEventArgs e)
    {
        _cancellationTokenSource?.Cancel();
    }

    private Task ProcessFilesAsync(string destinationPath, IReadOnlyList<string> files, ProcessingOptions options, CancellationToken cancellationToken)
    {
        return Task.Run(() => ProcessFiles(destinationPath, files, options, cancellationToken), cancellationToken);
    }

    private void ProcessFiles(string destinationPath, IReadOnlyList<string> files, ProcessingOptions options, CancellationToken cancellationToken)
    {
        var treatAsCsv = files.All(file => string.Equals(Path.GetExtension(file), ".csv", StringComparison.OrdinalIgnoreCase));
        var comparison = options.IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        string? header = null;

        using var writer = new StreamWriter(destinationPath, false);

        foreach (var file in files)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var isFirstLine = true;

            foreach (var line in File.ReadLines(file))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (treatAsCsv && isFirstLine)
                {
                    if (header == null)
                    {
                        header = line;
                        writer.WriteLine(header);
                    }

                    isFirstLine = false;
                    continue;
                }

                isFirstLine = false;

                if (ShouldKeepLine(line, options, comparison))
                {
                    writer.WriteLine(line);
                }
            }
        }
    }

    private static bool ShouldKeepLine(string line, ProcessingOptions options, StringComparison comparison)
    {
        if (!options.AdvancedEnabled)
        {
            return true;
        }

        var containsKeyword = options.Keywords.Any(keyword => line.Contains(keyword, comparison));

        return options.AdvancedMode == AdvancedFilterMode.Filter ? containsKeyword : !containsKeyword;
    }

    private sealed record ProcessingOptions(
        bool AdvancedEnabled,
        AdvancedFilterMode AdvancedMode,
        bool IgnoreCase,
        IReadOnlyList<string> Keywords);

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
