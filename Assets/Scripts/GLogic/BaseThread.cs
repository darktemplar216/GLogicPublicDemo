/****************************************************************** 
* Copyright (C): GPL 
* Create By: taowei@tencent.com 61197311@qq.com
* Description: 使用请保留版权头，其它没什么要求 
******************************************************************/

using System;
using System.Threading;

namespace GLogic.LMN
{
    /// <summary>
    /// 用于派生提供给独立线程驱动的逻辑核
    /// </summary>
    public interface IThreadedLogicCore
    {
        /// <summary>
        /// 工作函数
        /// </summary>
        void MainLoop();

        /// <summary>
        /// 启动回调
        /// </summary>
        void OnStart();

        /// <summary>
        /// 结束回调
        /// </summary>
        void OnStop();

        /// <summary>
        ///  获得线程名
        /// </summary>
        /// <returns></returns>
        string ThreadName { get; }
    }

    /// <summary>
    /// PMGame游戏线程类
    /// </summary>
    public class DemoGameThread
    {
        private static Thread mWorkingThread = null;
        /// <summary>
        /// 我的工作Thread类
        /// </summary>
        public static Thread WorkingThread { get { return mWorkingThread; } }

        /// <summary>
        /// 本线程驱动的逻辑核
        /// </summary>
        private IThreadedLogicCore mLogicCore = null;

        public bool isRunning = false;

        public void Start(IThreadedLogicCore inLogicCore)
        {
            if (inLogicCore == null)
            {
                return;
            }

            mLogicCore = inLogicCore;

            mWorkingThread = new Thread(new ThreadStart(mLogicCore.MainLoop));
            mLogicCore.OnStart();
            mWorkingThread.Name = inLogicCore.ThreadName;
            mWorkingThread.Start();
            isRunning = true;
        }

        public void Stop()
        {
            mLogicCore.OnStop();
            mLogicCore = null;

            if (mWorkingThread != null)
            {
                mWorkingThread.Join();
                mWorkingThread = null;
            }
            isRunning = false;
        }
    }
}
