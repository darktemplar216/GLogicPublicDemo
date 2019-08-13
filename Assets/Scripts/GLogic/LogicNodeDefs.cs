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

namespace GLogic.LMN
{
    /// <summary>
    /// 我们游戏用到的节点别名
    /// </summary>
    public enum ePMGameNodeNameDefs
    {
        LLogicCore = 1,
        NLogicCore = 2,
    }

    /// <summary>
    /// 这几个节点优先级在单线程时起到作用
    /// </summary>
    public enum eLogicCorePriority
    {
        NLogicCore = 999,
        LLogicCore = 500,
    }

}
