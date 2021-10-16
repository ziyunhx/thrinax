using System;
using System.Collections.Generic;
using Tabula;

namespace Thrinax.Models
{
    public class PDFModel
    {
        public int PageNumber { set; get; }
        public string FileName { set; get; }
        public DateTime CreateTime { set; get; }
        public DateTime ModifyTime { set; get; }
        public string Author { set; get; }
        public string Text { set; get; }
        public List<PdfPageModel> Pages { set; get; }
    }

    public class PdfPageModel
    {
        public int CurrentPage { set; get; }
        public string Text { set; get; }
        public List<string> Images { set; get; }
        public List<Table> Tables { set; get; }
        public List<string> Codes { set; get; }
        public List<string> Structs { set; get; }
    }
}
