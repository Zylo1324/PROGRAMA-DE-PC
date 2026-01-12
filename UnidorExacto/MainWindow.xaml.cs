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
    private bool _transformStripSite;
    private bool _transformTrimWhitespace;
    private bool _transformRemoveEmptyLines;
    private bool _transformNormalizeSeparators;
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

    public bool TransformStripSite
    {
        get => _transformStripSite;
        set
        {
            if (_transformStripSite == value)
            {
                return;
            }

            _transformStripSite = value;
            OnPropertyChanged();
        }
    }

    public bool TransformTrimWhitespace
    {
        get => _transformTrimWhitespace;
        set
        {
            if (_transformTrimWhitespace == value)
            {
                return;
            }

            _transformTrimWhitespace = value;
            OnPropertyChanged();
        }
    }

    public bool TransformRemoveEmptyLines
    {
        get => _transformRemoveEmptyLines;
        set
        {
            if (_transformRemoveEmptyLines == value)
            {
                return;
            }

            _transformRemoveEmptyLines = value;
            OnPropertyChanged();
        }
    }

    public bool TransformNormalizeSeparators
    {
        get => _transformNormalizeSeparators;
        set
        {
            if (_transformNormalizeSeparators == value)
            {
                return;
            }

            _transformNormalizeSeparators = value;
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
            StatusMessage = "Preparando...";
            _cancellationTokenSource = new CancellationTokenSource();
            var progress = new Progress<ProgressInfo>(info =>
            {
                StatusMessage =
                    $"Procesando archivo {info.CurrentFileIndex}/{info.TotalFiles}. " +
                    $"Líneas leídas: {info.LinesRead:N0}. " +
                    $"Líneas escritas: {info.LinesWritten:N0}.";
            });
            await MergeAndSaveAsync(saveDialog.FileName, _cancellationTokenSource.Token, progress);
            MessageBox.Show("Archivos unidos correctamente.", "UnidorExacto",
                MessageBoxButton.OK, MessageBoxImage.Information);
            SelectedFiles.Clear();
        }
        catch (OperationCanceledException)
        {
            StatusMessage = "Cancelado.";
            MessageBox.Show("Cancelado.", "UnidorExacto",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (IOException ex)
        {
            MessageBox.Show($"Error al unir archivos: {ex.Message}", "UnidorExacto",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (UnauthorizedAccessException ex)
        {
            MessageBox.Show($"Acceso denegado: {ex.Message}", "UnidorExacto",
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

    private Task MergeAndSaveAsync(string outputPath, CancellationToken ct, IProgress<ProgressInfo> progress)
    {
        var files = SelectedFiles.ToList();
        var options = new ProcessingOptions(
            AdvancedEnabled,
            AdvancedMode,
            IgnoreCase,
            Keywords.ToList(),
            TransformStripSite,
            TransformTrimWhitespace,
            TransformRemoveEmptyLines,
            TransformNormalizeSeparators);

        return Task.Run(() => MergeAndSave(outputPath, files, options, ct, progress), ct);
    }

    private void MergeAndSave(string outputPath, IReadOnlyList<string> files, ProcessingOptions options, CancellationToken ct,
        IProgress<ProgressInfo> progress)
    {
        var comparison = options.IgnoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
        var keywords = options.Keywords
            .Select(keyword => keyword.Trim())
            .Where(keyword => !string.IsNullOrWhiteSpace(keyword))
            .Distinct(options.IgnoreCase ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal)
            .ToArray();
        var totalFiles = files.Count;
        var linesRead = 0L;
        var linesWritten = 0L;
        const int progressInterval = 5000;

        using var outFs = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 1024 * 1024,
            useAsync: false);
        using var writer = new StreamWriter(outFs);

        for (var fileIndex = 0; fileIndex < totalFiles; fileIndex++)
        {
            ct.ThrowIfCancellationRequested();
            progress.Report(new ProgressInfo(fileIndex + 1, totalFiles, linesRead, linesWritten));

            using var fs = new FileStream(files[fileIndex], FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 1024 * 1024,
                useAsync: false);
            using var reader = new StreamReader(fs, detectEncodingFromByteOrderMarks: true);

            string? line;
            while ((line = reader.ReadLine()) != null)
            {
                ct.ThrowIfCancellationRequested();
                linesRead++;

                if (options.TransformNormalizeSeparators)
                {
                    line = line.Replace('|', ':')
                        .Replace(';', ':')
                        .Replace(',', ':');
                }

                if (options.TransformTrimWhitespace)
                {
                    line = line.Trim();
                }

                if (options.TransformStripSite)
                {
                    if (!TryExtractCredential(line, out var extractedCredential))
                    {
                        continue;
                    }

                    line = extractedCredential;
                }

                if (options.AdvancedEnabled && keywords.Length > 0)
                {
                    var containsKeyword = keywords.Any(keyword => line.Contains(keyword, comparison));
                    if (options.AdvancedMode == AdvancedFilterMode.Filter ? !containsKeyword : containsKeyword)
                    {
                        continue;
                    }
                }

                if (options.TransformRemoveEmptyLines && line.Length == 0)
                {
                    continue;
                }

                writer.WriteLine(line);
                linesWritten++;

                if (linesRead % progressInterval == 0)
                {
                    progress.Report(new ProgressInfo(fileIndex + 1, totalFiles, linesRead, linesWritten));
                }
            }
        }

        progress.Report(new ProgressInfo(totalFiles, totalFiles, linesRead, linesWritten));
    }

    public sealed class ProgressInfo
    {
        public ProgressInfo(int currentFileIndex, int totalFiles, long linesRead, long linesWritten)
        {
            CurrentFileIndex = currentFileIndex;
            TotalFiles = totalFiles;
            LinesRead = linesRead;
            LinesWritten = linesWritten;
        }

        public int CurrentFileIndex { get; }
        public int TotalFiles { get; }
        public long LinesRead { get; }
        public long LinesWritten { get; }
    }

    private sealed record ProcessingOptions(
        bool AdvancedEnabled,
        AdvancedFilterMode AdvancedMode,
        bool IgnoreCase,
        IReadOnlyList<string> Keywords,
        bool TransformStripSite,
        bool TransformTrimWhitespace,
        bool TransformRemoveEmptyLines,
        bool TransformNormalizeSeparators);

    private static bool TryExtractCredential(string line, out string credential)
    {
        credential = string.Empty;
        if (string.IsNullOrWhiteSpace(line))
        {
            return false;
        }

        var parts = line.Split(':');
        if (parts.Length < 2)
        {
            return false;
        }

        for (var index = parts.Length - 2; index >= 0; index--)
        {
            var user = parts[index].Trim();
            var pass = parts[index + 1].Trim();

            if (!IsLikelyCredential(user, pass))
            {
                continue;
            }

            credential = $"{user}:{pass}";
            return true;
        }

        return false;
    }

    private static bool IsLikelyCredential(string user, string pass)
    {
        if (string.IsNullOrWhiteSpace(user) || string.IsNullOrWhiteSpace(pass))
        {
            return false;
        }

        if (user.Any(char.IsWhiteSpace) || pass.Any(char.IsWhiteSpace))
        {
            return false;
        }

        if (user.Contains('/') || user.Contains('\\') || pass.Contains('/') || pass.Contains('\\'))
        {
            return false;
        }

        if (IsLikelyEmail(user))
        {
            return pass.Length >= 2;
        }

        return IsLikelyUsername(user) && pass.Length >= 2;
    }

    private static bool IsLikelyEmail(string value)
    {
        var atIndex = value.IndexOf('@');
        if (atIndex <= 0 || atIndex == value.Length - 1)
        {
            return false;
        }

        var dotIndex = value.IndexOf('.', atIndex + 1);
        if (dotIndex <= atIndex + 1 || dotIndex >= value.Length - 1)
        {
            return false;
        }

        return true;
    }

    private static bool IsLikelyUsername(string value)
    {
        if (value.Length < 3)
        {
            return false;
        }

        if (value.Contains('/') || value.Contains('\\'))
        {
            return false;
        }

        var lower = value.ToLowerInvariant();
        if (lower.StartsWith("http") || lower.StartsWith("www."))
        {
            return false;
        }

        if (lower.Contains(".com/") || lower.Contains(".net/") || lower.Contains(".org/"))
        {
            return false;
        }

        return true;
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
