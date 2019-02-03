using java.io;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using technology.tabula;
using technology.tabula.writers;

namespace Thrinax.Parser.PDFParser
{
    public class TableSaveToFile
    {
        public static bool SaveTable(string filePath, Table table)
        {
            FileInfo _csvfilesave = new FileInfo(filePath);

            if (!Directory.Exists(_csvfilesave.DirectoryName))
            {
                Directory.CreateDirectory(_csvfilesave.DirectoryName);
            }

            java.io.File outputFile = new java.io.File(filePath);

            BufferedWriter bufferedWriter = null;
            try
            {
                //FileWriter fileWriter = new FileWriter(outputFile.getAbsoluteFile());
                var fileWriter = new OutputStreamWriter(new FileOutputStream(outputFile.getAbsoluteFile()), "UTF-8");
                bufferedWriter = new BufferedWriter(fileWriter);

                outputFile.createNewFile();

                technology.tabula.writers.Writer writer = new CSVWriter();
                writer.write(bufferedWriter, table);
                //extractFile(pdfFile, bufferedWriter);
            }
            catch
            {
                return false;
            }
            return true;
        }

    }
}
