// Generated by TinyPG v1.4

using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Serialization;

namespace TinyPG
{
    #region Scanner

    public class Scanner
    {
        public string Input;
        public int StartPos;
        public int EndPos;
        public string CurrentFile;
        public int CurrentLine;
        public int CurrentColumn;
        public int CurrentPosition;
        public List<Token> Skipped; // tokens that were skipped
        public Dictionary<TokenType, Regex> Patterns;

        private Token _lookAheadToken;
        private readonly List<TokenType> _tokens;
        private readonly List<TokenType> _skipList; // tokens to be skipped
        private readonly TokenType FileAndLine;

        public Scanner()
        {
            Patterns = new Dictionary<TokenType, Regex>();
            _tokens = new List<TokenType>();
            _lookAheadToken = null;
            Skipped = new List<Token>();

            _skipList = new List<TokenType>
            {
                TokenType.WHITESPACE,
                TokenType.COMMENTLINE,
                TokenType.COMMENTBLOCK,
            };

            var regex = new Regex(@"\(", RegexOptions.Compiled);
            Patterns.Add(TokenType.BRACKETOPEN, regex);
            _tokens.Add(TokenType.BRACKETOPEN);

            regex = new Regex(@"\)", RegexOptions.Compiled);
            Patterns.Add(TokenType.BRACKETCLOSE, regex);
            _tokens.Add(TokenType.BRACKETCLOSE);

            regex = new Regex(@"\{[^\}]*\}([^};][^}]*\}+)*;", RegexOptions.Compiled);
            Patterns.Add(TokenType.CODEBLOCK, regex);
            _tokens.Add(TokenType.CODEBLOCK);

            regex = new Regex(@",", RegexOptions.Compiled);
            Patterns.Add(TokenType.COMMA, regex);
            _tokens.Add(TokenType.COMMA);

            regex = new Regex(@"\[", RegexOptions.Compiled);
            Patterns.Add(TokenType.SQUAREOPEN, regex);
            _tokens.Add(TokenType.SQUAREOPEN);

            regex = new Regex(@"\]", RegexOptions.Compiled);
            Patterns.Add(TokenType.SQUARECLOSE, regex);
            _tokens.Add(TokenType.SQUARECLOSE);

            regex = new Regex(@"=", RegexOptions.Compiled);
            Patterns.Add(TokenType.ASSIGN, regex);
            _tokens.Add(TokenType.ASSIGN);

            regex = new Regex(@"\|", RegexOptions.Compiled);
            Patterns.Add(TokenType.PIPE, regex);
            _tokens.Add(TokenType.PIPE);

            regex = new Regex(@";", RegexOptions.Compiled);
            Patterns.Add(TokenType.SEMICOLON, regex);
            _tokens.Add(TokenType.SEMICOLON);

            regex = new Regex(@"(\*|\+|\?)", RegexOptions.Compiled);
            Patterns.Add(TokenType.UNARYOPER, regex);
            _tokens.Add(TokenType.UNARYOPER);

            regex = new Regex(@"[a-zA-Z_][a-zA-Z0-9_]*", RegexOptions.Compiled);
            Patterns.Add(TokenType.IDENTIFIER, regex);
            _tokens.Add(TokenType.IDENTIFIER);

            regex = new Regex(@"[0-9]+", RegexOptions.Compiled);
            Patterns.Add(TokenType.INTEGER, regex);
            _tokens.Add(TokenType.INTEGER);

            regex = new Regex(@"[0-9]*\.[0-9]+", RegexOptions.Compiled);
            Patterns.Add(TokenType.DOUBLE, regex);
            _tokens.Add(TokenType.DOUBLE);

            regex = new Regex(@"(0x[0-9a-fA-F]{6})", RegexOptions.Compiled);
            Patterns.Add(TokenType.HEX, regex);
            _tokens.Add(TokenType.HEX);

            regex = new Regex(@"->", RegexOptions.Compiled);
            Patterns.Add(TokenType.ARROW, regex);
            _tokens.Add(TokenType.ARROW);

            regex = new Regex(@"<%\s*@", RegexOptions.Compiled);
            Patterns.Add(TokenType.DIRECTIVEOPEN, regex);
            _tokens.Add(TokenType.DIRECTIVEOPEN);

            regex = new Regex(@"%>", RegexOptions.Compiled);
            Patterns.Add(TokenType.DIRECTIVECLOSE, regex);
            _tokens.Add(TokenType.DIRECTIVECLOSE);

            regex = new Regex(@"^$", RegexOptions.Compiled);
            Patterns.Add(TokenType.EOF, regex);
            _tokens.Add(TokenType.EOF);

            regex = new Regex(@"@?\""(\""\""|[^\""])*\""", RegexOptions.Compiled);
            Patterns.Add(TokenType.STRING, regex);
            _tokens.Add(TokenType.STRING);

            regex = new Regex(@"\s+", RegexOptions.Compiled);
            Patterns.Add(TokenType.WHITESPACE, regex);
            _tokens.Add(TokenType.WHITESPACE);

            regex = new Regex(@"//[^\n]*\n?", RegexOptions.Compiled);
            Patterns.Add(TokenType.COMMENTLINE, regex);
            _tokens.Add(TokenType.COMMENTLINE);

            regex = new Regex(@"/\*[^*]*\*+(?:[^/*][^*]*\*+)*/", RegexOptions.Compiled);
            Patterns.Add(TokenType.COMMENTBLOCK, regex);
            _tokens.Add(TokenType.COMMENTBLOCK);
        }

        public void Init(string input, string fileName = "")
        {
            Input = input;
            StartPos = 0;
            EndPos = 0;
            CurrentFile = fileName;
            CurrentLine = 1;
            CurrentColumn = 1;
            CurrentPosition = 0;
            _lookAheadToken = null;
        }

        public Token GetToken(TokenType type)
        {
            var t = new Token(StartPos, EndPos)
            {
                Type = type,
            };
            return t;
        }

         /// <summary>
        /// executes a lookahead of the next token
        /// and will advance the scan on the input string
        /// </summary>
        /// <returns></returns>
        public Token Scan(params TokenType[] expectedTokens)
        {
            var tok = LookAhead(expectedTokens); // temporarily retrieve the lookahead
            _lookAheadToken = null; // reset lookahead token, so scanning will continue
            StartPos = tok.EndPos;
            EndPos = tok.EndPos; // set the tokenizer to the new scan position
            CurrentLine = tok.Line + (tok.Text.Length - tok.Text.Replace("\n", "").Length);
            CurrentFile = tok.File;
            return tok;
        }

        /// <summary>
        /// returns token with longest best match
        /// </summary>
        /// <returns></returns>
        public Token LookAhead(params TokenType[] expectedTokens)
        {
            var startPos = StartPos;
            var endPos = EndPos;
            var currentLine = CurrentLine;
            var currentFile = CurrentFile;
            Token tok;
            List<TokenType> scanTokens;

            // this prevents double scanning and matching
            // increased performance
            if (_lookAheadToken != null
                && _lookAheadToken.Type != TokenType._UNDETERMINED_
                && _lookAheadToken.Type != TokenType._NONE_)
            {
                return _lookAheadToken;
            }

            // if no scan tokens specified, then scan for all of them (= backward compatible)
            if (expectedTokens.Length == 0)
            {
                scanTokens = _tokens;
            }
            else
            {
                scanTokens = new List<TokenType>(expectedTokens);
                scanTokens.AddRange(_skipList);
            }

            do
            {

                var len = -1;
                var index = (TokenType)int.MaxValue;
                var input = Input.Substring(startPos);

                tok = new Token(startPos, endPos);

                int i;
                for (i = 0; i < scanTokens.Count; i++)
                {
                    var r = Patterns[scanTokens[i]];
                    var m = r.Match(input);
                    if (m.Success && m.Index == 0 && (m.Length > len || (scanTokens[i] < index && m.Length == len )))
                    {
                        len = m.Length;
                        index = scanTokens[i];
                    }
                }

                if (index >= 0 && len >= 0)
                {
                    tok.EndPos = startPos + len;
                    tok.Text = Input.Substring(tok.StartPos, len);
                    tok.Type = index;
                }
                else if (tok.StartPos == tok.EndPos)
                {
                    tok.Text = tok.StartPos < Input.Length ? Input.Substring(tok.StartPos, 1) : "EOF";
                }

                // Update the line and column count for error reporting.
                tok.File = currentFile;
                tok.Line = currentLine;
                if (tok.StartPos < Input.Length)
                {
                    tok.Column = tok.StartPos - Input.LastIndexOf('\n', tok.StartPos);
                }

                if (_skipList.Contains(tok.Type))
                {
                    startPos = tok.EndPos;
                    endPos = tok.EndPos;
                    currentLine = tok.Line + (tok.Text.Length - tok.Text.Replace("\n", "").Length);
                    currentFile = tok.File;
                    Skipped.Add(tok);
                }
                else
                {
                    // only assign to non-skipped tokens
                    tok.Skipped = Skipped; // assign prior skips to this token
                    Skipped = new List<Token>(); //reset skips
                }

                // Check to see if the parsed token wants to
                // alter the file and line number.
                if (tok.Type == FileAndLine)
                {
                    var match = Patterns[tok.Type].Match(tok.Text);
                    var fileMatch = match.Groups["File"];
                    if (fileMatch.Success)
                    {
                        currentFile = fileMatch.Value.Replace("\\\\", "\\");
                    }

                    var lineMatch = match.Groups["Line"];
                    if (lineMatch.Success)
                    {
                        currentLine = int.Parse(lineMatch.Value, NumberStyles.Integer, CultureInfo.InvariantCulture);
                    }
                }
            }
            while (_skipList.Contains(tok.Type));

            _lookAheadToken = tok;
            return tok;
        }
    }

    #endregion

    #region Token

    public enum TokenType
    {

            //Non terminal tokens:
            _NONE_  = 0,
            _UNDETERMINED_= 1,

            //Non terminal tokens:
            Start   = 2,
            Directive= 3,
            NameValue= 4,
            ExtProduction= 5,
            Attribute= 6,
            Params  = 7,
            Param   = 8,
            Production= 9,
            Rule    = 10,
            Subrule = 11,
            ConcatRule= 12,
            Symbol  = 13,

            //Terminal tokens:
            BRACKETOPEN= 14,
            BRACKETCLOSE= 15,
            CODEBLOCK= 16,
            COMMA   = 17,
            SQUAREOPEN= 18,
            SQUARECLOSE= 19,
            ASSIGN  = 20,
            PIPE    = 21,
            SEMICOLON= 22,
            UNARYOPER= 23,
            IDENTIFIER= 24,
            INTEGER = 25,
            DOUBLE  = 26,
            HEX     = 27,
            ARROW   = 28,
            DIRECTIVEOPEN= 29,
            DIRECTIVECLOSE= 30,
            EOF     = 31,
            STRING  = 32,
            WHITESPACE= 33,
            COMMENTLINE= 34,
            COMMENTBLOCK= 35,
    }

    public class Token
    {
        // contains all prior skipped symbols

        public string File { get; set; }

        public int Line { get; set; }

        public int Column { get; set; }

        public int StartPos { get; set; }

        public int Length => EndPos - StartPos;

        public int EndPos { get; set; }

        public string Text { get; set; }

        public List<Token> Skipped { get; set; }

        public object Value { get; set; }

        [XmlAttribute]
        public TokenType Type;

        public Token()
            : this(0, 0)
        {
        }

        public Token(int start, int end)
        {
            Type = TokenType._UNDETERMINED_;
            StartPos = start;
            EndPos = end;
            Text = ""; // must initialize with empty string, may cause null reference exceptions otherwise
            Value = null;
        }

        public void UpdateRange(Token token)
        {
            if (token.StartPos < StartPos)
            {
                StartPos = token.StartPos;
            }

            if (token.EndPos > EndPos)
            {
                EndPos = token.EndPos;
            }
        }

        public override string ToString()
        {
            return Text != null ? $"{Type} '{Text}'" : Type.ToString();
        }
    }

    #endregion
}
