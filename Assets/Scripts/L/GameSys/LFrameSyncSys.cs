/****************************************************************** 
* Copyright (C): GPL 
* Create By: taowei@tencent.com 61197311@qq.com
* Description: 使用请保留版权头，其它没什么要求 
******************************************************************/

using BridgeNodes.LMN;
using Event.FrameSync.LMN;
using Events.LMN;
using GLogic;
using GLogic.LMN;
using Log.LMN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrameSync.L
{
    /// <summary>
    /// 假的帧同步系统
    /// </summary>
    public class LFrameSyncSys : ObjLogicNode, IGEventListener
    {
        public override void OnAttached(IGLogicNode inLogicNode)
        {
            base.OnAttached(inLogicNode);

            if (X2LBridge.IsValid)
            {
                X2LBridge.Instance.AttachListener((int)eFakeEventIdDefs.ServerGoOneFrameForDemo3, this);
            }
        }

        public override void OnDetached(IGLogicNode inLogicNode)
        {
            base.OnDetached(inLogicNode);

            if (X2LBridge.IsValid)
            {
                X2LBridge.Instance.DetachListener((int)eFakeEventIdDefs.ServerGoOneFrameForDemo3, this);
            }
        }

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

            switch (inEvent.EventKey)
            {
                case (int)eFakeEventIdDefs.ServerGoOneFrameForDemo3: { ret = OnServerGoOneFrameForDemo3(inEvent); } break;
            }

            return ret;
        }

        /// <summary>
        /// 假的帧缓冲
        /// </summary>
        private Dictionary<int, LMNEvent_ServerGoOneFrameForDemo3> mFakeFrameBuffer = new Dictionary<int, LMNEvent_ServerGoOneFrameForDemo3>();

        /// <summary>
        /// 用于客户端计时
        /// </summary>
        private float mTimeAccumForOnSec = 0;

        /// <summary>
        /// 客户端帧
        /// </summary>
        private int mClientFrame = 0;

        public override void OnLogicNodeUpdate(float inDeltaTime)
        {
            base.OnLogicNodeUpdate(inDeltaTime);

            mTimeAccumForOnSec += inDeltaTime;

            //这里认为时间到了并且收到了服务器的帧驱动//
            if (mTimeAccumForOnSec >= 1.0f && mFakeFrameBuffer.ContainsKey(mClientFrame + 1))
            {
                mTimeAccumForOnSec -= 1.0f;

                mClientFrame++;

                //这个时候客户端能走一帧了//
                LogUtil.Warning("LFrameSyncSys.OnLogicNodeUpdate: now client entered SyncFrame -> {0}", mClientFrame);
            }
        }

        #region ServerGoOneFrameForDemo3

        private bool OnServerGoOneFrameForDemo3(IGEvent inEvent)
        {
            bool ret = false;

            LMNEvent_ServerGoOneFrameForDemo3 realEvt = inEvent as LMNEvent_ServerGoOneFrameForDemo3;

            //这里模拟收到了服务器的帧驱动//

            //注意，如果你真的写好了多线程自动回收对象池的话，这个地方是不能直接缓存来自网络线程的对象的//
            mFakeFrameBuffer.Add(realEvt.mServerFrame, realEvt);

            return ret;
        }

        #endregion

    }
}
