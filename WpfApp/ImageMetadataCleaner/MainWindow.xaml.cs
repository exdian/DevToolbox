using ImageFunction;
using Microsoft.Win32;
using System.IO;
using System.Windows;
using System.Windows.Media;

namespace ImageMetadataCleaner;

public partial class MainWindow : Window
{
    private Brush ErrorBrush { get; } = new SolidColorBrush(Color.FromRgb(0xC0, 0x2B, 0x2B));
    private Brush SuccessBrush { get; } = new SolidColorBrush(Color.FromRgb(0x2E, 0x7D, 0x32));
    private Brush InfoBrush { get; } = new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66));

    public MainWindow()
    {
        InitializeComponent();
    }

    // ---------- 默认输出路径 ----------
    private static string BuildDefaultOutput(string? input)
    {
        string dir = string.IsNullOrWhiteSpace(input)
            ? AppContext.BaseDirectory
            : Path.GetDirectoryName(Path.GetFullPath(input)) ?? AppContext.BaseDirectory;

        return Path.Combine(dir, $"Image {DateTime.Now:yyyy-MM-dd HH-mm-ss}.png");
    }

    // ---------- 浏览输入 ----------
    private void BrowseInput_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "选择图片",
            Filter = "图片文件|*.png;*.jpg;*.jpeg;*.bmp;*.gif;*.tif;*.tiff;*.webp|所有文件|*.*"
        };
        if (dlg.ShowDialog(this) == true)
        {
            InputBox.Text = dlg.FileName;
            if (string.IsNullOrWhiteSpace(OutputBox.Text))
                OutputBox.Text = BuildDefaultOutput(dlg.FileName);
        }
    }

    // ---------- 浏览输出 ----------
    private void BrowseOutput_Click(object sender, RoutedEventArgs e)
    {
        string? suggested = string.IsNullOrWhiteSpace(InputBox.Text)
            ? null
            : BuildDefaultOutput(InputBox.Text);

        var dlg = new SaveFileDialog
        {
            Title = "保存为 PNG",
            Filter = "PNG 图片|*.png",
            DefaultExt = ".png",
            AddExtension = true,
            OverwritePrompt = true,
            FileName = suggested is null ? null : Path.GetFileName(suggested),
            InitialDirectory = suggested is null ? null : Path.GetDirectoryName(suggested)
        };

        if (dlg.ShowDialog(this) == true)
            OutputBox.Text = dlg.FileName;
    }

    // ---------- 执行转换 ----------
    private void Convert_Click(object sender, RoutedEventArgs e)
    {
        string input = InputBox.Text?.Trim() ?? string.Empty;
        string output = OutputBox.Text?.Trim() ?? string.Empty;

        if (string.IsNullOrEmpty(input))
        {
            ShowError("请先选择输入图片");
            return;
        }

        if (!File.Exists(input))
        {
            ShowError($"错误：文件不存在 - {input}");
            return;
        }

        if (string.IsNullOrEmpty(output))
        {
            output = BuildDefaultOutput(input);
            OutputBox.Text = output;
        }

        try
        {
            ImageSave.SaveAsPng(input, output);
            ShowSuccess($"✔ 已保存：{output}");
        }
        catch (Exception ex)
        {
            ShowError($"✘ {ex.Message}");
        }
    }

    // ---------- 拖拽支持（使用 Preview 隧道事件，避免被 TextBox 拦截） ----------
    private void Window_PreviewDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true; // 阻止 TextBox 再插手，光标才会变成可拖拽
    }

    private void Window_Drop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (files.Length > 0)
            {
                InputBox.Text = files[0];
                OutputBox.Text = BuildDefaultOutput(files[0]);
                ShowInfo("已载入文件，点击「转换并保存」开始");
            }
        }
    }

    // ---------- 小工具 ----------
    private void InputBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
    {
        // 输入框变化时如果输出为空,给一个建议路径
        if (string.IsNullOrWhiteSpace(OutputBox.Text) &&
            !string.IsNullOrWhiteSpace(InputBox.Text) &&
            File.Exists(InputBox.Text))
        {
            OutputBox.Text = BuildDefaultOutput(InputBox.Text);
        }
    }

    private void ShowInfo(string msg)
    {
        StatusText.Foreground = InfoBrush;
        StatusText.Text = msg;
    }

    private void ShowError(string msg)
    {
        StatusText.Foreground = ErrorBrush;
        StatusText.Text = msg;
    }

    private void ShowSuccess(string msg)
    {
        StatusText.Foreground = SuccessBrush;
        StatusText.Text = msg;
    }
}