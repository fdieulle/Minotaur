using System;

namespace Minotaur.Core
{
    public class CorruptedDataException : Exception
    {
        public CorruptedDataException(string message, Exception innerException = null)
            : base(message, innerException)
        {

        }
    }
}
