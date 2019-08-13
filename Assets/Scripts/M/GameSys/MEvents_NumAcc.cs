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

namespace Event.GameSys.M
{

    /// <summary>
    /// 用于Demo2中 显示数值界面
    /// </summary>
    public class LMEvent_ShowDisplayUIForDemo2 : BaseEvent
    {
        public override int EventKey
        {
            get { return (int)eFakeEventIdDefs.ShowDisplayUIForDemo2; }
        }

        /// <summary>
        /// 就是记这个数
        /// </summary>
        public int mNumberToAccum = 0;
    }



}
