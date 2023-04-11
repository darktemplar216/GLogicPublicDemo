
/****************************************************************** 
* Copyright (C): GPL 
* Create By: taowei@tencent.com 61197311@qq.com
* Description: 使用请保留版权头，其它没什么要求 
******************************************************************/

using System;
using System.Collections.Generic;

using ObjPoolModule.LMN;

namespace GLogic
{
    /// <summary>
    /// 消息的接口定义
    /// </summary>
    public interface IGEvent : IRecycleable
    {
        /// <summary>
        /// 消息Id
        /// </summary>
        int EventKey { get; }
    }

    /// <summary>
    /// 消息的基类
    /// </summary>
    public class BaseEvent : IGEvent
    {
        /// <summary>
        /// 消息Id
        /// </summary>
        public virtual int EventKey { get; }

        /// <summary>
        /// 我是属于哪个线程的
        /// </summary>
        public int ThreadId { get; set; }

        /// <summary>
        /// 通过为这个属性赋值true来告知对应对象池可以回收该对象了
        /// 这个手动赋值
        /// </summary>
        public bool IsReadyToRecycle { get; set; }

        /// <summary>
        /// 取出回调
        /// </summary>
        public virtual void OnFetch() { }

        /// <summary>
        /// 回收回调
        /// </summary>
        public virtual void OnRecycle() { }

        /// <summary>
        /// 如果我是在池子里的，那我属于这个池子
        /// 不要手动赋值
        /// </summary>
        public IPool MyPool { get; set; }

        /// <summary>
        /// 是否是自动释放
        /// 不要手动赋值
        /// </summary>
        public bool IsAutoRecycle { get; set; }
    }

    /// <summary>
    /// 消息处理器的接口定义
    /// </summary>
    public interface IGEventListener
    {

        /// <summary>
        /// 处理输入的消息
        /// </summary>
        /// <param name="inEvent"></param>
        /// <returns> 如果希望阻止消息的继续派发，返回true </returns>
        bool HandleEvent(IGEvent inEvent);

        /// <summary>
        /// 1. 返回这个节点的消息优先级
        /// 2. 一般写为一个针对inEventKey的switch case, 针对不同的inEventKey返回不同的优先级
        /// 3. 返回值越大，就会在被AttachListener到的节点上更早的接收到消息
        /// 4. LogicNode的消息优先级排序只会发生在AttachListener的时候，任何之后的修改均无效
        /// </summary>
        /// <param name="inEventKey"></param>
        /// <returns></returns>
        int GetPriority(int inEventKey);
    }

    /// <summary>
    /// 逻辑节点的接口定义
    /// </summary>
    public interface IGLogicNode
    {
        #region 逻辑节点属性+状态

        /// <summary>
        /// 逻辑节点的名称，默认是0
        /// 如果是0，那上层逻辑节点认为这个节点不需要GetNodeByName支持
        /// </summary>
        int NodeName { get; set; }

        /// <summary>
        /// 1.这个节点是否允许工作
        /// 2.如果为false则它不会执行OnUpdate也不会广播消息
        /// </summary>
        bool IsNodeActive { get; set; }

        /// <summary>
        /// 逻辑节点的当前状态，不要手动去赋值
        /// </summary>
        /// <value>The node status.</value>
        AttachableStatus NodeStatus { get; set; }

        /// <summary>
        /// 逻辑树需不需要更新
        /// 在发生了 _AttachNodeAsync 的时候就需要递归向根节点置脏
        /// </summary>
        bool IsLogicTreeNeedsUpdate { get; set; }

        /// <summary>
        /// 1.逻辑节点的执行优先级
        /// 2.逻辑节点优先级只在AttachNode的时候使用，任何之后的修改均无效
        /// </summary>
        int LogicNodePriority { get; set; }

        /// <summary>
        /// 1.本节点是否参与消息广播
        /// 2.是优化消息广播量的重要手段
        /// </summary>
        bool AsEventDispatcher { get; set; }

        /// <summary>
        /// 1.当为null的时候，本节点不是一个线程安全节点
        /// 2.不要尝试在AttachNode之后修改，会导致多线程问题
        /// </summary>
        object ThreadSafeLock { get; set; }

        /// <summary>
        /// 1.逻辑树结构锁，非线程锁，用于在遍历树结构的时候阻止修改操作
        /// 2.当为true的时候AttachNode和DetachNode都会生成异步操作
        /// 3.在OnAttached/OnDetached/OnUpdate/UpdateEvents的时候为true
        /// </summary>
        bool IsNodeTreeLocked { get; set; }

        /// <summary>
        /// 返回父节点，根节点返回null
        /// </summary>
        IGLogicNode ParentNode { get; set; }

        /// <summary>
        /// 返回根节点，根节点返回本身
        /// </summary>
        IGLogicNode RootNode { get; }

        #endregion

        #region 查找/添加/移除 逻辑节点

        /// <summary>
        /// 用名字查找一个LogicNode
        /// 默认值0不会找到任何节点，参见NodeName
        /// </summary>
        /// <param name="inNodeName"></param>
        /// <returns></returns>
        IGLogicNode GetNodeByName(int inNodeName);

        /// <summary>
        /// 1.向本节点挂接一个LogicNode
        /// 2.非线程安全
        /// 3.会在以下方法中生成异步操作OnAttached/OnDetached/OnUpdate/UpdateEvents, 参见IsNodeTreeLocked
        /// </summary>
        /// <param name="inLogicNode"></param>
        /// <returns></returns>
        bool AttachNode(IGLogicNode inLogicNode);

        /// <summary>
        /// 1.从本节点摘除一个LogicNode
        /// 2.非线程安全
        /// 3.会在以下方法中生成异步操作OnAttached/OnDetached/OnUpdate/UpdateEvents, 参见IsNodeTreeLocked
        /// </summary>
        /// <param name="inLogicNode"></param>
        /// <returns></returns>
        bool DetachNode(IGLogicNode inLogicNode);

        /// <summary>
        /// 1.摘除本节点下的所有节点
        /// 2.非线程安全
        /// 3.会在以下方法中生成异步操作OnAttached/OnDetached/OnUpdate/UpdateEvents, 参见IsNodeTreeLocked
        /// </summary>
        /// <returns></returns>
        bool DetachAllSubNodes();

        #endregion

        #region 关联节点回调

        /// <summary>
        /// 在挂入逻辑树的时候进行回调
        /// </summary>
        /// <param name="inLogicNode"></param>
        void OnAttached(IGLogicNode inLogicNode);

        /// <summary>
        /// 在将要从逻辑树摘除时回调，OnDetached 之前
        /// </summary>
        /// <param name="inLogicNode"></param>
        void OnPreDetach(IGLogicNode inLogicNode);

        /// <summary>
        /// 在从逻辑树摘除的时候进行回调
        /// </summary>
        /// <param name="inLogicNode"></param>
        void OnDetached(IGLogicNode inLogicNode);
        #endregion

        #region 消息监听和广播

        /// <summary>
        /// 1.向本逻辑节点挂入一个消息监听器，并开始在该节点上监听inEventKey消息
        /// 2.会在以下方法中生成异步操作UpdateEvents, 参见IsListenerTreeLocked
        /// 3.消息监听器的在逻辑节点中是以弱引用存在的，你必须自己管理它的生命周期
        /// </summary>
        /// <param name="inEventKey"></param>
        /// <param name="inListener"></param>
        /// <returns></returns>
        bool AttachListener(int inEventKey, IGEventListener inListener);

        /// <summary>
        /// 1.从本逻辑节点上摘除监听inEventKey的inListener消息监听器
        /// 2.会在以下方法中生成异步操作UpdateEvents, 参见IsListenerTreeLocked
        /// 3.消息监听器的在逻辑节点中是以弱引用存在的，你必须自己管理它的生命周期
        /// </summary>
        /// <param name="inEventKey"></param>
        /// <param name="inListener"></param>
        /// <returns></returns>
        bool DetachListener(int inEventKey, IGEventListener inListener);

        /// <summary>
        /// 发送同步消息，立即完成消息的执行
        /// 不可以跨线程使用
        /// </summary>
        /// <returns><c>true</c>, if event sync was sent, <c>false</c> otherwise.</returns>
        /// <param name="inEvt">In evt.</param>
        bool SendEventSync(IGEvent inEvt);

        /// <summary>
        /// 发送异步消息，会在下一帧执行
        /// 在节点是线程安全节点的时候，可以跨线程使用
        /// </summary>
        /// <returns><c>true</c>, if event async was sent, <c>false</c> otherwise.</returns>
        /// <param name="inEvt">In evt.</param>
        /// <param name="inCacheUpWhenInactive">If set to <c>true</c> in cach up when inactive.</param>
        bool SendEventAsync(IGEvent inEvt, bool inCacheUpWhenInactive);

        #endregion

        #region 内部更新接口
        
        /// <summary>
        /// 更新Logic节点树
        /// 导致 IsNodeTreeLocked 为真
        /// </summary>
        void UpdateLogicTree();

        /// <summary>
        /// 引发消息逻辑执行
        /// 导致 IsNodeTreeLocked, IsListenerTreeLocked 为真
        /// </summary>
        void UpdateEvents();

        /// <summary>
        /// 广播消息
        /// </summary>
        void DispatchEvents();

        /// <summary>
        /// 广播一个消息
        /// </summary>
        /// <param name="inEvent">Evt.</param>
        bool DispatchAnEvent(IGEvent inEvent);

        /// <summary>
        /// 尝试去释放一个消息
        /// </summary>
        /// <param name="inEvent"></param>
        /// <returns></returns>
        void TryToRecycleThisEvent(IGEvent inEvent);

        /// <summary>
        /// 引发消息处理
        /// </summary>
        /// <param name="inEvent">Evt.</param>
        bool TriggerEvent(IGEvent inEvent);

        /// <summary>
        /// 用于提供双buffer缓冲规范
        /// </summary>
        void SwapEventQueues();

        /// <summary>
        /// 用于实现对消息监听器Dic等 的GC
        /// </summary>
        void GC();

        /// <summary>
        /// 逻辑帧
        /// </summary>
        /// <param name="inDeltaTime"></param>
        void OnLogicNodeUpdate(float inDeltaTime);
        
        #endregion

    }


    /// <summary>
    /// 用于LogicNode所需的优先级查找
    /// </summary>
    public class PrioritySortUtil
    {
        /// <summary>
        /// 提供优先级查找接口
        /// </summary>
        public interface IPriorityComparable
        {
            int PriorityVal { get; }
        }

        /// <summary>
        /// 在降序列表里面定位一个优先级段的任意位置
        /// 注意：这里得到的是一个用于插入 inPriority 的最近段中的任意位置，不是 inPriority 本身要插入的位置
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="inPriority"></param>
        /// <param name="inArray"></param>
        /// <returns></returns>
        public static int _GetDecSeqArrayNearestRefIndex<T>(int inPriority, List<T> inArray) where T : IPriorityComparable
        {
            int upperBound = inArray.Count;
            int lowerBound = 0;

            // 0 + 99 -> 0
            // 0 0 + 99 -> 0
            // 0 0 0 + 99 -> 0
            // 99 + 0 -> 0
            // 99 99 + 0 -> 1
            // 99 99 99 + 0 -> 2
            // 99 50 + 50 -> 1
            // 99 50 0 + 50 -> 1
            // 99 49 0 + 50 -> 0

            int ret = 0;
            int lastRet = ret;

            while (lowerBound < upperBound)
            {
                ret = (lowerBound + upperBound) >> 1;
                int curPriority = inArray[ret].PriorityVal;
                if (curPriority == inPriority)
                {
                    break;
                }
                else if (curPriority > inPriority)
                {
                    lowerBound = ret == lastRet ? ret + 1 : ret;
                }
                else
                {
                    upperBound = ret == lastRet ? ret - 1 : ret;
                }

                lastRet = ret;
            }

            return ret;
        }

        /// <summary>
        /// 找到拥有同样优先级的节点首次出现位置
        /// 在没有相同优先级节点的时候返回二分查找得出的位置
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="inPriority"></param>
        /// <param name="inArray"></param>
        /// <returns></returns>
        public static int GetDecSeqArrayFirstIndex<T>(int inPriority, List<T> inArray) where T : IPriorityComparable
        {
            int ret = _GetDecSeqArrayNearestRefIndex(inPriority, inArray);
            // 这段的逻辑主要是找到头在哪里
            for (; ret > 0 && inArray[ret - 1].PriorityVal == inPriority; ret--) { }
            return ret;
        }

        /// <summary>
        /// 获得降序排列优先级列表用于插入的位置
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="inPriority"></param>
        /// <param name="inArray"></param>
        /// <returns></returns>
        public static int GetDecSeqArrayInsertIndex<T>(int inPriority, List<T> inArray) where T : IPriorityComparable
        {
            int ret = _GetDecSeqArrayNearestRefIndex(inPriority, inArray);
            // 这段的逻辑主要是找到头尾在哪里，新加的位置为了保证稳定，要放在区段的头尾
            for (; ret < inArray.Count && inArray[ret].PriorityVal >= inPriority; ret++) { }
            for (; ret > 0 && inArray[ret - 1].PriorityVal < inPriority; ret--) { }
            return ret;
        }
    }

    /// <summary>
    ///逻辑节点和消息监听器的当前状态
    /// </summary>
    public enum AttachableStatus
    {
        /// <summary>
        /// 没有被挂入到逻辑树中
        /// </summary>
        Detached,

        /// <summary>
        /// 正在从逻辑树中被摘除
        /// </summary>
        Detaching,

        /// <summary>
        /// 已经挂入到逻辑树中
        /// </summary>
        Attached,

        /// <summary>
        /// 正在挂入到逻辑树中
        /// </summary>
        Attaching,
    }

    /// <summary>
    /// 用于在逻辑树中保存GLogicNode, 方便针对优先级的遍历
    /// </summary>
    public struct GLogicNodeWrapper : PrioritySortUtil.IPriorityComparable
    {
        public IGLogicNode mLogicNode;

        public int PriorityVal
        {
            get { return mLogicNode == null ? 0 : mLogicNode.LogicNodePriority; }
        }
    }

    /// <summary>
    /// 用于在逻辑树种保存GEventListener, 注意是弱引用，本身并不维持Listener的生命周期
    /// </summary>
    public struct GEventListenerWrapper : PrioritySortUtil.IPriorityComparable
    {
        public WeakReference mEventListenerWeakRef;

        public int mEventListenerPriority;

        public AttachableStatus m_AttachableStatus;

        public int PriorityVal 
		{
            get { return mEventListenerPriority; }
        }
    }
}
