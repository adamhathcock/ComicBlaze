using System;
using System.Linq;
using System.Threading.Tasks;
using Blazorise;
using Microsoft.AspNetCore.Components;

namespace ComicBlaze.Pages
{
    public partial class Index : BaseComponent
    {
        private string? _pageInfo;
        private string? _comicBytes;
        private ComicPageInfo? _currentInfo;

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
            await LoadPage((_currentInfo?.Index ?? 0)  + 1);
        }

        private async Task Previous()
        {
            await LoadPage((_currentInfo?.Index ?? 0)  +-1);
        }

        private async ValueTask LoadPage(int page)
        {
            if (_reader is not null)
            {
                var info = await _reader.GetPageByIndex(page);
                if (info is null)
                {
                    _currentInfo = null;
                    return;
                }
                _currentInfo = info;
                if (info.Page is null)
                {
                    _comicBytes = null;
                }
                else
                {
                    _comicBytes = $"data:image/jpg;base64,{info.Page}";
                }
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
                _reader.LoadingPage = s =>
                {
                    _pageInfo = s;
                    _loadingModal.Show();
                    StateHasChanged();
                };
                _reader.LoadedPage = s =>
                {
                    _loadingModal.Hide();
                    StateHasChanged();
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
    }
}