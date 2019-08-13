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
    /// 这是一个假的多线程安全对象池管理器
    /// </summary>
    public class FakeObjPoolMgr
    {
        /// <summary>
        /// 返回一个IRecycleable 的 T
        /// </summary>
        /// <returns></returns>
        public static T FetchAutoRecycleObj<T>() where T : class, IRecycleable, new()
        {
            /*
             * 注意因为这里是Demo，这里直接就new了一个出去
             * 实际上你需要实现自己的多线程安全自回收对象池
             * 因为商业问题，这里笔者就不提供真正的实现了
             */
            return (new FakeMultiThreadObjPool<T>()).FetchAutoRecycleObj() as T;
        }

    }
}
