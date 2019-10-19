namespace Minotaur.Meta
{
    public class ColumnInfo
    {
        public string Name { get; set; }
        public FieldType Type { get; set; }

        public override string ToString() => $"[{Name}] {Type}";
    }
}