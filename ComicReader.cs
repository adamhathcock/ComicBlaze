using System;
using System.IO;
using System.Linq;
using System.Text;
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

        public string GetPage(int page)
        {
            var archiveEntry = _archive.Entries
                .Where(x => !x.IsDirectory)
                .Skip(page).FirstOrDefault();
            if (archiveEntry == null)
            {
                return null;
            }
            var memoryStream = new MemoryStream();
            archiveEntry.OpenEntryStream().CopyTo(memoryStream);
            return Convert.ToBase64String(memoryStream.ToArray());
        }

        public void Dispose()
        {
            _archive.Dispose();
        }
    }
}
