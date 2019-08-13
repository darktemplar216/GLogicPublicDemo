/****************************************************************** 
* Copyright (C): GPL 
* Create By: taowei@tencent.com 61197311@qq.com
* Description: 使用请保留版权头，其它没什么要求 
******************************************************************/

using Events.LMN;
using GLogic;
using ObjPoolModule.LMN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Event.Log.LMN
{
    /// <summary>
    /// Demo用的示例log分级
    /// </summary>
    public enum DemoLogLevel
    {
        Debug,
        Warning,
        Error
    }

    /// <summary>
    /// 利用unity打印一段log
    /// </summary>
    public class LMEvent_LogToUnity : BaseEvent
    {
        public override int EventKey
        {
            get { return (int)eFakeEventIdDefs.LogToUnity; }
        }

        /// <summary>
        /// 具体要打印的内容
        /// </summary>
        public string mLogStr = null;

        /// <summary>
        /// 这次需要用什么级别的Log来打印
        /// </summary>
        public DemoLogLevel mLogLevel = DemoLogLevel.Debug;
    }


}
