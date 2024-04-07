using AutoGen.OpenAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoGenTestTask
{
    internal class LLMConfiguration
    {
        public static OpenAIConfig GetOpenAIGPT4()
        {
            var openAIKey = Environment.GetEnvironmentVariable("OPENAI_API_KEY") ?? throw new Exception("Please set OPENAI_API_KEY environment variable.");
            var modelId = "gpt-4-1106-preview";
            return new OpenAIConfig(openAIKey, modelId);
        }
    }
}
