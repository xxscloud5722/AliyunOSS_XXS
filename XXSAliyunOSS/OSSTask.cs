using Aliyun.OSS;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace XXSAliyunOSS
{
    public class OssTask
    {

        //默认配置
        private readonly AliyunOssConfig aliyunOSSConfig;

        //客户端
        private readonly OssClient client;

        //任务列表
        private volatile List<OssTaskDO> taskList;

        //线程数量
        private readonly int threadCount = 5;

        //ref
        CancellationTokenSource tokenSource = null;

        public delegate void TaskProgressChangeCallback(OssTaskDO ossTasks, long speed, double progress);
        public delegate void TaskChangeCallback(List<OssTaskDO> ossTasks);
        public delegate void DownloadCompleteCallBack(OssTaskDO task);


        private TaskProgressChangeCallback taskProgressChangeCallback;
        private DownloadCompleteCallBack downloadCompleteCallBack;

        public OssTask(AliyunOssConfig config)
        {
            if (File.Exists(Environment.CurrentDirectory + @"\ossTask.json"))
            {
                var configString = File.ReadAllText(Environment.CurrentDirectory + @"\ossTask.json");
                taskList = JsonConvert.DeserializeObject<List<OssTaskDO>>(configString);
                taskList.ForEach(it =>
                {
                    if (it.Status == OssTaskStatus.RUN)
                    {
                        it.Status = OssTaskStatus.WAIT;
                    }
                });
            }
            else
            {
                taskList = new List<OssTaskDO>();
                SaveOssTaskConfig(taskList);
            }
            aliyunOSSConfig = config;
            client = new OssClient(config.Endpoint, config.AccessKeyId, config.AccessKeySecret);
        }

        public OssTask(AliyunOssConfig config, TaskProgressChangeCallback taskProgressChangeCallback) : this(config)
        {
            this.taskProgressChangeCallback = taskProgressChangeCallback;
        }

        public OssTask(AliyunOssConfig config, TaskProgressChangeCallback taskProgressChangeCallback, DownloadCompleteCallBack downloadCompleteCallBack) : this(config, taskProgressChangeCallback)
        {
            this.downloadCompleteCallBack = downloadCompleteCallBack;
        }

        /// <summary>
        /// 添加下载任务到列表
        /// </summary>
        /// <param name="ossPath">OSS路径</param>
        /// <param name="ossName">OSS名称</param>
        /// <param name="downloadPath">下载保存路径</param>
        /// <returns>任务信息</returns>
        public OssTaskDO AddDownloadTask(string ossPath, string ossName, string downloadPath)
        {
            var t = this.taskList.Find(r => r.DownloadOssName == ossName);
            if (t != null)
            {
                throw new Exception("文件已经在任务列表");
            }


            var task = new OssTaskDO()
            {
                Id = Guid.NewGuid().ToString(),
                Type = OssTaskType.DOWNLOAD,
                Status = OssTaskStatus.WAIT,

                DownloadOssPath = ossPath,
                DownloadOssName = ossName,

                DownloadName = Path.GetFileName(downloadPath),
                DownloadPath = Path.GetDirectoryName(downloadPath)
            };
            taskList.Add(task);
            SaveOssTaskConfig(taskList);
            return task;
        }


        /// <summary>
        /// 添加上传任务到列表
        /// </summary>
        /// <param name="ossPath">OSS保存路径</param>
        /// <param name="downloadPath">上传文件路径</param>
        /// <returns>任务信息</returns>
        public OssTaskDO AddUploadTask(string ossPath, string uploadPath)
        {
            var name = Path.GetFileName(uploadPath);
            var t = this.taskList.Find(r => r.UploadName == name);
            if (t != null)
            {
                throw new Exception("文件已经在任务列表");
            }


            var task = new OssTaskDO()
            {
                Id = Guid.NewGuid().ToString(),
                Type = OssTaskType.UPLOAD,
                Status = OssTaskStatus.WAIT,

                UploadOssPath = ossPath,
                UploadOssName = name,

                UploadName = name,
                UploadPath = Path.GetDirectoryName(uploadPath)
            };
            taskList.Add(task);
            SaveOssTaskConfig(taskList);
            return task;
        }


        /// <summary>
        /// 获得列表
        /// </summary>
        /// <returns></returns>
        public List<OssTaskDO> GetTaskList()
        {
            return this.taskList;
        }

        /// <summary>
        /// 开始任务
        /// </summary>
        /// <param name="id">任务Id</param>
        /// <returns>是否成功</returns>
        public bool StartTask(string id)
        {
            var item = this.taskList.Find(r => r.Id == id);
            if (item == null)
            {
                return false;
            }
            if (item.Status == OssTaskStatus.DELETE)
            {
                return false;
            }
            if (item.Status == OssTaskStatus.RUN)
            {
                return true;
            }
            if (item.Status == OssTaskStatus.COMPLETE)
            {
                return true;
            }

            //待运行状态, 如果是下载任务
            item.Status = OssTaskStatus.RUN;
            if (item.Type == OssTaskType.DOWNLOAD)
            {
                StartDownload(item);
            }
            else if (item.Type == OssTaskType.UPLOAD)
            {
                StartUpload(item);
            }
            SaveOssTaskConfig(taskList);
            return true;
        }


        /// <summary>
        /// 移除任务
        /// </summary>
        /// <param name="id">任务Id</param>
        /// <returns>是否成功</returns>
        public bool RemoveTask(string id)
        {
            var item = this.taskList.Find(r => r.Id == id);
            if (item == null)
            {
                return false;
            }
            if (item.Status == OssTaskStatus.DELETE)
            {
                return true;
            }
            //如果是运行状态
            if (item.Status == OssTaskStatus.RUN)
            {
                if (item.Type == OssTaskType.DOWNLOAD)
                {
                    StopDownload(item);
                }
                else if (item.Type == OssTaskType.UPLOAD)
                {
                    StopUpload(item);
                }
                this.taskList.Remove(item);
                SaveOssTaskConfig(taskList);
                return true;
            }
            //如果是待运行状态
            this.taskList.Remove(item);
            SaveOssTaskConfig(taskList);
            return true;
        }


        /// <summary>
        /// 停止任务
        /// </summary>
        /// <param name="id">任务Id</param>
        /// <returns>是否成功</returns>
        public bool StopTask(string id)
        {
            var item = this.taskList.Find(r => r.Id == id);
            if (item == null)
            {
                return false;
            }
            if (item.Status == OssTaskStatus.WAIT)
            {
                return true;
            }
            if (item.Status == OssTaskStatus.DELETE)
            {
                return false;
            }
            //如果是运行状态
            if (item.Type == OssTaskType.DOWNLOAD)
            {
                StopDownload(item);
            }
            else if (item.Type == OssTaskType.UPLOAD)
            {
                StopUpload(item);
            }
            item.Status = OssTaskStatus.WAIT;
            SaveOssTaskConfig(taskList);
            return true;
        }


        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            this.taskList.ForEach(it =>
            {
                if (it.Status != OssTaskStatus.RUN)
                {
                    Close(it);
                }
            });
        }








        private void RefreshTask()
        {
            lock (taskList)
            {
                if (tokenSource == null)
                {
                    tokenSource = new CancellationTokenSource();
                    new Task(() =>
                    {
                        for (; ; )
                        {
                            try
                            {
                                //如果没有运行的任务跳出循环不允许
                                var index = this.taskList.FindIndex(r => r.Status == OssTaskStatus.RUN);
                                if (index < 0)
                                {
                                    tokenSource.Cancel();
                                    tokenSource = null;
                                    return;
                                }
                                this.taskList.ForEach(it =>
                                {
                                    if (it.Status == OssTaskStatus.RUN)
                                    {
                                        RefreshTaskItem(it);
                                    }
                                });
                                Thread.Sleep(1000);
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine(e.StackTrace);
                            }
                        }
                    }, tokenSource.Token).Start();
                }
            }
        }

        private void RefreshTaskItem(OssTaskDO task)
        {
            //速度
            var speed = task.Progress - task.LastProgress;

            //总体进度百分比
            var progress = task.TotalProgress <= 0 ?
                0 : task.Progress * 1.0 / task.TotalProgress;

            task.LastProgress = task.Progress;

            //委托调用
            if (task.LastProgress > 0 && taskProgressChangeCallback != null)
            {
                taskProgressChangeCallback(task, speed, Math.Round(progress, 2) * 100);
            }
        }



        private void StartDownload(OssTaskDO task)
        {
            //开启扫描
            this.RefreshTask();

            //是否需要创建文件
            if (!File.Exists(task.DownloadPath + @"\" + task.DownloadName + ".download") || task.DownloadFileLength == 0)
            {

                //如果文件已经存在则删除
                if (File.Exists(task.DownloadPath + @"\" + task.DownloadName + ".download"))
                {
                    File.Delete(task.DownloadPath + @"\" + task.DownloadName + ".download");
                }

                //读取线上文件信息
                var ossPath = task.DownloadOssPath + @"\" + task.DownloadOssName;
                ossPath = ossPath.Substring(1);
                var fileInfo = client.GetObject(aliyunOSSConfig.BucketName, ossPath);
                task.DownloadFileLength = fileInfo.ContentLength;
                fileInfo.Dispose();

                //磁盘创建一个一样大小空文件
                var fs = new FileStream(task.DownloadPath + @"\" + task.DownloadName + ".download", FileMode.Create);
                task.DownloadDebrisTotalCount = task.DownloadFileLength % task.DebrisSize == 0 ? task.DownloadFileLength / task.DebrisSize : task.DownloadFileLength / task.DebrisSize + 1;
                fs.Position = 0;
                for (int i = 1; i <= task.DownloadDebrisTotalCount; i++)
                {
                    if (i == task.DownloadDebrisTotalCount)
                    {
                        var size = task.DownloadFileLength - task.DebrisSize * (i - 1);
                        fs.Write(new byte[size], 0, (int)size);
                    }
                    else
                    {
                        fs.Write(new byte[task.DebrisSize], 0, (int)task.DebrisSize);
                    }
                }
                task.Stream = fs;
            }
            else
            {
                task.Stream = new FileStream(task.DownloadPath + @"\" + task.DownloadName + ".download", FileMode.Open);
            }

           

            //创建碎片任务说明
            task.DebrisProgress = new Boolean?[task.DownloadDebrisTotalCount];
            for (int i = 0; i < task.DownloadDebrisTotalCount; i++)
            {
                task.Stream.Position = i * task.DebrisSize;
                var temp = new byte[task.DebrisSize];
                task.Stream.ReadAsync(temp, 0, (int)task.DebrisSize);

                //查询内存块
                var result = temp.ToList().FindIndex(r => r > 0);
                task.DebrisProgress[i] = result >= 0;
            }


            //总长度
            task.Progress = task.DebrisProgress.ToList().Where(r => r == true).ToList().Count * task.DebrisSize;
            task.TotalProgress = task.DownloadFileLength;

            //保存配置
            SaveOssTaskConfig(taskList);

            //开启线程下载文件
            task.ThreadList = new List<Task>();
            task.ThreadTokenList = new List<CancellationTokenSource>();
            for (int i = 0; i < threadCount; i++)
            {
                var token = new CancellationTokenSource();
                task.ThreadList.Add(new Task(() =>
                {
                    for (; ; )
                    {
                        try
                        {
                            if (token.IsCancellationRequested)
                            {
                                return;
                            }
                            if (DownloadItem(task) == null)
                            {
                                return;
                            }
                            if (token.IsCancellationRequested)
                            {
                                return;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.StackTrace);
                        }
                    }
                }, token.Token));
            }


            //等待任务完成结束
            Task.WhenAll(task.ThreadList.ToArray()).ContinueWith(s =>
            {
                DownloadComplete(task);
            });
            //开始
            task.ThreadList.ForEach(it => it.Start());
        }

        private object DownloadItem(OssTaskDO task)
        {
            //获取索引
            var p = DownloadReadAsync(task);

            if (p < 0)
            {
                return null;
            }

            try
            {
                //阿里云下载
                var ossPath = task.DownloadOssPath + @"\" + task.DownloadOssName;
                ossPath = ossPath.Substring(1);
                GetObjectRequest request = new GetObjectRequest(aliyunOSSConfig.BucketName, ossPath);

                request.SetRange(p * task.DebrisSize,
                    p + 1 >= task.DownloadDebrisTotalCount ? task.DownloadFileLength : (p + 1) * task.DebrisSize);

                if (p + 1 >= task.DownloadDebrisTotalCount)
                {
                    request.SetRange(p * task.DebrisSize, task.DownloadFileLength - 1);
                }

                //保存到磁盘
                OssObject result = client.GetObject(request);
                if (result.HttpStatusCode.ToString() != "OK" && result.HttpStatusCode.ToString() != "PartialContent")
                {
                    throw new Exception("Response NOT OK");
                }
                DownloadWriteAsync(task, result.Content, p);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                DownloadWriteReduction(task, p);
            }
            return true;
        }

        private int DownloadReadAsync(OssTaskDO task)
        {
            lock (task)
            {
                var index = task.DebrisProgress.ToList().FindIndex(r => r == false);
                if (index < 0)
                {
                    return -1;
                }
                task.DebrisProgress[index] = null;
                return index;
            }
        }

        private void DownloadWriteAsync(OssTaskDO task, Stream stream, int i)
        {
            //获取流
            var memoryStream = new MemoryStream();
            CopyStream(stream, memoryStream);
            byte[] bytes = new byte[memoryStream.Length - 1];
            memoryStream.Read(bytes, 0, bytes.Length);



            //保存到文件
            lock (task.Stream)
            {
                task.Stream.Position = i * task.DebrisSize;
                task.Stream.Write(bytes, 0, bytes.Length);
                task.Progress += bytes.Length;
            }
        }

        private void DownloadWriteReduction(OssTaskDO task, int i)
        {
            lock (task)
            {
                Console.WriteLine("回滚任务:" + i);
                task.DebrisProgress[i] = false;
            }
        }

        private void StopDownload(OssTaskDO task)
        {
            if (task.ThreadTokenList == null)
            {
                return;
            }
            task.ThreadTokenList.ForEach(it =>
            {
                it.Cancel();
            });
        }


        private void DownloadComplete(OssTaskDO task)
        {
            //保存配置
            task.Status = OssTaskStatus.COMPLETE;
            SaveOssTaskConfig(taskList);

            //回收内存
            this.Close(task);

            //重命名
            File.Move(task.DownloadPath + @"\" + task.DownloadName + ".download", task.DownloadPath + @"\" + task.DownloadName);


            //执行回执
            downloadCompleteCallBack?.Invoke(task);
        }


        private void StartUpload(OssTaskDO task)
        {
            //开启扫描
            this.RefreshTask();

            //先上阿里云OSS进行上传初始化
            if (task.UploadOssId == null)
            {
                var ossPath = task.UploadOssPath + @"/" + task.UploadOssName;
                ossPath = ossPath.Substring(1);
                var request = new InitiateMultipartUploadRequest(aliyunOSSConfig.BucketName, ossPath);
                var result = client.InitiateMultipartUpload(request);
                task.UploadOssId = result.UploadId;
            }

            //打开文件流
            task.Stream = new FileStream(task.UploadPath + @"\" + task.UploadName, FileMode.Open);
            var fileStream = new FileStream(task.UploadPath + @"\" + task.UploadName + ".upload", FileMode.OpenOrCreate);
            //在相同上传文件目录生成一个隐藏配置文件
            task.ConfigStream = new StreamWriter(fileStream);
            //存放Etag 读取Tag
            task.UploadETag = new List<string>();
            var readStream = new StreamReader(fileStream);
            while (true)
            {
                var row = readStream.ReadLine();
                if (row == null)
                {
                    break;
                }

                var values = row.Split(',');
                if (values.Length == 4)
                {
                    task.UploadETag.Add(row);
                }
            }


            //计算碎片总数和进度
            var debrisTotal = task.Stream.Length % task.DebrisSize == 0 ? task.Stream.Length / task.DebrisSize : task.Stream.Length / task.DebrisSize + 1;
            var debrisProgress = new bool?[debrisTotal];
            for (int i = 0; i < debrisProgress.Length; i++)
            {
                var s = task.UploadETag.FindIndex(r => r.Split(',')[0] == (i + 1).ToString());
                debrisProgress[i] = s >= 0;
            }
            task.DebrisProgress = debrisProgress;

            //总长度
            task.Progress = task.DebrisProgress.ToList().Where(r => r == true).ToList().Count * task.DebrisSize;
            task.TotalProgress = task.Stream.Length;

            //保存配置
            SaveOssTaskConfig(taskList);

            //开始任务
            task.ThreadList = new List<Task>();
            task.ThreadTokenList = new List<CancellationTokenSource>();
            for (int i = 0; i < threadCount; i++)
            {
                var token = new CancellationTokenSource();
                task.ThreadList.Add(new Task(() =>
                {
                    for (; ; )
                    {
                        try
                        {
                            if (token.IsCancellationRequested)
                            {
                                return;
                            }
                            if (UploadItem(task) == null)
                            {
                                return;
                            }
                            if (token.IsCancellationRequested)
                            {
                                return;
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine(ex.StackTrace);
                        }
                    }
                }, token.Token));
            }

            //等待任务完成结束
            Task.WhenAll(task.ThreadList.ToArray()).ContinueWith(s =>
            {
                UploadComplete(task);
            });

            //开始
            task.ThreadList.ForEach(it => it.Start());
        }

        private object UploadItem(OssTaskDO task)
        {
            //获取索引
            var p = UploadReadAsync(task);

            if (p == null)
            {
                return null;
            }

            try
            {
                //读取文件
                byte[] data = (byte[])p[1];

                //生成文件
                var ossPath = task.UploadOssPath + @"/" + task.UploadOssName;
                ossPath = ossPath.Substring(1);
                var request = new UploadPartRequest(aliyunOSSConfig.BucketName, ossPath, task.UploadOssId)
                {
                    InputStream = new MemoryStream(data),
                    PartSize = data.Length,
                    PartNumber = ((int)p[0]) + 1
                };

                //上传记录下来
                var result = client.UploadPart(request);
                if (result.HttpStatusCode.ToString() != "OK")
                {
                    throw new Exception("Response NOT OK");
                }

                //保存进度配置
                lock (task.ConfigStream)
                {
                    var resultRow = result.PartNumber + "," + result.ETag + "," + result.Crc64 + "," + result.Length;
                    task.UploadETag.Add(resultRow);
                    task.Progress += data.Length;
                    task.ConfigStream.WriteLine(resultRow);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.StackTrace);
                //如果执行失败还原
                UploadReadReduction(task, (int)p[0]);
            }
            return true;
        }

        private object[] UploadReadAsync(OssTaskDO task)
        {
            lock (task)
            {
                var index = task.DebrisProgress.ToList().FindIndex(r => r == false);
                if (index < 0)
                {
                    return null;
                }
                task.DebrisProgress[index] = null;

                var data = new byte[task.Stream.Length > (index + 1) * task.DebrisSize
                    ? task.DebrisSize
                    : task.Stream.Length - index * task.DebrisSize];

                task.Stream.Position = index * task.DebrisSize;
                task.Stream.Read(data, 0, data.Length);
                return new object[] { index, data };
            }
        }

        private void UploadReadReduction(OssTaskDO task, int i)
        {
            lock (task)
            {
                Console.WriteLine("回滚任务:" + i);
                task.DebrisProgress[i] = false;
            }
        }

        private void UploadComplete(OssTaskDO task)
        {
            //线上检查
            var ossPath = task.UploadOssPath + @"/" + task.UploadOssName;
            ossPath = ossPath.Substring(1);
            var listPartsRequest = new ListPartsRequest(aliyunOSSConfig.BucketName, ossPath, task.UploadOssId);
            var listPartsResult = client.ListParts(listPartsRequest);

            //生成Tag
            var completeMultipartUploadRequest = new CompleteMultipartUploadRequest(aliyunOSSConfig.BucketName, ossPath, task.UploadOssId);
            List<PartETag> eTags = new List<PartETag>();
            foreach (var item in task.UploadETag)
            {
                var values = item.Split(',');
                eTags.Add(new PartETag(Convert.ToInt32(values[0]), values[1], values[2], Convert.ToInt64(values[3])));
            }
            eTags.Sort((x, y) => x.PartNumber.CompareTo(y.PartNumber));
            eTags.ForEach(it => completeMultipartUploadRequest.PartETags.Add(it));


            //合并文件上传完成
            client.CompleteMultipartUpload(completeMultipartUploadRequest);


            //保存配置
            task.Status = OssTaskStatus.COMPLETE;
            SaveOssTaskConfig(taskList);

            //回收内存
            this.Close(task);

            //执行回执
            downloadCompleteCallBack?.Invoke(task);
        }


        private void StopUpload(OssTaskDO task)
        {
            if (task.ThreadTokenList == null)
            {
                return;
            }
            task.ThreadTokenList.ForEach(it =>
            {
                it.Cancel();
            });
            this.Close(task);
        }


        private void Close(OssTaskDO task)
        {
            //回收内存
            task.ThreadTokenList = null;
            task.ThreadList = null;
            task.DebrisProgress = null;

            if (task.Stream != null)
            {
                task.Stream.Close();
            }
            if (task.ConfigStream != null)
            {
                task.ConfigStream.Close();
            }
            if (File.Exists(task.UploadPath + @"/" + task.UploadName + ".upload"))
            {
                File.Delete(task.UploadPath + @"/" + task.UploadName + ".upload");
            }
        }

        public static void CopyStream(Stream input, Stream output)
        {
            var p = output.Position;
            byte[] buffer = new byte[32768];
            int read;
            while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                output.Write(buffer, 0, read);
            }
            output.Position = p;
        }

        private static void SaveOssTaskConfig(List<OssTaskDO> taskList)
        {
            lock (taskList)
            {
                File.WriteAllText(Environment.CurrentDirectory + @"\ossTask.json", JsonConvert.SerializeObject(taskList));
            }
        }
    }
}
