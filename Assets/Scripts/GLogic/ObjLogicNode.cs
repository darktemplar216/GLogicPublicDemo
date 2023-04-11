/****************************************************************** 
* Copyright (C): GPL 
* Create By: taowei@tencent.com 61197311@qq.com
* Description: 使用请保留版权头，其它没什么要求 
******************************************************************/


#define DEBUG_G_LOGIC_NODE

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using GLogic;

namespace GLogic.LMN
{
    public class ObjLogicNode : object, IGLogicNode
    {
        #region ObjLogicNode特征

        public ObjLogicNode()
        {
            mEventQueueForThisFrame = mEventQueueFrameOdd;
            mEventQueueForNextFrame = mEventQueueFrameEven;
        }

        ~ObjLogicNode()
        {
            DetachAllSubNodes();

            if (ParentNode != null)
            {
                ParentNode.DetachNode(this);
            }
        }

        #endregion

        #region 节点名称

        /// <summary>
        /// 节点名称
        /// 默认值为0
        /// 如果使用默认值，那父节点将不缓存这个节点和名字的对应，GetNodeByName将不起效
        /// </summary>
        public virtual int NodeName { get; set; }

        #endregion

        #region 激活状态

        private bool mActive = true;

        /// <summary>
        /// 1.该节点激活与否
        /// 2.如果没有激活那OnUpdate和DispatchEvents均不起效
        /// </summary>
        public bool IsNodeActive
        {
            get { return mActive; }
            set { mActive = value; }
        }

        #endregion

        #region 逻辑节点优先级

        private int mLogicNodePriority = 0;

        /// <summary>
        /// 1.这个逻辑节点的优先级，数值越大越优先于其它兄弟节点执行
        /// 2.优先级只在AttachNode的时候起效，任何之后的修改均无效
        /// </summary>
        public int LogicNodePriority
        {
            get { return mLogicNodePriority; }
            set { mLogicNodePriority = value; }
        }

        #endregion

        #region 节点状态

        private AttachableStatus mNodeStatus = AttachableStatus.Detached;

        /// <summary>
        /// 逻辑节点的当前状态
        /// </summary>
        public AttachableStatus NodeStatus
        {
            get { return mNodeStatus; }
            set { mNodeStatus = value; }
        }

        #endregion

        #region 逻辑树需不需要更新

        /// <summary>
        /// 逻辑树需不需要更新
        /// </summary>
        public bool IsLogicTreeNeedsUpdate { get; set; }

        #endregion

        #region 是否是消息派发者

        private bool mAsEventDispatcher = true;

        /// <summary>
        /// 1.是否会将自己或上层的消息向子节点广播
        /// 2.用于优化消息广播消耗
        /// </summary>
        public bool AsEventDispatcher
        {
            get { return mAsEventDispatcher; }
            set { mAsEventDispatcher = value; }
        }

        #endregion

        #region 线程安全

        private object mThreadSafeLock = null;

        /// <summary>
        /// 1.如果是null那说明这个节点不是一个线程安全节点
        /// 2.不要在AttachNode之后尝试去修改这个值，会导致多线程问题
        /// </summary>
        public object ThreadSafeLock
        {
            get { return mThreadSafeLock; }
            set { mThreadSafeLock = value; }
        }

        #endregion

        #region 逻辑节点树形结构锁

        private bool mIsNodeTreeLocked = false;

        /// <summary>
        /// 1.锁住逻辑树结构，不允许修改LogicNode部分
        /// 2.在值为true的时候，AttachNode和DetachNode将形成异步操作
        /// 3.OnAttached, OnDetached, OnUpdate 的时候值为true
        /// </summary>
        public bool IsNodeTreeLocked
        {
            get { return mIsNodeTreeLocked; }
            set
            {
                mIsNodeTreeLocked = value;

                for (int i = 0; i < mSubNodes.Count; i++)
                {
                    GLogicNodeWrapper wrapper = mSubNodes[i];
                    wrapper.mLogicNode.IsNodeTreeLocked = value;
                }
            }
        }

        #endregion

        #region 消息Id锁

        /// <summary>
        /// 1.锁住逻辑树结构，不允许修改Listener部分
        /// 2.对应EventId
        /// </summary>
        private List<int> mLockedEventIds = new List<int>(0);

        #endregion

        #region 父节点

        private IGLogicNode mParentNode = null;

        /// <summary>
        /// 返回父节点，如果已经是根节点那么返回null
        /// </summary>
        public IGLogicNode ParentNode
        {
            get { return mParentNode; }
            set { mParentNode = value; }
        }

        #endregion

        #region 根节点

        /// <summary>
        /// 返回根节点，如果自己就是根节点那会返回自己
        /// </summary>
        public IGLogicNode RootNode
        {
            get
            {
                IGLogicNode ret = this;
                while (ret.ParentNode != null)
                {
                    ret = ret.ParentNode;
                }

                return ret;
            }
        }

        #endregion

        #region 获取指定名称的节点

        public IGLogicNode GetNodeByName(int inNodeName)
        {
            IGLogicNode ret = null;
            mNameAndNodeMap.TryGetValue(inNodeName, out ret);
            return ret;
        }

        #endregion

        #region 逻辑节点的挂接和摘除

        /// <summary>
        /// 记录对逻辑节点的异步处理操作
        /// </summary>
        public struct LogicNodeUpdateOperation
        {
            /// <summary>
            /// 目标节点
            /// </summary>
            public IGLogicNode mLogicNode;

            /// <summary>
            /// 添加还是删除
            /// </summary>
            public bool mIsAddOrRemove;

            /// <summary>
            /// 如果是删除是全部都删除吗
            /// </summary>
            public bool mIsRemoveAll;
        }

        /// <summary>
        /// 子节点列表
        /// </summary>
        protected List<GLogicNodeWrapper> mSubNodes = new List<GLogicNodeWrapper>(0);

        /// <summary>
        /// 目前将会被执行的异步节点修改操作
        /// </summary>
        private List<LogicNodeUpdateOperation> mAsyncNodeOps = new List<LogicNodeUpdateOperation>(0);

        /// <summary>
        /// 子节点和它的名称对应
        /// </summary>
        private Dictionary<int, IGLogicNode> mNameAndNodeMap = new Dictionary<int, IGLogicNode>(0);

        /// <summary>
        /// 1.将一个LogicNode挂到我身上
        /// 2.非线程安全
        /// 3.在 OnAttached/OnDetached/OnUpdate 这几个API中调用的时候会形成异步命令, 参见IsNodeTreeLocked
        /// </summary>
        /// <param name="inLogicNode"></param>
        /// <returns></returns>
        public bool AttachNode(IGLogicNode inLogicNode)
        {
            bool ret = false;

            if (inLogicNode == null)
            {
                //请替换成你的log函数//
                Log.LMN.LogUtil.Error("ObjLogicNode.AttachNode: inLogicNode is null");
                return ret;
            }

            if (IsNodeTreeLocked)
            {
                ret = _AttachNodeAsync(inLogicNode);
            }
            else
            {
                ret = _AttachNodeSync(inLogicNode);
            }

            return ret;
        }

        private bool _AttachNodeSync(IGLogicNode inLogicNode)
        {
            bool ret = false;

            if (inLogicNode == null)
            {
                //请替换成你的log函数//
                Log.LMN.LogUtil.Error("ObjLogicNode._AttachNodeSync: inLogicNode is null");
                return ret;
            }

#if DEBUG_G_LOGIC_NODE

            bool foundDuplicate = false;

            for (int i = 0; i < mSubNodes.Count; i++)
            {
                GLogicNodeWrapper wrapper = mSubNodes[i];
                if (wrapper.mLogicNode == inLogicNode)
                {
                    foundDuplicate = true;
                    break;
                }
            }

            //请替换成你的log函数//
            System.Diagnostics.Debug.Assert(!foundDuplicate, "ObjLogicNode._AttachNodeSync: found duplicated LogicNode -> " + inLogicNode);
#endif

            GLogicNodeWrapper newLogicNodeWrapper = new GLogicNodeWrapper();
            newLogicNodeWrapper.mLogicNode = inLogicNode;
            newLogicNodeWrapper.mLogicNode.NodeStatus = AttachableStatus.Attached;

            //找出插入位置//
            int index = PrioritySortUtil.GetDecSeqArrayInsertIndex<GLogicNodeWrapper>(inLogicNode.LogicNodePriority, mSubNodes);
            mSubNodes.Insert(index, newLogicNodeWrapper);

            //赋值父节点//
            inLogicNode.ParentNode = this;

            //如果节点有名字，那注册名字//
            if (inLogicNode.NodeName != 0)
            {
                mNameAndNodeMap.Add(inLogicNode.NodeName, inLogicNode);
            }

            try
            {
                inLogicNode.OnAttached(this);
            }
            catch (Exception e)
            {
                //请替换成你的log函数//
                Log.LMN.LogUtil.Error("ObjLogicNode._AttachNodeSync: Exception: {e.Message}");
            }

            ret = true;

            return ret;
        }

        private bool _AttachNodeAsync(IGLogicNode inLogicNode)
        {
            bool ret = false;

            if (inLogicNode == null)
            {
                //请替换成你的log函数//
                Log.LMN.LogUtil.Error("ObjLogicNode._AttachNodeAsync: inLogicNode is null");
                return ret;
            }

            //置脏，导致这棵子树需要进入更新流程//
            IsLogicTreeNeedsUpdate = true;
            IGLogicNode parent = ParentNode;
            while (parent != null && !parent.IsLogicTreeNeedsUpdate)
            {
                parent.IsLogicTreeNeedsUpdate = true;
                parent = parent.ParentNode;
            }

            LogicNodeUpdateOperation updateOp = new LogicNodeUpdateOperation();
            updateOp.mIsAddOrRemove = true;
            updateOp.mLogicNode = inLogicNode;
            mAsyncNodeOps.Add(updateOp);

            ret = true;
            return ret;
        }

        /// <summary>
        /// 1.将一个逻辑节点从我身上摘除
        /// 2.非线程安全
        /// 3.在 OnAttached/OnDetached/OnUpdate 这几个API中调用的时候会形成异步命令, 参见IsNodeTreeLocked
        /// 4.在调用后该节点会立即停止工作，参见 mIsAttachedLogically
        /// </summary>
        /// <param name="inLogicNode"></param>
        /// <returns></returns>
        public bool DetachNode(IGLogicNode inLogicNode)
        {
            bool ret = false;

            if (inLogicNode == null)
            {
                //请替换成你的log函数//
                Log.LMN.LogUtil.Error("ObjLogicNode.DetachNode: inLogicNode is null");
                return ret;
            }

            if (IsNodeTreeLocked)
            {
                ret = _DetachNodeAsync(inLogicNode);
            }
            else
            {
                ret = _DetachNodeSync(inLogicNode);
            }

            return ret;
        }

        private bool _DetachNodeSync(IGLogicNode inLogicNode)
        {
            bool ret = false;

            if (inLogicNode == null)
            {
                //请替换成你的log函数//
                Log.LMN.LogUtil.Error("ObjLogicNode._DetachNodeSync: inLogicNode is null");
                return ret;
            }

            //快速找出节点位置//
            int index = PrioritySortUtil.GetDecSeqArrayFirstIndex<GLogicNodeWrapper>(inLogicNode.LogicNodePriority, mSubNodes);
            int indexToRemove = -1;
            for (int i = index; i < mSubNodes.Count && mSubNodes[i].PriorityVal == inLogicNode.LogicNodePriority; i++)
            {
                GLogicNodeWrapper wrapper = mSubNodes[i];
                if (wrapper.mLogicNode == inLogicNode)
                {
                    indexToRemove = i;
                    break;
                }
            }

            if (indexToRemove != -1)
            {
                mSubNodes.RemoveAt(indexToRemove);

                //如果节点有名字，那注销//
                if (inLogicNode.NodeName != 0)
                {
                    mNameAndNodeMap.Remove(inLogicNode.NodeName);
                }

                inLogicNode.NodeStatus = AttachableStatus.Detached;
                
                try
                {
                    //回调PreDetach接口//
                    inLogicNode.OnPreDetach(this);
                    //回调Detach接口//
                    inLogicNode.OnDetached(this);
                }
                catch (Exception e)
                {
					//请替换成你的log函数//
                    Log.LMN.LogUtil.Error($"ObjLogicNode._DetachAllSubNodesSync: Exception: {e.Message}");
                }

                inLogicNode.ParentNode = null;

                ret = true;
            }

            return ret;
        }

        private bool _DetachNodeAsync(IGLogicNode inLogicNode)
        {
            bool ret = false;

            if (inLogicNode == null)
            {
                //请替换成你的log函数//
                Log.LMN.LogUtil.Error("ObjLogicNode._DetachNodeAsync: inLogicNode is null");
                return ret;
            }

            //快速找出节点位置//
            int index = PrioritySortUtil.GetDecSeqArrayFirstIndex<GLogicNodeWrapper>(inLogicNode.LogicNodePriority, mSubNodes);
            for (int i = index; i < mSubNodes.Count && mSubNodes[i].PriorityVal == inLogicNode.LogicNodePriority; i++)
            {
                GLogicNodeWrapper wrapper = mSubNodes[i];
                if (wrapper.mLogicNode == inLogicNode)
                {
                    wrapper.mLogicNode.NodeStatus = AttachableStatus.Detaching;
                    break;
                }
            }

            //置脏，导致这棵子树需要进入更新流程//
            IsLogicTreeNeedsUpdate = true;
            IGLogicNode parent = ParentNode;
            while (parent != null && !parent.IsLogicTreeNeedsUpdate)
            {
                parent.IsLogicTreeNeedsUpdate = true;
                parent = parent.ParentNode;
            }

            LogicNodeUpdateOperation updateOp = new LogicNodeUpdateOperation();
            updateOp.mIsAddOrRemove = false;
            updateOp.mLogicNode = inLogicNode;
            mAsyncNodeOps.Add(updateOp);

            ret = true;
            return ret;
        }

        /// <summary>
        /// 1.摘除本节点下的所有节点以及它们的子节点（递归）
        /// 2.非线程安全
        /// 3.会在以下方法中生成异步操作OnAttached/OnDetached/OnUpdate, 参见IsNodeTreeLocked
        /// </summary>
        /// <returns></returns>
        public bool DetachAllSubNodes()
        {
            bool ret = false;

            if (IsNodeTreeLocked)
            {
                ret = _DetachAllSubNodesAsync();
            }
            else
            {
                ret = _DetachAllSubNodesSync();
            }

            return ret;
        }

        private bool _DetachAllSubNodesSync()
        {
            bool ret = false;

            for (int i = 0; i < mSubNodes.Count; i++)
            {
                GLogicNodeWrapper wrapper = mSubNodes[i];

                //如果节点有名字，那注销//
                if (wrapper.mLogicNode.NodeName != 0)
                {
                    mNameAndNodeMap.Remove(wrapper.mLogicNode.NodeName);
                }

                //标注自己的状态是Detach//
                wrapper.mLogicNode.NodeStatus = AttachableStatus.Detached;

                //递归进入Detach所有的子节点//
                wrapper.mLogicNode.DetachAllSubNodes();

                try
                {
                    //回调PreDetach接口//
                    wrapper.mLogicNode.OnPreDetach(this);
                    //回调Detach接口//
                    wrapper.mLogicNode.OnDetached(this);
                }
                catch (Exception e)
                {
                    //请替换成你的log函数//
                    Log.LMN.LogUtil.Error("ObjLogicNode._DetachAllSubNodesSync: Exception: {e.Message}");
                }
                
                wrapper.mLogicNode.ParentNode = null;
            }

            mSubNodes.Clear();

            ret = true;
            return ret;
        }

        private bool _DetachAllSubNodesAsync()
        {
            bool ret = false;

            for (int i = 0; i < mSubNodes.Count; i++)
            {
                GLogicNodeWrapper wrapper = mSubNodes[i];
                wrapper.mLogicNode.NodeStatus = AttachableStatus.Detaching;
            }

            //置脏，导致这棵子树需要进入更新流程//
            IsLogicTreeNeedsUpdate = true;
            IGLogicNode parent = ParentNode;
            while (parent != null && !parent.IsLogicTreeNeedsUpdate)
            {
                parent.IsLogicTreeNeedsUpdate = true;
                parent = parent.ParentNode;
            }

            LogicNodeUpdateOperation updateOp = new LogicNodeUpdateOperation();
            updateOp.mIsRemoveAll = true;
            updateOp.mIsAddOrRemove = false;
            mAsyncNodeOps.Add(updateOp);

            return ret;
        }

        /// <summary>
        /// 在挂入逻辑树的时候进行回调
        /// </summary>
        /// <param name="inLogicNode"></param>
        public virtual void OnAttached(IGLogicNode inLogicNode)
        {
        }

        /// <summary>
        /// 在将要从逻辑树摘除时回调，OnDetached 之前
        /// </summary>
        /// <param name="inLogicNode"></param>
        public virtual void OnPreDetach(IGLogicNode inLogicNode)
        {
            //这里清空没有来得及发出去的消息//
            if (ThreadSafeLock != null)
            {
                lock (ThreadSafeLock)
                {
                    foreach (IGEvent evt in mEventQueueFrameOdd)
                    {
                        if (evt.IsAutoRecycle)
                        {
                            evt.IsReadyToRecycle = true;
                        }
                    }
                    mEventQueueFrameOdd.Clear();

                    foreach (IGEvent evt in mEventQueueFrameEven)
                    {
                        if (evt.IsAutoRecycle)
                        {
                            evt.IsReadyToRecycle = true;
                        }
                    }
                    mEventQueueFrameEven.Clear();
                }
            }
            else
            {
                foreach (IGEvent evt in mEventQueueFrameOdd)
                {
                    if (evt.IsAutoRecycle)
                    {
                        evt.IsReadyToRecycle = true;
                    }
                }
                mEventQueueFrameOdd.Clear();

                foreach (IGEvent evt in mEventQueueFrameEven)
                {
                    if (evt.IsAutoRecycle)
                    {
                        evt.IsReadyToRecycle = true;
                    }
                }
                mEventQueueFrameEven.Clear();
            }
        }

        /// <summary>
        /// 在从逻辑树摘除的时候进行回调
        /// </summary>
        /// <param name="inLogicNode"></param>
        public virtual void OnDetached(IGLogicNode inLogicNode)
        {

        }

        #endregion

        #region 消息监听器的挂载与摘除, 消息发送

        /// <summary>
        /// 记录对消息监听器的异步处理操作
        /// </summary>
        public struct EventListenerUpdateOperation
        {
            /// <summary>
            /// 目标消息监听器
            /// </summary>
            public IGEventListener mListener;

            /// <summary>
            /// 添加还是删除
            /// </summary>
            public bool mIsAddOrRemove;
        }

        /// <summary>
        /// 目前将会被执行的异步消息监听器修改操作
        /// </summary>
        private Dictionary<int, List<EventListenerUpdateOperation>> mAsyncListenerOps = new Dictionary<int, List<EventListenerUpdateOperation>>(0);

        /// <summary>
        /// 用于保存所有消息ID对应消息监听器列表的信息
        /// </summary>
        protected Dictionary<int, List<GEventListenerWrapper>> mEventListenerMap = new Dictionary<int, List<GEventListenerWrapper>>(0);

        /// <summary>
        /// 1.向本逻辑节点挂入一个消息监听器，并开始在该节点上监听inEventKey消息
        /// 2.会在以下方法中生成异步操作DispatchEvent, 参见IsListenerTreeLocked
        /// 3.消息监听器的在逻辑节点中是以弱引用存在的，你必须自己管理它的生命周期
        /// </summary>
        /// <param name="inEventKey"></param>
        /// <param name="inListener"></param>
        /// <returns></returns>
        public virtual bool AttachListener(int inEventKey, IGEventListener inListener)
        {
            bool ret = false;

            if (inListener == null)
            {
                //请替换成你的log函数//
                Log.LMN.LogUtil.Error("ObjLogicNode.AttachListener: inListener is null");
                return ret;
            }

            List<GEventListenerWrapper> eventListenerList = null;
            if (!mEventListenerMap.TryGetValue(inEventKey, out eventListenerList))
            {
                eventListenerList = new List<GEventListenerWrapper>();
                mEventListenerMap.Add(inEventKey, eventListenerList);
            }

            if (mLockedEventIds.Contains(inEventKey))
            {
                ret = _AttachListenerAsync(inEventKey, inListener);
            }
            else
            {
                ret = _AttachListenerSync(eventListenerList, inEventKey, inListener);
            }

            return ret;
        }

        private bool _AttachListenerSync(List<GEventListenerWrapper> inEventListenerList, int inEventKey, IGEventListener inListener)
        {
            bool ret = false;

#if DEBUG_G_LOGIC_NODE

            for (int i = 0; i < inEventListenerList.Count; i++)
            {
                GEventListenerWrapper wrapper = inEventListenerList[i];
                if (wrapper.mEventListenerWeakRef.IsAlive
                    && wrapper.mEventListenerWeakRef.Target == inListener)
                {
                    //请替换成你的log函数//
                    Log.LMN.LogUtil.Error("ObjLogicNode._AttachListenerSync: duplicated -> " + inListener + ", in -> " + this + ", for -> " + inEventKey);
                    return ret;
                }
            }

#endif

            int priority = inListener.GetPriority(inEventKey);
            GEventListenerWrapper listenerWrapper;
            listenerWrapper.mEventListenerWeakRef = new WeakReference(inListener);
            listenerWrapper.mEventListenerPriority = priority;
            listenerWrapper.m_AttachableStatus = AttachableStatus.Attached;

            int index = PrioritySortUtil.GetDecSeqArrayInsertIndex<GEventListenerWrapper>(priority, inEventListenerList);
            inEventListenerList.Insert(index, listenerWrapper);

            ret = true;
            return ret;
        }

        private bool _AttachListenerAsync(int inEventKey, IGEventListener inListener)
        {
            bool ret = false;

            EventListenerUpdateOperation updateOp = new EventListenerUpdateOperation();
            updateOp.mListener = inListener;
            updateOp.mIsAddOrRemove = true;

            List<EventListenerUpdateOperation> asyncOpList = null;
            if (!mAsyncListenerOps.TryGetValue(inEventKey, out asyncOpList))
            {
                asyncOpList = new List<EventListenerUpdateOperation>();
                mAsyncListenerOps.Add(inEventKey, asyncOpList);
            }
            asyncOpList.Add(updateOp);

            ret = true;
            return ret;
        }

        /// <summary>
        /// 1.从本逻辑节点上摘除监听inEventKey的inListener消息监听器
        /// 2.会在以下方法中生成异步操作DispatchEvent, 参见IsListenerTreeLocked
        /// 3.消息监听器的在逻辑节点中是以弱引用存在的，你必须自己管理它的生命周期
        /// </summary>
        /// <param name="inEventKey"></param>
        /// <param name="inListener"></param>
        /// <returns></returns>
        public virtual bool DetachListener(int inEventKey, IGEventListener inListener)
        {
            bool ret = false;

            if (inListener == null)
            {
                //请替换成你的log函数//
                Log.LMN.LogUtil.Error("ObjLogicNode.DetachListener: inListener is null");
                return ret;
            }

            List<GEventListenerWrapper> eventListenerList = null;
            if (!mEventListenerMap.TryGetValue(inEventKey, out eventListenerList))
            {
                //请替换成你的log函数//
                Log.LMN.LogUtil.Error("ObjLogicNode.DetachListener: no event listener for -> {0} on {1}", inEventKey, inListener);
                return ret;
            }

            if (mLockedEventIds.Contains(inEventKey))
            {
                ret = _DetachListenerAsync(eventListenerList, inEventKey, inListener);
            }
            else
            {
                ret = _DetachListenerSync(eventListenerList, inEventKey, inListener);
            }

            return ret;
        }

        private bool _DetachListenerSync(List<GEventListenerWrapper> eventListenerList, int inEventKey, IGEventListener inListener)
        {
            bool ret = false;

            //快速找出需要移除的节点位置,注意这里其实兼顾了同一优先级节点的GC功能//
            int priority = inListener.GetPriority(inEventKey);
            int index = PrioritySortUtil.GetDecSeqArrayFirstIndex<GEventListenerWrapper>(priority, eventListenerList);
            List<GEventListenerWrapper> listenersToRemove = new List<GEventListenerWrapper>();
            for (int i = index; i < eventListenerList.Count && eventListenerList[i].PriorityVal == priority; i++)
            {
                GEventListenerWrapper wrapper = eventListenerList[i];
                object listenerObj = wrapper.mEventListenerWeakRef.Target;
                if (listenerObj == null || listenerObj == inListener)
                {
                    listenersToRemove.Add(wrapper);
                }
            }

            for (int i = 0; i < listenersToRemove.Count; i++)
            {
                eventListenerList.Remove(listenersToRemove[i]);
            }

            ret = listenersToRemove.Count != 0;
            return ret;
        }

        private bool _DetachListenerAsync(List<GEventListenerWrapper> eventListenerList, int inEventKey, IGEventListener inListener)
        {
            bool ret = false;

            //快速找出需要移除的节点位置//
            int priority = inListener.GetPriority(inEventKey);
            int index = PrioritySortUtil.GetDecSeqArrayFirstIndex<GEventListenerWrapper>(priority, eventListenerList);
            for (int i = index; i < eventListenerList.Count && eventListenerList[i].PriorityVal == priority; i++)
            {
                GEventListenerWrapper wrapper = eventListenerList[i];
                object listenerObj = wrapper.mEventListenerWeakRef.Target;
                if (listenerObj == inListener)
                {
                    GEventListenerWrapper newWrapper = new GEventListenerWrapper();
                    newWrapper.mEventListenerWeakRef = wrapper.mEventListenerWeakRef;
                    newWrapper.mEventListenerPriority = wrapper.mEventListenerPriority;
                    newWrapper.m_AttachableStatus = AttachableStatus.Detaching;

                    eventListenerList[i] = newWrapper;
                    ret = true;
                    break;
                }
            }

            if (ret)
            {
                EventListenerUpdateOperation updateOp = new EventListenerUpdateOperation();
                updateOp.mListener = inListener;
                updateOp.mIsAddOrRemove = false;
                
                List<EventListenerUpdateOperation> asyncOpList = null;
                if (!mAsyncListenerOps.TryGetValue(inEventKey, out asyncOpList))
                {
                    asyncOpList = new List<EventListenerUpdateOperation>();
                    mAsyncListenerOps.Add(inEventKey, asyncOpList);
                }
                asyncOpList.Add(updateOp);
            }

            return ret;
        }

        /// <summary>
        /// 发送同步消息，立即完成消息的执行
        /// 不可以跨线程使用
        /// </summary>
        /// <returns><c>true</c>, if event sync was sent, <c>false</c> otherwise.</returns>
        /// <param name="inEvt">In evt.</param>
        public virtual bool SendEventSync(IGEvent inEvt)
        {
            bool ret = false;

            if (inEvt == null)
            {
                return ret;
            }

            if (inEvt.IsAutoRecycle && inEvt.IsReadyToRecycle)
            {
                //请替换成你的log函数//
                Log.LMN.LogUtil.Error("ObjLogicNode.SendEventSync: sending recycled event, key: {0} on: {1}", inEvt.EventKey, this);
                return ret;
            }

            if (NodeStatus != AttachableStatus.Attached)
            {
                //这种时候发送消息是不成功的，这个消息要准备被释放//
                if (inEvt.IsAutoRecycle)
                {
                    inEvt.IsReadyToRecycle = true;
                }

                return ret;
            }

            if (!IsNodeActive)
            {
                return ret;
            }

            DispatchAnEvent(inEvt);
            TryToRecycleThisEvent(inEvt);


            ret = true;
            return ret;
        }

        /// <summary>
        /// 发送异步消息，会在下一帧执行
        /// 在节点是线程安全节点的时候，可以跨线程使用
        /// </summary>
        /// <returns><c>true</c>, if event async was sent, <c>false</c> otherwise.</returns>
        /// <param name="inEvt">In evt.</param>
        /// <param name="inCacheUpWhenInactive">If set to <c>true</c> in cache up when inactive.</param>
        public virtual bool SendEventAsync(IGEvent inEvt, bool inCacheUpWhenInactive = false)
        {
            bool ret = false;

            if (inEvt == null)
            {
                return ret;
            }

            if (inEvt.IsAutoRecycle && inEvt.IsReadyToRecycle)
            {
                //请替换成你的log函数//
                Log.LMN.LogUtil.Error("ObjLogicNode.SendEventAsync: sending recycled event, key: {0} on: {1}", inEvt.EventKey, this);
                return ret;
            }

            if (NodeStatus != AttachableStatus.Attached)
            {
                //这种时候发送消息是不成功的，这个消息要准备被释放//
                if (inEvt.IsAutoRecycle)
                {
                    inEvt.IsReadyToRecycle = true;
                }

                return ret;
            }

            if (!IsNodeActive && !inCacheUpWhenInactive)
            {
                return ret;
            }

            if (ThreadSafeLock != null)
            {
                lock (ThreadSafeLock)
                {
                    mEventQueueForNextFrame.Add(inEvt);
                }
            }
            else
            {
                mEventQueueForNextFrame.Add(inEvt);
            }

            ret = true;
            return ret;
        }

        #endregion

        #region 帧循环，消息广播，树结构更新

        /// <summary>
        /// 消息队列双buffer, 奇数帧
        /// </summary>
        protected readonly List<IGEvent> mEventQueueFrameOdd = new List<IGEvent>();

        /// <summary>
        /// 消息队列双buffer, 偶数帧
        /// </summary>
        protected readonly List<IGEvent> mEventQueueFrameEven = new List<IGEvent>();

        /// <summary>
        /// 当前帧的用于缓存消息的消息队列
        /// </summary>
        protected List<IGEvent> mEventQueueForNextFrame = null;

        /// <summary>
        /// 当前帧的用于执行的消息队列
        /// </summary>
        protected List<IGEvent> mEventQueueForThisFrame = null;

        /// <summary>
        /// 更新Logic节点树
        /// 导致 IsNodeTreeLocked 为真
        /// </summary>
        public void UpdateLogicTree()
        {
            if (!IsNodeActive)
            {
                return;
            }

            //对树结构上锁, 注意这里不是递归进，因为这个方法本身在递归进//
            mIsNodeTreeLocked = true;

            if(IsLogicTreeNeedsUpdate)
            {
                IsLogicTreeNeedsUpdate = false;

                //注意，这个Update是深度优先的，防止子节点的异步操作被跳过//
                for (int i = 0; i < mSubNodes.Count; i++)
                {
                    mSubNodes[i].mLogicNode.UpdateLogicTree();
                }

                for (int i = 0; i < mAsyncNodeOps.Count; i++)
                {
                    LogicNodeUpdateOperation op = mAsyncNodeOps[i];
                    if (op.mIsAddOrRemove)
                    {
                        _AttachNodeSync(op.mLogicNode);
                    }
                    else
                    {
                        if (op.mIsRemoveAll)
                        {
                            _DetachAllSubNodesSync();
                        }
                        else
                        {
                            _DetachNodeSync(op.mLogicNode);
                        }
                    }
                }

                mAsyncNodeOps.Clear();
            }

            //对树结构解锁, 注意这里不是递归进，因为这个方法本身在递归进//
            mIsNodeTreeLocked = false;
        }

        /// <summary>
        /// 引发消息逻辑执行
        /// 导致 IsNodeTreeLocked, IsListenerTreeLocked 为真
        /// </summary>
        public void UpdateEvents()
        {
            if (!IsNodeActive)
            {
                return;
            }

            //对树结构上锁, 注意这里是递归进的//
            IsNodeTreeLocked = true;

            //开始广播消息//
            DispatchEvents();

            //对树结构解锁, 注意这里是递归进的//
            IsNodeTreeLocked = false;
        }

        /// <summary>
        /// 广播消息
        /// </summary>
        public virtual void DispatchEvents()
        {
            //先把在我身上抛的消息dispatch掉//
            for (int i = 0; i < mEventQueueForThisFrame.Count; i++)
            {
                IGEvent evt = mEventQueueForThisFrame[i];
                DispatchAnEvent(evt);
                TryToRecycleThisEvent(evt);
            }

            mEventQueueForThisFrame.Clear();

            if (AsEventDispatcher)
            {
                for (int i = 0; i < mSubNodes.Count; i++)
                {
                    GLogicNodeWrapper wrapper = mSubNodes[i];
                    if (wrapper.mLogicNode.NodeStatus != AttachableStatus.Detaching 
						&& wrapper.mLogicNode.IsNodeActive)
                    {
                        wrapper.mLogicNode.DispatchEvents();
                    }
                }
            }
        }

        /// <summary>
        /// 尝试去释放一个消息
        /// </summary>
        /// <param name="inEvent"></param>
        /// <returns></returns>
        public virtual void TryToRecycleThisEvent(IGEvent inEvent)
        {
            //如果是自动释放类消息，那这里标记它可以释放了//
            if (inEvent.IsAutoRecycle)
            {
                inEvent.IsReadyToRecycle = true;
            }
        }

        /// <summary>
        /// 广播一个消息
        /// </summary>
        /// <param name="inEvent">Evt.</param>
        public virtual bool DispatchAnEvent(IGEvent inEvent)
        {
            if (AsEventDispatcher)
            {
                if (TriggerEvent(inEvent))
                {
                    return true;
                }

                for (int i = 0; i < mSubNodes.Count; i++)
                {
                    GLogicNodeWrapper wrapper = mSubNodes[i];
                    if (wrapper.mLogicNode.NodeStatus != AttachableStatus.Detaching && wrapper.mLogicNode.IsNodeActive)
                    {
                        if (wrapper.mLogicNode.DispatchAnEvent(inEvent))
                        {
                            //说明某个消息监听器要求拦截消息，终止了消息的继续广播//
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// 引发消息处理
        /// </summary>
        /// <param name="inEvent">Evt.</param>
        public bool TriggerEvent(IGEvent inEvent)
        {
            bool ret = false;
            
            if (mEventListenerMap.TryGetValue(inEvent.EventKey, out List<GEventListenerWrapper> listenerList))
            {
                mLockedEventIds.Add(inEvent.EventKey);

                //1.正常的HandleEvent，如果产生了针对当前listenerList的增删操作的话，会进入mAsyncListenerOps//
                for (int i = 0; i < listenerList.Count; i++)
                {
                    GEventListenerWrapper wrapper = listenerList[i];
                    IGEventListener listener = wrapper.mEventListenerWeakRef.Target as IGEventListener;
                    try
                    {
                        //在这个HandleEvent中如果产生了针对本当前listenerList的增删操作的话，会进入mAsyncListenerOps//
                        if (listener != null
                            && wrapper.m_AttachableStatus != AttachableStatus.Detaching
                            && listener.HandleEvent(inEvent))
                        {
                            //说明这个监听器要求拦截消息，终止消息的继续广播//
                            ret = true;
                            break;
                        }
                    }
                    catch (Exception e)
                    {
                        //请替换成你的log函数//
                        Log.LMN.LogUtil.Error("ObjLogicNode.TriggerEvent: listener -> {0}, exception -> {1}", listener, e);
                    }
                }

                if (mAsyncListenerOps.TryGetValue(inEvent.EventKey, out List<EventListenerUpdateOperation> asyncOps))
                {
                    mAsyncListenerOps.Remove(inEvent.EventKey);
                    if (asyncOps != null && asyncOps.Count != 0)
                    {
                        for (int i = 0; i < asyncOps.Count; i++)
                        {
                            EventListenerUpdateOperation op = asyncOps[i];
                            if (op.mIsAddOrRemove)
                            {
                                _AttachListenerSync(listenerList, inEvent.EventKey, op.mListener);
                            }
                            else
                            {
                                _DetachListenerSync(listenerList, inEvent.EventKey, op.mListener);
                            }
                        }
                    }
                }

                mLockedEventIds.Remove(inEvent.EventKey);
            }

            return ret;
        }

        /// <summary>
        /// 用于提供双buffer缓冲规范
        /// </summary>
        public void SwapEventQueues()
        {
            if (IsNodeActive)
            {
                //交换消息队列和listener操作队列//
                if (ThreadSafeLock != null)
                {
                    lock (ThreadSafeLock)
                    {
                        List<IGEvent> tempEventList = mEventQueueForNextFrame;
                        mEventQueueForNextFrame = mEventQueueForThisFrame;
                        mEventQueueForThisFrame = tempEventList;
                    }
                }
                else
                {
                    List<IGEvent> tempEventList = mEventQueueForNextFrame;
                    mEventQueueForNextFrame = mEventQueueForThisFrame;
                    mEventQueueForThisFrame = tempEventList;
                }

                for (int i = 0; i < mSubNodes.Count; i++)
                {
                    GLogicNodeWrapper wrapper = mSubNodes[i];
                    if (wrapper.mLogicNode.NodeStatus != AttachableStatus.Detaching && wrapper.mLogicNode.IsNodeActive)
                    {
                        wrapper.mLogicNode.SwapEventQueues();
                    }
                }
            }
        }

        /// <summary>
        /// 帧循环
        /// </summary>
        /// <param name="inDeltaTime"></param>
        public virtual void OnLogicNodeUpdate(float inDeltaTime)
        {
            if (IsNodeActive)
            {
                for (int i = 0; i < mSubNodes.Count; i++)
                {
                    GLogicNodeWrapper wrapper = mSubNodes[i];
                    if (wrapper.mLogicNode.NodeStatus != AttachableStatus.Detaching && wrapper.mLogicNode.IsNodeActive)
                    {
                        wrapper.mLogicNode.OnLogicNodeUpdate(inDeltaTime);
                    }
                }
            }
        }

        #endregion

        #region GC

        /// <summary>
        /// 如果wearkRef是null就可以准备gc掉了
        /// </summary>
        /// <param name="wrapper"></param>
        /// <returns></returns>
        private static bool IfWrapperWeakRefIsNull(GEventListenerWrapper wrapper)
        {
            return wrapper.mEventListenerWeakRef.Target == null;
        }

        /// <summary>
        /// 创建静态delegate 防止gc
        /// </summary>
        private static Predicate<GEventListenerWrapper> mEventListenerWrapperRemoveDelegate = IfWrapperWeakRefIsNull;

        /// <summary>
        /// 用于实现对消息监听器Dic等 的GC
        /// </summary>
        public void GC()
        {
            Dictionary<int, List<GEventListenerWrapper>>.Enumerator iter = mEventListenerMap.GetEnumerator();
            while (iter.MoveNext())
            {
                List<GEventListenerWrapper> wrapperList = iter.Current.Value;
                wrapperList.RemoveAll(mEventListenerWrapperRemoveDelegate);
            }

            for (int i = 0; i < mSubNodes.Count; i++)
            {
                GLogicNodeWrapper wrapper = mSubNodes[i];
                if (wrapper.mLogicNode.NodeStatus != AttachableStatus.Detaching)
                {
                    wrapper.mLogicNode.GC();
                }
            }
        }

        #endregion

    }
}
