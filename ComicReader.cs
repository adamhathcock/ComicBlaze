using System;
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

        public ComicReader(Stream stream)
        {
            _archive = ArchiveFactory.Open(stream, new ReaderOptions()
            {
                ArchiveEncoding = new ArchiveEncoding(Encoding.Default, Encoding.Default)
            });
        }

        public int Count => _archive.Entries.Count();

        public string GetPageInfo(int page)
        {
            var archiveEntry = _archive.Entries
                .Where(x => !x.IsDirectory && (x.Key?.EndsWith("jpg") ?? false))
                .OrderBy(x => x.Key)
                .Skip(page).FirstOrDefault();
            if (archiveEntry == null)
            {
                return null;
            }
            return archiveEntry.Key;
        }

        public async Task<string> GetPage(int page)
        {
            var archiveEntry = _archive.Entries
                .Where(x => !x.IsDirectory && (x.Key?.EndsWith("jpg") ?? false))
                .OrderBy(x => x.Key)
                .Skip(page).FirstOrDefault();
            if (archiveEntry == null)
            {
                return null;
            }
            var memoryStream = new MemoryStream();
            await archiveEntry.OpenEntryStream().CopyToAsync(memoryStream);
            return Convert.ToBase64String(memoryStream.ToArray());
        }

        public void Dispose()
        {
            _archive.Dispose();
        }
    }
}
