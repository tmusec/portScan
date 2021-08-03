using Amib.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace portScan
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine(@"                                                                                                                 
                  /@@                      *@@@@@]*         ,@@^     /@@        ,@\             ,/@@]                    
                  =@`                       @@  =@^         *@/      =@`                /@    ,@/  =@^                   
                 ,@@@\@^ ,\@/`\@/`         =@^ /@[ ,//[\@`  /@@/@@* ,@@@\@^  ,[\@^   ,[@@[[[ =@/   /@^                   
                 @@  *@@  =@^/@           ,@/=@^    ,]]/@* =@^  =@^ @@  *@@   ,@/     =@^   ,@@   =@/                    
                =@`  /@*  =@@/            @@ =@^ ,@@* =@^ *@/  ,@^ =@`  /@*   @@     *@/    =@@@@\@/                     
               ,@@\/@`    =@`           ,/@\*=@\`,@\]@@@/ /@@]@/* ,@@\/@`  ,]/@\]    ,@\@[   \@/@/*                      
                       =@@/                                                                    *[[                       
                                                                                                                ");

            string strNotice = "使用cmd打开,如不给-p参数则默认扫描1-65535全部端口，例：portScan.exe -u ip -p 1,2,3-5 -t 线程数";
            string ports = "1-65535";
            int thread = 100;
            int count = 0;
            int count2 = args.Length;
            String ip = "";
            while (count< args.Length) {
                switch (args[count]) {
                    case "-u":
                        ip= args[count + 1];
                        
                        break;
                    case "-p":
                        ports = args[count+1];
                        break;
                    case "-t":
                        thread=int.Parse(args[count + 1]);
                        break;
                    default:
                        throw new Exception(strNotice);
                }
                count = count + 2;
            }
            
            if (args.Length==0)
            {
                Console.WriteLine(strNotice);
                return;
            }
            Console.WriteLine("开始扫描，当前时间："+DateTime.Now);
            scan(ip, calculatePort(ports),thread);
        }

        private static List<string> calculatePort(string ports)
        {
            List<string> listResult = new List<string>();
            List<string> listPort = ports.Split(',').ToList();
            HashSet<string> hashSet = new HashSet<string>();
            try
            {
                if (listPort.Count > 0)
                {
                    for (int i = 0; i < listPort.Count; i++)
                    {
                        string strPort = listPort[i];
                        if (strPort.Contains("-"))
                        {
                            List<string> temp01 = strPort.Split('-').ToList();
                            int min = int.Parse(temp01[0]);
                            int max = int.Parse(temp01[1]);
                            for (int j = min; j <= max; j++)
                            {
                                string strTmpPort = j.ToString();
                                if (!hashSet.Contains(strTmpPort))
                                {
                                    hashSet.Add(strTmpPort);
                                }
                            }
                        }
                        else if (!hashSet.Contains(strPort))
                        {
                            hashSet.Add(strPort);
                        }
                    }
                }
                else
                {
                    throw new Exception("端口参数错误，检查后重新输入");
                }
            }
            catch
            {
                throw new Exception("端口参数错误，检查后重新输入");
            }
            return hashSet.ToList();
        }
        private static void scan(string target, List<string> ports,int thread)
        {
           // Console.WriteLine(DateTime.Now);
            bool isSuccess = false;
            Dictionary<string, string> dicResult = new Dictionary<string, string>();
            List<string> listRst = new List<string>();
            SmartThreadPool stp2 = new SmartThreadPool();
            //stp2.MaxThreads = thread;
           
            stp2.MinThreads = thread;
            
            List<IWorkItemResult> resultList = new List<IWorkItemResult>();//不对IWorkItemResult定义其类型，其结果需要自己做类型转换.
            foreach (string port in ports)
            {
                try
                {
                    
                    resultList.Add(stp2.QueueWorkItem(new WorkItemCallback(GetScanResult), new string[] { target, port }));
                }
                catch
                {
                }
                int threadNum = stp2.ActiveThreads;

            }
           
            stp2.Start();

            //等待所需的结果返回
            if (SmartThreadPool.WaitAll(resultList.ToArray()))
            {
                string strTemp = "";
                Console.WriteLine("扫描结束，当前时间：" + DateTime.Now);
                JObject jobPortService = new JObject();
                string jsonfile = "portService.json";//JSON文件路径

                using (System.IO.StreamReader file = System.IO.File.OpenText(jsonfile))
                {
                    using (JsonTextReader reader = new JsonTextReader(file))
                    {
                        jobPortService = (JObject)JToken.ReadFrom(reader);
                       
                    }
                }
                Console.WriteLine("端口\t类型\t名称          简介");
                foreach (IWorkItemResult t in resultList)
                {
                    strTemp =string.Format("{0}", t.Result);
                    if (strTemp != "") {
                        //dicResult.Add
                        JArray jarr = JArray.Parse(jobPortService[strTemp].ToString());
                        JObject jobFirst = (JObject)jarr[0];
                        Console.WriteLine(strTemp + "\t" + jobFirst["Type"] + "\t" + jobFirst["synopsis"]  + "\t" + jobFirst["description"]);
                        if ((jarr.Count > 1)) {
                            for (var i = 1; i < jarr.Count; i++)
                            {
                                JObject jobTemp = (JObject)jarr[i];
                                Console.WriteLine("\t" + jobTemp["Type"] + "\t" + jobTemp["synopsis"]  + "\t" + jobTemp["description"]);
                            }
                        }
                        //listRst.Add(strTemp);
                        //Console.WriteLine(strTemp);
                    }
                    //Console.WriteLine("{0} : {1}", t.State, t.Result);
                }
            }
        }

        private static object GetScanResult(object obj)
        {

            string target = ((string[])obj)[0];
            int port =int.Parse(((string[])obj)[1]);
            //Console.WriteLine(port);
            TcpClient objTCP = null;

            objTCP = new TcpClient();
            IAsyncResult oAsyncResult = objTCP.BeginConnect(target, Convert.ToInt32(port), null, null);
            oAsyncResult.AsyncWaitHandle.WaitOne(200, true);//1000为超时时间 

            if (objTCP.Connected)
            {
                objTCP.Close();
                return port;

                // result.Add(port);
            }
            else
            {
                return "";
            }
            //IPAddress ip = IPAddress.Parse((string)target);
            //IPEndPoint point = new IPEndPoint(ip, port);
            //Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            //try
            //{
            //    socket.Connect(point);

            //    socket.Close();

            //    return port;
            //}
            //catch
            //{
            //    return "";
            //}
        }
    }
}
