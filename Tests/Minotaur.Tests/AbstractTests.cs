using System;
using System.Diagnostics;

namespace Minotaur.Tests
{
    public abstract class AbstractTests
    {
        [DebuggerStepThrough]
        protected static T[] A<T>(params T[] a) => a;

        [DebuggerStepThrough]
        protected static DateTime Dt(string timestamp) => timestamp.ToDateTime();
    }
}
