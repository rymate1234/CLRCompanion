namespace CLRCompanion.Data
{
    public class OpenAIFunction
    {
        public int Id { get; set; }

        public string Name { get; set; }
        public string Description { get; set; }

        public OpenAIFunctionParam[]? Parameters { get; set; }

        /*
         * If this is set, when the user invokes the function, we format the template with the parameters
         */
        public string? Template { get; set; }
    }
}
