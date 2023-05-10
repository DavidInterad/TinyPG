using System.IO;
using TinyPG.CodeGenerators;
using TinyPG.Compiler;

namespace TinyPG
{
    public class GeneratedFilesWriter
    {

        private readonly Grammar _grammar;

        public GeneratedFilesWriter(Grammar grammar)
        {
            _grammar = grammar;
        }

        public void Generate(bool debug)
        {
            var language = _grammar.Directives["TinyPG"]["Language"];
            foreach (var d in _grammar.Directives)
            {
                var generator = CodeGeneratorFactory.CreateGenerator(d.Name, language);

                if (generator != null && d.TryGetValue("FileName", out var fileName))
                {
                    generator.FileName = fileName;
                }

                if (generator != null && d["Generate"].ToLower() == "true")
                {
                    File.WriteAllText(
                        Path.Combine(_grammar.GetOutputPath(), generator.FileName),
                        generator.Generate(_grammar, debug));
                }
            }
        }
    }
}
