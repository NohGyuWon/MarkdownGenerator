using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Clipboard = System.Windows.Clipboard;

namespace MarkdownGenerator
{
    public class MainViewModel : INotifyPropertyChanged
    {
        // Microsoft Naming Convention: private fields use _camelCase
        private string _selectedFolderPath = string.Empty;
        private string _generatedMarkdown = "폴더를 선택하고 '마크다운 생성' 버튼을 눌러주세요.";
        private string _statusMessage = "준비 완료";

        // INotifyPropertyChanged 구현을 위한 이벤트
        public event PropertyChangedEventHandler? PropertyChanged;

        // Microsoft Naming Convention: public properties use PascalCase
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

        // UI의 버튼과 연결될 커맨드(ICommand)
        public ICommand SelectFolderCommand { get; }
        public ICommand GenerateMarkdownCommand { get; }
        public ICommand CopyToClipboardCommand { get; }

        public MainViewModel()
        {
            // 커맨드 초기화
            SelectFolderCommand = new RelayCommand(SelectFolder);
            GenerateMarkdownCommand = new AsyncRelayCommand(GenerateMarkdownAsync, CanGenerate);
            CopyToClipboardCommand = new RelayCommand(CopyToClipboard, CanCopy);
        }

        /// <summary>
        /// '폴더 선택' 버튼의 실행 로직
        /// </summary>
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

        /// <summary>
        /// '마크다운 생성' 버튼의 비동기 실행 로직
        /// </summary>
        private async Task GenerateMarkdownAsync()
        {
            StatusMessage = "파일을 읽고 마크다운을 생성하는 중입니다...";
            var stringBuilder = new StringBuilder();

            try
            {
                // 지정된 폴더와 모든 하위 폴더의 파일 목록을 가져옵니다.
                var allFiles = Directory.GetFiles(SelectedFolderPath, "*.*", SearchOption.AllDirectories);

                await Task.Run(() =>
                {
                    foreach (var filePath in allFiles)
                    {
                        var fileInfo = new FileInfo(filePath);

                        // 파일 이름 추가
                        stringBuilder.AppendLine($"**`{fileInfo.Name}`**");

                        // 코드 블록 시작
                        stringBuilder.AppendLine($"```{GetLanguageIdentifier(fileInfo.Extension)}");

                        // 파일 내용 읽어서 추가
                        stringBuilder.AppendLine(File.ReadAllText(filePath));

                        // 코드 블록 종료
                        stringBuilder.AppendLine("```");
                        stringBuilder.AppendLine(); // 파일 간 구분을 위한 빈 줄
                    }
                });
                
                GeneratedMarkdown = stringBuilder.ToString();
                StatusMessage = $"{allFiles.Length}개의 파일 변환 완료!";
            }
            catch (Exception ex)
            {
                GeneratedMarkdown = $"오류가 발생했습니다: {ex.Message}";
                StatusMessage = "오류 발생";
            }
        }

        /// <summary>
        /// '마크다운 생성' 버튼의 활성화 조건
        /// </summary>
        private bool CanGenerate(object? parameter)
        {
            // 선택된 폴더 경로가 비어있지 않을 때만 버튼을 활성화합니다.
            return !string.IsNullOrEmpty(SelectedFolderPath);
        }

        /// <summary>
        /// '클립보드로 복사' 버튼의 실행 로직
        /// </summary>
        private void CopyToClipboard(object? parameter)
        {
            if (!string.IsNullOrEmpty(GeneratedMarkdown))
            {
                Clipboard.SetText(GeneratedMarkdown);
                StatusMessage = "클립보드에 복사되었습니다!";
            }
        }

        /// <summary>
        /// '클립보드로 복사' 버튼의 활성화 조건
        /// </summary>
        private bool CanCopy(object? parameter)
        {
            // 생성된 마크다운 텍스트가 있을 때만 버튼을 활성화합니다.
            return !string.IsNullOrEmpty(GeneratedMarkdown);
        }

        /// <summary>
        /// 파일 확장자에 따라 마크다운 언어 식별자를 반환합니다.
        /// </summary>
        private string GetLanguageIdentifier(string extension)
        {
            // Microsoft Naming Convention: local variables use camelCase
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
                _ => "" // 언어 식별자가 없으면 일반 텍스트로 처리됩니다.
            };
        }

        // INotifyPropertyChanged 인터페이스의 메서드 구현
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