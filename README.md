# AliyunOSS_XXS
AliyunOSS_XXS 


### 简单说明
项目已经经过反复测试已经可以在阿里云官方DLL 基础上开发

1. 提供一个任务（上传任务、下载任务）管理类
2. 上传任务可以断点续传、委托函数回调
3. 下载任务可以断点续传、委托函数回执



### 使用方法
/build/*
项目编译好的DLL
```java
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
            var item2 = task.AddDownloadTask("", "src.zip", @"D:\src222222.zip");

            task.StartTask(task.GetTaskList()[0].Id);


            


            //获取任务列表
            task.GetTaskList();

            //停止任务 任务ID
            //task.StopTask("");

            //移除任务 任务ID
            //task.RemoveTask("");


            //释放所有资源
            //task.Dispose();
```


### 后记
在开发切片下载的时候，阿里云官方并没有指出结束标记符号，询问工单给我一个gitlab地址自己分析终于踩过这个大坑
所以大公司也是有很多文档并不是很全面，我已经踩过坑，你们参考也可以，代码注释很详细

另：
 m15629116378@163.com 问题联系邮箱 
 如果想白嫖问问题还是算了，程序员时间也很宝贵


### 已使用公司
 `柯美`


2020.07.25
