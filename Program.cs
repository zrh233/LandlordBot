using System.Threading.Tasks;
using Sora;
using Sora.Interfaces;
using Sora.Net.Config;
using Sora.Util;
using YukariToolBox.LightLog;
using System.Net.Http;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;

namespace qqbot2
{

    public class Bot_api_ans
    {
        public int result;
        public string content;
    }

    class Program
    {
        static readonly HttpClient client = new();

        static Hashtable groupDdzObj = new();

        public static async Task<string> BotApi(string strQuestion)
        {
            try
            {
                HttpResponseMessage response = await client.GetAsync("https://api.qingyunke.com/api.php?key=free&appid=0&msg=" + System.Web.HttpUtility.UrlEncode(strQuestion));
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();
                // Above three lines can be replaced with new helper method below
                // string responseBody = await client.GetStringAsync(uri);

                Bot_api_ans aaa = JsonConvert.DeserializeObject<Bot_api_ans>(responseBody);
                return aaa.content;
                //await eventArgs.Reply(aaa.content);
            }
            catch (HttpRequestException e)
            {
                Console.WriteLine("\nException Caught!");
                Console.WriteLine("Message :{0} ", e.Message);
                return "api错误!";
            }
        }

        static async Task Main(string[] args)
        {
            //设置log等级
            Log.LogConfiguration
               .EnableConsoleOutput()
               .SetLogLevel(LogLevel.Info);

            var conf = new ServerConfig
            {
                Host = "0.0.0.0",
                Port = 8080
            };
            //默认端口为8080
            //实例化Sora服务器
            var service = SoraServiceFactory.CreateService(conf);

            //群消息接收回调
            service.Event.OnGroupMessage += async (sender, eventArgs) =>
            {
                var rand = new Random();
                //最简单的复读
                //Console.WriteLine(eventArgs.Message.MessageBody.IndexOf(Sora.Entities.MessageElement.CQCodes.CQAt(eventArgs.LoginUid)));//, eventArgs.Message

                //接入机器人api
                if (eventArgs.Message.MessageBody.IndexOf(Sora.Entities.Segment.SoraSegment.At(eventArgs.LoginUid)) >= 0)
                //if (rand.Next(100) < 20)  //不at情况下随机回复
                {
                    var ms2 = new Sora.Entities.MessageBody() { };

                    ms2.Add(Sora.Entities.Segment.SoraSegment.At(eventArgs.Sender.Id));
                    ms2.Add(await BotApi(eventArgs.Message.RawText));


                    await eventArgs.Reply(ms2);
                }

                if (!groupDdzObj.ContainsKey(eventArgs.SourceGroup))
                {
                    try
                    {
                        groupDdzObj.Add(eventArgs.SourceGroup, new Doudizhu());
                    }
                    catch { }
                }

                var nowDdzObj = (Doudizhu)groupDdzObj[eventArgs.SourceGroup];
                var ddzAns = await nowDdzObj.Solve(eventArgs.Message.RawText, eventArgs.Sender.Id, eventArgs.SourceGroup, eventArgs);

                //Console.WriteLine(ddzAns[0]);//输出回复结果
                if (ddzAns != null)
                {
                    await eventArgs.Reply(ddzAns);
                }

            };

            //私人消息接收回调
            service.Event.OnPrivateMessage += async (sender, eventArgs) =>
            {
                var rand = new Random();

                //BOT回复
                await eventArgs.Reply(await BotApi(eventArgs.Message.RawText));
            };

            //自动添加好友
            service.Event.OnFriendRequest += async (sender, eventArgs) =>
           {
               await eventArgs.Accept(eventArgs.Sender.Id.ToString() + "-Bot加的");
           };


            //启动服务并捕捉错误
            await service.StartService().RunCatch(e => Log.Error("Sora Service", Log.ErrorLogBuilder(e)));
            await Task.Delay(-1);


        }
    }
}
