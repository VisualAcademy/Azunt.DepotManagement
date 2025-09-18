using Microsoft.AspNetCore.Mvc;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using System.IO;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Azunt.DepotManagement;

// using Microsoft.AspNetCore.Authorization;

namespace Azunt.Apis.Depots
{
    //[Authorize(Roles = "Administrators")]
    [Route("api/[controller]")]
    [ApiController]
    public class DepotDownloadController : ControllerBase
    {
        private readonly IDepotRepository _repository;

        public DepotDownloadController(IDepotRepository repository)
        {
            _repository = repository;
        }

        /// <summary>
        /// 창고 리스트 엑셀 다운로드
        /// GET /api/DepotDownload/ExcelDown
        /// </summary>
        [HttpGet("ExcelDown")]
        public async Task<IActionResult> ExcelDown()
        {
            var items = (await _repository.GetAllAsync()).ToList();

            if (!items.Any())
            {
                return NotFound("No depot records found.");
            }

            // 투영: 문자열/날짜 통일, Active는 0/1 숫자
            var rows = items.Select(m => new
            {
                m.Id,
                m.Name,
                CreatedAt = FormatLocalTimestamp(m.CreatedAt),
                Active = BoolToInt(m.Active),
                m.CreatedBy
            }).ToList();

            byte[] content;
            using (var ms = new MemoryStream())
            {
                using (var doc = SpreadsheetDocument.Create(ms, SpreadsheetDocumentType.Workbook, true))
                {
                    // 워크북/워크시트 구성
                    var wbPart = doc.AddWorkbookPart();
                    wbPart.Workbook = new Workbook();

                    var wsPart = wbPart.AddNewPart<WorksheetPart>();
                    var sheetData = new SheetData();

                    // 컬럼 폭(B~F = 2~6) 22
                    var columns = new Columns(new Column { Min = 2U, Max = 6U, Width = 22D, CustomWidth = true });

                    // 스타일
                    var styles = wbPart.AddNewPart<WorkbookStylesPart>();
                    styles.Stylesheet = BuildStylesheet();
                    styles.Stylesheet.Save();

                    // 워크시트 = Columns + SheetData
                    wsPart.Worksheet = new Worksheet(columns, sheetData);

                    // 시트 등록
                    var sheets = wbPart.Workbook.AppendChild(new Sheets());
                    sheets.Append(new Sheet
                    {
                        Id = wbPart.GetIdOfPart(wsPart),
                        SheetId = 1U,
                        Name = "Depots"
                    });

                    // 스타일 인덱스
                    const uint headerStyle = 1; // Bold + White on DarkBlue + Medium border
                    const uint bodyStyle = 2;   // WhiteSmoke + Medium border

                    // 헤더(B2~F2)
                    string[] headers = { "Id", "Name", "CreatedAt", "Active", "CreatedBy" };
                    string[] cols = { "B", "C", "D", "E", "F" };

                    uint rowIndex = 2;
                    var headerRow = new Row { RowIndex = rowIndex };
                    for (int i = 0; i < headers.Length; i++)
                    {
                        headerRow.Append(MakeTextCell($"{cols[i]}{rowIndex}", headers[i], headerStyle));
                    }
                    sheetData.Append(headerRow);

                    // 데이터(B3~)
                    rowIndex = 3;
                    foreach (var r in rows)
                    {
                        var dataRow = new Row { RowIndex = rowIndex };
                        dataRow.Append(MakeNumberCell($"{cols[0]}{rowIndex}", r.Id, bodyStyle));
                        dataRow.Append(MakeTextCell($"{cols[1]}{rowIndex}", r.Name ?? string.Empty, bodyStyle));
                        dataRow.Append(MakeTextCell($"{cols[2]}{rowIndex}", r.CreatedAt ?? string.Empty, bodyStyle));
                        dataRow.Append(MakeNumberCell($"{cols[3]}{rowIndex}", r.Active, bodyStyle)); // 0/1
                        dataRow.Append(MakeTextCell($"{cols[4]}{rowIndex}", r.CreatedBy ?? string.Empty, bodyStyle));
                        sheetData.Append(dataRow);
                        rowIndex++;
                    }

                    // 조건부 서식: Active 컬럼(E3:E{마지막})
                    var cfRange = $"E3:E{rows.Count + 2}";
                    AddThreeColorScaleConditionalFormatting(wsPart.Worksheet, cfRange);

                    wbPart.Workbook.Save();
                }

                content = ms.ToArray();
            }

            return File(
                content,
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                $"{DateTime.Now:yyyyMMddHHmmss}_Depots.xlsx"
            );
        }

        // ================= Helpers =================

        private static Stylesheet BuildStylesheet()
        {
            // Fonts
            var fonts = new Fonts() { Count = 2U };
            // 0: 기본
            fonts.Append(new Font(
                new FontSize() { Val = 11D },
                new Color() { Theme = 1 },
                new FontName() { Val = "Calibri" }
            ));
            // 1: 헤더(굵게, 흰색)
            fonts.Append(new Font(
                new Bold(),
                new FontSize() { Val = 11D },
                new Color() { Rgb = "FFFFFFFF" },
                new FontName() { Val = "Calibri" }
            ));

            // Fills (0=None, 1=Gray125 예약)
            var fills = new Fills() { Count = 4U };
            fills.Append(new Fill(new PatternFill() { PatternType = PatternValues.None }));    // 0
            fills.Append(new Fill(new PatternFill() { PatternType = PatternValues.Gray125 })); // 1
            // 2: 헤더 배경 (DarkBlue)
            fills.Append(new Fill(new PatternFill(
                new ForegroundColor() { Rgb = "FF00008B" },
                new BackgroundColor() { Indexed = 64U }
            )
            { PatternType = PatternValues.Solid }));
            // 3: 본문 배경 (WhiteSmoke)
            fills.Append(new Fill(new PatternFill(
                new ForegroundColor() { Rgb = "FFF5F5F5" },
                new BackgroundColor() { Indexed = 64U }
            )
            { PatternType = PatternValues.Solid }));

            // Borders
            var borders = new Borders() { Count = 2U };
            borders.Append(new Border()); // 0 기본
            borders.Append(new Border(    // 1 Medium 사방
                new LeftBorder() { Style = BorderStyleValues.Medium },
                new RightBorder() { Style = BorderStyleValues.Medium },
                new TopBorder() { Style = BorderStyleValues.Medium },
                new BottomBorder() { Style = BorderStyleValues.Medium },
                new DiagonalBorder()
            ));

            // CellFormats
            var cellFormats = new CellFormats() { Count = 3U };
            cellFormats.Append(new CellFormat()); // 0 기본
            // 1: 헤더
            cellFormats.Append(new CellFormat
            {
                FontId = 1U,
                FillId = 2U,
                BorderId = 1U,
                ApplyFont = true,
                ApplyFill = true,
                ApplyBorder = true,
                Alignment = new Alignment { Horizontal = HorizontalAlignmentValues.Left, Vertical = VerticalAlignmentValues.Center }
            });
            // 2: 본문
            cellFormats.Append(new CellFormat
            {
                FontId = 0U,
                FillId = 3U,
                BorderId = 1U,
                ApplyFont = true,
                ApplyFill = true,
                ApplyBorder = true,
                Alignment = new Alignment { Horizontal = HorizontalAlignmentValues.Left, Vertical = VerticalAlignmentValues.Center }
            });

            return new Stylesheet(fonts, fills, borders, cellFormats);
        }

        private static Cell MakeTextCell(string cellRef, string text, uint styleIndex) =>
            new Cell
            {
                CellReference = cellRef,
                DataType = CellValues.String, // SharedString 미사용
                CellValue = new CellValue(text ?? string.Empty),
                StyleIndex = styleIndex
            };

        private static Cell MakeNumberCell(string cellRef, long number, uint styleIndex) =>
            new Cell
            {
                CellReference = cellRef,
                DataType = CellValues.Number,
                CellValue = new CellValue(number.ToString()),
                StyleIndex = styleIndex
            };

        private static string FormatLocalTimestamp(object value)
        {
            return value switch
            {
                DateTimeOffset dto => dto.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
                DateTime dt => dt.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss"),
                _ => value?.ToString() ?? string.Empty
            };
        }

        private static int BoolToInt(object value)
        {
            return value switch
            {
                bool b => b ? 1 : 0,
                int i => i != 0 ? 1 : 0,
                string s when bool.TryParse(s, out var b) => b ? 1 : 0,
                string s when int.TryParse(s, out var i) => i != 0 ? 1 : 0,
                _ => 0
            };
        }

        private static void AddThreeColorScaleConditionalFormatting(Worksheet ws, string a1Range)
        {
            // 3색 스케일: Min -> Percentile(50) -> Max = Red -> White -> Green
            var cf = ws.Elements<ConditionalFormatting>()
                       .FirstOrDefault(c => c.SequenceOfReferences?.InnerText == a1Range);
            if (cf == null)
            {
                cf = new ConditionalFormatting
                {
                    SequenceOfReferences = new ListValue<StringValue> { InnerText = a1Range }
                };
                ws.Append(cf);
            }

            var rule = new ConditionalFormattingRule
            {
                Type = ConditionalFormatValues.ColorScale,
                Priority = 1
            };

            var colorScale = new ColorScale();

            colorScale.Append(new ConditionalFormatValueObject
            {
                Type = ConditionalFormatValueObjectValues.Min
            });
            colorScale.Append(new ConditionalFormatValueObject
            {
                Type = ConditionalFormatValueObjectValues.Percentile,
                Val = "50" // <- StringValue 필요 (컴파일 오류 방지)
            });
            colorScale.Append(new ConditionalFormatValueObject
            {
                Type = ConditionalFormatValueObjectValues.Max
            });

            colorScale.Append(new Color { Rgb = "FFFF0000" }); // Red
            colorScale.Append(new Color { Rgb = "FFFFFFFF" }); // White
            colorScale.Append(new Color { Rgb = "FF00FF00" }); // Green

            rule.Append(colorScale);
            cf.Append(rule);
        }
    }
}
