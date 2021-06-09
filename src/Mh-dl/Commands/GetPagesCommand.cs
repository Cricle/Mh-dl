using Anf;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Threading.Tasks;

namespace Mh_dl.Commands
{
    public class GetPagesCommand : AsyncCommand<GetPagesCommand.GetPagesCommandSettings>
    {
        public override ValidationResult Validate(CommandContext context, GetPagesCommandSettings settings)
        {
            if (string.IsNullOrEmpty(settings.ChapterUrl) ||
                !UrlHelper.IsWebsite(settings.ChapterUrl))
            {
                return ValidationResult.Error("输入的不是合法地址");
            }
            return ValidationResult.Success();
        }
        public override async Task<int> ExecuteAsync(CommandContext context, GetPagesCommandSettings settings)
        {
            var res=AnsiConsole.Status()
                .Start("正在从远程服务器获取实体", ctx =>
                {
                    var url = settings.EntityUrl;
                    if (url==null)
                    {
                        AnsiConsole.MarkupLine("[yellow]缺失实体地址，寻找可用解析器使用章节地址[/]");
                        url = settings.ChapterUrl;
                    }
                    var eng = AppEngine.GetRequiredService<ComicEngine>();
                    var type = eng.GetComicSourceProviderType(url);
                    if (type == null)
                    {
                        AnsiConsole.MarkupLine("[red]当前不支持路径{0}[/]", url);
                    }
                    return type;
                });
            if (res==null)
            {
                return -1;
            }
            var factory = AppEngine.GetRequiredService<IServiceScopeFactory>();
            using (var scope = factory.CreateScope())
            {
                var targetEng = (IComicSourceProvider)scope.ServiceProvider.GetRequiredService(res.ProviderType);
                try
                {
                    AnsiConsole.Markup("开始从目标地址");
                    AnsiConsole.Render(new Markup($"[green]{settings.ChapterUrl}[/]", new Style(link: settings.ChapterUrl)));
                    AnsiConsole.MarkupLine("获取数据");

                    var pages = await AnsiConsole.Status()
                        .StartAsync("正在获取章节内容", nctx => targetEng.GetPagesAsync(settings.ChapterUrl));
                    if (pages == null)
                    {
                        AnsiConsole.MarkupLine("[yellow]无法获取章节信息[/]");
                        return -1;
                    }
                    AnsiConsole.MarkupLine("地址[green]{0}[/]所包含以下图片", settings.ChapterUrl);
                    var table = new Table();
                    table.AddColumns("名字/序号", "地址");
                    foreach (var item in pages)
                    {
                        table.AddRow(
                            new Markup(item.Name),
                            new Markup(item.TargetUrl, new Style(link: item.TargetUrl)));
                    }
                    AnsiConsole.Render(table);
                }
                catch (Exception ex)
                {
                    AnsiConsole.WriteException(ex);
                }
                return 0;
            }

        }

        public class GetPagesCommandSettings:CommandSettings
        {
            [CommandArgument(0, "[ChapterUrl]")]
            public string ChapterUrl { get; set; }

            [CommandOption("-e|--entityurl <ENTITYURL>")]
            public string EntityUrl { get; set; }

        }
    }
}
