using System;
using System.Collections.Generic;

namespace Minotaur.Meta
{
    public class FileTimeSlice : TimeSlice
    {
        public List<BlockTimeSlice> Blocks { get; set; }

        public long GetOffset(DateTime start)
        {
            if (Blocks == null) return 0;

            var offset = 0L;
            for(var i = 0; i < Blocks.Count; i++)
            {
                if (start >= Blocks[i].Start)
                    offset = Blocks[i].Offset;
                else break;
            }

            return offset;
        }
    }
}