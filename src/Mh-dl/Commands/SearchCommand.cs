using Anf;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;
using Spectre.Console.Cli;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mh_dl.Commands
{
    public class SearchCommand : AsyncCommand<SearchCommand.SearchCommandSettings>
    {
        public override Task<int> ExecuteAsync(CommandContext context, SearchCommandSettings settings)
        {
            return AnsiConsole.Status()
                .StartAsync($"正在搜索[green]{settings.Keyword}[/]", async ctx =>
                  {
                      var take = settings.Take == 0 ? 20 : settings.Take;
                      var eng = AppEngine.GetRequiredService<SearchEngine>();
                      using (var res = await eng.GetSearchCursorAsync(settings.Keyword,0, take))
                      {
                          ctx.Status="已成功建立搜索连接";
                          await res.MoveNextAsync();
                          var table = new Table();
                          table.AddColumn("提供者");
                          table.AddColumn("名字");
                          table.AddColumn("路径");
                          foreach (var item in res.Current.Snapshots)
                          {
                              var first = true;
                              foreach (var source in item.Sources)
                              {
                                  if (first)
                                  {
                                      first = false;
                                      table.AddRow(
                                        new Markup(item.Name),
                                        new Markup(source.Name, new Style(link: source.TargetUrl)),
                                        new Markup(source.TargetUrl));
                                  }
                                  else
                                  {
                                      table.AddRow(
                                        new Markup(string.Empty),
                                        new Markup(source.Name, new Style(link: source.TargetUrl)),
                                        new Markup(source.TargetUrl));
                                  }
                              }
                          }
                          AnsiConsole.Render(table);
                          AnsiConsole.MarkupLine($"总共有[green]{res.Current.Total}[/]个结果,当前只显示了[green]{take}[/]个");
                          if (res.Current != null && res.Current.Snapshots.Length != 0)
                          {
                              var url = res.Current.Snapshots[0].TargetUrl;
                              AnsiConsole.Markup("原数据地址: ");
                              AnsiConsole.Render(new Markup(url, new Style(link: url)));
                          }
                          return 0;
                      }
                  });
        }

        public class SearchCommandSettings : CommandSettings
        {
            [CommandArgument(0, "[keyword]")]
            public string Keyword { get; set; }
            
            [CommandOption("-t|--take <TAKE>")]
            public int Take { get; set; }
        }
    }
}
