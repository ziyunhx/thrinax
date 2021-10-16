using System.IO;
using Tabula;
using Tabula.Writers;
using Thrinax.Enums;

namespace Thrinax.Utility
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

            try
            {
                using (FileStream fs = new FileStream(filePath, FileMode.Create))
                {
                    using (var stream = new StreamWriter(fs) { AutoFlush = true })
                    {
                        IWriter writer = null;

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

                        writer.Write(stream, table);

                        return true;
                    }
                }
            }
            catch
            {
                return false;
            }
        }

        public static string ToString(Table table, TableContainType tableContainType)
        {
            try
            {
                using (var stream = new MemoryStream())
                {
                    using (var sb = new StreamWriter(stream) { AutoFlush = true })
                    {
                        IWriter writer = null;

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

                        writer.Write(sb, table);

                        var reader = new StreamReader(stream);
                        stream.Position = 0;
                        return reader.ReadToEnd().Trim(); // trim to remove last new line
                    }
                }
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}
