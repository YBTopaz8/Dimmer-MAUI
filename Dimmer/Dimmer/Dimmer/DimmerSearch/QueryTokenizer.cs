using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dimmer.DimmerSearch;

public static class QueryTokenizer
{
    private enum TokenizerState { Default, InQuote }

    public static List<string> Tokenize(string text)
    {
        var tokens = new List<string>();
        var state = TokenizerState.Default;
        var currentToken = new StringBuilder();

        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];

            switch (state)
            {
                case TokenizerState.Default:
                    if (char.IsWhiteSpace(c))
                    {
                        if (currentToken.Length > 0)
                        {
                            tokens.Add(currentToken.ToString());
                            currentToken.Clear();
                        }
                    }
                    else if (c == '"')
                    {
                        state = TokenizerState.InQuote;
                    }
                    else
                    {
                        currentToken.Append(c);
                    }
                    break;

                case TokenizerState.InQuote:
                    if (c == '"')
                    {
                        // End of quoted token
                        tokens.Add(currentToken.ToString());
                        currentToken.Clear();
                        state = TokenizerState.Default;
                    }
                    else
                    {
                        currentToken.Append(c);
                    }
                    break;
            }
        }

        // Add the last token if it exists
        if (currentToken.Length > 0)
        {
            tokens.Add(currentToken.ToString());
        }

        return tokens;
    }
}