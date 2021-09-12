using System;
using System.Linq;
using System.Threading.Tasks;
using Blazorise;
using Microsoft.AspNetCore.Components;

namespace ComicBlaze.Pages
{
    public partial class Index : BaseComponent
    {
        private string _comicBytes;
        private string _pageInfo;

        private int _currentPage;
        private Modal _modalRef;
        private ComicReader _reader;
        private ElementReference _inputTypeFileElement;
        
        private async ValueTask LoadPage(int page)
        {
            if (_reader == null)
            {
                return;
            }

            Console.WriteLine("1");
            _pageInfo = _reader.GetPageInfo(page);
            Console.WriteLine("2");
            _modalRef.Show();
            StateHasChanged();
            await Task.Yield();
            
            Console.WriteLine("3");
            if (page < 0)
            {
                return;
            }

            if (page >= _reader.Count)
            {
                return;
            }

            Console.WriteLine("4");
            _currentPage = page;

            StateHasChanged();
            await Task.Yield();

            await InvokeAsync(async () =>
            {
                Console.WriteLine("5");
                _comicBytes = $"data:image/jpg;base64,{await _reader.GetPage(_currentPage)}";
                StateHasChanged();
                await Task.Yield();
                _modalRef.Hide();
                Console.WriteLine("6");
            });
        }

        public async Task Home()
        {
            await LoadPage(0);
        }

        private async Task Next()
        {
            await LoadPage(++_currentPage);
        }

        private async Task Previous()
        {
            await LoadPage(--_currentPage);
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
                await LoadPage(0);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}