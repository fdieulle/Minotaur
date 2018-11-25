using Minotaur.Core.Platform;
using Minotaur.IO;

namespace Minotaur.Providers
{
    public interface IStreamFactory<TPlatform> 
        where TPlatform : IPlatform
    {
        IStream Create(FileMetaData meta);
    }
}