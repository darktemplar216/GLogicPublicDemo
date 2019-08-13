/****************************************************************** 
* Copyright (C): GPL 
* Create By: taowei@tencent.com 61197311@qq.com
* Description: 使用请保留版权头，其它没什么要求 
******************************************************************/

using BridgeNodes.LMN;
using Event.Log.LMN;
using Events.LMN;
using GLogic;
using GLogic.LMN;
using GLogic.M;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Log.M
{
    /// <summary>
    /// 在Demo中用于把每个地方的Log打印出来
    /// </summary>
    public class MLogger : ObjLogicNode, IGEventListener
    {
        public override void OnAttached(IGLogicNode inLogicNode)
        {
            base.OnAttached(inLogicNode);

            if(X2MBridge.IsValid)
            {
                X2MBridge.Instance.AttachListener((int)eFakeEventIdDefs.LogToUnity, this);
            }
        }

        public override void OnDetached(IGLogicNode inLogicNode)
        {
            base.OnDetached(inLogicNode);

            if (X2MBridge.IsValid)
            {
                X2MBridge.Instance.DetachListener((int)eFakeEventIdDefs.LogToUnity, this);
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

            switch(inEvent.EventKey)
            {
                case (int)eFakeEventIdDefs.LogToUnity: { ret = OnLogToUnity(inEvent); } break;
            }

            return ret;
        }

        #region LogToUnity

        private bool OnLogToUnity(IGEvent inEvent)
        {
            bool ret = false;

            LMEvent_LogToUnity realEvt = inEvent as LMEvent_LogToUnity;

            switch(realEvt.mLogLevel)
            {
                case DemoLogLevel.Error: { UnityEngine.Debug.LogError(realEvt.mLogStr); } break;
                case DemoLogLevel.Warning: { UnityEngine.Debug.LogWarning(realEvt.mLogStr); } break;
                case DemoLogLevel.Debug: { UnityEngine.Debug.Log(realEvt.mLogStr); } break;
            }

            return ret;
        }

        #endregion
    }
}
