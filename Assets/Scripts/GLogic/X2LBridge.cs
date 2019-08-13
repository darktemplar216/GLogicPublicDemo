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
using GLogic;
using GLogic.LMN;
using Log.LMN;

namespace BridgeNodes.LMN
{
    /// <summary>
    /// 用于任意线程给逻辑线程发送消息
    /// 本身挂接在MLogicCore
    /// </summary>    
    public class X2LBridge : ObjLogicNode
    {
        private X2LBridge() : base()
        {
            //我是一个线程安全节点//
            ThreadSafeLock = new object();
        }

        #region 指针

        private static X2LBridge mInstance = null;

        public static X2LBridge Instance
        {
            get
            {
                return mInstance;
            }
        }

        public static bool IsValid
        {
            get { return (mInstance != null); }
        }

        /// <summary>
        /// 保留指针
        /// </summary>
        public static void InitStaticInstance()
        {
            mInstance = new X2LBridge();
        }

        /// <summary>
        /// 销毁指针
        /// </summary>
        public static void ShutdownStaticInstance()
        {
            mInstance = null;
        }

        #endregion

        public override bool SendEventSync(IGEvent inEvt)
        {
            LogUtil.Error("X2LBridge.SendEventSync: don't Call this func, it's not safe");
            return false;
        }

        /// <summary>
        /// 用于在Bridge上面需要发同步消息的情况
        /// 注意必须是这个Bridge所在的线程才能来调用这个方法，不然就是多线程问题
        /// 
        /// 如果是网络请求，就自动把它变成自动释放的包
        /// </summary>
        /// <param name="inEvt"></param>
        /// <returns></returns>
        public bool SendEventSyncOnlyWhenYouKnownWhatYouAreDoing(IGEvent inEvt)
        {
            return base.SendEventSync(inEvt);
        }
    }

}
