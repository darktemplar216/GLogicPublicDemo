/****************************************************************** 
* Copyright (C): GPL 
* Create By: taowei@tencent.com 61197311@qq.com
* Description: 使用请保留版权头，其它没什么要求 
******************************************************************/

using System.Collections;
using System.Collections.Generic;

namespace ObjPoolModule.LMN
{

    /// <summary>
    /// 可以异步回收的对象接口
    /// </summary>
    public interface IRecycleable
    {
        /// <summary>
        /// 我是属于哪个线程的
        /// </summary>
        int ThreadId { get; set; }

        /// <summary>
        /// 通过为这个属性赋值true来告知对应对象池可以回收该对象了
        /// 这个手动赋值
        /// </summary>
        bool IsReadyToRecycle { get; set; }

        /// <summary>
        /// 取出回调
        /// </summary>
        void OnFetch();

        /// <summary>
        /// 回收回调
        /// </summary>
        void OnRecycle();

        /// <summary>
        /// 如果我是在池子里的，那我属于这个池子
        /// 不要手动赋值
        /// </summary>
        IPool MyPool { get; set; }

        /// <summary>
        /// 是否是自动释放
        /// 不要手动赋值
        /// </summary>
        bool IsAutoRecycle { get; set; }
    }
}