using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Sora;
using Sora.Interfaces;
using Sora.Net.Config;
using Sora.Util;
using YukariToolBox.LightLog;

namespace qqbot2
{
    class Doudizhu
    {
        //public long[] idPlayer = new long[4];
        //public int numPlayer = 0;
        public List<long> idPlayer = new();
        public SortedList users = new();
        public DateTime dateToday = DateTime.Today;
        public const string fileName = "fightlandlord.ini";

        public string status = "waiting";

        public List<List<int>> idCards = new();
        public string[] cardShow = new string[]
        {
            "3","4","5","6","7","8","9","10","J","Q","K","A","2","鬼","王","s"
        };
        public int[] cardNum = new int[]
        {
            4,4,4,4,4,4,4,4,4,4,4,4,4,1,1
        };
        public List<int> landlordCards = new();
        public List<long> todaySign = new();
        public List<long> todayGuess = new();
        public int todayNumber = 0;
        public int todayRangeLeft = 0;
        public int todayRangeRight = 1500;
        public long sourceGroupId;
        public int MAXNUM = 1500;
        public int MAXJIANG = 1500;
        public int MAXTOPUSER = 10;
        public int INITBOTTOMSCORE = 5;
        public int GAMETICKET = 10;
        public long ADMINQQID = 11111111;
        public int gameBottomScore = 5, nowPlayer = 0, firstPlayer = 0, landlordPlayer = 0, countMax = 0, playerMax = 0;
        public int[] countPlayer = new int[5];
        public long nowUid = 0;

        public Sora.EventArgs.SoraEvent.GroupMessageEventArgs eArgs = null;



        private Sora.Entities.MessageBody Txt2msg(string txt)
        {
            var aaaaaaa = new Sora.Entities.MessageBody() { };
            aaaaaaa.Add(txt);
            return aaaaaaa;
        }

        private Sora.Entities.MessageBody AtPlayer(Sora.Entities.MessageBody ansMsg, int thePlayer)
        {
            ansMsg.Add(Sora.Entities.Segment.SoraSegment.At(idPlayer[thePlayer]));
            return ansMsg;
        }

        public async Task<Sora.Entities.MessageBody> Solve(string text, long qqId, long groupId, Sora.EventArgs.SoraEvent.GroupMessageEventArgs eventArgs)
        {
            var ansMsg = new Sora.Entities.MessageBody() { };
            eArgs = eventArgs;
            sourceGroupId = groupId;
            var isBotAdmin = await eventArgs.SourceGroup.GetGroupMemberInfo(eventArgs.LoginUid, true);
            if (eventArgs.Sender.Id == 80000000)
                return null;
            /*
            if (isBotAdmin.memberInfo.Role == Sora.Enumeration.EventParamsType.MemberRoleType.Member)
                return null;
            */
            //Debug 群主或管理员可以强制模拟成员进行发言
            //      格式： Debug_"QQ号"_内容
            var isUserAdmin = await eventArgs.SourceGroup.GetGroupMemberInfo(qqId, true);
            if (text.IndexOf("Debug_") > -1
                && (qqId == ADMINQQID))
            {
                try
                {
                    string[] tx = text.Split('_');
                    qqId = long.Parse(tx[1]);
                    text = tx[2];
                }
                catch
                {
                    return null;
                };
            }
            //!Debug
            nowUid = qqId;


            if (text[0] == '出' || text[0] == '+')
            {
                return GamePlayCards(ansMsg, text[1..], eventArgs);
            }

            if (text.IndexOf("我猜") == 0 && text.Length < 10)
            {
                try
                {
                    var guessNumber = int.Parse(text[2..]);
                    //Console.WriteLine(guessNumber.ToString());
                    return UserGuess(ansMsg, qqId, groupId, guessNumber);
                }
                catch
                {
                    ansMsg.Add(" 猜数字失败，请检查你的数字！");
                    return ansMsg;
                }


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
                    if (qqId != ADMINQQID)
                    {
                        ansMsg.Add("你没有权限重置！");
                        return ansMsg;
                    }
                    return Reset(ansMsg);
                case "逃跑":
                    return GameRun(ansMsg, qqId);
                case "开打":
                    return await GameStartAsync(ansMsg, eventArgs);
                case "1分":
                    return GameAnsCount(ansMsg, 1);
                case "2分":
                    return GameAnsCount(ansMsg, 2);
                case "3分":
                    return GameAnsCount(ansMsg, 3);
                case "不叫":
                    return GameAnsCount(ansMsg, 0);
                case "不出":
                    return GamePlayCards(ansMsg, "", eventArgs);
                case "状况" or "牌局" or "=":
                    return GameStatus(ansMsg);
                case "查询":
                    return UserStaus(ansMsg, qqId, groupId);
                case "签到":
                    return UserSign(ansMsg, qqId, groupId);
                case "猜数字":
                    return UserGuess(ansMsg, qqId, groupId, -1);
                case "斗榜" or "排行榜":
                    return UserTop(ansMsg, groupId);
                case "加倍":
                    return GameDouble(ansMsg, qqId, 2);
                case "超级加倍":
                    return GameDouble(ansMsg, qqId, 4);
                default:
                    return null;
            }
        }

        //查询
        public Sora.Entities.MessageBody UserStaus(Sora.Entities.MessageBody ansMsg, long qqId, long groupId)
        {
            ansMsg.Add(Sora.Entities.Segment.SoraSegment.At(qqId));
            var uIntegral = Udata.GetUserIntegral(qqId, groupId);
            ansMsg.Add($" 你的积分为：{uIntegral}! ");
            return ansMsg;
        }

        //签到
        public Sora.Entities.MessageBody UserSign(Sora.Entities.MessageBody ansMsg, long qqId, long groupId)
        {
            var rand = new Random();
            ansMsg.Add(Sora.Entities.Segment.SoraSegment.At(qqId));

            if (dateToday != DateTime.Today)
            {
                dateToday = DateTime.Today;
                todaySign.Clear();
                todayGuess.Clear();
                todayNumber = rand.Next(MAXNUM - 2) + 1;//MAXNUM
                todayRangeLeft = 0;
                todayRangeRight = MAXNUM;
                Log.Info("Landlord", $"今日数字：{todayNumber}");
            }

            if (todaySign.Contains(qqId))
            {
                ansMsg.Add(" 签到失败！你今天已经签到了!");
                return ansMsg;
            }

            todaySign.Add(qqId);
            var uIntegral = Udata.GetUserIntegral(qqId, groupId);
            int signIntergral = rand.Next(150) + 50;
            var nowIntegral = Udata.ChangeUserIntegral(qqId, groupId, uIntegral + signIntergral);
            ansMsg.Add($" 签到成功！获得积分{signIntergral}! 你的当前积分为：{nowIntegral}! ");
            return ansMsg;
        }

        public Sora.Entities.MessageBody GameDouble(Sora.Entities.MessageBody ansMsg, long qqId, int times)
        {
            if (status != "counting")
            {
                ansMsg.Add(Sora.Entities.Segment.SoraSegment.At(qqId));
                ansMsg.Add("现在不能加倍！");
                return ansMsg;
            }
            if (!idPlayer.Contains(qqId))
            {
                ansMsg.Add(Sora.Entities.Segment.SoraSegment.At(qqId));
                ansMsg.Add("你不是玩家！不能加倍。");
                return ansMsg;
            }
            gameBottomScore *= times;
            ansMsg.Add(Sora.Entities.Segment.SoraSegment.At(qqId));
            ansMsg.Add($"加倍成功！当前底分为 {gameBottomScore}");
            return ansMsg;
        }

        //我猜**
        public Sora.Entities.MessageBody UserGuess(Sora.Entities.MessageBody ansMsg, long qqId, long groupId, int number)
        {
            var rand = new Random();
            ansMsg.Add(Sora.Entities.Segment.SoraSegment.At(qqId));
            //Console.WriteLine(dateToday.ToString() + dateToday.ToString());
            if (dateToday != DateTime.Today || todayNumber == 0)
            {
                dateToday = DateTime.Today;
                todaySign.Clear();
                todayGuess.Clear();
                todayNumber = rand.Next(MAXNUM);//MAXNUM
                todayRangeLeft = 0;
                todayRangeRight = MAXNUM;
                Log.Info("Landlord", $"今日数字：{todayNumber}");
            }

            if (number <= 0)
            {
                if (todayRangeLeft == todayRangeRight)
                    ansMsg.Add($" 数字猜完了！今天的数字是 {todayRangeLeft}");
                else
                    ansMsg.Add($" 当前猜数字范围为：({todayRangeLeft},{todayRangeRight})");
                return ansMsg;
            }

            if (todayGuess.Contains(qqId))
            {
                ansMsg.Add(" 猜数字失败！你今天已经猜数字了!");
                return ansMsg;
            }

            if (number <= todayRangeLeft || number >= todayRangeRight)
            {
                ansMsg.Add($" 猜数字失败，超出数据范围！当前猜数字范围为：({todayRangeLeft},{todayRangeRight})");
                return ansMsg;
            }



            if (number == todayNumber)
            {
                todayRangeLeft = todayNumber;
                todayRangeRight = todayNumber;
                int jiang = MAXJIANG / (3 + todayGuess.Count);
                var uIntegral = Udata.GetUserIntegral(qqId, groupId);
                var nowIntegral = Udata.ChangeUserIntegral(qqId, groupId, uIntegral + jiang * 3);
                ansMsg.Add($" 猜数字成功！奖励 {jiang * 3} 积分。其余参与者获得 {jiang} 积分。");
                foreach (long thisid in todayGuess)
                {
                    uIntegral = Udata.GetUserIntegral(thisid, groupId);
                    nowIntegral = Udata.ChangeUserIntegral(thisid, groupId, uIntegral + jiang);
                }


                return ansMsg;
            }

            todayGuess.Add(qqId);

            if (number < todayNumber)
            {
                todayRangeLeft = number;
            }
            else
            {
                todayRangeRight = number;
            }

            ansMsg.Add($" 猜错了！当前猜数字范围为：({todayRangeLeft},{todayRangeRight})");
            return ansMsg;
        }

        //"斗榜"
        public Sora.Entities.MessageBody UserTop(Sora.Entities.MessageBody ansMsg, long groupId)
        {
            ansMsg.Add("本群积分排行榜：");
            int count = 0;
            var topUsers = Udata.GetTopUsers(groupId, MAXTOPUSER);
            foreach (var user in topUsers)
            {
                ++count;
                Log.Debug("Landlord", user.qqId.ToString());
                ansMsg.Add($"\n{count}  ");
                ansMsg.Add(Sora.Entities.Segment.SoraSegment.At(user.qqId));
                ansMsg.Add($"  {user.integral}积分");
            }
            return ansMsg;
        }


        //"上桌"
        public Sora.Entities.MessageBody UpDesk(Sora.Entities.MessageBody ansMsg, long qqId)
        {
            ansMsg.Add(Sora.Entities.Segment.SoraSegment.At(qqId));
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
        public Sora.Entities.MessageBody DownDesk(Sora.Entities.MessageBody ansMsg, long qqId)
        {
            if (status == "playing" || status == "counting")
            {
                ansMsg.Add(Sora.Entities.Segment.SoraSegment.At(qqId));
                ansMsg.Add("现在不能下桌！你可以选择扣分[逃跑]");
                return ansMsg;
            }
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


        //逃跑
        public Sora.Entities.MessageBody GameRun(Sora.Entities.MessageBody ansMsg, long qqId)
        {
            ansMsg.Add(Sora.Entities.Segment.SoraSegment.At(qqId));
            if (status == "waiting")
            {
                ansMsg.Add("现在无需逃跑！");
                return ansMsg;
            }
            if (!idPlayer.Contains(qqId))
            {
                ansMsg.Add("你不是玩家！");
                return ansMsg;
            }
            var nowIntegral = Udata.GetUserIntegral(qqId, sourceGroupId);
            nowIntegral = Udata.ChangeUserIntegral(qqId, sourceGroupId, nowIntegral - gameBottomScore * 4);
            ansMsg.Add($"逃跑！积分 -{gameBottomScore * 4} (当前{nowIntegral})\n");

            idPlayer.Remove(qqId);
            foreach (long id in idPlayer)
            {
                var nowIntegral2 = Udata.GetUserIntegral(id, sourceGroupId);
                nowIntegral2 = Udata.ChangeUserIntegral(id, sourceGroupId, nowIntegral2 + gameBottomScore * 2);
                ansMsg.Add(Sora.Entities.Segment.SoraSegment.At(id));
                ansMsg.Add($"积分 +{gameBottomScore * 2} (当前{nowIntegral2})\n)");
            }

            status = "waiting";
            return Reset(ansMsg);
        }

        //当前玩家
        public Sora.Entities.MessageBody CurrentPlayers(Sora.Entities.MessageBody ansMsg)
        {
            if (idPlayer.Count == 0)
            {
                ansMsg.Add("当前没有人在桌上");
                return ansMsg;
            }
            else
            {
                ansMsg.Add($"当前底分为 {gameBottomScore} 积分。桌上有 {idPlayer.Count} 个玩家,他们是 ");
                for (int i = 0; i <= idPlayer.Count - 1; ++i)
                {
                    ansMsg.Add(Sora.Entities.Segment.SoraSegment.At((long)idPlayer[i]));
                    if (status == "playing")
                    {
                        ansMsg.Add($"(还剩{idCards[i].Count}张)   ");
                    }
                }
                if (idPlayer.Count == 3 && status == "waiting")
                {
                    ansMsg.Add("\n可以开打了！");
                }
                return ansMsg;
            }
        }

        //重置
        public Sora.Entities.MessageBody Reset(Sora.Entities.MessageBody ansMsg)
        {
            status = "waiting";
            gameBottomScore = INITBOTTOMSCORE;
            idPlayer.Clear();
            ansMsg.Add("成功重置!");
            return ansMsg;
        }

        //开始游戏
        public async Task<Sora.Entities.MessageBody> GameStartAsync(Sora.Entities.MessageBody ansMsg, Sora.EventArgs.SoraEvent.GroupMessageEventArgs eventArgs)
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

            var friendListAns = await eventArgs.SoraApi.GetFriendList();
            var friendList = friendListAns.friendList;

            for (int i = 0; i < 3; i++)
            {
                if (!friendList.Any(nn => nn.UserId.Equals(idPlayer[i])))
                {
                    ansMsg.Add(Sora.Entities.Segment.SoraSegment.At(idPlayer[i]));
                    ansMsg.Add(" 请先添加我为好友再重新开打！");
                    return ansMsg;
                }
            }
            /*
            for (int i = 0; i < 3; i++)
            {
                var nowIntegral = Udata.GetUserIntegral(idPlayer[i], eventArgs.SourceGroup.Id);
                if (nowIntegral <= GAMETICKET)
                {
                    ansMsg.Add(Sora.Entities.Segment.SoraSegment.At(idPlayer[i]));
                    ansMsg.Add(" 你已经没有积分可以开打了! 请下桌！");
                    return CurrentPlayers(ansMsg);
                }
            }*/
            for (int i = 0; i < 3; i++)
            {
                var nowIntegral = Udata.GetUserIntegral(idPlayer[i], eventArgs.SourceGroup.Id);
                _ = Udata.ChangeUserIntegral(idPlayer[i], eventArgs.SourceGroup.Id, nowIntegral - GAMETICKET);
            }

            status = "counting";
            //Console.WriteLine("tring to play");
            //开打
            idCards.Clear();

            //扣除积分


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


            //Console.WriteLine("printing");
            //输出牌组
            for (int i = 0; i <= 2; ++i)
            {
                PrintCards(i, eventArgs);
            }

            gameBottomScore = INITBOTTOMSCORE;
            ansMsg.Add($"游戏开始，每人扣除门票 {GAMETICKET} 积分。已发送牌组！\n");
            //计算第一个上桌顺序
            nowPlayer = rand.Next(0, 3);
            firstPlayer = nowPlayer;
            countMax = 0;
            GameAskCount(ansMsg);


            return ansMsg;
        }

        //发送牌组
        public async void PrintCards(int userIndex, Sora.EventArgs.SoraEvent.GroupMessageEventArgs eventArgs)
        {
            idCards[userIndex].Sort();
            string sendPrivateMsg = "";
            foreach (int cardInt in idCards[userIndex])
            {
                sendPrivateMsg += $"[{cardShow[cardInt]}]";
            }
            Log.Info("Landlord", sendPrivateMsg);
            //await eventArgs.SoraApi.SendTemporaryMessage(idPlayer[userIndex], eventArgs.SourceGroup.Id, sendPrivateMsg);
            await eventArgs.SoraApi.SendPrivateMessage(idPlayer[userIndex], sendPrivateMsg);
        }

        //询问叫分
        public Sora.Entities.MessageBody GameAskCount(Sora.Entities.MessageBody ansMsg)
        {
            ansMsg.Add(Sora.Entities.Segment.SoraSegment.At(idPlayer[nowPlayer]));
            ansMsg.Add($"当前底分：{gameBottomScore}。 请输入你想叫的分数：[不叫][1分][2分][3分]\n（同时在场玩家亦可[加倍][超级加倍]）");
            return ansMsg;
        }


        public Sora.Entities.MessageBody GameAnsCount(Sora.Entities.MessageBody ansMsg, int ansCount)
        {
            if (status != "counting")
            {
                ansMsg.Add("现在不能叫分！");
                return ansMsg;
            }
            if (nowUid != idPlayer[nowPlayer])
            {
                ansMsg.Add("关你P事！现在轮到：");
                ansMsg = AtPlayer(ansMsg, nowPlayer);
                return ansMsg;
            }
            countPlayer[nowPlayer] = ansCount;
            if (ansCount > 0)
            {
                gameBottomScore *= ansCount;
            }
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
                    ansMsg.Add("请重开。\n");
                    return Reset(ansMsg);
                }
                else
                {
                    landlordPlayer = playerMax;
                    nowPlayer = landlordPlayer;
                    return GameShowLandlord(ansMsg);
                }
            }
            return GameAskCount(ansMsg);
        }

        public Sora.Entities.MessageBody GameShowLandlord(Sora.Entities.MessageBody ansMsg)
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

        public void GameNextPlayer()
        {
            ++nowPlayer;
            if (nowPlayer == 3)
            {
                nowPlayer = 0;
            }
        }

        public Sora.Entities.MessageBody GameStatus(Sora.Entities.MessageBody ansMsg)
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

        public Sora.Entities.MessageBody GamePlayCards(Sora.Entities.MessageBody ansMsg, string cards, Sora.EventArgs.SoraEvent.GroupMessageEventArgs eventArgs)
        {
            if (status != "playing")
            {
                ansMsg.Add(Sora.Entities.Segment.SoraSegment.At(eventArgs.Sender.Id));
                ansMsg.Add("现在不是出牌的时候！");
                return ansMsg;
            }
            if (nowUid != idPlayer[nowPlayer])
            {
                ansMsg.Add(Sora.Entities.Segment.SoraSegment.At(eventArgs.Sender.Id));
                ansMsg.Add("关你P事！现在轮到：");
                ansMsg = AtPlayer(ansMsg, nowPlayer);
                return ansMsg;
            }

            //Console.WriteLine("chupai");
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
            //Log.Info("Landlord","zhengli");
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

            //统计各牌的张数
            Dictionary<int, int> countCards = new();
            foreach (int card in nowCardList)
            {
                if (countCards.ContainsKey(card))
                {
                    countCards[card]++;
                }
                else
                {
                    countCards.Add(card, 1);
                }
            }

            //处理各种牌型
            if (nowCardList.Count == 4 && countCards.ContainsValue(4))
            {
                gameBottomScore *= 2;
                ansMsg.Add("炸弹！加倍！\n");
            }
            else if (nowCardList.Count == 2 && nowCardList.Contains(13) && nowCardList.Contains(14))
            {
                gameBottomScore *= 2;
                ansMsg.Add("火箭！加倍！\n");
            }
            else if (nowCardList.Count == 0)
            {
                ansMsg.Add("不出\n");
            }
            else if (nowCardList.Count == 1)
            {
                ansMsg.Add("单个牌\n");
            }
            else if (nowCardList.Count == 2 && countCards.ContainsValue(2))
            {
                ansMsg.Add("对子牌\n");
            }
            else if (nowCardList.Count == 3 && countCards.ContainsValue(3))
            {
                ansMsg.Add("三张牌\n");
            }
            else if (nowCardList.Count == 4 && countCards.ContainsValue(3) && countCards.ContainsValue(1))
            {
                ansMsg.Add("三带一\n");
            }
            else if (nowCardList.Count == 5 && countCards.ContainsValue(3) && countCards.ContainsValue(2))
            {
                ansMsg.Add("三带二\n");
            }
            else if (nowCardList.Count == 6 && countCards.ContainsValue(4) && countCards.ContainsValue(1))
            {
                ansMsg.Add("四带二\n");
            }
            else if (nowCardList.Count == 8 && countCards.ContainsValue(2) && !countCards.ContainsValue(2))
            {
                ansMsg.Add("四带两对\n");
            }
            else if (nowCardList.Count >= 5
                && countCards.Count == nowCardList.Count
                && nowCardList[0] + countCards.Count - 1 == nowCardList[^1]
                && nowCardList.Max() < 12)
            {
                ansMsg.Add("顺子\n");
            }
            else if (nowCardList.Count >= 6
                && countCards.Count * 2 == nowCardList.Count
                && !countCards.Values.ToList().Exists(x => x != 2)
                && nowCardList[0] + countCards.Count - 1 == nowCardList[^1]
                && nowCardList.Max() < 12)
            {
                ansMsg.Add("连对\n");
            }
            else if (nowCardList.Count >= 6
                && countCards.Count * 3 == nowCardList.Count
                && !countCards.Values.ToList().Exists(x => x != 3)
                && nowCardList[0] + countCards.Count - 1 == nowCardList[^1]
                && nowCardList.Max() < 12)
            {
                ansMsg.Add("飞机\n");
            }
            else if (nowCardList.Count >= 8)
            {
                //飞机带翅膀
                //分离一张两张和三张
                List<List<int>> arrayCountCards = new();
                for (var i = 0; i < 5; i++)
                {
                    arrayCountCards.Add(new List<int>());
                }
                foreach (var cardCount in countCards)
                {
                    if (cardCount.Value <= 3)
                    {
                        arrayCountCards[cardCount.Value].Add(cardCount.Key);
                    }
                    else
                    {
                        ansMsg = AtPlayer(ansMsg, nowPlayer);
                        ansMsg.Add("出牌不符合规则！请重试。\n");
                        Log.Info("Landlord", "错误1");
                        return ansMsg;
                    }
                }
                if (arrayCountCards[1].Count > 0 && arrayCountCards[2].Count > 0)
                {
                    ansMsg = AtPlayer(ansMsg, nowPlayer);
                    ansMsg.Add("出牌不符合规则！请重试。\n");
                    Log.Info("Landlord", "错误2");
                    return ansMsg;
                }
                if (arrayCountCards[1].Count + arrayCountCards[2].Count != arrayCountCards[3].Count)
                {
                    ansMsg = AtPlayer(ansMsg, nowPlayer);
                    ansMsg.Add("出牌不符合规则！请重试。\n");
                    Log.Info("Landlord", "错误4");
                    return ansMsg;
                }
                arrayCountCards[3].Sort();
                if (arrayCountCards[3][0] + arrayCountCards[3].Count - 1 == arrayCountCards[3][^1]
                    && arrayCountCards[3].Max() < 12)
                {
                    ansMsg.Add("飞机带翅膀！\n");
                }
                else
                {
                    ansMsg = AtPlayer(ansMsg, nowPlayer);
                    ansMsg.Add("出牌不符合规则！请重试。\n");
                    Log.Info("Landlord", "错误5");
                    return ansMsg;
                }
            }
            else
            {
                ansMsg = AtPlayer(ansMsg, nowPlayer);
                ansMsg.Add("出牌不符合规则！请重试。\n");
                Log.Info("Landlord", "错误6");
                return ansMsg;
            }



            idCards[nowPlayer] = diffCardList;
            if (diffCardList.Count == 0)
            {
                return GameWin(ansMsg, eventArgs);
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
                //Console.WriteLine(ansMsg);
                ansMsg = CurrentPlayers(ansMsg);
                ansMsg.Add("\n现在轮到");
                ansMsg = AtPlayer(ansMsg, nowPlayer);
                ansMsg.Add("出牌。");
                return ansMsg;
            }

            //var ifif = idCards[nowPlayer].Contains(nowCardList); ;
            //if (idCards[nowPlayer].(nowCardList))

        }

        public Sora.Entities.MessageBody GameWin(Sora.Entities.MessageBody ansMsg, Sora.EventArgs.SoraEvent.GroupMessageEventArgs eventArgs)
        {
            ansMsg = AtPlayer(ansMsg, nowPlayer);
            var winner = "农民";
            if (nowPlayer == landlordPlayer)
            {
                winner = "地主";
            }
            ansMsg.Add($"{winner}胜利！");
            for (int i = 0; i < 3; i++)
            {
                ansMsg.Add("\n");
                ansMsg = AtPlayer(ansMsg, i);

                var playerCount = gameBottomScore;
                if (i == landlordPlayer)
                {
                    playerCount *= 2;
                    if (winner == "农民")
                    {
                        playerCount *= -1;
                    }
                }
                else
                {
                    if (winner == "地主")
                    {
                        playerCount *= -1;
                    }
                }
                var nowIntegral = Udata.GetUserIntegral(idPlayer[i], eventArgs.SourceGroup.Id);
                nowIntegral = Udata.ChangeUserIntegral(idPlayer[i], eventArgs.SourceGroup.Id, nowIntegral + playerCount);

                if (playerCount > 0)
                {
                    ansMsg.Add("+");
                }
                ansMsg.Add(playerCount.ToString());
                ansMsg.Add($"积分（当前{nowIntegral}）");

                ansMsg.Add($"  剩余牌：");
                foreach (var card in idCards[i])
                {
                    ansMsg.Add($"[{cardShow[card]}]");
                }
            }
            ansMsg = CurrentPlayers(ansMsg);

            gameBottomScore = INITBOTTOMSCORE;
            return Reset(ansMsg);
        }

    }
}
