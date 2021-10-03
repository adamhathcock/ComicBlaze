using System;
using System.Linq;
using System.Threading.Tasks;
using Blazorise;
using ComicBlaze.Components;
using Microsoft.AspNetCore.Components;

namespace ComicBlaze.Pages
{
    public partial class Index : BaseComponent
    {
        private string? _pageInfo;
        private string? _comicBytes;
        private ComicReaderPage? _currentInfo;

        private Modal _loadingModal = default!;
        private Modal _pagesModal = default!;
        private Modal _openModal = default!;
        private Modal _aboutModal = default!;
        private ComicReader? _reader;
        private ElementReference _inputTypeFileElement;

        public async Task Home()
        {
            await LoadPage(0);
        }

        private async Task Next()
        {
            await InvokeAsync(() =>
            {
                StateHasChanged();
                LoadPage((_currentInfo?.Index ?? 0) + 1);
            });
            await Task.Yield();
        }

        private async Task Previous()
        {
            await InvokeAsync(() =>
            {
                StateHasChanged();
                LoadPage((_currentInfo?.Index ?? 0) - 1);
            });
            await Task.Yield();
        }
        
        private async ValueTask LoadPage(ComicPageIndex page)
        {
            await LoadPage(_reader!.GetPage(page));
        } 

        private async ValueTask LoadPage(int page)
        {
            await LoadPage(_reader!.GetPageByIndex(page));
        }
        
        private async ValueTask LoadPage(ValueTask<ComicReaderPage?> loader)
        {
            if (_reader is not null)
            {
                StateHasChanged();
                await Task.Yield();
                var info = await loader;
                if (info is null)
                {
                    _currentInfo = null;
                    return;
                }
                _currentInfo = info;
                Console.WriteLine($"Switching bytes to {info.Name}");
                if (info.Page is null)
                {
                    _comicBytes = null;
                }
                else
                {
                    _comicBytes = $"data:image/jpg;base64,{info.Page}";
                }
                StateHasChanged();
                await Task.Yield();
            }
        }

        private async Task ReadFile()
        {
            var file = (await FileReaderService.CreateReference(_inputTypeFileElement).EnumerateFilesAsync()).FirstOrDefault();
            if (file is null)
            {
                return;
            }
            try
            {
                var stream = await file.CreateMemoryStreamAsync();
                _reader = new ComicReader(stream);
                _reader.LoadingPage = async s =>
                {
                    Console.WriteLine($"Loading {s}");
                    _pageInfo = s;
                    _loadingModal.Show();
                    StateHasChanged();
                    await Task.Yield();
                    Console.WriteLine($"Loading2 {s}");
                };
                _reader.LoadedPage = async s =>
                {
                    Console.WriteLine($"Loaded {s}");
                    _loadingModal.Hide();
                    StateHasChanged();
                    await Task.Yield();
                    Console.WriteLine($"Loaded2 {s}");
                };
                await Home();
                _openModal.Hide();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private void PagesList()
        {
            _pagesModal.Show();
        }
        private void Open()
        {
            _openModal.Show();
        }
        private void About()
        {
            _aboutModal.Show();
        }
        
        private async Task PageClicked(ComicPageIndex page)
        {
            _pagesModal.Hide();
            await InvokeAsync(() =>
            {
                StateHasChanged();
                LoadPage(page);
            });
            await Task.Yield();
        }
    }
}