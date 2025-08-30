using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
namespace RAHM.BD.Controllers
{
    public class HealthController : Controller
    {
        private static readonly HttpClient _httpClient = new HttpClient();
        // GET: /Health/Tips
        public IActionResult Tips()
        {
            return View();
        }

        public IActionResult ConditionTips()
        {
            return View(); // This will look for /Views/Health/ConditionTips.cshtml
        }

        [HttpPost]
        public async Task<IActionResult> SearchConditions([FromBody] SymptomRequest req)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(req.Symptoms))
                    return Json(new { conditions = Array.Empty<object>() });

                // Use OpenFDA API to find conditions associated with symptoms
                var conditions = await GetConditionsFromOpenFDA(req.Symptoms);

                // If no results from OpenFDA, try NLM as fallback
                if (!conditions.Any())
                {
                    conditions = await GetConditionsFromNLM(req.Symptoms);
                }

                return Json(new { conditions = conditions.Take(10) }); // Limit to 10 results

            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                return StatusCode(500, new { error = "Unable to fetch conditions. Please try again." });
            }
        }

        private async Task<List<object>> GetConditionsFromOpenFDA(string symptoms)
        {
            var conditions = new List<object>();
            var uniqueConditions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            try
            {
                // Clean and prepare symptoms for search
                var cleanSymptoms = CleanSymptomsForSearch(symptoms);

                // Search for adverse events related to these symptoms
                string url = $"https://api.fda.gov/drug/event.json?search=patient.reaction.reactionmeddrapt.exact:\"{Uri.EscapeDataString(cleanSymptoms)}\"&limit=20";

                using var client = new HttpClient();
                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();
                    using var doc = JsonDocument.Parse(responseJson);

                    // Parse the response to extract conditions
                    if (doc.RootElement.TryGetProperty("results", out var results) && results.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var result in results.EnumerateArray())
                        {
                            ExtractConditionsFromResult(result, uniqueConditions);
                        }
                    }
                }

                // Convert unique conditions to list
                foreach (var condition in uniqueConditions.OrderBy(c => c))
                {
                    conditions.Add(new { name = condition });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"OpenFDA API error: {ex.Message}");
                // Continue with empty list - we'll fallback to NLM
            }

            return conditions;
        }

        private string CleanSymptomsForSearch(string symptoms)
        {
            // Convert to lowercase and remove special characters
            var clean = symptoms.ToLower();

            // Remove common stop words and non-medical terms
            var stopWords = new[] { "my", "the", "a", "an", "and", "or", "but", "have", "has", "had", "feel", "feeling", "experienced", "experiencing" };
            foreach (var word in stopWords)
            {
                clean = clean.Replace(word, "");
            }

            // Remove extra spaces and trim
            clean = Regex.Replace(clean, @"\s+", " ").Trim();

            // Take only the first few words to avoid overly specific searches
            var words = clean.Split(' ').Take(3).ToArray();
            return string.Join(" ", words);
        }

        private void ExtractConditionsFromResult(JsonElement result, HashSet<string> uniqueConditions)
        {
            try
            {
                // Look for patient reaction data
                if (result.TryGetProperty("patient", out var patient))
                {
                    // Extract from reaction field
                    if (patient.TryGetProperty("reaction", out var reactions) && reactions.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var reaction in reactions.EnumerateArray())
                        {
                            if (reaction.TryGetProperty("reactionmeddrapt", out var condition) &&
                                condition.ValueKind == JsonValueKind.String)
                            {
                                var conditionName = condition.GetString();
                                if (!string.IsNullOrEmpty(conditionName) && conditionName.Length > 3)
                                {
                                    // Clean up the condition name
                                    var cleanName = CleanConditionName(conditionName);
                                    if (!string.IsNullOrEmpty(cleanName))
                                    {
                                        uniqueConditions.Add(cleanName);
                                    }
                                }
                            }
                        }
                    }

                    // Also check for drugs that might indicate conditions
                    if (patient.TryGetProperty("drug", out var drugs) && drugs.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var drug in drugs.EnumerateArray())
                        {
                            if (drug.TryGetProperty("drugindication", out var indication) &&
                                indication.ValueKind == JsonValueKind.String)
                            {
                                var indicationText = indication.GetString();
                                if (!string.IsNullOrEmpty(indicationText))
                                {
                                    var cleanName = CleanConditionName(indicationText);
                                    if (!string.IsNullOrEmpty(cleanName))
                                    {
                                        uniqueConditions.Add(cleanName);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // Skip any malformed entries
            }
        }

        private string CleanConditionName(string conditionName)
        {
            // Remove content in parentheses and brackets
            string result = Regex.Replace(conditionName, @"[\(\[].*?[\)\]]", "").Trim();

            // Remove numbers and special characters at the end
            result = Regex.Replace(result, @"[\d-]+$", "").Trim();

            // Remove any remaining special characters
            result = result.Trim('-', ',', '.', ';', ':');

            // Capitalize first letter of each word
            result = System.Globalization.CultureInfo.CurrentCulture.TextInfo.ToTitleCase(result.ToLower());

            return result;
        }

        private async Task<List<object>> GetConditionsFromNLM(string symptoms)
        {
            var conditions = new List<object>();

            try
            {
                string url = $"https://clinicaltables.nlm.nih.gov/api/conditions/v3/search?terms={Uri.EscapeDataString(symptoms)}";

                using var client = new HttpClient();
                var response = await client.GetAsync(url);

                if (response.IsSuccessStatusCode)
                {
                    var responseJson = await response.Content.ReadAsStringAsync();

                    using var doc = JsonDocument.Parse(responseJson);

                    if (doc.RootElement.ValueKind == JsonValueKind.Array && doc.RootElement.GetArrayLength() >= 2)
                    {
                        var termsElement = doc.RootElement[1];

                        if (termsElement.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var term in termsElement.EnumerateArray())
                            {
                                var conditionName = term.GetString();
                                if (!string.IsNullOrEmpty(conditionName) && conditionName.Length > 3)
                                {
                                    var cleanName = CleanConditionName(conditionName);
                                    if (!string.IsNullOrEmpty(cleanName))
                                    {
                                        conditions.Add(new { name = cleanName });
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch
            {
                // Silently fail
            }

            return conditions.Distinct().Take(8).ToList();
        }

        [HttpPost]
        public async Task<IActionResult> GetHealthTips([FromBody] ConditionRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Condition))
                return Json(new { tips = new string[0] });

            try
            {
                // WHO Health Topics API
                string searchTerm = req.Condition.Trim().ToLower();
                string url = $"https://ghoapi.azureedge.net/api/{searchTerm}?format=json";

                var response = await _httpClient.GetAsync(url);
                if (!response.IsSuccessStatusCode)
                    return Json(new { tips = new string[0] });

                var jsonString = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(jsonString);

                var tips = new List<string>();

                // Extract relevant information from JSON
                if (doc.RootElement.TryGetProperty("value", out var valueArray) && valueArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in valueArray.EnumerateArray())
                    {
                        if (item.TryGetProperty("Description", out var desc))
                        {
                            tips.Add(desc.GetString());
                        }
                    }
                }

                if (!tips.Any())
                    tips.Add("No tips available for this condition.");

                return Json(new { tips });

            }
            catch
            {
                return Json(new { tips = new string[] { "Error fetching tips. Please try again later." } });
            }
        }


        public class ConditionRequest
    {
        public string Condition { get; set; }
    }

    public class SymptomRequest
        {
            public string Symptoms { get; set; }
        }
    }
}