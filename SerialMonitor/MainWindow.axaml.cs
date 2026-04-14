using System;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using AvaloniaEdit.Document;
using AvaloniaEdit.Highlighting;
using SerialMonitor.Enumes;
using SerialMonitor.Helper;
using SerialMonitor.Services;
using SerialMonitor.Structures;
using SerialMonitor.ViewModels;

namespace SerialMonitor;

public partial class MainWindow : Window
{
    private MainWindowViewModel? _viewModel;


    public MainWindow()
    {
        InitializeComponent();
        this.Loaded += OnLoadAsync;
        LogTextBox.AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
    }

    private async void OnLoadAsync(object? sender, RoutedEventArgs routedEventArgs)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            await viewModel.InitializeAsync();
        }
    }

    private void OnPreviewKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            e.Handled = true;
            if (DataContext is MainWindowViewModel viewModel)
            {
                viewModel.EnterPressedCommand.Execute(null);
            }
        }
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
            _viewModel = new MainWindowViewModel();
            _viewModel.Editor = Editor;
            DataContext = _viewModel;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error initializing MainWindow: {ex.Message}");
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
        {
            viewModel.IsLogPanelVisible = !viewModel.IsLogPanelVisible;
        }
    }

    private void Button_OnClickSendMessage(object? sender, RoutedEventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.SendMessageComPort(LogTextBox.Text);
            LogTextBox.Text = String.Empty;
        }
    }

    private void Button_OnClickSelectMessageItem(object? sender, RoutedEventArgs e)
    {
        LogTextBox.Text = (sender as Button)?.Content as string;
    }

    private void Button_OnClickRemoveItem(object? sender, RoutedEventArgs e)
    {
        var button = sender as Button;

        var border = button?.Parent?.Parent as Border;
        var itemToRemove = border?.DataContext as string;

        if (itemToRemove != null && DataContext is MainWindowViewModel vm)
        {
            vm.ListCommands.Remove(itemToRemove);
            vm.SaveListCommands();
        }
    }

    private void Button_OnClickSelectSendCommand(object? sender, RoutedEventArgs e)
    {
        var button = sender as Button;
        var border = button?.Parent?.Parent as Border;
        var itemToRemove = border?.DataContext as string;

        if (DataContext is MainWindowViewModel viewModel &&
            !string.IsNullOrEmpty(itemToRemove))
        {
            viewModel.SendMessageComPort(itemToRemove);
        }
    }
}