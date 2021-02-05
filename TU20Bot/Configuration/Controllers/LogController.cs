using System.Linq;
using System.Collections.Generic;

using EmbedIO;
using EmbedIO.Routing;

using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;

namespace TU20Bot.Configuration.Controllers {
    [ControllerInfo(true)]
    public class LogController : ServerController {
        [Route(HttpVerbs.Get, "/logs/excel.xlsx")]
        public void getExcelLogs() {
            var workbook = new XSSFWorkbook();
            var sheet = workbook.CreateSheet("Log Data");

            var bold = workbook.CreateFont();
            bold.IsBold = true;

            static XSSFRichTextString style(string x, IFont index) {
                var text = new XSSFRichTextString(x);

                text.ApplyFont(0, x.Length, index);

                return text;
            }

            var guild = server.client.GetGuild(server.config.guildId);

            var header = sheet.CreateRow(0); 
            header.CreateCell(0).SetCellValue(style("Type", bold));
            header.CreateCell(1).SetCellValue(style("Id", bold));
            header.CreateCell(2).SetCellValue(style("Username", bold));
            header.CreateCell(3).SetCellValue(style("Name", bold));
            header.CreateCell(4).SetCellValue(style("Join Date (UTC)", bold));

            var logsSorted = server.config.logs.OrderBy(x => x.time).ToList();
            for (var a = 0; a < logsSorted.Count(); a++) {
                var log = logsSorted[a];

                var user = guild.GetUser(log.id);

                sheet.DefaultColumnWidth = 20;

                var row = sheet.CreateRow(a + 1);
                row.CreateCell(0).SetCellValue(log.logEvent switch {
                    LogEvent.UserJoin => "Joined",
                    LogEvent.UserLeave => "Left",
                    _ => "Unknown"
                });
                row.CreateCell(1).SetCellValue(log.id.ToString());
                row.CreateCell(2).SetCellValue($"{log.name}#{log.discriminator}");
                row.CreateCell(3).SetCellValue(user?.Nickname);
                row.CreateCell(4).SetCellValue(log.time);
            }

            HttpContext.Response.ContentType = "application/vnd.ms-excel";
            using var stream = HttpContext.OpenResponseStream();
            workbook.Write(stream);
        }
        
        [Route(HttpVerbs.Get, "/logs")]
        public IEnumerable<object> getLogs() {
            var guild = server.client.GetGuild(server.config.guildId);

            return server.config.logs
                .OrderBy(x => x.time)
                .Reverse()
                .Select(x => new {
                    type = x.logEvent switch {
                        LogEvent.UserJoin => "join",
                        LogEvent.UserLeave => "leave",
                        _ => "unknown"
                    },
                    id = x.id.ToString(),
                    joinDate = x.time,
                    username = x.name,
                    x.discriminator,
                    nickname = guild.GetUser(x.id)?.Nickname
                });
        }
    }
}