namespace CLRCompanion.Data
{
    public class OpenAIFunctionParam
    {
        public int Id { get; set; }

        public string Name { get; set; }
        public string Type { get; } = "string";

        private string _enum;

        public ICollection<string> Enum { get; set; }

        public string Description { get; set; }

        public bool Required { get; set; } = true;
    }
}
