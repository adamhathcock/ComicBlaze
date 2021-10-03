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

        private Modal _loadingModal = default!;
        private Modal _pagesModal = default!;
        private Modal _openModal = default!;
        private ComicReader? _reader;
        private ElementReference _inputTypeFileElement;
        
        
        private int _currentPage;

        public async Task Home()
        {
            await LoadPage(0);
            _currentPage = 0;
        }

        private async Task Next()
        {
            await LoadPage(++_currentPage);
        }

        private async Task Previous()
        {
            await LoadPage(--_currentPage);
        }

        private async ValueTask LoadPage(int page)
        {
            if (_reader is not null)
            {
                _comicBytes = $"data:image/jpg;base64,{await _reader.GetPageByIndex(page)}";
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
    }
}