using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using FileBoy.App.ViewModels;
using System.Windows.Input;

namespace FileBoy.App.Pages;

/// <summary>
/// Interaction logic for BrowserPage.xaml
/// </summary>
public partial class BrowserPage : Page
{
    private MainViewModel ViewModel => (MainViewModel)DataContext;

    public BrowserPage(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        
        // Subscribe to scroll request from ViewModel
        viewModel.ScrollToSelectedItem = ScrollSelectedItemIntoView;
        
        // Ensure page can receive keyboard input
        Focusable = true;
        Loaded += BrowserPage_Loaded;
    }

    private void BrowserPage_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
        // Restore selection when returning from detail view
        ViewModel.RestoreSelection();
        
        // Ensure focus is set after everything is loaded
        // Multiple priority levels to ensure focus happens after scroll
        Dispatcher.InvokeAsync(() =>
        {
            SetFocusOnActiveControl();
        }, System.Windows.Threading.DispatcherPriority.Loaded);
        
        Dispatcher.InvokeAsync(() =>
        {
            SetFocusOnActiveControl();
        }, System.Windows.Threading.DispatcherPriority.Input);
    }

    /// <summary>
    /// Scrolls the currently selected item into view and sets focus on it.
    /// Called by ViewModel when selection is restored or changed programmatically.
    /// </summary>
    private void ScrollSelectedItemIntoView()
    {
        if (ViewModel.SelectedItem == null)
            return;

        // Use multiple dispatcher calls to ensure proper timing
        Dispatcher.InvokeAsync(() =>
        {
            if (FileListGrid.Visibility == System.Windows.Visibility.Visible)
            {
                FileListGrid.ScrollIntoView(ViewModel.SelectedItem);
                FileListGrid.UpdateLayout();
                
                // Wait for container to be generated, then focus
                Dispatcher.InvokeAsync(() =>
                {
                    var row = FileListGrid.ItemContainerGenerator.ContainerFromItem(ViewModel.SelectedItem) as DataGridRow;
                    if (row != null)
                    {
                        row.Focus();
                        System.Windows.Input.Keyboard.Focus(row);
                    }
                    else
                    {
                        // Container not ready, just focus the grid
                        FileListGrid.Focus();
                    }
                }, System.Windows.Threading.DispatcherPriority.Input);
            }
            else if (ThumbnailList.Visibility == System.Windows.Visibility.Visible)
            {
                ThumbnailList.ScrollIntoView(ViewModel.SelectedItem);
                ThumbnailList.UpdateLayout();
                
                // Wait for container to be generated, then focus
                Dispatcher.InvokeAsync(() =>
                {
                    var listBoxItem = ThumbnailList.ItemContainerGenerator.ContainerFromItem(ViewModel.SelectedItem) as ListBoxItem;
                    if (listBoxItem != null)
                    {
                        listBoxItem.Focus();
                        System.Windows.Input.Keyboard.Focus(listBoxItem);
                    }
                    else
                    {
                        // Container not ready, just focus the listbox
                        ThumbnailList.Focus();
                    }
                }, System.Windows.Threading.DispatcherPriority.Input);
            }
        }, System.Windows.Threading.DispatcherPriority.Loaded);
    }

    /// <summary>
    /// Sets focus on the active view control (DataGrid or ListBox).
    /// </summary>
    private void SetFocusOnActiveControl()
    {
        if (FileListGrid.Visibility == System.Windows.Visibility.Visible)
        {
            FileListGrid.Focus();
        }
        else if (ThumbnailList.Visibility == System.Windows.Visibility.Visible)
        {
            ThumbnailList.Focus();
        }
    }

    private void PathBox_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            var textBox = (TextBox)sender;
            ViewModel.NavigateToPathCommand.Execute(textBox.Text);
            e.Handled = true;
        }
    }

    private async void FileListGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ViewModel.SelectedItem != null)
        {
            await ViewModel.OpenItemAsync(ViewModel.SelectedItem);
        }
    }

    private async void ThumbnailList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ViewModel.SelectedItem != null)
        {
            await ViewModel.OpenItemAsync(ViewModel.SelectedItem);
        }
    }

    private void GridSplitter_DragCompleted(object sender, DragCompletedEventArgs e)
    {
        // Save the new preview panel width when user finishes dragging
        ViewModel.PreviewPanelWidth = PreviewColumn.ActualWidth;
    }

    private void FileListGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Sync selected items to ViewModel for multi-selection support
        if (sender is DataGrid grid)
        {
            ViewModel.SelectedItems = grid.SelectedItems;
        }
    }

    private void ThumbnailList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // Sync selected items to ViewModel for multi-selection support
        if (sender is ListBox listBox)
        {
            ViewModel.SelectedItems = listBox.SelectedItems;
        }
    }

    private async void FileListGrid_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && ViewModel.SelectedItem != null)
        {
            await ViewModel.OpenItemAsync(ViewModel.SelectedItem);
            e.Handled = true;
        }
    }

    private async void ThumbnailList_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && ViewModel.SelectedItem != null)
        {
            await ViewModel.OpenItemAsync(ViewModel.SelectedItem);
            e.Handled = true;
        }
    }
}
