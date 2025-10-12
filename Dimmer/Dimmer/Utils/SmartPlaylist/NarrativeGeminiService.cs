using Google.Cloud.AIPlatform.V1;

using Value = Google.Cloud.AIPlatform.V1.Value;

namespace Dimmer.Utils.SmartPlaylist;
public class NarrativeGeminiService
{
    private readonly IRealmFactory _realmFactory;
    private readonly PredictionServiceClient _geminiClient;
    private readonly string _googleProjectId = "801778422381"; 

    // Constructor gets the Gemini client via DI
    public NarrativeGeminiService(IRealmFactory realmFactory, PredictionServiceClient geminiClient)
    {
        _realmFactory = realmFactory;
        _geminiClient = geminiClient;
    }

    public async Task<(string Title, List<SongModel> Songs)> GenerateNarrativePlaylistAsync()
    {
        var realm = _realmFactory.GetRealmInstance();

        // Stage 1: Vocabulary Acquisition (same as before)
        var allTitles = realm.All<SongModel>().Select(s => s.Title).ToList();

        // Stage 2: Creative Generation with a Gemini-tuned Prompt
        string prompt = BuildGeminiPrompt(allTitles);

        // This is the part that changes - how you call the API
        var endpointName = new EndpointName(_googleProjectId, "us-central1", "publishers/google/models/gemini-1.5-pro-preview-0409");
        /*
         * StructValue = new Struct
            {
                Fields = { { "prompt", Value.ForString(prompt) } }
            }
         * 
         * 
         * 
         */




        var instance = new Value
        {
            DoubleValue = 0.5, // Adjust temperature for creativity
            StringValue = prompt,
            IntValue = 8 // Max tokens to generate
        };

        var respp = await _geminiClient.PredictAsync(new PredictRequest(new PredictRequest()
        {
            Endpoint = endpointName.ToString(),
            EndpointAsEndpointName = endpointName,
        }));
        string aiRespons = respp.Predictions[0].StructValue.Fields["content"].StringValue;

        //string aiResponse = response.Predictions[0].StructValue.Fields["content"].StringValue;

        // Stage 3: Parse and retrieve songs (same as before)
        List<string> generatedTitles = ParseResponse(aiRespons);
        var playlistSongs = new List<SongModel>();

        // ... (rest of the logic is identical) ...

        return ("A Gemini Story", playlistSongs);
    }

    private string BuildGeminiPrompt(List<string> titles)
    {
        // Gemini responds well to structured, role-playing prompts.
        string titleList = string.Join("\n", titles);

        return $@"
        You are 'Dimmer DJ', a creative AI playlist curator for the Dimmer music player.
        Your task is to create a 5-song narrative playlist by arranging song titles into a coherent and clever short story or sentence.
        
        **RULES:**
        1. You MUST use ONLY titles from the provided list.
        2. Do NOT invent titles.
        3. The final output must be a JSON array of strings, with each string being an exact song title from the list.
        4. Do not include any other text, explanation, or commentary in your response. Just the JSON.

        **AVAILABLE SONG TITLES:**
        ---
        {titleList}
        ---

        Now, generate the playlist as a JSON array.
        ";
    }

    private List<string> ParseResponse(string aiResponse)
    {
        // Simple JSON parsing
        try
        {
            return JsonSerializer.Deserialize<List<string>>(aiResponse) ?? new List<string>();
        }
        catch
        {
            // Fallback if the AI messes up the format - split by newline
            return aiResponse.Split('\n', StringSplitOptions.RemoveEmptyEntries).ToList();
        }
    }
}