using BridgeNodes.LMN;
/****************************************************************** 
* Copyright (C): GPL 
* Create By: taowei@tencent.com 61197311@qq.com
* Description: 使用请保留版权头，其它没什么要求 
******************************************************************/

using DataOceanModule.LMN;
using Event.Log.LMN;
using ObjPoolModule.LMN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace Log.LMN
{
    public class LogUtil
    {
        /// <summary>
        /// Demo的log我们打到Unity的Log中去
        /// </summary>
        /// <param name="inStr"></param>
        public static void Error(string inStr, params object[] param)
        {
            _Log(inStr, DemoLogLevel.Error, param);
        }

        /// <summary>
        /// Demo的log我们打到Unity的Log中去
        /// </summary>
        /// <param name="inStr"></param>
        public static void Warning(string inStr, params object[] param)
        {
            _Log(inStr, DemoLogLevel.Warning, param);
        }

        /// <summary>
        /// Demo的log我们打到Unity的Log中去
        /// </summary>
        /// <param name="inStr"></param>
        public static void Debug(string inStr, params object[] param)
        {
            _Log(inStr, DemoLogLevel.Debug, param);
        }

        /// <summary>
        /// 内部实现
        /// </summary>
        /// <param name="inStr"></param>
        /// <param name="inLevel"></param>
        /// <param name="param"></param>
        private static void _Log(string inStr, DemoLogLevel inLevel, params object[] param)
        {
            if (param != null && param.Length > 0)
            {
                inStr = string.Format(inStr, param);
            }

            LMEvent_LogToUnity evt = FakeObjPoolMgr.FetchAutoRecycleObj<LMEvent_LogToUnity>();
            evt.mLogLevel = inLevel;
            evt.mLogStr = inStr;
            X2MBridge.Instance.SendEventAsync(evt);
        }
    }
}
