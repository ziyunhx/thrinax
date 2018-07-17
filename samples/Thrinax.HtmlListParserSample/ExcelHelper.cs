using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Thrinax.Utility
{
    public class ExcelHelper
    {
        #region 通过NPOI导出小数据量结果
        /// <summary>
        /// 将对象组绑定到Excel(当T为Datarow时必须传入ColumnHeader)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="strSheetName"></param>
        /// <returns></returns>
        public static IWorkbook FillExcelSheet<T>(IEnumerable<T> collection, string strSheetName, List<string> ColumnHeader = null, Boolean IsExcel2003 = true)
        {
            IWorkbook workbook = null;
            ISheet worksheet = null;
            if (IsExcel2003)
            {
                HSSFWorkbook workbookTemp = new HSSFWorkbook();
                worksheet = workbookTemp.CreateSheet(strSheetName);
                workbook = workbookTemp;
            }
            else
            {
                XSSFWorkbook workbookTemp = new XSSFWorkbook();
                worksheet = workbookTemp.CreateSheet(strSheetName);
                workbook = workbookTemp;
            }

            var type = typeof(T);
            var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            int sheetCount = collection.Count() % 50000 == 0 ? collection.Count() / 50000 : collection.Count() / 50000 + 1;

            for (int i = 0; i < sheetCount; i++)
            {
                bool hasWriteHeader = false;
                int rowPos = 0;
                IEnumerable<T> outputData = null;
                if (i != 0)
                {
                    worksheet = workbook.CreateSheet(strSheetName + "-" + i);
                }
                if (collection.Count() >= (i + 1) * 50000)
                    outputData = collection.Skip(i * 50000).Take(50000);
                else
                    outputData = collection.Skip(i * 50000).Take(collection.Count() - i * 50000);

                foreach (var item in outputData)
                {
                    int colPos = 0;
                    if (!hasWriteHeader)
                    {
                        if (type == typeof(DataRow) || (ColumnHeader != null && ColumnHeader.Count > 0))
                        {
                            if (ColumnHeader != null)
                            {
                                foreach (string info in ColumnHeader)
                                {
                                    if (worksheet.GetRow(rowPos) != null)
                                        worksheet.GetRow(rowPos).CreateCell(colPos).SetCellValue(info);
                                    else
                                        worksheet.CreateRow(rowPos).CreateCell(colPos).SetCellValue(info);
                                    colPos++;
                                }
                                hasWriteHeader = true;
                                colPos = 0;
                                rowPos++;
                            }
                            else
                            {
                                hasWriteHeader = true;
                            }
                        }
                        else
                        {
                            foreach (var propertyInfo in properties)
                            {
                                var value = propertyInfo.GetValue(item, null);
                                if (propertyInfo != null && (propertyInfo.PropertyType == typeof(String) || propertyInfo.PropertyType == typeof(DateTime) || propertyInfo.PropertyType == typeof(DateTime?) || propertyInfo.PropertyType.IsPrimitive))
                                {
                                    var customAttributes = propertyInfo.GetCustomAttributes(typeof(DescriptionAttribute), true);
                                    if (customAttributes.Any())
                                    {
                                        var attr = customAttributes.FirstOrDefault() as DescriptionAttribute;
                                        var desc = attr.Description;

                                        if (worksheet.GetRow(rowPos) != null)
                                            worksheet.GetRow(rowPos).CreateCell(colPos).SetCellValue(desc);
                                        else
                                            worksheet.CreateRow(rowPos).CreateCell(colPos).SetCellValue(desc);
                                    }
                                    else
                                    {
                                        var desc = propertyInfo.Name;
                                        if (worksheet.GetRow(rowPos) != null)
                                            worksheet.GetRow(rowPos).CreateCell(colPos).SetCellValue(desc);
                                        else
                                            worksheet.CreateRow(rowPos).CreateCell(colPos).SetCellValue(desc);
                                    }
                                    colPos++;
                                }
                                else if (propertyInfo.PropertyType.BaseType == typeof(Enum))
                                {
                                    var customAttributes = propertyInfo.GetCustomAttributes(typeof(DescriptionAttribute), true);
                                    if (customAttributes.Any())
                                    {
                                        var attr = customAttributes.FirstOrDefault() as DescriptionAttribute;
                                        var desc = attr.Description;

                                        if (worksheet.GetRow(rowPos) != null)
                                            worksheet.GetRow(rowPos).CreateCell(colPos).SetCellValue(desc);
                                        else
                                            worksheet.CreateRow(rowPos).CreateCell(colPos).SetCellValue(desc);
                                    }
                                    else
                                    {
                                        var desc = propertyInfo.Name;
                                        if (worksheet.GetRow(rowPos) != null)
                                            worksheet.GetRow(rowPos).CreateCell(colPos).SetCellValue(desc);
                                        else
                                            worksheet.CreateRow(rowPos).CreateCell(colPos).SetCellValue(desc);
                                    }
                                    colPos++;
                                }
                            }
                            hasWriteHeader = true;
                            colPos = 0;
                            rowPos++;
                        }
                    }

                    if (type == typeof(DataRow) && ColumnHeader != null)
                    {
                        DataRow dr = item as DataRow;
                        foreach (string column in ColumnHeader)
                        {
                            var value = dr[column];
                            if (value != null && value.GetType() == typeof(DateTime))
                                value = ((DateTime)value).ToString("yyyy-MM-dd HH:mm:ss");

                            if (worksheet.GetRow(rowPos) != null)
                                worksheet.GetRow(rowPos).CreateCell(colPos).SetCellValue(value.ToString());
                            else
                                worksheet.CreateRow(rowPos).CreateCell(colPos).SetCellValue(value.ToString());
                            colPos++;
                        }
                        rowPos++;
                    }
                    else
                    {
                        foreach (var propertyInfo in properties)
                        {
                            var value = propertyInfo.GetValue(item, null);
                            if (propertyInfo != null && (propertyInfo.PropertyType == typeof(String) || propertyInfo.PropertyType == typeof(DateTime) || propertyInfo.PropertyType == typeof(DateTime?) || propertyInfo.PropertyType.IsPrimitive))
                            {
                                if (value != null)
                                {
                                    if (propertyInfo.DeclaringType == typeof(DateTime) || (value != null && value.GetType() == typeof(DateTime)) || propertyInfo.DeclaringType == typeof(DateTime?) || (value != null && value.GetType() == typeof(DateTime?)))
                                        value = ((DateTime)value).ToString("yyyy-M-d H:mm:ss");
                                    if (worksheet.GetRow(rowPos) != null)
                                        worksheet.GetRow(rowPos).CreateCell(colPos).SetCellValue(value.ToString());
                                    else
                                        worksheet.CreateRow(rowPos).CreateCell(colPos).SetCellValue(value.ToString());
                                }
                                colPos++;
                            }
                            else if (propertyInfo.PropertyType.BaseType == typeof(Enum))
                            {
                                string tempValue = value.ToString() + ":" + (int)value;
                                if (value != null)
                                {
                                    if (worksheet.GetRow(rowPos) != null)
                                        worksheet.GetRow(rowPos).CreateCell(colPos).SetCellValue(tempValue);
                                    else
                                        worksheet.CreateRow(rowPos).CreateCell(colPos).SetCellValue(tempValue);
                                }
                                colPos++;
                            }
                        }
                        rowPos++;
                    }
                }
            }

            return workbook;
        }

        /// <summary>
        /// 将对象组绑定到Excel并保存到文件
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <param name="strFileName"></param>
        /// <param name="strSheetName"></param>
        /// <returns></returns>
        public static bool SaveExcelSheet<T>(IEnumerable<T> collection, string strFileName, string strSheetName, List<string> ColumnHeader = null)
        {
            try
            {
                bool IsExcel2003 = true;
                if (strFileName.EndsWith(".xlsx"))
                    IsExcel2003 = false;

                IWorkbook workbook = FillExcelSheet(collection, strSheetName, ColumnHeader, IsExcel2003);
                using (FileStream fs = new FileStream(strFileName, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    workbook.Write(fs);
                    fs.Close();
                }
                GC.Collect();
                return true;
            }
            catch (UnauthorizedAccessException ex)
            {
                Logger.Error("当前文件被打开！");
                return false;
            }
            catch (Exception e)
            {
                Logger.Error("导出文件到Excel失败！" + e.ToString());
                return false;
            }
        }

        /// <summary>
        /// 保存DataTable到文件
        /// </summary>
        /// <param name="argDataTable"></param>
        /// <param name="strFileName"></param>
        /// <param name="strSheetName"></param>
        /// <returns></returns>
        public static bool SaveExcelSheet(DataTable argDataTable, string strFileName, string strSheetName)
        {
            List<string> columnNames = new List<string>();
            foreach (DataColumn dr in argDataTable.Columns)
            {
                columnNames.Add(dr.ColumnName);
            }

            return SaveExcelSheet(argDataTable.Select(), strFileName, strSheetName, columnNames);
        }

        #endregion

        #region 将Excel转换为指定对象
        /// <summary>
        /// 从Excel中获取数据到DataTable
        /// </summary>
        /// <param name="strFileName">Excel文件全路径(服务器路径)</param>
        /// <param name="SheetName">要获取数据的工作表名称</param>
        /// <param name="HeaderRowIndex">工作表标题行所在行号(从0开始)</param>
        /// <param name="ColumnCount">需要导入的行数，用于剔除表格中不需要的备注元素（-1为全部）</param>
        /// <returns></returns>
        public static DataTable RenderDataTableFromExcel(string strFileName, string SheetName, int HeaderRowIndex, int ColumnCount = -1)
        {
            IWorkbook workbook;
            if (strFileName.EndsWith(".xlsx"))
                workbook = new XSSFWorkbook(new FileStream(strFileName, FileMode.Open, FileAccess.Read));
            else
                workbook = new HSSFWorkbook(new FileStream(strFileName, FileMode.Open, FileAccess.Read));

            return RenderDataTableFromExcel(workbook, SheetName, HeaderRowIndex, ColumnCount);
        }

        /// <summary>
        /// 从Excel中获取数据到DataTable
        /// </summary>
        /// <param name="strFileName">Excel文件全路径(服务器路径)</param>
        /// <param name="SheetIndex">要获取数据的工作表序号(从0开始)</param>
        /// <param name="HeaderRowIndex">工作表标题行所在行号(从0开始)</param>
        /// <param name="ColumnCount">需要导入的行数，用于剔除表格中不需要的备注元素（-1为全部）</param>
        /// <returns></returns>
        public static DataTable RenderDataTableFromExcel(string strFileName, int SheetIndex, int HeaderRowIndex, int ColumnCount = -1)
        {
            IWorkbook workbook;
            if (strFileName.EndsWith(".xlsx"))
                workbook = new XSSFWorkbook(new FileStream(strFileName, FileMode.Open, FileAccess.Read));
            else
                workbook = new HSSFWorkbook(new FileStream(strFileName, FileMode.Open, FileAccess.Read));

            string SheetName = workbook.GetSheetName(SheetIndex);
            return RenderDataTableFromExcel(workbook, SheetName, HeaderRowIndex, ColumnCount);
        }

        /// <summary>
        /// 从Excel中获取数据到DataTable
        /// </summary>
        /// <param name="ExcelFileStream">Excel文件流</param>
        /// <param name="SheetIndex">要获取数据的工作表序号(从0开始)</param>
        /// <param name="HeaderRowIndex">工作表标题行所在行号(从0开始)</param>
        /// <param name="ColumnCount">需要导入的行数，用于剔除表格中不需要的备注元素（-1为全部）</param>
        /// <returns></returns>
        public static DataTable RenderDataTableFromExcel(IWorkbook workbook, int SheetIndex, int HeaderRowIndex, int ColumnCount = -1)
        {
            string SheetName = workbook.GetSheetName(SheetIndex);
            return RenderDataTableFromExcel(workbook, SheetName, HeaderRowIndex, ColumnCount);
        }

        /// <summary>
        /// 从Excel中获取数据到DataTable
        /// </summary>
        /// <param name="workbook">要处理的工作薄</param>
        /// <param name="SheetName">要获取数据的工作表名称</param>
        /// <param name="HeaderRowIndex">工作表标题行所在行号(从0开始)</param>
        /// <param name="ColumnCount">需要导入的行数，用于剔除表格中不需要的备注元素（-1为全部）</param>
        /// <returns></returns>
        public static DataTable RenderDataTableFromExcel(IWorkbook workbook, string SheetName, int HeaderRowIndex, int ColumnCount)
        {
            ISheet sheet = workbook.GetSheet(SheetName);
            DataTable table = new DataTable();
            try
            {
                IRow headerRow = sheet.GetRow(HeaderRowIndex);
                int cellCount = ColumnCount == -1 ? headerRow.PhysicalNumberOfCells : ColumnCount;

                for (int i = headerRow.FirstCellNum; i < cellCount; i++)
                {
                    DataColumn column = new DataColumn(headerRow.GetCell(i).StringCellValue);
                    table.Columns.Add(column);
                }

                int rowCount = sheet.LastRowNum;

                #region 循环各行各列,写入数据到DataTable
                for (int i = (sheet.FirstRowNum + 1); i < sheet.LastRowNum; i++)
                {
                    IRow row = sheet.GetRow(i);
                    DataRow dataRow = table.NewRow();
                    for (int j = row.FirstCellNum; j < cellCount; j++)
                    {
                        ICell cell = row.GetCell(j);
                        if (cell == null)
                        {
                            dataRow[j] = null;
                        }
                        else
                        {
                            //dataRow[j] = cell.ToString();
                            switch (cell.CellType)
                            {
                                case CellType.Blank:
                                    dataRow[j] = null;
                                    break;
                                case CellType.Boolean:
                                    dataRow[j] = cell.BooleanCellValue;
                                    break;
                                case CellType.Numeric:
                                    try //数字类型时可能是日期，故先使用date来适配
                                    {
                                        dataRow[j] = cell.DateCellValue;
                                    }
                                    catch
                                    {
                                        dataRow[j] = cell.NumericCellValue;
                                    }                                    
                                    break;
                                case CellType.String:
                                    dataRow[j] = cell.StringCellValue;
                                    break;
                                case CellType.Error:
                                    dataRow[j] = cell.ErrorCellValue;
                                    break;
                                case CellType.Formula:
                                default:
                                    dataRow[j] = "=" + cell.CellFormula;
                                    break;
                            }
                        }
                    }
                    table.Rows.Add(dataRow);
                    //dataRow[j] = row.GetCell(j).ToString();
                }
                #endregion
            }
            catch (System.Exception ex)
            {
                table.Clear();
                table.Columns.Clear();
                table.Columns.Add("出错了");
                DataRow dr = table.NewRow();
                dr[0] = ex.Message;
                table.Rows.Add(dr);
                return table;
            }
            finally
            {
                //sheet.Dispose();
                workbook = null;
                sheet = null;
            }
            #region 清除最后的空行
            for (int i = table.Rows.Count - 1; i > 0; i--)
            {
                bool isnull = true;
                for (int j = 0; j < table.Columns.Count; j++)
                {
                    if (table.Rows[i][j] != null)
                    {
                        if (table.Rows[i][j].ToString() != "")
                        {
                            isnull = false;
                            break;
                        }
                    }
                }
                if (isnull)
                {
                    table.Rows[i].Delete();
                }
            }
            #endregion
            return table;
        }

        /// <summary>
        /// 从Excel中获取数据到Object
        /// </summary>
        /// <param name="strFileName">Excel文件全路径(服务器路径)</param>
        /// <param name="SheetName">要获取数据的工作表名称</param>
        /// <param name="HeaderRowIndex">工作表标题行所在行号(从0开始)</param>
        /// <param name="ColumnCount">需要导入的行数，用于剔除表格中不需要的备注元素（-1为全部）</param>
        /// <returns></returns>
        public static IEnumerable<T> RenderObjFromExcel<T>(string strFileName, string SheetName, int HeaderRowIndex, int ColumnCount = -1)
        {
            IWorkbook workbook;
            if (strFileName.EndsWith(".xlsx"))
                workbook = new XSSFWorkbook(new FileStream(strFileName, FileMode.Open, FileAccess.Read));
            else
                workbook = new HSSFWorkbook(new FileStream(strFileName, FileMode.Open, FileAccess.Read));

            return RenderObjFromExcel<T>(workbook, SheetName, HeaderRowIndex, ColumnCount);
        }

        /// <summary>
        /// 从Excel中获取数据到Object
        /// </summary>
        /// <param name="strFileName">Excel文件全路径(服务器路径)</param>
        /// <param name="SheetIndex">要获取数据的工作表序号(从0开始)</param>
        /// <param name="HeaderRowIndex">工作表标题行所在行号(从0开始)</param>
        /// <param name="ColumnCount">需要导入的行数，用于剔除表格中不需要的备注元素（-1为全部）</param>
        /// <returns></returns>
        public static IEnumerable<T> RenderObjFromExcel<T>(string strFileName, int SheetIndex, int HeaderRowIndex, int ColumnCount = -1)
        {
            IWorkbook workbook;
            if (strFileName.EndsWith(".xlsx"))
                workbook = new XSSFWorkbook(new FileStream(strFileName, FileMode.Open, FileAccess.Read));
            else
                workbook = new HSSFWorkbook(new FileStream(strFileName, FileMode.Open, FileAccess.Read));

            string SheetName = workbook.GetSheetName(SheetIndex);
            return RenderObjFromExcel<T>(workbook, SheetName, HeaderRowIndex, ColumnCount);
        }

        /// <summary>
        /// 从Excel中获取数据到Object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="workbook"></param>
        /// <param name="SheetIndex"></param>
        /// <param name="HeaderRowIndex"></param>
        /// <param name="ColumnCount">需要导入的行数，用于剔除表格中不需要的备注元素（-1为全部）</param>
        /// <returns></returns>
        public static IEnumerable<T> RenderObjFromExcel<T>(IWorkbook workbook, int SheetIndex, int HeaderRowIndex, int ColumnCount = -1)
        {
            string SheetName = workbook.GetSheetName(SheetIndex);
            return RenderObjFromExcel<T>(workbook, SheetName, HeaderRowIndex, ColumnCount);
        }

        /// <summary>
        /// 从Excel中获取数据到Object
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="workbook"></param>
        /// <param name="SheetName"></param>
        /// <param name="HeaderRowIndex"></param>
        /// <param name="ColumnCount">需要导入的行数，用于剔除表格中不需要的备注元素（-1为全部）</param>
        /// <returns></returns>
        public static IEnumerable<T> RenderObjFromExcel<T>(IWorkbook workbook, string SheetName, int HeaderRowIndex, int ColumnCount)
        {
            ISheet sheet = workbook.GetSheet(SheetName);
            List<T> result = new List<T>();

            try
            {
                IRow headerRow = sheet.GetRow(HeaderRowIndex);
                //int cellCount = headerRow.PhysicalNumberOfCells;
                int cellCount = ColumnCount == -1 ? headerRow.PhysicalNumberOfCells : ColumnCount;

                int rowCount = sheet.PhysicalNumberOfRows;

                #region 循环各行各列,写入数据到DataTable
                for (int i = HeaderRowIndex + 1; i < sheet.PhysicalNumberOfRows; i++)
                {
                    IRow row = sheet.GetRow(i);

                    int j = 0;
                    T entity = System.Activator.CreateInstance<T>();
                    Type type = typeof(T);
                    //取得属性集合
                    PropertyInfo[] pi = type.GetProperties();
                    foreach (PropertyInfo item in pi.Take(cellCount))
                    {
                        ICell cell = row.GetCell(j);
                        //给属性赋值
                        if (cell != null)
                        {
                            switch (cell.CellType)
                            {
                                case CellType.Blank:
                                    try
                                    {
                                        item.SetValue(entity, Convert.ChangeType(null, item.PropertyType), null);
                                    }
                                    catch { }
                                    break;
                                case CellType.Boolean:
                                    try
                                    {
                                        item.SetValue(entity, Convert.ChangeType(cell.BooleanCellValue, item.PropertyType), null);
                                    }
                                    catch { }
                                    break;
                                case CellType.Numeric:
                                    try //数字类型时可能是日期，故先使用date来适配
                                    {
                                        item.SetValue(entity, Convert.ChangeType(cell.DateCellValue, item.PropertyType), null);
                                    }
                                    catch
                                    {
                                        try 
                                        {
                                            item.SetValue(entity, Convert.ChangeType(cell.NumericCellValue, item.PropertyType), null);
                                        }
                                        catch //也有可能是枚举
                                        {
                                            try
                                            {
                                                item.SetValue(entity, Convert.ToInt32(cell.NumericCellValue), null);
                                            }
                                            catch
                                            {
                                                try {
                                                    item.SetValue(entity, Convert.ToSByte(cell.NumericCellValue), null);
                                                }
                                                catch (Exception ex2){
                                                }
                                            }
                                        }
                                    }
                                    break;
                                case CellType.String:
                                    try
                                    {
                                        item.SetValue(entity, Convert.ChangeType(cell.StringCellValue, item.PropertyType), null);
                                    }
                                    catch
                                    {
                                        try
                                        {
                                            if (cell.StringCellValue.Contains(":"))
                                            {
                                                if (item.PropertyType.IsEnum)
                                                {
                                                    int tempValue = 0;
                                                    Int32.TryParse(cell.StringCellValue.Split(':')[1], out tempValue);
                                                    item.SetValue(entity, tempValue, null);
                                                }
                                                else
                                                {
                                                    sbyte tempValue = 0;
                                                    SByte.TryParse(cell.StringCellValue.Split(':')[1], out tempValue);
                                                    item.SetValue(entity, tempValue, null);
                                                }
                                            }
                                        }
                                        catch (Exception ex3) { }
                                    }
                                    break;
                                case CellType.Error:
                                    item.SetValue(entity, Convert.ChangeType(cell.ErrorCellValue, item.PropertyType), null);
                                    break;
                                case CellType.Formula:
                                default:
                                    item.SetValue(entity, Convert.ChangeType("=" + cell.CellFormula, item.PropertyType), null);
                                    break;
                            }
                        }
                        j++;
                    }
                    if (entity != null)
                        result.Add(entity);
                }
                #endregion
            }
            catch (System.Exception ex)
            {                
            }
            finally
            {
                //sheet.Dispose();
                workbook = null;
                sheet = null;
            }
            return result;
        }

        #endregion
    }
}