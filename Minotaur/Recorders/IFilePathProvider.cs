using System;

namespace Minotaur.Recorders
{
    public interface IFilePathProvider
    {
        string GetPath(string symbol, string column, DateTime timestamp);
    }
}