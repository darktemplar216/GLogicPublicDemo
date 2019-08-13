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
using UnityEngine;

namespace GameSys.M
{

    /// <summary>
    /// Demo里这个类用于和L层的 LNumAccSys 通讯
    /// 大家一起计数
    /// </summary>
    public class MNumAccSys : ObjLogicNode, IGEventListener
    {
        public override void OnAttached(IGLogicNode inLogicNode)
        {
            base.OnAttached(inLogicNode);

            if (X2MBridge.IsValid)
            {
                X2MBridge.Instance.AttachListener((int)eFakeEventIdDefs.NumAccumForDemo1, this);
                X2MBridge.Instance.AttachListener((int)eFakeEventIdDefs.ShowDisplayUIForDemo2, this);
            }
        }

        public override void OnDetached(IGLogicNode inLogicNode)
        {
            base.OnDetached(inLogicNode);

            if (X2MBridge.IsValid)
            {
                X2MBridge.Instance.DetachListener((int)eFakeEventIdDefs.NumAccumForDemo1, this);
                X2MBridge.Instance.DetachListener((int)eFakeEventIdDefs.ShowDisplayUIForDemo2, this);
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
                case (int)eFakeEventIdDefs.NumAccumForDemo1: { ret = OnNumAccumForDemo1(inEvent); } break;
                case (int)eFakeEventIdDefs.ShowDisplayUIForDemo2: { ret = OnShowDisplayUIForDemo2(inEvent); } break;

            }

            return ret;
        }

        #region NumAccumForDemo1

        private bool OnNumAccumForDemo1(IGEvent inEvent)
        {
            bool ret = false;

            LMEvent_NumAccumForDemo1 realEvt = inEvent as LMEvent_NumAccumForDemo1;

            if (X2LBridge.IsValid)
            {
                LMEvent_NumAccumForDemo1 evt = FakeObjPoolMgr.FetchAutoRecycleObj<LMEvent_NumAccumForDemo1>();
                evt.mNumberToAccum = realEvt.mNumberToAccum + 1;

                //注意这里发送给了L层//
                X2LBridge.Instance.SendEventAsync(evt);

                LogUtil.Debug("MNumAccSys.OnNumAccum: MFrame {0}, from {1} to {2}",
                    LMDataOcean.mMainThreadFrameCounter, realEvt.mNumberToAccum, evt.mNumberToAccum);
            }

            return ret;
        }

        #endregion

        #region ShowDisplayUIForDemo2

        private bool OnShowDisplayUIForDemo2(IGEvent inEvent)
        {
            bool ret = false;

            GameObject ui = GameObject.Instantiate(Resources.Load("TextForDemo2")) as GameObject;
            GameObject canvas = GameObject.FindGameObjectWithTag("Demo2Canvas");
            (ui.transform as RectTransform).SetParent(canvas.transform);

            return ret;
        }

        #endregion
    }
}
