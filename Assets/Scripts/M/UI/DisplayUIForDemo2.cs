/****************************************************************** 
* Copyright (C): GPL 
* Create By: taowei@tencent.com 61197311@qq.com
* Description: 使用请保留版权头，其它没什么要求 
******************************************************************/

using BridgeNodes.LMN;
using Event.GameSys.LM;
using Events.LMN;
using GLogic;
using Log.LMN;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DisplayUIForDemo2 : MonoBehaviour, IGEventListener
{
    /// <summary>
    /// 用于展示的Text控件
    /// </summary>
    public Text mDisplayText = null;

    public void Awake()
    {
        if(X2MBridge.IsValid)
        {
            X2MBridge.Instance.AttachListener((int)eFakeEventIdDefs.NumAccumForDemo2, this);
        }
        else
        {
            LogUtil.Error("DisplayUIForDemo2.Awake: X2MBridge is not ready yet");
        }
    }

    public void OnDestroy()
    {
        if (X2MBridge.IsValid)
        {
            X2MBridge.Instance.DetachListener((int)eFakeEventIdDefs.NumAccumForDemo2, this);
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
            case (int)eFakeEventIdDefs.NumAccumForDemo2: { ret = OnNumAccumForDemo2(inEvent); } break;
        }

        return ret;
    }

    #region NumAccumForDemo2

    private bool OnNumAccumForDemo2(IGEvent inEvent)
    {
        bool ret = false;

        if(mDisplayText == null)
        {
            LogUtil.Error("DisplayUIForDemo2.OnNumAccumForDemo2: mDisplayText invalid");
            return ret;
        }

        LMEvent_NumAccumForDemo2 realEvt = inEvent as LMEvent_NumAccumForDemo2;
        mDisplayText.text = realEvt.mNumberToAccum.ToString();

        return ret;
    }

    #endregion
}
