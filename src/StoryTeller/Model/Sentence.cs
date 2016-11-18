using System.Collections.Generic;
using System.Linq;
using Baseline;
using StoryTeller.Util;

namespace StoryTeller.Model
{
    public class Sentence : GrammarModel, IModelWithCells
    {
        private readonly IList<Cell> _cells = new List<Cell>();

        public Cell[] cells
        {
            get { return _cells.ToArray(); }
            set
            {
                _cells.Clear();
                if (value != null)
                {
                    _cells.AddRange(value);
                }
            }
        }

        public string format;


        public Sentence() : base("sentence")
        {
        }

        public Sentence(string key, string format) : base("sentence")
        {
            this.key = key;
            this.format = format;

            var cellKeys = TemplateParser.GetSubstitutions(format);
            foreach (var cellKey in cellKeys)
            {
                var cell = Cell.For<string>(cellKey);
                _cells.Add(cell);
            }
        }

        public Cell FindCell(string cell)
        {
            return cells.FirstOrDefault(x => x.Key == cell);
        }

        public void AddCell(Cell cell)
        {
            _cells.Add(cell);
        }

        public override GrammarModel ApplyOverrides(GrammarModel grammar)
        {
            var sentence = new Sentence {key = key};

            var over = grammar as Sentence;
            if (over == null)
            {
                sentence.format = format;
                sentence.cells = cells.Select(c => c.ApplyOverrides(null)).ToArray();
                return sentence;
            }

            sentence.format = over.format.IsNotEmpty() ? over.format : format;
            sentence.cells = cells?.Select(c =>
            {
                var match = sentence.cells.FirstOrDefault(x => x.Key == c.Key);
                return c.ApplyOverrides(match);
            }).ToArray();

            var keys = sentence.cells.Select(x => x.Key).ToList();
            var missing = over.cells.Where(x => !keys.Contains(x.Key));
            missing.Each(c =>
            {
                sentence.AddCell(c.ApplyOverrides(null));
            });

            return sentence;
        }

        public override string ToMissingCode()
        {
            string args = _cells.OrderBy(x => x.result).Select(x => x.ToDeclaration()).Join(", ");
            var returns = _cells.Where(x => x.result).ToArray();

            var returnType = "void";
            var decoration = "";

            if (returns.Length == 1)
            {
                args = _cells.Where(x => !x.result).Select(x => x.ToDeclaration()).Join(", ");
                returnType = "string";
                decoration = $"[return: AliasAs(\"{returns.Single().Key}\")]";
            }

            return $@"
        {decoration}
        public {returnType} {key}({args})
        {{
            throw new NotImplementedException();
        }}
";
        }

        public override string ToString()
        {
            return $"Sentence: {format} ({key})";
        }

        public override string TitleOrFormat()
        {
            return format;
        }
    }
}
