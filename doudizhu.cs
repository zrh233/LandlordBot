using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace qqbot2
{
    class Doudizhu
    {
        //static public long[] idPlayer = new long[4];
        //static public int numPlayer = 0;
        public static List<long> idPlayer = new();
        public static SortedList users = new();
        public static DateTime dateToday = DateTime.Today;
        public const string fileName = "fightlandlord.ini";

        public static string status = "waiting";

        static public List<List<int>> idCards = new();
        static public string[] cardShow = new string[]
        {
            "3","4","5","6","7","8","9","10","J","Q","K","A","2","鬼","王","s"
        };
        static public int[] cardNum = new int[]
        {
            4,4,4,4,4,4,4,4,4,4,4,4,4,1,1
        };
        static public List<int> landlordCards = new();
        static public List<long> todaySign= new();
        static public List<long> todayGuess = new();
        static public int todayNumber = 0;
        static public int todayRangeLeft = 0;
        static public int todayRangeRight = 2000;
        static public int MAXNUM = 2000;
        static public int MAXJIANG = 1500;
        static public int nowPlayer = 0, firstPlayer = 0, landlordPlayer = 0, countMax = 0, playerMax = 0;
        static public int[] countPlayer = new int[5];
        static public long nowUid = 0;

        static public Sora.EventArgs.SoraEvent.GroupMessageEventArgs eArgs = null;



        static private Sora.Entities.MessageBody Txt2msg(string txt)
        {
            var aaaaaaa = new Sora.Entities.MessageBody() { };
            aaaaaaa.Add(txt);
            return aaaaaaa;
        }

        static private Sora.Entities.MessageBody AtPlayer(Sora.Entities.MessageBody ansMsg, int thePlayer)
        {
            ansMsg.Add(Sora.Entities.MessageElement.CQCodes.CQAt(idPlayer[thePlayer]));
            return ansMsg;
        }

        static public void Load()
        {
            Console.WriteLine("正在读取配置");
            if (!File.Exists(fileName))
            {
                WriteFile();
            }
            LoadFile();
        }

        static public async void LoadFile()
        {
            string line = "";
            FileStream fs = File.OpenRead(fileName);

            string conf = "";
            //fs.Read(conf,0,)

        }

        static public async void WriteFile()
        {

        }

        static public async Task<Sora.Entities.MessageBody> Solve(string text, long qqId,long groupId, Sora.EventArgs.SoraEvent.GroupMessageEventArgs eventArgs)
        {
            var ansMsg = new Sora.Entities.MessageBody() { };
            eArgs = eventArgs;
            var isAdmin = await eventArgs.SourceGroup.GetGroupMemberInfo(eventArgs.LoginUid, true);
            if (eventArgs.Sender.Id == 80000000)
                return null;

            if (isAdmin.memberInfo.Role == Sora.Enumeration.EventParamsType.MemberRoleType.Member)
                return null;

            //Debug
            if (text.IndexOf("Debug_") > -1)
            {
                string[] tx = text.Split('_');
                qqId = Int32.Parse(tx[1]);
                text = tx[2];
            }
            //!Debug
            nowUid = qqId; //Console.WriteLine(nowUid);


            if (text[0] == '出' || text[0] == '+')
            {
                return GamePlayCards(ansMsg, text[1..]);
            }

            if (text.IndexOf("我猜") > -1)
            {                
                var guessNumber = int.Parse(text[2..]);
                //Console.WriteLine(guessNumber.ToString());
                return UserGuess(ansMsg, qqId, groupId, guessNumber);
            }

            switch (text)
            {
                case "斗地主" or "说明":
                    ansMsg.Add("斗地主的说明");
                    return ansMsg;
                case "上桌":
                    return UpDesk(ansMsg, qqId);
                case "下桌":
                    return DownDesk(ansMsg, qqId);
                case "重置":
                    return Reset(ansMsg);
                case "开打":
                    return GameStart(ansMsg, eventArgs);
                case "1分":
                    return GameAnsCount(ansMsg, 1);
                case "2分":
                    return GameAnsCount(ansMsg, 2);
                case "3分":
                    return GameAnsCount(ansMsg, 3);
                case "不叫":
                    return GameAnsCount(ansMsg, 0);
                case "不出":
                    return GamePlayCards(ansMsg, "");
                case "状况" or "牌局" or "=":
                    return GameStatus(ansMsg);
                case "查询":
                    return UserStaus(ansMsg,qqId,groupId);
                case "签到":
                    return UserSign(ansMsg, qqId, groupId);
                case "猜数字":
                    return UserGuess(ansMsg, qqId, groupId, -1);
                default:
                    return null;
            }
        }

        //查询
        static public Sora.Entities.MessageBody UserStaus(Sora.Entities.MessageBody ansMsg,long qqId,long groupId)
        {
            ansMsg.Add(Sora.Entities.MessageElement.CQCodes.CQAt(qqId));
            var uIntegral = Udata.GetUserIntegral(qqId, groupId);
            ansMsg.Add(" 你的积分为：" + uIntegral.ToString() + "! \n");
            return ansMsg;
        }

        //签到
        static public Sora.Entities.MessageBody UserSign(Sora.Entities.MessageBody ansMsg, long qqId, long groupId)
        {
            var rand=new Random();
            ansMsg.Add(Sora.Entities.MessageElement.CQCodes.CQAt(qqId));

            if (dateToday!=DateTime.Today)
            {
                todaySign.Clear();
                todayGuess.Clear();
                todayNumber=rand.Next(MAXNUM-2)+1;//MAXNUM
                todayRangeLeft = 0;
                todayRangeRight = MAXNUM;
                Console.WriteLine("今日数字：" + todayNumber.ToString());
            }

            if (todaySign.Contains(qqId))
            {
                ansMsg.Add(" 签到失败！你今天已经签到了!\n");
                return ansMsg;
            }

            todaySign.Add(qqId);
            var uIntegral = Udata.GetUserIntegral(qqId, groupId);
            int signIntergral = rand.Next(150) + 50;
            var nowIntegral = Udata.ChangeUserIntegral(qqId, groupId, uIntegral + signIntergral);
            ansMsg.Add(" 签到成功！获得积分"+signIntergral.ToString()+"! 你的当前积分为：" + nowIntegral.ToString() + "! \n");
            return ansMsg;
        }

        public static Sora.Entities.MessageBody UserGuess(Sora.Entities.MessageBody ansMsg, long qqId, long groupId,int number)
        {
            var rand = new Random();
            ansMsg.Add(Sora.Entities.MessageElement.CQCodes.CQAt(qqId));
            //Console.WriteLine(dateToday.ToString() + dateToday.ToString());
            if (dateToday != DateTime.Today || todayNumber==0)
            {
                todaySign.Clear();
                todayGuess.Clear();
                todayNumber = rand.Next(MAXNUM);//MAXNUM
                todayRangeLeft = 0;
                todayRangeRight = MAXNUM;
                Console.WriteLine("今日数字：" + todayNumber.ToString());
            }

            if (number <= 0)
            {
                if (todayRangeLeft == todayRangeRight)
                    ansMsg.Add(" 数字猜完了！今天的数字是 " + todayRangeLeft);
                else
                    ansMsg.Add(" 当前猜数字范围为：("
                        + todayRangeLeft.ToString()
                        + ","
                        + todayRangeRight.ToString()
                        + ")");
                return ansMsg;
            }

            if (todayGuess.Contains(qqId))
            {
                ansMsg.Add(" 猜数字失败！你今天已经猜数字了!\n");
                return ansMsg;
            }

            if (number<=todayRangeLeft || number>=todayRangeRight)
            {
                ansMsg.Add(" 猜数字失败，超出数据范围！当前猜数字范围为：("
                        + todayRangeLeft.ToString()
                        + ","
                        + todayRangeRight.ToString()
                        + ")");
                return ansMsg;
            }

            

            if (number==todayNumber)
            {
                todayRangeLeft = todayNumber;
                todayRangeRight = todayNumber;
                int jiang=MAXJIANG/(3+todayGuess.Count);
                var uIntegral = Udata.GetUserIntegral(qqId, groupId);
                var nowIntegral = Udata.ChangeUserIntegral(qqId, groupId, uIntegral + jiang*3);
                ansMsg.Add(" 猜数字成功！奖励 "+(jiang*3).ToString()+" 积分。其余参与者获得 "+jiang.ToString()+" 积分。\n");
                foreach (int thisid in todayGuess)
                {
                    uIntegral = Udata.GetUserIntegral(thisid, groupId);
                    nowIntegral = Udata.ChangeUserIntegral(thisid, groupId, uIntegral + jiang);
                }
                

                return ansMsg;
            }

            todayGuess.Add(qqId);

            if (number<todayNumber)
            {
                todayRangeLeft = number;
            } else
            {
                todayRangeRight = number;
            }

            ansMsg.Add(" 猜错了！当前猜数字范围为：("
                        + todayRangeLeft.ToString()
                        + ","
                        + todayRangeRight.ToString()
                        + ")");
            return ansMsg;
        }

        //"上桌"
        static public Sora.Entities.MessageBody UpDesk(Sora.Entities.MessageBody ansMsg, long qqId)
        {
            ansMsg.Add(Sora.Entities.MessageElement.CQCodes.CQAt(qqId));
            if (idPlayer.Contains(qqId))
            //if (qqId==idPlayer[i])
            {
                ansMsg.Add(" 你已经在桌上了! \n");
                return CurrentPlayers(ansMsg);

            }
            if (idPlayer.Count == 3)
            {
                ansMsg.Add("人数已满！\n");
                return CurrentPlayers(ansMsg);
            }
            idPlayer.Add(qqId);
            ansMsg.Add(" 成功上桌!\n");
            return CurrentPlayers(ansMsg);

        }

        //“下桌”
        static public Sora.Entities.MessageBody DownDesk(Sora.Entities.MessageBody ansMsg, long qqId)
        {
            if (idPlayer.Contains(qqId))
            {
                idPlayer.Remove(qqId);
                ansMsg.Add("下桌成功！\n");
                CurrentPlayers(ansMsg);
                return ansMsg;
            }
            else
            {
                ansMsg.Add("你还没有上桌！\n");
                CurrentPlayers(ansMsg);
                return ansMsg;
            }
        }


        static public Sora.Entities.MessageBody CurrentPlayers(Sora.Entities.MessageBody ansMsg)
        {
            if (idPlayer.Count == 0)
            {
                ansMsg.Add("当前没有人在桌上");
                return ansMsg;
            }
            else
            {
                ansMsg.Add($"当前有 {idPlayer.Count} 个玩家,他们是 ");
                for (int i = 0; i <= idPlayer.Count - 1; ++i)
                {
                    ansMsg.Add(Sora.Entities.MessageElement.CQCodes.CQAt((long)idPlayer[i]));
                    if (status == "playing")
                    {
                        ansMsg.Add($"(还剩{idCards[i].Count}张) ");
                    }
                }
                if (idPlayer.Count == 3 && status == "waiting")
                {
                    ansMsg.Add("\n可以开打了！");
                }
                return ansMsg;
            }
        }
        static public Sora.Entities.MessageBody Reset(Sora.Entities.MessageBody ansMsg)
        {
            status = "waiting";
            idPlayer.Clear();
            ansMsg.Add("成功重置!");
            return ansMsg;
        }

        static public Sora.Entities.MessageBody GameStart(Sora.Entities.MessageBody ansMsg, Sora.EventArgs.SoraEvent.GroupMessageEventArgs eventArgs)
        {
            var rand = new Random();
            if (idPlayer.Count != 3)
            {
                ansMsg.Add("当前人数未满！\n");
                return CurrentPlayers(ansMsg);
            }


            if (status != "waiting")
            {
                ansMsg.Add("游戏已经开始了!\n");
                return CurrentPlayers(ansMsg);
            }


            status = "counting";
            Console.WriteLine("tring to play");
            //开打
            idCards.Clear();
            //清空牌组
            for (int i = 1; i <= 5; ++i)
            {

                idCards.Add(new List<int>());

                //idCards[i] = new List<int>();
            }

            //随机生成牌组
            for (int i = 0; i <= 14; ++i)
            {
                for (int j = 1; j <= cardNum[i]; ++j)
                {
                    int addPlayer = rand.Next(0, 3);
                    if (idCards[addPlayer] != null)
                    {
                        while (idCards[addPlayer].Count >= 18)
                        {
                            addPlayer = rand.Next(0, 3);
                        }
                    }
                    idCards[addPlayer].Add(i);
                }
            }
            //每人取走一张牌生成地主牌
            landlordCards = new List<int>();
            for (int i = 0; i <= 2; ++i)
            {
                int rndIndex = rand.Next(0, idCards[i].Count);
                landlordCards.Add(idCards[i][rndIndex]);
                idCards[i].RemoveAt(rndIndex);
                //idCards[i].;
            }


            Console.WriteLine("printing");
            //输出牌组
            for (int i = 0; i <= 2; ++i)
            {
                PrintCards(i, eventArgs);
            }


            ansMsg.Add("已发送牌组！\n");
            //计算第一个上桌顺序
            nowPlayer = rand.Next(0, 3);
            firstPlayer = nowPlayer;
            countMax = 0;
            GameAskCount(ansMsg);


            return ansMsg;
            //ansMsg.Add(Sora.Entities.MessageElement.CQCodes.)
            //Sora
        }

        static public async void PrintCards(int userIndex, Sora.EventArgs.SoraEvent.GroupMessageEventArgs eventArgs)
        {
            idCards[userIndex].Sort();
            string sendPrivateMsg = "";
            foreach (int cardInt in idCards[userIndex])
            {
                sendPrivateMsg += $"[{cardShow[cardInt]}]";
            }
            Console.WriteLine(sendPrivateMsg);
            await eventArgs.SoraApi.SendTemporaryMessage(idPlayer[userIndex], eventArgs.SourceGroup.Id, sendPrivateMsg);
        }

        static public Sora.Entities.MessageBody GameAskCount(Sora.Entities.MessageBody ansMsg)
        {
            ansMsg.Add(Sora.Entities.MessageElement.CQCodes.CQAt(idPlayer[nowPlayer]));
            ansMsg.Add("请输入你想叫的分数：[不叫][1分][2分][3分]");
            return ansMsg;
        }

        static public Sora.Entities.MessageBody GameAnsCount(Sora.Entities.MessageBody ansMsg, int ansCount)
        {
            if (nowUid != idPlayer[nowPlayer])
            {
                ansMsg.Add("关你P事！现在轮到：");
                ansMsg = AtPlayer(ansMsg, nowPlayer);
                return ansMsg;
            }
            countPlayer[nowPlayer] = ansCount;
            if (ansCount == 3)
            {
                landlordPlayer = nowPlayer;
                return GameShowLandlord(ansMsg);
            }
            if (ansCount > countMax)
            {
                countMax = ansCount;
                playerMax = nowPlayer;
            }
            GameNextPlayer();
            if (nowPlayer == firstPlayer)
            {
                if (countMax == 0)
                {
                    ansMsg.Add("请自行重开。\n");
                    return Reset(ansMsg);
                }
                else
                {
                    landlordPlayer = playerMax;
                    return GameShowLandlord(ansMsg);
                }
            }
            return GameAskCount(ansMsg);
        }

        static public Sora.Entities.MessageBody GameShowLandlord(Sora.Entities.MessageBody ansMsg)
        {
            status = "playing";
            ansMsg.Add("地主牌为");
            foreach (int paper in landlordCards)
            {
                ansMsg.Add($"[{cardShow[paper]}]");
            }
            idCards[landlordPlayer].AddRange(landlordCards);
            PrintCards(landlordPlayer, eArgs);
            //landlordCards.CopyTo();
            ansMsg.Add("地主为：");
            ansMsg = AtPlayer(ansMsg, landlordPlayer);
            ansMsg.Add("请出牌：");
            return ansMsg;
        }

        static public void GameNextPlayer()
        {
            ++nowPlayer;
            if (nowPlayer == 3)
            {
                nowPlayer = 0;
            }
        }

        static public Sora.Entities.MessageBody GameStatus(Sora.Entities.MessageBody ansMsg)
        {
            switch (status)
            {
                case "waiting":
                    ansMsg.Add("正在等待开局\n");
                    return CurrentPlayers(ansMsg);
                case "counting":
                    ansMsg.Add("正在叫分\n");
                    return CurrentPlayers(ansMsg);
                default:
                    ansMsg.Add("正在出牌\n");
                    //输出牌组
                    for (int i = 0; i <= 2; ++i)
                    {
                        PrintCards(i, eArgs);
                    }
                    return CurrentPlayers(ansMsg);
            }

        }

        static public Sora.Entities.MessageBody GamePlayCards(Sora.Entities.MessageBody ansMsg, string cards)
        {
            if (nowUid != idPlayer[nowPlayer])
            {
                ansMsg.Add("关你P事！现在轮到：");
                ansMsg = AtPlayer(ansMsg, nowPlayer);
                return ansMsg;
            }

            Console.WriteLine("chupai");
            cards = cards.ToUpper();
            cards = cards.Replace("10", "s");

            foreach (char car in cards)
            {
                string card = "";
                card += car;
                if (Array.IndexOf(cardShow, card) < 0)
                {
                    ansMsg = AtPlayer(ansMsg, nowPlayer);
                    ansMsg.Add("出牌不合理！请重新出牌。");
                    return ansMsg;
                }
            }
            Console.WriteLine("zhengli");
            List<int> nowCardList = new();
            foreach (char car in cards)
            {
                string card = "";
                card += car;
                if (card == "s")
                {
                    nowCardList.Add(7);
                }
                else
                {
                    nowCardList.Add(Array.IndexOf(cardShow, card));
                }
            }
            nowCardList.Sort();

            var diffCardList = new List<int>();
            diffCardList.AddRange(idCards[nowPlayer]);
            //idCards[nowPlayer].CopyTo(diffCardList);
            foreach (var cardId in nowCardList)
            {
                if (!diffCardList.Remove(cardId))
                {
                    ansMsg = AtPlayer(ansMsg, nowPlayer);
                    ansMsg.Add("你出别人的牌干什么？");
                    return ansMsg;
                }
            }
            Console.WriteLine("te");
            idCards[nowPlayer] = diffCardList;
            if (diffCardList.Count == 0)
            {
                return GameWin(ansMsg);
            }
            else
            {
                ansMsg = AtPlayer(ansMsg, nowPlayer);
                ansMsg.Add("出牌：");
                foreach (var card in nowCardList)
                {
                    ansMsg.Add($"[{cardShow[card]}]");
                }
                PrintCards(nowPlayer, eArgs);
                GameNextPlayer();
                Console.WriteLine(ansMsg);
                ansMsg = CurrentPlayers(ansMsg);
                ansMsg.Add("\n现在轮到");
                ansMsg = AtPlayer(ansMsg, nowPlayer);
                ansMsg.Add("出牌。");
                return ansMsg;
            }

            //var ifif = idCards[nowPlayer].Contains(nowCardList); ;
            //if (idCards[nowPlayer].(nowCardList))

        }

        static public Sora.Entities.MessageBody GameWin(Sora.Entities.MessageBody ansMsg)
        {
            ansMsg = AtPlayer(ansMsg, nowPlayer);
            ansMsg.Add("胜利！");
            for (int i = 0; i < 3; i++)
            {
                ansMsg.Add("\n");
                ansMsg = AtPlayer(ansMsg, i);
                ansMsg.Add("的牌：");
                foreach (var card in idCards[i])
                {
                    ansMsg.Add($"[{cardShow[card]}]");
                }
            }
            ansMsg = CurrentPlayers(ansMsg);
            return Reset(ansMsg);
        }

    }
}
