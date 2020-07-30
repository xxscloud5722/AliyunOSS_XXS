using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace XXSAliyunOSS
{
    public class OssTaskDO
    {
        //Id
        public string Id { set; get; }

        //任务类型
        public OssTaskType Type { set; get; }


        //任务状态
        public OssTaskStatus Status { set; get; }






        //OSS下载名称
        public string DownloadOssName { set; get; }

        //OSS下载路径
        public string DownloadOssPath { set; get; }

        //本地存放路径
        public string DownloadPath { set; get; }

        //本地存放文件名
        public string DownloadName { set; get; }

        //下载文件的大小
        public long DownloadFileLength { set; get; }


        //下载文件的碎片大小
        public long DownloadDebrisTotalCount { get; set; }



        //OSS上传名称
        public string UploadOssName { set; get; }

        //OSS上传路径
        public string UploadOssPath { set; get; }

        //上传本地存放路径
        public string UploadPath { set; get; }

        //上传本地存放文件名
        public string UploadName { set; get; }

        //阿里云OSSID
        public string UploadOssId { set; get; }

        //阿里云碎片Tag
        [JsonIgnore]
        public List<string> UploadETag { set; get; }


        //线程任务     
        [JsonIgnore]
        public List<Task> ThreadList { set; get; }

        //线程token
        [JsonIgnore]
        public List<CancellationTokenSource> ThreadTokenList { set; get; }

        //文件流
        [JsonIgnore]
        public FileStream Stream { set; get; }

        //配置文件流
        [JsonIgnore]
        public StreamWriter ConfigStream { set; get; }

        //进度
        [JsonIgnore]
        public bool?[] DebrisProgress { set; get; }

        //文件的碎片大小
        public long DebrisSize { get; set; }

        //最小的碎片
        [JsonIgnore]
        public long MinDebrisSize { get { return 1024 * 100; } }

        //上次已处理的长度
        [JsonIgnore]
        public long LastProgress { set; get; }

        //当前进度
        [JsonIgnore]
        public long Progress { set; get; }

        //总长度
        [JsonIgnore]
        public long TotalProgress { set; get; }

        //UI显示的进度
        public long ActualProgress { set; get; }
    }

    public enum OssTaskType
    {
        DOWNLOAD,
        UPLOAD
    }

    public enum OssTaskStatus
    {
        WAIT,
        RUN,
        DELETE,
        COMPLETE
    }
}
