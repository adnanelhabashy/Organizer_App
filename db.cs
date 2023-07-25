using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//using MySql.Data.MySqlClient;
using Oracle.ManagedDataAccess;
using QuickFix;
using QuickFix.Fields;
using QuickFix.FIX44;
using System.Collections;
using Oracle.ManagedDataAccess.Client;
using System.Threading;

namespace OPR0409
{
    class DB
    {
        public static string myConnectionString = "ConnectionString";
        public FIX y = new FIX();
        public DB()
        {

        }
        public class MP
        {
            public string Price;
            public string quan;
            public string coid;
            public string stat;
            public string Len;
        }
        public void Order_Cancel(OracleDataReader dx, SessionID sid, QuickFix.FIX44.ExecutionReport EX)
        {
            EX = y.AddStat(EX, new EncodedText(dx["stat"].ToString()), new EncodedTextLen(Int32.Parse(dx["len"].ToString())));
            string Sql31 = "update actions set source='FIX', userid='" + dx["NUID"].ToString() + "',done=0,Status='Canceld' where ID=" + dx["ID"].ToString() + " and done=1 and firm='" + sid.TargetCompID.ToString() + "'";
            if (Update(Sql31) == 1)
            {

                EX.SetField(new OrdStatus(OrdStatus.CANCELED));
                EX.SetField(new ExecID(DateTime.UtcNow.Ticks.ToString()));
                EX.SetField(new ExecID(DateTime.UtcNow.Ticks.ToString()));
                EX.SetField(new LeavesQty(0));
                EX.SetField(new CumQty(0));
                EX.SetField(new AvgPx(0));
                if (dx["Source"].ToString() != "DTP")
                {
                    EX.SetField(new ClOrdID(dx["origcoid"].ToString()));
                    EX.SetField(new ExecType(ExecType.CANCELED));
                }
                else
                {
                    EX.SetField(new ClOrdID(dx["coid"].ToString()));
                    EX.SetField(new ExecType(ExecType.RESTATED));
                }
                EX.Header.SetField(new TargetSubID(dx["USERID"].ToString()));
                SendOrSave(EX, sid);
                Console.WriteLine("Accepted Cancel " + dx["coid"] + " from " + sid.TargetCompID.ToString() + " **** " + DateTime.Now.ToString());
            }
            else
            {
                Console.WriteLine(Sql31);
                Console.WriteLine("Cant Execute");
            }
        }
        public MP getPrice(string COID)
        {

            MP mm = new MP();
            using (var conn = new OracleConnection(myConnectionString))
            {
                if (conn.State.ToString() != "Open")
                {
                    conn.Open();
                }
                string SQL = "select new_stat,new_len,newprice,newquan,origcoid from actions where COID='" + COID + "'";
                var comm = new OracleCommand(SQL, conn);

                try
                {
                    var dx = comm.ExecuteReader();


                    if (dx.Read())
                    {
                        mm.Price = dx["newprice"].ToString();
                        mm.quan = dx["newquan"].ToString();
                        mm.coid = dx["origcoid"].ToString();
                        mm.stat = dx["New_stat"].ToString();
                        mm.Len = dx["New_len"].ToString();
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine("******************** 5 ***************");
                    Console.WriteLine(ex.Message);
                    Console.WriteLine("******************** 5 ***************");
                }
            }


            return mm;
        }
        public void chekcache()
        {

            foreach (SessionID sid in Executor.Meen)

            {

                //   Console.WriteLine(sid.ToString());

                //   Console.WriteLine("Checkcache called" + DateTime.Now.ToString());

                string firm = sid.TargetCompID.ToString();
                if (Session.LookupSession(sid).IsEnabled && Session.LookupSession(sid).IsLoggedOn && Executor.Meen_Lock[firm] == false)
                {

                    //       Console.WriteLine("Checking cache of "+firm);

                    readme(firm, sid);

                }


            }
        }
        public void readme(string firm, SessionID sid)
        {
            Executor.Meen_Lock[firm] = true;
            // Console.Write("Checking Cach");
            //   ArrayList g = Select2("select * from messages where status = 0 and firm='" + firm + "' AND ROWNUM <= 2 order by ID Asc");
            ArrayList g = Select2("select * from messages where status = 0 and firm='" + firm + "' order by ID Asc");
            //            Console.WriteLine("___________________________");
            //          Console.WriteLine(g.Count+" Messages for firm "+firm);
            //          Console.WriteLine("___________________________");
            while (g.Count > 0)
            {

                foreach (MessID c in g)
                {

                    if (Executor.FIX1.send(c.M, c.Sid))
                    {
                        try
                        {
                            //                        Console.WriteLine(c.ID);
                            int iz = Update("update messages set status=1 where ID='" + c.ID.ToString() + "'");
                            if (iz == -1)
                            {
                                Console.WriteLine("wrong update:");

                            }
                        }
                        catch (Exception o2)
                        {
                            Console.WriteLine("****************** V1 **********");
                            Console.WriteLine(o2.Message);
                            Console.WriteLine("****************** V1 **********");
                        }

                    }


                }
                //   g.Clear();
                g = Select2("select * from messages where status = 0 and firm='" + firm + "' order by ID Asc ");
                //    g = Select2("select * from messages where status = 0 and firm='" + firm + "' AND ROWNUM <= 2 order by ID Asc ");
            }
            Executor.Meen_Lock[firm] = false;

        }
        public ArrayList checkstat(string COID, string sid)
        {
            ArrayList stat = new ArrayList();
            stat.Add("");
            stat.Add("");
            stat.Add("");
            stat.Add("");

            using (var conn = new OracleConnection(myConnectionString))
            {
                if (conn.State.ToString() != "Open")
                {
                    conn.Open();
                }

                string SQL = "select Status,account,security,custodian from actions where COID='" + COID + "' and session_id='" + sid.ToString() + "'";
                var comm = new OracleCommand(SQL, conn);

                try
                {
                    var dx = comm.ExecuteReader();


                    if (dx.Read())
                    {
                        stat[0] = dx["status"].ToString();
                        stat[1] = dx["Account"].ToString();
                        stat[2] = dx["Security"].ToString();
                        stat[3] = dx["Custodian"].ToString();
                    }

                }
                catch (Exception ex)
                {
                    Console.WriteLine("******************** 6 ***************");
                    Console.WriteLine(ex.Message);
                    Console.WriteLine("******************** 6 ***************");
                }
            }
            return stat;
        }
        public static bool Locked = false;
        public void checkupdate()
        {
            if (Locked == true)
            {
                return;
            }
            else
            {
                Locked = true;
                try
                {

                    //  Console.WriteLine("Rading actions table");

                    Select("select * from actions where Done=1 and firm in ('" + String.Join("','", Executor.Meen_Lock.Keys.ToList()) + "')  order by timestamp asc");

                }
                catch (Exception fg)
                {
                    Console.WriteLine("******************** 7 ***************");
                    Console.WriteLine(fg.Message);
                    Console.WriteLine("******************** 7 ***************");

                }
                finally
                {
                    Locked = false;
                }
                {

                }
            }
        }
        public bool Insert(string query)
        {

            using (var conn = new OracleConnection(myConnectionString))
            {
                // Console.WriteLine("Executing " + query);
                if (conn.State.ToString() != "Open")
                {
                    conn.Open();
                }
                var comm = conn.CreateCommand();
                comm.CommandText = query;
                try
                {
                    comm.ExecuteNonQuery();

                }
                catch (Exception ex)
                {
                    Console.WriteLine("******************** 8 ***************");
                    Console.WriteLine(ex.Message);
                    Console.WriteLine("******************** 8 ***************");
                    return false;
                }
                conn.Close();
                return true;
            }




        }
