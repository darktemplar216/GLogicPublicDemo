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

namespace Event.GameSys.LM
{
    /// <summary>
    /// 用于在Demo1中 用于给 MNumAccSys 和 LNumAccSys 一起累加一个数
    /// </summary>
    public class LMEvent_NumAccumForDemo1 : BaseEvent
    {
        public override int EventKey
        {
            get { return (int)eFakeEventIdDefs.NumAccumForDemo1; }
        }

        /// <summary>
        /// 就是记这个数
        /// </summary>
        public int mNumberToAccum = 0;

    }

    /// <summary>
    /// 用在Demo2中 用于 LNumAccSys 让自己累加数值
    /// </summary>
    public class LMEvent_NumAccumForDemo2 : BaseEvent
    {
        public override int EventKey
        {
            get { return (int)eFakeEventIdDefs.NumAccumForDemo2; }
        }

        /// <summary>
        /// 就是记这个数
        /// </summary>
        public int mNumberToAccum = 0;
    }



}
