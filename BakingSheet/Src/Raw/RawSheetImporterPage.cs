﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using Cathei.BakingSheet.Internal;
using Microsoft.Extensions.Logging;

namespace Cathei.BakingSheet.Raw
{
    public interface IRawSheetImporterPage
    {
        string GetCell(int col, int row);
    }

    public static class RawSheetImporterPageExtensions
    {
        public static bool IsEmptyCell(this IRawSheetImporterPage page, int col, int row)
        {
            return string.IsNullOrEmpty(page.GetCell(col, row));
        }

        /// <summary>
        /// If the row has no value in all valid column it count as empty row
        /// </summary>
        public static bool IsEmptyRow(this IRawSheetImporterPage page, int row)
        {
            for (int col = 0; IsValidColumn(page, col, row); ++col)
            {
                if (!page.IsEmptyCell(col, row))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// If the column has any value until current row, it count as valid column
        /// </summary>
        private static bool IsValidColumn(IRawSheetImporterPage page, int col, int row)
        {
            for (int prevRow = 0; prevRow <= row; ++prevRow)
            {
                if (!page.IsEmptyCell(col, prevRow))
                    return true;
            }

            return false;
        }

        public static void Import(this IRawSheetImporterPage page, RawSheetImporter importer, SheetConvertingContext context, ISheet sheet)
        {
            var idColumnName = page.GetCell(0, 0);

            if (idColumnName != nameof(ISheetRow.Id))
            {
                context.Logger.LogError("First column \"{ColumnName}\" must be named \"Id\"", idColumnName);
                return;
            }

            var columnNames = new List<string>();
            var metaRows = new List<string>();

            for (int pageRow = 0; pageRow == 0 || (page.IsEmptyCell(0, pageRow) && !page.IsEmptyRow(pageRow)); ++pageRow)
                metaRows.Add(null);

            StringBuilder builder = new StringBuilder();

            for (int pageColumn = 0; ; ++pageColumn)
            {
                int lastValidRow = -1;

                for (int pageRow = 0; pageRow < metaRows.Count; ++pageRow)
                {
                    if (!page.IsEmptyCell(pageColumn, pageRow))
                    {
                        lastValidRow = pageRow;
                        metaRows[pageRow] = page.GetCell(pageColumn, pageRow);
                    }
                }

                if (lastValidRow == -1)
                    break;

                builder.Clear();
                builder.AppendJoin(Config.Delimiter, metaRows.Take(lastValidRow + 1));

                columnNames.Add(builder.ToString());
            }

            PropertyMap propertyMap = new PropertyMap(context, sheet.GetType(), Config.IsConvertable);

            ISheetRow sheetRow = null;
            string rowId = null;
            int sameRow = 0;

            for (int pageRow = metaRows.Count; !page.IsEmptyRow(pageRow); ++pageRow)
            {
                string idCellValue = page.GetCell(0, pageRow);

                if (!string.IsNullOrEmpty(idCellValue))
                {
                    if (idCellValue.StartsWith(Config.Comment))
                        continue;

                    rowId = idCellValue;
                    sheetRow = Activator.CreateInstance(sheet.RowType) as ISheetRow;
                    sameRow = 0;
                }

                using (context.Logger.BeginScope(rowId))
                {
                    for (int pageColumn = 0; pageColumn < columnNames.Count; ++pageColumn)
                    {
                        string columnValue = columnNames[pageColumn];

                        if (columnValue.StartsWith(Config.Comment))
                            continue;

                        using (context.Logger.BeginScope(columnValue))
                        {
                            string cellValue = page.GetCell(pageColumn, pageRow);
                            if (string.IsNullOrEmpty(cellValue))
                                continue;

                            try
                            {
                                propertyMap.SetValue(sheetRow, sameRow, columnValue, cellValue, importer.StringToValue);
                            }
                            catch (Exception ex)
                            {
                                context.Logger.LogError(ex, "Failed to set value {CellValue}", cellValue);
                            }
                        }
                    }

                    if (sameRow == 0)
                    {
                        if (sheet.Contains(sheetRow.Id))
                        {
                            context.Logger.LogError("Already has row with id \"{RowId}\"", sheetRow.Id);
                        }
                        else
                        {
                            sheet.Add(sheetRow);
                        }
                    }

                    sameRow++;
                }
            }
        }
    }
}
