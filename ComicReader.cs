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
    public class ComicReader : IDisposable
    {
        private readonly IArchive _archive;

        private readonly Dictionary<string, string> _loadedPages = new ();

        private static readonly string[] ImageExtensions = { ".jpg", ".png", ".jpeg" };

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
        
        public async Task<string> GetPageByInfo(string key)
        {
            if (!_loadedPages.TryGetValue(key, out var page))
            {
                var archiveEntry = _archive.Entries
                    .Where(IsImage)
                    .FirstOrDefault(x => x.Key == key);
                if (archiveEntry == null)
                {
                    return null;
                }

                var memoryStream = new MemoryStream();
                //has a yield to CopyToAsync to let UI go
                await CopyToAsync(archiveEntry.OpenEntryStream(), memoryStream, 82 * 1000);
                page = Convert.ToBase64String(memoryStream.GetBuffer(), 0, (int)memoryStream.Length);
                _loadedPages.Add(key, page);
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
