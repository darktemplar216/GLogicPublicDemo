/****************************************************************** 
* Copyright (C): GPL 
* Create By: taowei@tencent.com 61197311@qq.com
* Description: 使用请保留版权头，其它没什么要求 
******************************************************************/

using ObjPoolModule.LMN;
using System.Collections;
using System.Collections.Generic;

namespace ObjPoolModule.LMN
{
    /// <summary>
    /// 这是一个假的多线程安全对象池
    /// </summary>
    public class FakeMultiThreadObjPool<T> : IPool where T : class, IRecycleable, new()
    {
        /// <summary>
        /// 返回一个IRecycleable
        /// </summary>
        /// <returns></returns>
        public IRecycleable FetchAutoRecycleObj()
        {
            /*
             * 注意因为这里是Demo，这里直接就new了一个出去
             * 实际上你需要实现自己的多线程安全自回收对象池
             * 因为商业问题，这里笔者就不提供真正的实现了
             */
            return new T();
        }

        /// <summary>
        /// 清空内容
        /// </summary>
        public void Clear()
        {

        }

        /// <summary>
        /// 释放，包括自己
        /// 注意这个和Clear的意义不一样哈
        /// 这个是删除整个池子了
        /// </summary>
        public void Release()
        {

        }

    }
}
