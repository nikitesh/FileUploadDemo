
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using ExcelDataReader;
using CsvHelper;
using System.Globalization;

namespace DemoFileUpload.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FileUploadController : ControllerBase
    {
        [HttpPost("upload")]
        public async Task<IActionResult> UploadFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No file uploaded.");
            }

            var fileExtension = Path.GetExtension(file.FileName).ToLower();
            
            if (fileExtension == ".csv")
            {
                var csvData = await ProcessCsvFile(file);
                return Ok(new { Data = csvData });
            }
            else if (fileExtension == ".xlsx")
            {
                var excelData = await ProcessExcelFile(file);
                return Ok(new { Data = excelData });
            }
            else
            {
                return BadRequest("Unsupported file type. Please upload a CSV or Excel (.xlsx) file.");
            }
        }

        private async Task<List<string[]>> ProcessCsvFile(IFormFile file)
        {
            var records = new List<string[]>();
            using (var stream = new StreamReader(file.OpenReadStream()))
            {
                using (var csv = new CsvReader(stream, CultureInfo.InvariantCulture))
                {
                    await foreach (var record in csv.GetRecordsAsync<dynamic>())
                    {
                        records.Add(record.Values.ToArray());
                    }
                }
            }
            return records;
        }

        private async Task<List<string[]>> ProcessExcelFile(IFormFile file)
        {
            var records = new List<string[]>();
            System.Text.Encoding.RegisterProvider(System.Text.CodePagesEncodingProvider.Instance);
            using (var stream = file.OpenReadStream())
            {
                using (var reader = ExcelReaderFactory.CreateReader(stream))
                {
                    var result = reader.AsDataSet(new ExcelDataSetConfiguration()
                    {
                        ConfigureDataTable = (_) => new ExcelDataTableConfiguration()
                        {
                            UseHeaderRow = true
                        }
                    });

                    DataTable table = result.Tables[0];
                    foreach (DataRow row in table.Rows)
                    {
                        var rowData = row.ItemArray.Select(x => x.ToString()).ToArray();
                        records.Add(rowData);
                    }
                }
            }
            return records;
        }
    }
}
