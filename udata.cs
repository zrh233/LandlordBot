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

        public static int GetUserIntegral(long qqid, long groupid)
        {
            using (var db = new SqliteConnection("Data Source=" + DATABASE_FILE))
            {
                db.Open();
                var command = db.CreateCommand();
                command.CommandText =
                    @"SELECT count(*) FROM uinfos WHERE qqid=$qqid AND groupid=$groupid";
                command.Parameters.AddWithValue("$qqid", qqid);
                command.Parameters.AddWithValue("$groupid", groupid);

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
                                "INSERT INTO uinfos (qqid, integral, groupid, lastchange) VALUES ($qqid, 0, $groupid, time('now'));";
                            cmd.Parameters.AddWithValue("$qqid", qqid);
                            cmd.Parameters.AddWithValue("$groupid", groupid);

                            cmd.ExecuteReader();
                            db.Close();
                            return 0;
                        }
                    }
                }

                command= db.CreateCommand();
                command.CommandText =
                    @"SELECT integral FROM uinfos WHERE qqid=$qqid AND groupid=$groupid";
                command.Parameters.AddWithValue("$qqid", qqid);
                command.Parameters.AddWithValue("$groupid", groupid);

                using (var reader = command.ExecuteReader())
                {
                    //if ()
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

        public static int ChangeUserIntegral(long qqid,long groupid,int values)
        {
            using (var db = new SqliteConnection("Data Source=" + DATABASE_FILE))
            {
                db.Open();
                var command = db.CreateCommand();
                command.CommandText =
                    "UPDATE uinfos SET integral=$values WHERE qqid=$qqid AND groupid=$groupid";
                command.Parameters.AddWithValue("$values", values);
                command.Parameters.AddWithValue("$qqid", qqid);
                command.Parameters.AddWithValue("$groupid", groupid);

                command.ExecuteReader();
                Log.Info("UData", "修改用户 " + qqid.ToString() + " 的积分为 " + values.ToString());
                
                db.Close();
                return values;
            }
        }

    }
}
