using System;

namespace Minotaur.Providers
{
    public class FileMetaData
    {
        public string Symbol { get; set; }

        public string Column { get; set; }

        public FieldType Type { get; set; }

        public DateTime Start { get; set; }

        public DateTime End { get; set; }

        // Todo: The file path creation has to be owmn by the data collector
        public string FilePath { get; set; }
        
        //public string GetFilePath(string folder)
        //    => Path.Combine(folder, Start.Year.ToString(), Symbol, $"{Symbol}_{Column}_{Start:yyyy-MM-dd_HH:mm:ss}.min");
    }
}