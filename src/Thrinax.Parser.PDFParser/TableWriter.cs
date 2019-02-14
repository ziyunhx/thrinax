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
    public class TableWriter
    {
        public static bool ToFile(string filePath, Table table, TableContainType tableContainType)
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
                var fileWriter = new OutputStreamWriter(new FileOutputStream(outputFile.getAbsoluteFile()), "UTF-8");
                bufferedWriter = new BufferedWriter(fileWriter);

                outputFile.createNewFile();

                technology.tabula.writers.Writer writer = null;
                switch (tableContainType)
                {
                    case TableContainType.CSV:
                        writer = new CSVWriter();
                        break;
                    case TableContainType.Json:
                        writer = new JSONWriter();
                        break;
                    case TableContainType.TSV:
                        writer = new TSVWriter();
                        break;
                    default:
                        writer = new JSONWriter();
                        break;
                }
                
                writer.write(bufferedWriter, table);
            }
            catch
            {
                return false;
            }
            return true;
        }

        public static string ToString(Table table, TableContainType tableContainType)
        {
            java.io.StringWriter stringWriter = new java.io.StringWriter();
            try
            {
                technology.tabula.writers.Writer writer = null;
                switch (tableContainType)
                {
                    case TableContainType.CSV:
                        writer = new CSVWriter();
                        break;
                    case TableContainType.Json:
                        writer = new JSONWriter();
                        break;
                    case TableContainType.TSV:
                        writer = new TSVWriter();
                        break;
                    default:
                        writer = new JSONWriter();
                        break;
                }

                writer.write(stringWriter, table);
            }
            catch
            {
                return string.Empty;
            }
            return stringWriter.toString();
        }
    }
}
