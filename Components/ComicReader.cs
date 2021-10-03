using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SharpCompress.Archives;
using SharpCompress.Common;
using SharpCompress.Readers;

namespace ComicBlaze.Components
{
    public record ComicReaderPage(int Index, string Name, string? Page);
    public record ComicPageIndex(string Name, bool IsImage);
    public class ComicReader : IDisposable
    {
        private readonly IArchive _archive;

        private readonly Dictionary<ComicPageIndex, string?> _loadedPages = new ();

        private static readonly string[] ImageExtensions = { ".jpg", ".png", ".jpeg" };

        public Func<string, Task>? LoadingPage { get; set; }
        public Func<string, Task>? LoadedPage { get; set; }

        public readonly List<ComicPageIndex> Index = new();

        public ComicReader(Stream stream)
        {
            _archive = ArchiveFactory.Open(stream, new ReaderOptions()
            {
                ArchiveEncoding = new ArchiveEncoding(Encoding.Default, Encoding.Default)
            });
            LoadIndex();
        }

        private void LoadIndex()
        {
            foreach (var archiveEntry in _archive.Entries)
            {
                Index.Add(new(archiveEntry.Key, IsImage(archiveEntry))); 
            }
            Index.Sort((x, y) => string.Compare(x.Name, y.Name, StringComparison.Ordinal));
        }
        
        public async ValueTask<ComicReaderPage?> GetPageByIndex(int index)
        {
            if (index >= Index.Count || index < 0)
            {
                return null;
            }
            var key = Index[index];
            return await GetPage(key);
        }
        
        public async ValueTask<ComicReaderPage?> GetPageByInfo(string name)
        {
            var key = Index.FirstOrDefault(x => x.Name == name);
            if (key is null || key.IsImage)
            {
                return null;
            }
            return await GetPage(key);
        }

        public async ValueTask<ComicReaderPage?> GetPage(ComicPageIndex key)
        {
            var index = Index.IndexOf(key);
            if (!_loadedPages.TryGetValue(key, out var page))
            {
                var archiveEntry = _archive.Entries
                    .FirstOrDefault(x => x.Key == key.Name);
                if (archiveEntry == null)
                {
                    return null;
                }
                if (LoadingPage is not null)
                {
                    await LoadingPage.Invoke(archiveEntry.Key);
                }

                var memoryStream = new MemoryStream();
                //has a yield to CopyToAsync to let UI go
                await CopyToAsync(archiveEntry.OpenEntryStream(), memoryStream, 82 * 1000);
                page = Convert.ToBase64String(memoryStream.GetBuffer(), 0, (int)memoryStream.Length);
                _loadedPages.Add(key, page);
                if (LoadedPage is not null)
                {
                    await LoadedPage.Invoke(archiveEntry.Key);
                }
                return new(index, archiveEntry.Key, page);
            }
            return new(index, key.Name, page); 
        }
        
        private async ValueTask CopyToAsync(Stream source, Stream destination, int bufferSize)
        {
            byte[] buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
            try
            {
                while (true)
                {
                    int bytesRead = await source.ReadAsync(new Memory<byte>(buffer)).ConfigureAwait(false);
                    if (bytesRead == 0) break;
                    await destination.WriteAsync(new ReadOnlyMemory<byte>(buffer, 0, bytesRead)).ConfigureAwait(false);
                    //lets UI thread update
                    await Task.Yield();    
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        private bool IsImage(IArchiveEntry entry)
        {
            return !entry.IsDirectory && ImageExtensions.Any(x => entry.Key.EndsWith(x, StringComparison.OrdinalIgnoreCase));
        }

        public void Dispose()
        {
            _archive.Dispose();
        }
    }
}
