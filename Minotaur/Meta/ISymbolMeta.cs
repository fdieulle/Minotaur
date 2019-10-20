namespace Minotaur.Meta
{
    public interface ISymbolMeta
    {
        string Symbol { get; }

        ColumnMeta GetOrCreateColumn(string column, FieldType type = FieldType.Unknown);

        ColumnMeta[] GetColumns(string[] names = null);
    }
}