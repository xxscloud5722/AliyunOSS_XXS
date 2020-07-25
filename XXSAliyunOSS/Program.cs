using Aliyun.OSS;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XXSAliyunOSS
{
    class Program
    {
        static void Main(string[] args)
        {
            AliyunOssConfig aliyunOSSConfig = new AliyunOssConfig()
            {
                Endpoint = "oss-cn-beijing.aliyuncs.com",
                AccessKeyId = "000",
                AccessKeySecret = "0000",
                BucketName = "my-xxscloud",
            };



            var task = new OssTask(aliyunOSSConfig, (t, s, p) =>
            {
                Console.WriteLine("任务：" + t.Id + ", 速度" + (s / 1024) + "KB 进度：" + p);
            }, (t) =>
            {
                Console.WriteLine("任务：" + t.Id + " -- 完成");
            });

            //var item = task.AddUploadTask("", @"D:\src.zip");

            var item = task.AddDownloadTask("", "src.zip", @"D:\src222222.zip");

            task.StartTask(task.GetTaskList()[0].Id);

            Console.ReadKey();
        }
    }
}
