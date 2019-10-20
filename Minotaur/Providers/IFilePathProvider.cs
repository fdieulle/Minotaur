using System;

namespace Minotaur.Providers
{
    public interface IFilePathProvider
    {
        string GetMetaFilePath(string symbol);
        string GetFilePath(string symbol, string column, DateTime timestamp);
        string GetTmpFilePath(string symbol, string column, DateTime timestamp);
    }
}