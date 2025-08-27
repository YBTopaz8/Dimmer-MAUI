using Fastenshtein;

namespace Dimmer.DimmerSearch.TQL;
public record ValidationResult(bool IsValid, string Message = "", int Position = -1);

public static class QueryValidator
{
    public static ValidationResult Validate(List<Token> tokens)
    {
        // Check for balanced parentheses
        int parenBalance = 0;
        foreach (var token in tokens)
        {
            if (token.Type == TokenType.LeftParen)
                parenBalance++;
            if (token.Type == TokenType.RightParen)
                parenBalance--;
            if (parenBalance < 0)
                return new(false, "Mismatched ')' found.", token.Position);
        }
        if (parenBalance > 0)
            return new(false, "Missing ')' in query.", tokens[^1].Position);

        // Iterate through tokens to find specific clause errors
        for (int i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];

            // Look for a field definition: `identifier` followed by `colon`
            if (token.Type == TokenType.Identifier && i + 1 < tokens.Count && tokens[i + 1].Type == TokenType.Colon)
            {
                if (!FieldRegistry.FieldsByAlias.TryGetValue(token.Text, out var fieldDef))
                {
                    return new(false, $"Unknown field '{token.Text}'.", token.Position);
                }

                // We have a valid field, now check the value that follows
                int valueIndex = i + 2; // Position of the value token after the colon
                if (valueIndex < tokens.Count)
                {
                    var result = ValidateValueForField(fieldDef, tokens[valueIndex]);
                    if (!result.IsValid)
                        return result;
                }
            }
        }

        return new(true, "Query is valid.");
    }

    private static ValidationResult ValidateValueForField(FieldDefinition field, Token valueToken)
    {
        switch (field.Type)
        {
            case FieldType.Numeric:
                if (!double.TryParse(valueToken.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                {
                    return new(false, $"Field '{field.PrimaryName}' expects a number, but got '{valueToken.Text}'.", valueToken.Position);
                }
                break;
            case FieldType.Boolean:
                var val = valueToken.Text.ToLowerInvariant();
                if (val != "true" && val != "false" && val != "yes" && val != "no" && val != "1" && val != "0")
                {
                    return new(false, $"Field '{field.PrimaryName}' expects a boolean (true/false), but got '{valueToken.Text}'.", valueToken.Position);
                }
                break;
            case FieldType.Duration:
                // A simple check: must contain numbers and optionally colons.
                if (!valueToken.Text.All(c => char.IsDigit(c) || c == ':'))
                {
                    return new(false, $"Field '{field.PrimaryName}' expects a duration (e.g., 3:30), but got '{valueToken.Text}'.", valueToken.Position);
                }
                break;
        }
        return new(true);
    }

    public static string? SuggestCorrectField(string invalidField)
    {
        const int threshold = 2; // Suggest if distance is 1 or 2

        string? bestMatch = null;
        int bestDistance = int.MaxValue;

        // Check against all known aliases
        foreach (var alias in FieldRegistry.FieldsByAlias.Keys)
        {
            int distance = Levenshtein.Distance(invalidField.ToLower(), alias.ToLower());
            if (distance <= threshold && distance < bestDistance)
            {
                bestDistance = distance;
                bestMatch = alias;
            }
        }
        return bestMatch;
    }
}
