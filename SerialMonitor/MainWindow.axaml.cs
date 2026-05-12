using System;
using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using AvaloniaEdit.Highlighting;
using SerialMonitor.Services;
using SerialMonitor.ViewModels;

namespace SerialMonitor;

public partial class MainWindow : Window
{
    private MainWindowViewModel? _viewModel;

    public MainWindow()
    {
        InitializeComponent();
        LogTextBox.AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
    }

    private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter) return;

        e.Handled = true;
        if (DataContext is MainWindowViewModel viewModel)
            viewModel.EnterPressedCommand.Execute(null);
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        CustomHighlightingManager.RegisterAllHighlightings();
        Editor.SyntaxHighlighting = HighlightingManager.Instance.GetDefinition("LOG");
        Editor.Options.IndentationSize = 4;
        Editor.Options.ConvertTabsToSpaces = true;
        Editor.Options.EnableHyperlinks = true;
        Editor.Options.EnableEmailHyperlinks = true;

        try
        {
            _viewModel = new MainWindowViewModel { Editor = Editor };
            DataContext = _viewModel;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error initializing MainWindow: {ex.Message}");
        }
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);
        _viewModel?.Dispose();
    }

    private void Button_OnClickShowListMessages(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
            viewModel.IsLogPanelVisible = !viewModel.IsLogPanelVisible;
    }

    private void Button_OnClickSendMessage(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.SendMessageComPort(LogTextBox.Text);
            LogTextBox.Text = string.Empty;
        }
    }

    private void Button_OnClickSelectMessageItem(object? sender, RoutedEventArgs e)
    {
        LogTextBox.Text = (sender as Button)?.Content as string;
    }

    private void Button_OnClickRemoveItem(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;

        var itemToRemove = (button.Parent?.Parent as Border)?.DataContext as string;

        if (itemToRemove != null && DataContext is MainWindowViewModel vm)
        {
            vm.ListCommands.Remove(itemToRemove);
            vm.SaveListCommands();
        }
    }

    private void Button_OnClickSelectSendCommand(object? sender, RoutedEventArgs e)
    {
        if (sender is not Button button) return;

        var command = (button.Parent?.Parent as Border)?.DataContext as string;

        if (DataContext is MainWindowViewModel viewModel && !string.IsNullOrEmpty(command))
            viewModel.SendMessageComPort(command);
    }
}