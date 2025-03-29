using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.VisualTree;

namespace AudioConsolidator.Views;

public partial class DragAndDropPage : UserControl
{
    private readonly TextBlock _dropState;
    private const string CustomFormat = "application/xxx-avalonia-controlcatalog-custom";
    public DragAndDropPage()
    {
        InitializeComponent();
        _dropState = this.Get<TextBlock>("DropState");

        int textCount = 0;

        SetupDnd(
            "Text",
            d => d.Set(DataFormats.Text, $"Text was dragged {++textCount} times"),
            DragDropEffects.Copy | DragDropEffects.Move | DragDropEffects.Link);

        SetupDnd(
            "Custom",
            d => d.Set(CustomFormat, "Test123"),
            DragDropEffects.Move);

        SetupDnd(
            "Files",
            async d =>
            {
                if (Assembly.GetEntryAssembly()?.GetModules().FirstOrDefault()?.FullyQualifiedName is { } name &&
                    TopLevel.GetTopLevel(this) is { } topLevel &&
                    await topLevel.StorageProvider.TryGetFileFromPathAsync(name) is { } storageFile)
                {
                    d.Set(DataFormats.Files, new[] { storageFile });
                }
            },
            DragDropEffects.Copy);
    }

    private void SetupDnd(string suffix, Action<DataObject> factory, DragDropEffects effects) =>
        SetupDnd(
            suffix,
            o =>
            {
                factory(o);
                return Task.CompletedTask;
            },
            effects);

    private void SetupDnd(string suffix, Func<DataObject, Task> factory, DragDropEffects effects)
    {
        var dragMe = this.Get<Border>("DragMe" + suffix);
        var dragState = this.Get<TextBlock>("DragState" + suffix);

        async void DoDrag(object? sender, PointerPressedEventArgs e)
        {
            var dragData = new DataObject();
            await factory(dragData);

            var result = await DragDrop.DoDragDrop(e, dragData, effects);
            switch (result)
            {
                case DragDropEffects.Move:
                    dragState.Text = "Data was moved";
                    break;
                case DragDropEffects.Copy:
                    dragState.Text = "Data was copied";
                    break;
                case DragDropEffects.Link:
                    dragState.Text = "Data was linked";
                    break;
                case DragDropEffects.None:
                    dragState.Text = "The drag operation was canceled";
                    break;
                default:
                    dragState.Text = "Unknown result";
                    break;
            }
        }

        void DragOver(object? sender, DragEventArgs e)
        {
            if (e.Source is Control c && c.Name == "MoveTarget")
            {
                e.DragEffects &= (DragDropEffects.Move);

                if (c.FindDescendantOfType<TextBlock>() is { } text) {
                    text.Text = "dragging over!";
                }
            }
            else
            {
                e.DragEffects = e.DragEffects & (DragDropEffects.Copy);
            }

            // Only allow if the dragged data contains text or filenames.
            if (!e.Data.Contains(DataFormats.Text)
                && !e.Data.Contains(DataFormats.Files)
                && !e.Data.Contains(CustomFormat))
                e.DragEffects = DragDropEffects.None;
        }

        async void Drop(object? sender, DragEventArgs e)
        {
            if (e.Source is Control c && c.Name == "MoveTarget")
            {
                e.DragEffects = e.DragEffects & (DragDropEffects.Move);
                if (c.FindDescendantOfType<TextBlock>() is { } text) {
                    text.Text = "dragged";
                }
            }
            else
            {
                e.DragEffects = e.DragEffects & (DragDropEffects.Copy);
            }

            if (e.Data.Contains(DataFormats.Text))
            {
                _dropState.Text = e.Data.GetText();
            }
            else if (e.Data.Contains(DataFormats.Files))
            {
                var files = e.Data.GetFiles() ?? Array.Empty<IStorageItem>();
                var contentStr = "";

                foreach (var item in files)
                {
                    if (item is IStorageFile file)
                    {
                        var content = await File.ReadAllTextAsync(file.TryGetLocalPath()!);
                        contentStr += $"File {item.Name}:{Environment.NewLine}{content}{Environment.NewLine}{Environment.NewLine}";
                    }
                    else if (item is IStorageFolder folder)
                    {
                        var childrenCount = 0;
                        await foreach (var _ in folder.GetItemsAsync())
                        {
                            childrenCount++;
                        }
                        contentStr += $"Folder {item.Name}: items {childrenCount}{Environment.NewLine}{Environment.NewLine}";
                    }
                }

                _dropState.Text = contentStr;
            }
#pragma warning disable CS0618 // Type or member is obsolete
            else if (e.Data.Contains(DataFormats.FileNames))
            {
                var files = e.Data.GetFileNames();
                _dropState.Text = string.Join(Environment.NewLine, files ?? Array.Empty<string>());
            }
#pragma warning restore CS0618 // Type or member is obsolete
            else if (e.Data.Contains(CustomFormat))
            {
                _dropState.Text = "Custom: " + e.Data.Get(CustomFormat);
            }
        }

        dragMe.PointerPressed += DoDrag;

        AddHandler(DragDrop.DropEvent, Drop);
        AddHandler(DragDrop.DragOverEvent, DragOver);
        AddHandler(DragDrop.DragLeaveEvent, Handler);
        return;

        void Handler(object? sender, DragEventArgs e) {
            if (e.Source is not Control c || c.Name != "MoveTarget") return;
            if (c.FindDescendantOfType<TextBlock>() is { } text) {
                text.Text = "left :<";
            }
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}