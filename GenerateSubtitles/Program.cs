using Azure;
using Azure.AI.OpenAI;
using Microsoft.Extensions.Configuration;
using NAudio.Wave;

if (args.Length != 1)
{
    Console.WriteLine("Usage: GenerateSubtitles.exe <mp4 video file path>");
    return;
}

var videoFilePath = args[0];
if (!File.Exists(videoFilePath))
{
    Console.WriteLine("File not found: " + videoFilePath);
    return;
}

var mp3FilePath = Path.ChangeExtension(videoFilePath, ".mp3");
ConvertMp4ToMp3File(videoFilePath, mp3FilePath);

var configuration = new ConfigurationBuilder().AddUserSecrets<Program>().Build();
var apiKey = configuration["azure-openai-api-key"];
var endpoint = configuration["azure-openai-endpoint-url"];
var deployName = configuration["azure-openai-deploy-name"];

var client = new OpenAIClient(new Uri(endpoint), new AzureKeyCredential(apiKey));

await using var audioStreamFromFile = new FileStream(mp3FilePath, FileMode.Open);
var transcriptionOptions = new AudioTranscriptionOptions
{
    DeploymentName = deployName,
    AudioData = BinaryData.FromStream(audioStreamFromFile),
    ResponseFormat = AudioTranscriptionFormat.Vtt,
    Filename = Path.GetFileName(mp3FilePath)
};

Response<AudioTranscription> transcriptionResponse = await client.GetAudioTranscriptionAsync(transcriptionOptions);
File.WriteAllText(Path.ChangeExtension(videoFilePath, ".vtt"), transcriptionResponse.Value.Text);

return;

void ConvertMp4ToMp3File(string sourceFilePath, string targetFilePath)
{
    using var reader = new MediaFoundationReader(sourceFilePath);
    MediaFoundationEncoder.EncodeToMp3(reader, targetFilePath);
}
