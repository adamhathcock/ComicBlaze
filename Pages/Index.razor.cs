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

        private Modal _loadingModal = default!;
        private Modal _pagesModal = default!;
        private ComicReader? _reader;
        private ElementReference _inputTypeFileElement;

        private Carousel? _carousel;

        private void SelectedSlideChanged()
        {
            _carousel?.Timer?.Stop();
        }

        protected override void OnParametersSet()
        {
            base.OnParametersSet();
            _carousel?.Timer?.Stop();
        }

        private async Task ReadFile()
        {
            _carousel?.Timer?.Stop();
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
                     _carousel?.Timer?.Stop();
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

        private void OnButtonClicked()
        {
            _pagesModal.Show();
        }
    }
}