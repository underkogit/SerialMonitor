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
    private CancellationTokenSource _cts;

    public MainWindow()
    {
        InitializeComponent();
        LogTextBox.AddHandler(KeyDownEvent, OnPreviewKeyDown, RoutingStrategies.Tunnel);
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

        _cts?.Cancel();
        _cts?.Dispose();
        _viewModel?.Dispose();
    }
}