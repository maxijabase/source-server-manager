using Avalonia.Controls;
using Avalonia.Platform.Storage;
using System.Threading.Tasks;

namespace SourceServerManager.Services;

public class FilesService
{
    private readonly Window _window;

    public FilesService(Window window)
    {
        _window = window;
    }

    public async Task<IStorageFile?> OpenFileAsync()
    {
        var files = await _window.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Select File to Upload",
            AllowMultiple = false
        });

        return files.Count >= 1 ? files[0] : null;
    }

    public async Task<IStorageFolder?> OpenFolderAsync()
    {
        var folders = await _window.StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Select Folder to Upload",
            AllowMultiple = false
        });

        return folders.Count >= 1 ? folders[0] : null;
    }

    public async Task<IStorageFile?> SaveFileAsync()
    {
        return await _window.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save File"
        });
    }
}