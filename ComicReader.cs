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

namespace ComicBlaze
{
    public record ComicPageIndex(int Index, string Name);
    public class ComicReader : IDisposable
    {
        private readonly IArchive _archive;

        private readonly Dictionary<ComicPageIndex, string> _loadedPages = new ();

        private static readonly string[] ImageExtensions = { ".jpg", ".png", ".jpeg" };

        public Action<string>? LoadingPage { get; set; }
        public Action<string>? LoadedPage { get; set; }

        public ComicReader(Stream stream)
        {
            _archive = ArchiveFactory.Open(stream, new ReaderOptions()
            {
                ArchiveEncoding = new ArchiveEncoding(Encoding.Default, Encoding.Default)
            });
        }
        
        public List<string> GetPageInfos()
        {
            return _archive.Entries
                .Where(IsImage)
                .OrderBy(x => x.Key)
                .Select(x => x.Key)
                .ToList();
        }
        
        public List<string> GetFiles()
        {
            return _archive.Entries
                .OrderBy(x => x.Key)
                .Select(x => x.Key)
                .ToList();
        }
        
        public async Task<string?> GetPageByIndex(int index)
        {
            var key = _loadedPages.Keys.FirstOrDefault(x => x.Index == index);
            if (key is null || !_loadedPages.TryGetValue(key, out var page))
            {
                var entries = _archive.Entries.ToList();
                if (index >= entries.Count || index < 0)
                {
                    return null;
                }
                var archiveEntry = entries[index];
                if (!IsImage(archiveEntry))
                {
                    return null;
                }

                LoadingPage?.Invoke(archiveEntry.Key);
                await Task.Yield();

                var memoryStream = new MemoryStream();
                //has a yield to CopyToAsync to let UI go
                await CopyToAsync(archiveEntry.OpenEntryStream(), memoryStream, 82 * 1000);
                page = Convert.ToBase64String(memoryStream.GetBuffer(), 0, (int)memoryStream.Length);
                _loadedPages.Add(new (index, archiveEntry.Key), page);
                LoadedPage?.Invoke(archiveEntry.Key);
                await Task.Yield();
            }
            return page;
        }
        
        public async Task<string?> GetPageByInfo(string name)
        {
            var key = _loadedPages.Keys.FirstOrDefault(x => x.Name == name);
            if (key is null || !_loadedPages.TryGetValue(key, out var page))
            {
                var archiveEntry = _archive.Entries
                    .Where(IsImage)
                    .FirstOrDefault(x => x.Key == name);
                if (archiveEntry == null)
                {
                    return null;
                }

                var index = _archive.Entries.ToList().IndexOf(archiveEntry);

                LoadingPage?.Invoke(archiveEntry.Key);
                await Task.Yield();

                var memoryStream = new MemoryStream();
                //has a yield to CopyToAsync to let UI go
                await CopyToAsync(archiveEntry.OpenEntryStream(), memoryStream, 82 * 1000);
                page = Convert.ToBase64String(memoryStream.GetBuffer(), 0, (int)memoryStream.Length);
                _loadedPages.Add(new(index, archiveEntry.Key), page);
                LoadedPage?.Invoke(archiveEntry.Key);
                await Task.Yield();
            }
            return page;
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
