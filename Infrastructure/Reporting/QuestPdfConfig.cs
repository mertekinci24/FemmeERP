using System;
using System.IO;
using QuestPDF.Drawing;
using QuestPDF.Infrastructure;

namespace InventoryERP.Infrastructure.Reporting
{
    public static class QuestPdfConfig
    {
        private static bool _initialized;

        public static void EnsureInitialized()
        {
            if (_initialized) return;
            QuestPDF.Settings.License = LicenseType.Community;

            try
            {
                // Prefer Arial from system fonts for Turkish glyph coverage
                var arial = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Windows), "Fonts", "arial.ttf");
                if (File.Exists(arial))
                {
                    FontManager.RegisterFont(File.OpenRead(arial));
                }
            }
            catch
            {
                // Best-effort; if not available, rely on default font fallback
            }

            _initialized = true;
        }
    }
}
