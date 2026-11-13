using IsoFile;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Windows;

namespace IsoPacker;

public partial class MainWindow : Window
{
    // 保存用户选择的文件或目录路径
    private readonly ObservableCollection<string> _selectedPaths = [];

    public MainWindow()
    {
        InitializeComponent();
        SelectedItemsListBox.ItemsSource = _selectedPaths;
    }

    private void SelectFilesButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "选择要打包的文件",
            Multiselect = true,
            Filter = "所有文件 (*.*)|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            foreach (var file in dialog.FileNames)
            {
                if (!_selectedPaths.Contains(file))
                    _selectedPaths.Add(file);
            }
        }
    }

    private void SelectFolderButton_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "选择要打包的文件夹",
            Multiselect = true
        };

        if (dialog.ShowDialog() == true)
        {
            foreach (string folder in dialog.FolderNames)
            {
                if (!_selectedPaths.Contains(folder))
                    _selectedPaths.Add(folder);
            }
        }
    }

    private void ClearButton_Click(object sender, RoutedEventArgs e)
    {
        _selectedPaths.Clear();
    }

    private async void PackIsoButton_Click(object sender, RoutedEventArgs e)
    {
        if (_selectedPaths.Count == 0)
        {
            MessageBox.Show("请先选择文件或文件夹。", "提示",
                MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        // 让用户选择 ISO 保存位置
        var saveDialog = new SaveFileDialog
        {
            Title = "保存 ISO 文件",
            Filter = "ISO 映像 (*.iso)|*.iso",
            DefaultExt = ".iso",
            FileName = "output.iso",
            AddExtension = true
        };

        if (saveDialog.ShowDialog() != true)
            return;

        string isoPath = saveDialog.FileName;

        PackIsoButton.IsEnabled = false;
        StatusTextBlock.Text = "正在打包…";

        try
        {
            // 在后台线程执行耗时操作，避免界面冻结
            await Task.Run(() => IsoBuilder.BuildIso(_selectedPaths, isoPath));

            StatusTextBlock.Text = "打包完成";
            MessageBox.Show($"ISO 文件已保存到：\n{isoPath}", "成功",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex)
        {
            StatusTextBlock.Text = "打包失败";
            MessageBox.Show($"打包过程中发生错误：\n{ex.Message}", "错误",
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            PackIsoButton.IsEnabled = true;
        }
    }
}