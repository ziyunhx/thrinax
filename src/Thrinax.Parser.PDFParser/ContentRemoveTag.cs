using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Thrinax.Parser.PDFParser
{
    public class ContentRemoveTag
    {
        public string Content { set; get; }
        public int OccurCount { set; get; }
        public List<TagPosition> tagPositions { set; get; }
    }

    public class TagPosition
    {
        public int PageNumber { set; get; }
        public int LineNumber { get; set; }
    }
}
