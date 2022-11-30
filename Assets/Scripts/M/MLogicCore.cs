/****************************************************************** 
* Copyright (C): GPL 
* Create By: taowei@tencent.com 61197311@qq.com
* Description: 使用请保留版权头，其它没什么要求 
******************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using UnityEngine;
using BridgeNodes.LMN;
using DataOceanModule.LMN;
using Log.M;
using Log.LMN;
using GameSys.M;
using Net.N;

namespace GLogic.M
{
    /// <summary>
    /// 引擎线程逻辑核
    /// </summary>
    public class MLogicCore : MBLogicNode
    {
        #region 指针

        private static MLogicCore mInstance = null;

        public static MLogicCore Instance
        {
            get { return mInstance; }
        }

        public static bool IsValid
        {
            get { return (mInstance != null); }
        }

        #endregion

        #region LogicNode GC

        /// <summary>
        /// gc计时
        /// </summary>
        private float mLogicNodeGCTimeAccum = 0;

        /// <summary>
        /// gc间隔
        /// </summary>
        private const float mLogicNodeGCGap = 10.0f;

        /// <summary>
        /// 尝试去启动一次LogicNode的GC
        /// </summary>
        private void TryToDoLogicNodeGC(float deltaTime)
        {
            mLogicNodeGCTimeAccum += deltaTime;
            if (mLogicNodeGCTimeAccum > mLogicNodeGCGap)
            {
                mLogicNodeGCTimeAccum -= mLogicNodeGCGap;
                GC();
            }
        }

        #endregion

        public void Awake()
        {
            mInstance = this;

            InitSubNodes();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
            mInstance = null;

            ShutdownSubNodes();
        }

        public void Update()
        {
            UpdateLogicTree();
            UpdateEvents();
            OnLogicNodeUpdate(Time.deltaTime);
            SwapEventQueues();
            TryToDoLogicNodeGC(Time.deltaTime);
        }

        public override void OnLogicNodeUpdate(float inDeltaTime)
        {
            //帧数统计//
            LMDataOcean.mMainThreadFrameCounter++;

            base.OnLogicNodeUpdate(inDeltaTime);
        }

        /// <summary>
        /// 启动子系统
        /// </summary>
        private void InitSubNodes()
        {
            //留下我自己的ThreadId//
            LMDataOcean.mMainThreadId = Thread.CurrentThread.ManagedThreadId;

            //启动逻辑线程到主线程的消息桥, 保留static指针//
            X2MBridge.InitStaticInstance();
            AttachNode(X2MBridge.Instance);

            //启动用于打log的MLogger//
            AttachNode(new MLogger());

            //启动游戏逻辑//
            AttachNode(new MNumAccSys());

            //Demo3才启动这个假帧同步系统//
            if (LMDataOcean.mCurDemo == eDemoType.Demo3)
            {
                AttachNode(new NFakeServerMgr());
            }

            //标识自己启动成功//
            LMDataOcean.mMainThreadStarted = true;

            LogUtil.Warning("MLogicCore.InitSubNodes: finished");
        }

        /// <summary>
        /// 关闭子系统
        /// </summary>
        private void ShutdownSubNodes()
        {
            //删除消息桥指针//
            X2MBridge.ShutdownStaticInstance();
        }
    }
}
