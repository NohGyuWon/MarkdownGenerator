using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Clipboard = System.Windows.Clipboard;

namespace MarkdownGenerator
{
    // 추가된 GenType 열거형
    public enum GenType
    {
        Cs,
        Cpp,
        Mssql,
        Mysql,
        Sqllite,
        Proto,
        Rust
    }

    public class MainViewModel : INotifyPropertyChanged
    {
        private string _selectedFolderPath = string.Empty;
        private string _generatedMarkdown = "폴더를 선택하고 생성 타입을 지정한 후 '마크다운 생성' 버튼을 눌러주세요.";
        private string _statusMessage = "준비 완료";
        private GenType _selectedGenType = GenType.Cs; // GenType 속성 추가 및 기본값 설정

        public event PropertyChangedEventHandler? PropertyChanged;

        public string SelectedFolderPath
        {
            get => _selectedFolderPath;
            set { _selectedFolderPath = value; OnPropertyChanged(); }
        }

        public string GeneratedMarkdown
        {
            get => _generatedMarkdown;
            set { _generatedMarkdown = value; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        // GenType 속성 추가
        public GenType SelectedGenType
        {
            get => _selectedGenType;
            set
            {
                _selectedGenType = value;
                OnPropertyChanged();
                // GenType이 변경되면 상태 메시지 업데이트 (선택적)
                StatusMessage = $"생성 타입이 '{_selectedGenType}'로 변경되었습니다.";
            }
        }

        public ICommand SelectFolderCommand { get; }
        public ICommand GenerateMarkdownCommand { get; }
        public ICommand CopyToClipboardCommand { get; }

        public MainViewModel()
        {
            SelectFolderCommand = new RelayCommand(SelectFolder);
            GenerateMarkdownCommand = new AsyncRelayCommand(GenerateMarkdownAsync, CanGenerate);
            CopyToClipboardCommand = new RelayCommand(CopyToClipboard, CanCopy);
        }

        private void SelectFolder(object? parameter)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog
            {
                Description = "코드를 변환할 폴더를 선택하세요.",
                UseDescriptionForTitle = true
            };

            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                SelectedFolderPath = dialog.SelectedPath;
                StatusMessage = $"폴더가 선택되었습니다: {SelectedFolderPath}";
            }
        }

        private async Task GenerateMarkdownAsync()
        {
            StatusMessage = "파일을 읽고 마크다운을 생성하는 중입니다...";
            var stringBuilder = new StringBuilder();
            var fileFilter = GetFileFilterForGenType();

            try
            {
                var directoryInfo = new DirectoryInfo(SelectedFolderPath);

                // 1. 루트 폴더의 파일 처리
                var rootFiles = directoryInfo.GetFiles(fileFilter, SearchOption.TopDirectoryOnly).OrderBy(f => f.Name);
                foreach (var file in rootFiles)
                {
                    await AppendFileContentAsync(stringBuilder, file.FullName);
                }

                // 2. 하위 폴더 순회하며 파일 처리
                var subDirectories = directoryInfo.GetDirectories("*", SearchOption.AllDirectories).OrderBy(d => d.FullName);
                foreach (var dir in subDirectories)
                {
                    var filesInDir = dir.GetFiles(fileFilter, SearchOption.TopDirectoryOnly).OrderBy(f => f.Name);
                    if (filesInDir.Any())
                    {
                        // 하위 폴더 경로 추가
                        stringBuilder.AppendLine($"### `{dir.FullName.Replace(SelectedFolderPath, "").TrimStart('\\')}`");
                        stringBuilder.AppendLine();

                        foreach (var file in filesInDir)
                        {
                            await AppendFileContentAsync(stringBuilder, file.FullName);
                        }
                    }
                }

                GeneratedMarkdown = stringBuilder.ToString();
                StatusMessage = "마크다운 생성 완료!";
            }
            catch (Exception ex)
            {
                GeneratedMarkdown = $"오류가 발생했습니다: {ex.Message}";
                StatusMessage = "오류 발생";
            }
        }

        /// <summary>
        /// 파일 내용을 읽고 StringBuilder에 추가하는 보조 메서드
        /// </summary>
        private async Task AppendFileContentAsync(StringBuilder sb, string filePath)
        {
            var fileInfo = new FileInfo(filePath);
            sb.AppendLine($"**`{fileInfo.Name}`**");
            sb.AppendLine($"```{GetLanguageIdentifier(fileInfo.Extension)}");
            sb.AppendLine(await File.ReadAllTextAsync(filePath));
            sb.AppendLine("```");
            sb.AppendLine();
        }

        /// <summary>
        /// 선택된 GenType에 맞는 파일 필터(*.cs 등)를 반환합니다.
        /// </summary>
        private string GetFileFilterForGenType()
        {
            return SelectedGenType switch
            {
                GenType.Cs => "*.cs",
                GenType.Cpp => "*.cpp", // .h도 필요한 경우 로직 추가 필요
                GenType.Mssql => "*.sql",
                GenType.Mysql => "*.sql",
                GenType.Sqllite => "*.sql", // 또는 .db
                GenType.Proto => "*.proto",
                GenType.Rust => "*.rs",
                _ => "*.*"
            };
        }

        private bool CanGenerate(object? parameter)
        {
            return !string.IsNullOrEmpty(SelectedFolderPath);
        }

        private void CopyToClipboard(object? parameter)
        {
            if (!string.IsNullOrEmpty(GeneratedMarkdown))
            {
                Clipboard.SetText(GeneratedMarkdown);
                StatusMessage = "클립보드에 복사되었습니다!";
            }
        }

        private bool CanCopy(object? parameter)
        {
            return !string.IsNullOrEmpty(GeneratedMarkdown);
        }

        private string GetLanguageIdentifier(string extension)
        {
            string lowerExtension = extension.ToLower();
            return lowerExtension switch
            {
                ".cs" => "csharp",
                ".xaml" => "xml",
                ".xml" => "xml",
                ".csproj" => "xml",
                ".json" => "json",
                ".js" => "javascript",
                ".html" => "html",
                ".css" => "css",
                ".py" => "python",
                ".java" => "java",
                ".cpp" => "cpp",
                ".h" => "cpp",
                ".md" => "markdown",
                ".txt" => "text",
                ".sql" => "sql",
                ".proto" => "protobuf",
                ".rs" => "rust",
                _ => ""
            };
        }

        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    #region "Command Implementations"
    // ICommand 인터페이스의 기본 구현체 (RelayCommand 패턴)
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Predicate<object?>? _canExecute;

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute == null || _canExecute(parameter);
        public void Execute(object? parameter) => _execute(parameter);
    }

    // 비동기 작업을 위한 ICommand 구현체
    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Predicate<object?>? _canExecute;
        private bool _isExecuting;

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public AsyncRelayCommand(Func<Task> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            return !_isExecuting && (_canExecute == null || _canExecute(parameter));
        }

        public async void Execute(object? parameter)
        {
            _isExecuting = true;
            CommandManager.InvalidateRequerySuggested();
            try
            {
                await _execute();
            }
            finally
            {
                _isExecuting = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }
    #endregion
}
