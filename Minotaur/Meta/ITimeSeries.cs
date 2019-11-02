namespace Minotaur.Meta
{
    public interface ITimeSeries
    {
        string Symbol { get; }

        Column GetOrCreateColumn(string column, FieldType type = FieldType.Unknown);

        Column[] GetColumns(string[] names = null);
    }
}