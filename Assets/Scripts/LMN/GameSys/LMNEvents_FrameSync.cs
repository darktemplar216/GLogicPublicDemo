/****************************************************************** 
* Copyright (C): GPL 
* Create By: taowei@tencent.com 61197311@qq.com
* Description: 使用请保留版权头，其它没什么要求 
******************************************************************/

using Events.LMN;
using GLogic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Event.FrameSync.LMN
{

    /// <summary>
    /// 服务器的帧驱动
    /// </summary>
    public class LMNEvent_ServerGoOneFrameForDemo3 : BaseEvent
    {
        public override int EventKey
        {
            get { return (int)eFakeEventIdDefs.ServerGoOneFrameForDemo3; }
        }

        /// <summary>
        /// 服务器帧号
        /// </summary>
        public int mServerFrame = 0;
    }



}
