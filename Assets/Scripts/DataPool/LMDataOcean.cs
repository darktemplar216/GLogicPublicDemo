/****************************************************************** 
* Copyright (C): GPL 
* Create By: taowei@tencent.com 61197311@qq.com
* Description: 使用请保留版权头，其它没什么要求 
******************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DataOceanModule.LMN
{
    /// <summary>
    /// 我们的跨线程裸奔数据池
    /// </summary>
    public class LMDataOcean
    {
        /// <summary>
        /// 可供选择的线程模式
        /// 默认用三线程
        /// </summary>
        public static eDemoThreadMode mCurThreadMode = eDemoThreadMode.L_M_N;

        /// <summary>
        /// 现在是哪个Demo
        /// </summary>
        public static eDemoType mCurDemo = eDemoType.Demo1;

        /// <summary>
        /// 主线程线程ID
        /// </summary>
        public static int mMainThreadId = -1;

        /// <summary>
        /// 逻辑线程线程ID
        /// </summary>
        public static int mLogicThreadId = -1;

        /// <summary>
        /// 网络线程ID
        /// </summary>
        public static int mNetThreadId = -1;

        /// <summary>
        /// 主线程帧计数
        /// </summary>
        public static long mMainThreadFrameCounter = 0;

        /// <summary>
        /// 逻辑线程帧计数
        /// </summary>
        public static long mLogicThreadFrameCounter = 0;

        /// <summary>
        /// 网络逻辑帧计数
        /// </summary>
        public static long mNetThreadFrameCounter = 0;

        /// <summary>
        /// 主线程启动标识
        /// </summary>
        public static bool mMainThreadStarted = false;

        /// <summary>
        /// 逻辑线程启动标识
        /// </summary>
        public static bool mLogicThreadStarted = false;

        /// <summary>
        /// 网络线程启动标识
        /// </summary>
        public static bool mNetThreadStarted = false;
    }

    /// <summary>
    /// 总共支持的线程模式
    /// </summary>
    public enum eDemoThreadMode
    {
        /// <summary>
        /// 单线程，全部在一起
        /// </summary>
        LMN,
        /// <summary>
        /// 双线程, 主和逻辑一个，网络一个
        /// </summary>
        LM_N,
        /// <summary>
        /// 三线程, 主一个，逻辑一个，网络一个
        /// </summary>
        L_M_N,
    }

    /// <summary>
    /// 现在是哪个Demo
    /// </summary>
    public enum eDemoType
    {
        Demo1,
        Demo2,
        Demo3,
    }

}


