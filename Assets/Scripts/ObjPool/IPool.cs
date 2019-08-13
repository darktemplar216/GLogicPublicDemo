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

namespace ObjPoolModule.LMN
{
    /// <summary>
    /// 数据池的接口定义
    /// </summary>
    public interface IPool 
    {
        /// <summary>
        /// 返回一个IRecycleable
        /// </summary>
        /// <returns></returns>
        IRecycleable FetchAutoRecycleObj();

        /// <summary>
        /// 清空内容
        /// </summary>
        void Clear();

        /// <summary>
        /// 释放，包括自己
        /// 注意这个和Clear的意义不一样哈
        /// 这个是删除整个池子了
        /// </summary>
        void Release();
    }
}
