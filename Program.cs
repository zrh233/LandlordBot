using System;
using Sora.Net;
using Sora.OnebotModel;
using System.Threading.Tasks;
using YukariToolBox.Extensions;
using YukariToolBox.FormatLog;
using System.Net.Http;
using Newtonsoft.Json;

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
            //Doudizhu.Load();

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

                //if (eventArgs.WaitForNextMessageAsync)

                //接入机器人api
                if (eventArgs.Message.MessageBody.IndexOf(Sora.Entities.MessageElement.CQCodes.CQAt(eventArgs.LoginUid)) >= 0)
                //if (eventArgs.Message.MessageBody[0].MessageType == Sora.Enumeration.CQType.At)
                //if (rand.Next(100) < 20)
                {
                    var ms2 = new Sora.Entities.MessageBody() { };

                    //var aaaa=Sora.Entities.MessageElement.CQCodes.CQText("12");

                    //ms2.Add(aaaa);
                    //await eventArgs.Reply(ms2);
                    //ms2.Add(Sora.Entities.MessageElement.CQCodes.CQReply(eventArgs.Message.MessageId));
                    ms2.Add(Sora.Entities.MessageElement.CQCodes.CQAt(eventArgs.Sender.Id));
                    ms2.Add(await BotApi(eventArgs.Message.RawText));


                    await eventArgs.Reply(ms2);
                }
                //var aaaa= eventArgs.WaitForNextMessageAsync;

                var ddzAns = await Doudizhu.Solve(eventArgs.Message.RawText, eventArgs.Sender.Id, eventArgs.SourceGroup, eventArgs);
                //Console.WriteLine(ddzAns[0]);
                if (ddzAns != null)
                {
                    await eventArgs.Reply(ddzAns);
                }

            };
            service.Event.OnPrivateMessage += async (sender, eventArgs) =>
            {
                var rand = new Random();
                //最简单的复读
                //Console.WriteLine(eventArgs.Message.MessageList);//, eventArgs.Message


                await eventArgs.Reply(await BotApi(eventArgs.Message.RawText));


                if (rand.Next(100) < 50)
                {
                    //var msg1 = new Sora.Entities.Message {

                    //};
                    //var msg1 = new System.Collections.Generic.List<Sora.Entities.CQCodes.CQCode>();
                    //msg1.Add(Sora.Entities.CQCodes.CQCode.CQFace(12));
                    //msg1.Add(Sora.Entities.CQCodes.CQCode.CQText("aaa123"));

                    //await eventArgs.Repeat();
                    //await eventArgs.Reply(eventArgs.Message.MessageList);
                    //await eventArgs.Reply(msg1);
                    //await eventArgs.Repeat();
                    //await eventArgs.Reply(eventArgs.Message.MessageList);
                    //await eventArgs.Reply("aaa233333");
                    //await eventArgs.SourceGroup.SendGroupMessage("233333");
                    //await eventArgs.RecallSourceMessage();
                    //await eventArgs.Reply("123233");
                }

            };

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
