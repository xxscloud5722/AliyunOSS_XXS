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
```
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

            //添加上传任务
            //var item = task.AddUploadTask("", @"D:\src.zip");

            //添加下载任务
            var item = task.AddDownloadTask("", "src.zip", @"D:\src222222.zip");

            //开始任务
            task.StartTask(task.GetTaskList()[0].Id);

            Console.ReadKey();
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
