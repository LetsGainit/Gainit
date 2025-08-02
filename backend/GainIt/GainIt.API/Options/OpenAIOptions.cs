namespace GainIt.API.Options
{
    public class OpenAIOptions
    {
        public string Endpoint { get; set; } = "";
        public string ApiKey { get; set; } = "";
        public string ChatDeploymentName { get; set; } = "";
        public string EmbeddingDeploymentName { get; set; } = "";
    }
}
