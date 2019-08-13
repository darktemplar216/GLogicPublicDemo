/****************************************************************** 
* Copyright (C): GPL 
* Create By: taowei@tencent.com 61197311@qq.com
* Description: 使用请保留版权头，其它没什么要求 
******************************************************************/

using BridgeNodes.LMN;
using DataOceanModule.LMN;
using Event.FrameSync.LMN;
using GLogic.LMN;
using ObjPoolModule.LMN;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Net.N
{
    /// <summary>
    /// 假的服务器
    /// </summary>
    public class NFakeServerMgr : ObjLogicNode
    {

        /// <summary>
        /// 用于模拟服务器计时
        /// </summary>
        private float mTimeAccumForOnSec = 0;

        /// <summary>
        /// 服务器的逻辑帧号
        /// </summary>
        private int mServerFrame = 0;

        public override void OnLogicNodeUpdate(float inDeltaTime)
        {
            base.OnLogicNodeUpdate(inDeltaTime);

            //所有线程都已经启动完成//
            if(LMDataOcean.mNetThreadStarted && LMDataOcean.mLogicThreadStarted && LMDataOcean.mMainThreadStarted)
            {
                mTimeAccumForOnSec += inDeltaTime;
                if (mTimeAccumForOnSec >= 1.0f)
                {
                    mTimeAccumForOnSec -= 1.0f;

                    //服务器认为自己可以走一帧了//
                    mServerFrame++;

                    if (X2LBridge.IsValid)
                    {
                        //通知客户端走一帧//
                        LMNEvent_ServerGoOneFrameForDemo3 evt = FakeObjPoolMgr.FetchAutoRecycleObj<LMNEvent_ServerGoOneFrameForDemo3>();
                        evt.mServerFrame = mServerFrame;
                        X2LBridge.Instance.SendEventAsync(evt);
                    }
                }
            }
        }

    }

}
