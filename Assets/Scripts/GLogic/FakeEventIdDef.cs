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

namespace Events.LMN
{
    /// <summary>
    /// Demo中我们为了方便把各种id都定义在一起
    /// 实际上我们应该做好模块的分段并且按模块定义
    /// </summary>
    public enum eFakeEventIdDefs
    {
        /// <summary>
        /// 利用unity打Log
        /// </summary>
        LogToUnity = 1,

        /// <summary>
        /// 用于在Demo1中 用于给 MNumAccSys 和 LNumAccSys 一起累加一个数
        /// </summary>
        NumAccumForDemo1,

        /// <summary>
        /// 用在Demo2中 用于 LNumAccSys 让自己累加数值
        /// </summary>
        NumAccumForDemo2,

        /// <summary>
        /// 用于Demo2中 在界面上展示数值
        /// </summary>
        DisplayNumToUIForDemo2,

        /// <summary>
        /// 用于Demo2中 显示数值界面
        /// </summary>
        ShowDisplayUIForDemo2,

        /// <summary>
        /// 用于Demo3中，服务器走了一帧
        /// </summary>
        ServerGoOneFrameForDemo3,
    }

}
