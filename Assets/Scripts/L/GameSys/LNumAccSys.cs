/****************************************************************** 
* Copyright (C): GPL 
* Create By: taowei@tencent.com 61197311@qq.com
* Description: 使用请保留版权头，其它没什么要求 
******************************************************************/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BridgeNodes.LMN;
using DataOceanModule.LMN;
using Event.GameSys.LM;
using Event.Log.LMN;
using Events.LMN;
using GLogic;
using GLogic.LMN;
using Log.LMN;
using ObjPoolModule.LMN;

namespace GameSys.L
{

    /// <summary>
    /// Demo里这个类用于和L层的 MNumAccSys 通讯
    /// 大家一起计数
    /// </summary>
    public class LNumAccSys : ObjLogicNode, IGEventListener
    {
        public override void OnAttached(IGLogicNode inLogicNode)
        {
            base.OnAttached(inLogicNode);

            if (X2LBridge.IsValid)
            {
                X2LBridge.Instance.AttachListener((int)eFakeEventIdDefs.NumAccumForDemo1, this);

                //EntranceForDemo2 第一次从这里收到//
                X2LBridge.Instance.AttachListener((int)eFakeEventIdDefs.NumAccumForDemo2, this);
            }

            //之后是自己发给自己的//
            AttachListener((int)eFakeEventIdDefs.NumAccumForDemo2, this);
        }

        public override void OnDetached(IGLogicNode inLogicNode)
        {
            base.OnDetached(inLogicNode);

            if (X2LBridge.IsValid)
            {
                X2LBridge.Instance.DetachListener((int)eFakeEventIdDefs.NumAccumForDemo1, this);


                X2LBridge.Instance.DetachListener((int)eFakeEventIdDefs.NumAccumForDemo2, this);
            }


            DetachListener((int)eFakeEventIdDefs.NumAccumForDemo2, this);
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
                case (int)eFakeEventIdDefs.NumAccumForDemo1: { ret = OnNumAccumForDemo1(inEvent); } break;
                case (int)eFakeEventIdDefs.NumAccumForDemo2: { ret = OnNumAccumForDemo2(inEvent); } break;
            }

            return ret;
        }

        #region NumAccumForDemo1

        private bool OnNumAccumForDemo1(IGEvent inEvent)
        {
            bool ret = false;

            LMEvent_NumAccumForDemo1 realEvt = inEvent as LMEvent_NumAccumForDemo1;

            if(X2MBridge.IsValid)
            {
                LMEvent_NumAccumForDemo1 evt = FakeObjPoolMgr.FetchAutoRecycleObj<LMEvent_NumAccumForDemo1>();
                evt.mNumberToAccum = realEvt.mNumberToAccum + 1;

                //注意这里发送给了M层//
                X2MBridge.Instance.SendEventAsync(evt);

                LogUtil.Debug("LNumAccSys.OnNumAccum: LFrame {0}, from {1} to {2}", 
                    LMDataOcean.mLogicThreadFrameCounter, realEvt.mNumberToAccum, evt.mNumberToAccum);
            }

            return ret;
        }

        #endregion

        #region NumAccumForDemo2

        private bool OnNumAccumForDemo2(IGEvent inEvent)
        {
            bool ret = false;

            LMEvent_NumAccumForDemo2 realEvt = inEvent as LMEvent_NumAccumForDemo2;

            int newVal = realEvt.mNumberToAccum + 1;

            //注意这里异步发给了自己，下一帧就会收到//
            LMEvent_NumAccumForDemo2 anEvtToMyself = FakeObjPoolMgr.FetchAutoRecycleObj<LMEvent_NumAccumForDemo2>();
            anEvtToMyself.mNumberToAccum = newVal;
            SendEventAsync(anEvtToMyself);

            //注意这里给M层也发了一份，那边是 DisplayUIForDemo2 等着收//
            if (X2MBridge.IsValid)
            {
                LMEvent_NumAccumForDemo2 anotherEvtToM = FakeObjPoolMgr.FetchAutoRecycleObj<LMEvent_NumAccumForDemo2>();
                anotherEvtToM.mNumberToAccum = newVal;
                //注意这里发送给了M层//
                X2MBridge.Instance.SendEventAsync(anotherEvtToM);
            }

            return ret;
        }

        #endregion
    }
}
