/****************************************************************** 
* Copyright (C): GPL 
* Create By: taowei@tencent.com 61197311@qq.com
* Description: 使用请保留版权头，其它没什么要求 
******************************************************************/

using System;
using System.Threading;
using GLogic.LMN;
using BridgeNodes.LMN;
using ObjPoolModule.LMN;
using DataOceanModule.LMN;
using Log.LMN;
using Event.GameSys.LM;
using GameSys.L;
using FrameSync.L;

namespace GLogic.L
{
    /// <summary>
    /// 逻辑线程逻辑核
    /// </summary>
    public class LLogicCore : ObjLogicNode, IThreadedLogicCore, IGEventListener
    {
        #region 指针

        private static LLogicCore mInstance = null;

        public static LLogicCore Instance
        {
            get { return mInstance; }
        }

        public static bool IsValid
        {
            get { return (mInstance != null); }
        }

        public LLogicCore()
        {
            LogicNodePriority = (int)eLogicCorePriority.LLogicCore;
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
        private const float mLogicNodeGCGap = 20.0f;

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

        #region 名字

        /// <summary>
        /// 名字
        /// </summary>
        public override int NodeName
        {
            get { return (int)ePMGameNodeNameDefs.LLogicCore; }
            set { }
        }


        public string ThreadName { get { return "LLogicCore"; } }

        #endregion

        #region deltaTime

        /// <summary>
        /// 上一帧执行了多久
        /// </summary>
        private double mDeltaTimeInMiliSec = 0;

        #endregion

        #region 工作频率
        /// <summary>
        /// 工作间隔
        /// </summary>
        private const double mLoopTimeGapInMiliSec = 1.0;

        #endregion

        #region 是否结束

        /// <summary>
        /// 是否结束
        /// </summary>
        public bool IsEnd
        {
            get; set;
        }

        #endregion

        #region IGEventListener

        /// <summary>
        /// 1. 返回这个节点的消息优先级
        /// 2. 一般写为一个针对inEventKey的switch case, 针对不同的inEventKey返回不同的优先级
        /// 3. 返回值越大，就会在被AttachListener到的节点上更早的接收到消息
        /// 4. LogicNode的消息优先级排序只会发生在AttachListener的时候，任何之后的修改均无效
        /// </summary>
        /// <param name="inEventKey"></param>
        /// <returns></returns>
        public int GetPriority(int inEventKey)
        {
            return 0;
        }

        /// <summary>
        /// 消息监听器的当前状态，不要手动去赋值
        /// </summary>
        /// <value>The node status.</value>
        public AttachableStatus ListenerStatus { get; set; }

        /// <summary>
        /// 处理输入的消息
        /// </summary>
        /// <param name="inEvent"></param>
        /// <returns> 如果希望阻止消息的继续派发，返回true </returns>
        public bool HandleEvent(IGEvent inEvent)
        {
            bool ret = false;

            return ret;
        }

        #endregion

        /// <summary>
        /// 注意LLogicCore的这个方法平时没有用，在单线程模式下才有用
        /// </summary>
        /// <param name="inLogicNode"></param>
        public override void OnAttached(IGLogicNode inLogicNode)
        {
            base.OnAttached(inLogicNode);

            OnStart();
            InitSubNodes();
        }

        /// <summary>
        /// 注意LLogicCore的这个方法平时没有用，在单线程模式下才有用
        /// </summary>
        /// <param name="inLogicNode"></param>
        public override void OnDetached(IGLogicNode inLogicNode)
        {
            base.OnDetached(inLogicNode);

            OnStop();
            ShutdownSubNodes();
        }

        public override void OnLogicNodeUpdate(float inDeltaTime)
        {
            //帧数统计//
            LMDataOcean.mLogicThreadFrameCounter++;

            base.OnLogicNodeUpdate(inDeltaTime);
        }

        /// <summary>
        /// 工作函数
        /// </summary>
        public void MainLoop()
        {
            InitSubNodes();

            while (!IsEnd)
            {
                _MainLoopForMultiThread();
            }

            ShutdownSubNodes();
        }

        /// <summary>
        /// 对于逻辑有意义的主循环
        /// </summary>
        private void _MainLoopForMultiThread()
        {
            double last = (new TimeSpan(DateTime.Now.Ticks)).TotalMilliseconds;

            float mDetaTimeFloatInSec = (float)(mDeltaTimeInMiliSec / 1000.0f);

            UpdateLogicTree();
            UpdateEvents();
            OnLogicNodeUpdate(mDetaTimeFloatInSec);
            SwapEventQueues();
            TryToDoLogicNodeGC(mDetaTimeFloatInSec);

            //极限: 睡0以上，200以下//
            int deltaTimeToSleep = (int)Math.Min(Math.Max(mLoopTimeGapInMiliSec - mDeltaTimeInMiliSec, 0.0), 200);
            Thread.Sleep(deltaTimeToSleep);

            double now = (new TimeSpan(DateTime.Now.Ticks)).TotalMilliseconds;
            mDeltaTimeInMiliSec = now - last;

            //防止子线程因为应用程序切到后台后Watcher计算出一个巨大的时间差，所以需要保护，这里主要是针对非Android写的//
            mDeltaTimeInMiliSec = Math.Min(Math.Max(mDeltaTimeInMiliSec, 0), 1000);
        }

        /// <summary>
        /// 启动回调
        /// 注意这句话被调用的时候仍然在主线程
        /// </summary>
        public void OnStart()
        {
            //重置时间//
            mDeltaTimeInMiliSec = 0;

            //保留指针//
            mInstance = this;

            /*
             * 启动主线程到逻辑线程的消息桥, 保留static指针 
             * 放在这里是为了确保早于主线程LMNDataOceanMgr.Instance.InitMobaLoading_MainThread
             * 注意：是有意让他在主线程后继mgr启动前执行完，因为之后的逻辑线程或者主线程就可能要来用了，但是它本身是挂在逻辑线程树里面的节点
             */
            X2LBridge.InitStaticInstance();
            AttachNode(X2LBridge.Instance);
        }

        /// <summary>
        /// 结束回调
        /// 注意这句话是在主线程调用的
        /// </summary>
        public void OnStop()
        {
            IsEnd = true;

            //很重要，不然就内存泄露了//
            mInstance = null;
        }

        /// <summary>
        /// 启动子系统
        /// </summary>
        private void InitSubNodes()
        {
            //不一定有效，但是先设上高优先级先//
            Thread.CurrentThread.Priority = ThreadPriority.Highest;

            //留下我自己的ThreadId//
            LMDataOcean.mLogicThreadId = Thread.CurrentThread.ManagedThreadId;

            //启动游戏逻辑//
            AttachNode(new LNumAccSys());

            //Demo3才启动这个假帧同步系统//
            if(LMDataOcean.mCurDemo == eDemoType.Demo3)
            {
                AttachNode(new LFrameSyncSys());
            }

            //标识自己启动成功//
            LMDataOcean.mLogicThreadStarted = true;

            LogUtil.Warning("LLogicCore.InitSubNodes: finished");
        }

        /// <summary>
        /// 关闭子系统
        /// </summary>
        private void ShutdownSubNodes()
        {
            //删除消息桥指针//
            X2LBridge.ShutdownStaticInstance();

            //关闭所有系统//
            DetachAllSubNodes();
        }
    }
}
