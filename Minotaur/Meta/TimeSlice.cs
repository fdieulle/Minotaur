using System;

namespace Minotaur.Meta
{
    public class TimeSlice
    {
        public DateTime Start { get; set; }
        public DateTime End { get; set; }

        public override string ToString() 
            => $"{nameof(Start)}: {Start:yyyy-MM-dd HH:mm:ss.fff}, {nameof(End)}: {End:yyyy-MM-dd HH:mm:ss.fff}";
    }
}