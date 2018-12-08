using System;

namespace Minotaur.Providers
{
    public interface IFilePathProvider
    {
        string GetMetaFilePath(string symbol, string column);
        string GetFilePath(string symbol, string column, DateTime timestamp);
    }
}