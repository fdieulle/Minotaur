using System;

namespace Minotaur.Recorders
{
    public interface IFilePathProvider
    {
        string GetMetaFilePath(string symbol, string column);
        string GetFilePath(string symbol, string column, DateTime timestamp);
    }
}