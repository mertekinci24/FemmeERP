using System;
using InventoryERP.Presentation.Helpers;
using Xunit;

namespace Tests.Reports
{
    public class ExportFileNameTests
    {
        [Fact]
        public void BuildAndSanitize_FileNameContainsPartsAndIsSanitized()
        {
            var from = new DateTime(2025, 10, 1);
            var to = new DateTime(2025, 10, 31);
            var partner = "ACME Corp / Ltd";
            var search = "invoice #123";

            var baseName = ExportFileNameHelper.BuildBaseName(from, to, partner, search);
            Assert.Contains("Documents_", baseName);
            Assert.Contains("_to_", baseName);
            Assert.Contains("Partner-", baseName);
            Assert.Contains("Search-", baseName);

            var fileName = ExportFileNameHelper.SanitizeFileName(baseName + ".pdf");
            Assert.True(fileName.Length <= 100);
            // ensure no illegal chars
            foreach (var c in System.IO.Path.GetInvalidFileNameChars()) Assert.DoesNotContain(c, fileName);
            // ensure spaces removed
            Assert.DoesNotContain(' ', fileName);
        }
    }
}
