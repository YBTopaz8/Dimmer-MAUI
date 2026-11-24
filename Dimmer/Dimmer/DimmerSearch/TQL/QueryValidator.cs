using Fastenshtein;

namespace Dimmer.DimmerSearch.TQL;

public record ValidationResult(bool IsValid, string Message = "", int Position = -1);

public static class QueryValidator
{
    public static ValidationResult Validate(List<Token> tokens)
    {
        int parenBalance = 0;
        foreach (var token in tokens)
        {
            if (token.Type == TokenType.LeftParen) parenBalance++;
            if (token.Type == TokenType.RightParen) parenBalance--;
            if (parenBalance < 0)
                return new(false, "Mismatched ')' found.", token.Position);
        }
        if (parenBalance > 0)
            return new(false, "Missing ')' in query.", tokens[^1].Position);

        for (int i = 0; i < tokens.Count; i++)
        {
            var token = tokens[i];

            if (token.Type == TokenType.Identifier && i + 1 < tokens.Count && tokens[i + 1].Type == TokenType.Colon)
            {
                if (!FieldRegistry.FieldsByAlias.TryGetValue(token.Text, out var fieldDef))
                    return new(false, $"Unknown field '{token.Text}'.", token.Position);

                int valueIndex = i + 2;

                if (valueIndex < tokens.Count && IsOperator(tokens[valueIndex].Type))
                    valueIndex++;

                if (valueIndex < tokens.Count && tokens[valueIndex].Type is TokenType.Not or TokenType.Bang)
                    valueIndex++;

                if (valueIndex < tokens.Count &&
                    tokens[valueIndex].Text.Equals("in", StringComparison.OrdinalIgnoreCase) &&
                    valueIndex + 1 < tokens.Count &&
                    tokens[valueIndex + 1].Type == TokenType.LeftParen)
                {
                    continue; // Skip list syntax; validated structurally by AstParser
                }

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

    private static bool IsOperator(TokenType type) =>
        type is TokenType.GreaterThan or TokenType.LessThan or TokenType.GreaterThanOrEqual
             or TokenType.LessThanOrEqual or TokenType.Equals or TokenType.Tilde
             or TokenType.Caret or TokenType.Dollar;

    private static ValidationResult ValidateValueForField(FieldDefinition field, Token valueToken)
    {
        switch (field.Type)
        {
            case FieldType.Numeric:
                if (!double.TryParse(valueToken.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out _))
                    return new(false, $"Field '{field.PrimaryName}' expects a number, but got '{valueToken.Text}'.", valueToken.Position);
                break;

            case FieldType.Boolean:
                var val = valueToken.Text.Trim().ToLowerInvariant();
                if (val is not ("true" or "false" or "yes" or "no" or "1" or "0" or "oui"))
                    return new(false, $"Field '{field.PrimaryName}' expects a boolean (true/false), but got '{valueToken.Text}'.", valueToken.Position);
                break;

            case FieldType.Duration:
                bool isColonDuration = valueToken.Text.All(c => char.IsDigit(c) || c == ':') && valueToken.Text.Any(char.IsDigit);
                bool isUnitDuration = Regex.IsMatch(valueToken.Text, @"^\d+(s|m|h)$", RegexOptions.IgnoreCase);
                if (!isColonDuration && !isUnitDuration)
                    return new(false, $"Field '{field.PrimaryName}' expects a duration (e.g., 3:30 or 3m), but got '{valueToken.Text}'.", valueToken.Position);
                break;
        }
        return new(true);
    }

    public static string? SuggestCorrectField(string invalidField)
    {
        const int threshold = 2;
        string? bestMatch = null;
        int bestDistance = int.MaxValue;

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