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



            //参数1 配置，  参数2： 进度回执函数 参数3：任务完成通过
            var task = new OssTask(aliyunOSSConfig, (t, s, p) =>
            {
                Console.WriteLine("任务：" + t.Id + ", 速度" + (s / 1024) + "KB 进度：" + p);
            }, (t) =>
            {
                Console.WriteLine("任务：" + t.Id + " -- 完成");
            });

            //添加上传任务 参数1：阿里云oss 路径只是路径  参数2：需要上传的本地文件带路径的本地文件路径
            var item = task.AddUploadTask("", @"D:\src.zip");

            //添加下载任务 参数1：阿里云oss 路径只是路径  参数2：路径下的阿里云oss文件名 参数3：需要下载的本地文件带路径的本地文件路径
            //var item2 = task.AddDownloadTask("", "src.zip", @"D:\src222222.zip");

            task.StartTask(task.GetTaskList()[0].Id);



            //获取任务列表
            task.GetTaskList();

            //停止任务 任务ID
            //task.StopTask("");

            //移除任务 任务ID
            //task.RemoveTask("");


            //释放所有资源
            //task.Dispose();


            Console.ReadKey();
        }
    }
}
