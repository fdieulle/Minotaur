using Minotaur.Core.Platform;
using Minotaur.Providers;

namespace Minotaur.Streams
{
    public interface IStreamFactory<TPlatform> 
        where TPlatform : IPlatform
    {
        IStream Create(FileMetaData meta);
    }
}