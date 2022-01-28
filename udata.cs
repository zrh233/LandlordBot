using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using YukariToolBox.LightLog;

namespace qqbot2
{
    public class Udata
    {
        const string DATABASE_FILE = "udata.db";

        public struct QQUser
        {
            public long qqId;
            public int integral;
        }

        public static int GetUserIntegral(long qqId, long groupId)
        {
            using (var db = new SqliteConnection("Data Source=" + DATABASE_FILE))
            {
                db.Open();
                var command = db.CreateCommand();
                command.CommandText =
                    @"SELECT count(*) FROM uinfos WHERE qqid=$qqId AND groupid=$groupId";
                command.Parameters.AddWithValue("$qqId", qqId);
                command.Parameters.AddWithValue("$groupId", groupId);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var counts = reader.GetInt64(0);
                        if (counts == 0)
                        {
                            //初始化用户
                            var cmd = db.CreateCommand();
                            cmd.CommandText =
                                "INSERT INTO uinfos (qqid, integral, groupid, lastchange) VALUES ($qqId, 0, $groupId, time('now'));";
                            cmd.Parameters.AddWithValue("$qqId", qqId);
                            cmd.Parameters.AddWithValue("$groupId", groupId);

                            cmd.ExecuteReader();
                            db.Close();
                            return 0;
                        }
                    }
                }

                command= db.CreateCommand();
                command.CommandText =
                    @"SELECT integral FROM uinfos WHERE qqid=$qqId AND groupid=$groupId";
                command.Parameters.AddWithValue("$qqId", qqId);
                command.Parameters.AddWithValue("$groupId", groupId);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var userIntegral = reader.GetInt32(0);
                        db.Close();
                        return userIntegral;
                    }
                }
                db.Close();
            }            
            return 0;
        }

        public static int ChangeUserIntegral(long qqId,long groupId,int values)
        {
            //Debug
            //return values;
            using (var db = new SqliteConnection("Data Source=" + DATABASE_FILE))
            {
                db.Open();
                var command = db.CreateCommand();
                command.CommandText =
                    "UPDATE uinfos SET integral=$values WHERE qqId=$qqId AND groupId=$groupId";
                command.Parameters.AddWithValue("$values", values);
                command.Parameters.AddWithValue("$qqId", qqId);
                command.Parameters.AddWithValue("$groupId", groupId);

                command.ExecuteReader();
                Log.Info("UData", "修改用户 " + qqId.ToString() + " 的积分为 " + values.ToString());
                
                db.Close();
                return values;
            }
        }

        public static List<QQUser> GetTopUsers(long groupId,int numUsers)
        {
            var list = new List<QQUser>();
            using (var db = new SqliteConnection("Data Source=" + DATABASE_FILE))
            {
                db.Open();
                var command = db.CreateCommand();
                command.CommandText =
                    "SELECT qqid,integral FROM uinfos WHERE groupId=$groupId ORDER BY integral DESC LIMIT $numUsers";
                command.Parameters.AddWithValue("$groupId", groupId);
                command.Parameters.AddWithValue("$numUsers", numUsers);

                using (var reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var user = new QQUser
                        {
                            qqId = reader.GetInt64(0),
                            integral = reader.GetInt32(1)
                        };
                        list.Add(user);
                    }
                }

                db.Close();
            }
            return list;
        }

    }
}
