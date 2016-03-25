using Extender.Debugging;
using Extender.UnitConversion;
using Extender.UnitConversion.Lengths;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows;

namespace Slate_EK.Models.IO
{
    public class ExcelExporter
    {
        private ExcelPackage   _BomPackage;
        private ExcelWorksheet _BomSheet;

        public FileInfo File { get; private set; }

        public ExcelExporter(string filename)
        {
            File = new FileInfo(filename);

            try
            {
                if (File.Exists)
                {
                    _BomPackage = new ExcelPackage(File);
                    _BomSheet   = _BomPackage.Workbook.Worksheets[1];
                }
                else
                    CreateNew();
            }
            catch (IOException e)
            {
                MessageBox.Show
                (
                    $"The following Excel file is open in another process: '{File.FullName}'\n\n" + 
                    "Close Excel and try again.",
                    "Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                ExceptionTools.WriteExceptionText(e, true);
            }
        }

        public void CreateNew()
        {
            if (File.Exists)
                File.Delete();

            _BomPackage   = new ExcelPackage(File);
            _BomSheet     = _BomPackage.Workbook.Worksheets.Add(NewWorksheetName);

            _BomSheet.Cells["A1"].Value = "Qty";
            _BomSheet.Cells["B1"].Value = "Size";
            _BomSheet.Cells["C1"].Value = "Pitch";
            _BomSheet.Cells["D1"].Value = "Length";
            _BomSheet.Cells["E1"].Value = "Type";
            _BomSheet.Cells["F1"].Value = "Total Mass";
            _BomSheet.Cells["G1"].Value = "Unit Price";
            _BomSheet.Cells["H1"].Value = "Sub Total";

            using (ExcelRange headerRow = _BomSheet.Cells["A1:H1"])
            {
                headerRow.Style.Font.Size = 10;
                headerRow.Style.Font.Color.SetColor(SheetHeaderColor);
                headerRow.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
            }
        }

        public void Append(UnifiedFastener[] bomList)
        {
            // 'i' is the index of the UnifiedFastener in the bomList
            // 'c' is the current row number in the excel file
            for (int i = 0, c = 1 + _BomSheet.Cells.Last().End.Row; i < bomList.Length; i++, c++)
            {
                _BomSheet.Cells["A" + c].Value   = bomList[i].Quantity;
                _BomSheet.Cells["B" + c].Value   = bomList[i].SizeDisplay;
                _BomSheet.Cells["E" + c].Value   = bomList[i].Type;
                _BomSheet.Cells["F" + c].Formula = $"A{c}*{bomList[i].Mass}";
                _BomSheet.Cells["G" + c].Value   = Math.Round(bomList[i].Price, 3);
                _BomSheet.Cells["H" + c].Formula = $"A{c}*G{c}";

                if (bomList[i].Unit == Units.Millimeters)
                {
                    _BomSheet.Cells["C" + c].Value = Math.Round(bomList[i].Pitch, 3);
                    _BomSheet.Cells["D" + c].Value = bomList[i].Length;
                }
                else
                {
                    _BomSheet.Cells["C" + c].Value = Math.Round(1f / Measure.Convert<Millimeter, Inch>(bomList[i].Pitch));
                    _BomSheet.Cells["D" + c].Value = Math.Round(Measure.Convert<Millimeter, Inch>(bomList[i].Length), 3);

                    if (UseFrac)
                        _BomSheet.Cells["D" + c].Style.Numberformat.Format = @"# ??/??";
                }
            }

            int end = _BomSheet.Cells.Last().End.Row;

            _BomSheet.Cells[$"B2:B{end}"].Style.Numberformat.Format = @"# ??/??";
            _BomSheet.Cells[$"A2:H{end}"].Style.HorizontalAlignment = ExcelHorizontalAlignment.Right;
        }

        public void Append(UnifiedFastener item)
        {
            Append(new []{item});
        }

        public bool Save()
        {
            try
            {
                _BomPackage.Save();
            }
            catch (Exception e)
            {
                MessageBox.Show
                (
                    $"Encountered an exception while saving Excel workbook '{File.FullName}':\n{e.Message}",
                    "Exception",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
                ExceptionTools.WriteExceptionText(e, true);
                return false;
            }

            return true;
        }

        public static void Test_Harness()
        {
            Debug.WriteMessage("ExcelExporter.Test_Harness()", WarnLevel.Debug);

            var t = new ExcelExporter(@"G:\User\Code\GitHub\Slate_EK\.example_files\2016-03-24\TestHarness.xlsx");
            t.CreateNew();

            var inv = new Inventory.Inventory(@"G:\User\Code\GitHub\Slate_EK\.example_files\TestInventory_004.mdf");
            t.Append(inv.Dump());

            t.Save();
        }

        private string NewWorksheetName      => Properties.Settings.Default.NewWorksheetName;
        private Color  SheetHeaderColor      => Properties.Settings.Default.WorksheetHeaderColor;
        private bool   ExportLengthFractions => Properties.Settings.Default.ExportLengthFractions;
        private bool   UseFrac               => Properties.Settings.Default.ExportLengthFractions;
    }
}
