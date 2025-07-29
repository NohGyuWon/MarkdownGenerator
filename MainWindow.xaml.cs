using System.Windows;

namespace MarkdownGenerator
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // ViewModel을 생성하고 Window의 DataContext로 설정합니다.
            // 이렇게 하면 XAML에서 ViewModel의 속성과 커맨드에 바인딩할 수 있습니다.
            DataContext = new MainViewModel();
        }
    }
}