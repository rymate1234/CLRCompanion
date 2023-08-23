namespace CLRCompanion.Bot.Services;

using Azure.AI.OpenAI;

public class OpenAIService : OpenAIClient
{
    private static readonly string? _token = Environment.GetEnvironmentVariable("OPENAI_API_KEY");

    public OpenAIService() : base(_token) {}
}