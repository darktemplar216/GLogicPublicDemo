
/****************************************************************** 
* Copyright (C): GPL 
* Create By: taowei@tencent.com 61197311@qq.com
* Description: 使用请保留版权头，其它没什么要求 
******************************************************************/

using GLogic;
using GLogic.L;
using GLogic.LMN;
using GLogic.M;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;

using DataOceanModule.LMN;
using UnityEngine;
using Event.GameSys.LM;
using ObjPoolModule.LMN;
using BridgeNodes.LMN;
using GLogic.N;
using Event.GameSys.M;

namespace EntranceModule.M
{
    public class EntranceForDemo3 : MonoBehaviour
    {
        /// <summary>
        /// 可供选择的线程模式
        /// 默认用三线程
        /// </summary>
        public eDemoThreadMode SelectThreadMode = eDemoThreadMode.L_M_N;

        public void Awake()
        {
            LMDataOcean.mCurDemo = eDemoType.Demo3;

            //这里确定了这次游戏会工作在什么线程模式下//
            LMDataOcean.mCurThreadMode = SelectThreadMode;

            Application.targetFrameRate = 60;

            //根节点不销毁//
            DontDestroyOnLoad(this.gameObject);

            //启动主线程//
            InitMainThread();
            //启动逻辑线程//
            InitLogicThread();
            //启动网络线程//
            InitNetThread();
        }

        public void OnDestroy()
        {
            //停止网络线程//
            ShutdownNetThread();
            //停止逻辑线程//
            ShutdownLogicThread();
            //停止主线程//
            ShutdownMainThread();
        }

        public void Start()
        {
            /*
             * ---------------------------------
             * 注意Demo3我们没有在这里启动
             * --------------------------------
             */
        }

        /// <summary>
        /// Unity的退出回调
        /// </summary>
        public void OnApplicationQuit()
        {

        }

        /// <summary>
        /// unity的暂停回调
        /// </summary>
        /// <param name="inIsPause"></param>
        private void OnApplicationPause(bool inIsPause)
        {

        }

        #region MainThread

        /// <summary>
        /// 启动主线程
        /// </summary>
        private void InitMainThread()
        {
            //启动主线程逻辑核//
            gameObject.AddComponent<MLogicCore>();
        }

        /// <summary>
        /// 停止主线程
        /// </summary>
        private void ShutdownMainThread()
        {
            UnityEngine.Debug.LogWarning("Entrance.ShutdownMainThread: finished");
        }

        #endregion

        #region LogicThread

        /// <summary>
        /// 逻辑线程
        /// </summary>
        DemoGameThread mLogicThread = null;

        /// <summary>
        /// 启动逻辑线程
        /// </summary>
        private void InitLogicThread()
        {
            switch (LMDataOcean.mCurThreadMode)
            {
                case eDemoThreadMode.LMN:
                case eDemoThreadMode.LM_N:
                    {
                        if (MLogicCore.IsValid) MLogicCore.Instance.AttachNode(new LLogicCore());
                    }
                    break;
                case eDemoThreadMode.L_M_N:
                default:
                    {
                        LLogicCore lLogicCore = new LLogicCore();
                        mLogicThread = new DemoGameThread();
                        mLogicThread.Start(lLogicCore);
                    }
                    break;
            }
        }

        /// <summary>
        /// 停止逻辑线程
        /// </summary>
        private void ShutdownLogicThread()
        {
            switch (LMDataOcean.mCurThreadMode)
            {
                case eDemoThreadMode.LMN:
                case eDemoThreadMode.LM_N:
                    {
                        if (MLogicCore.IsValid) MLogicCore.Instance.DetachNode(LLogicCore.Instance);
                    }
                    break;
                case eDemoThreadMode.L_M_N:
                default:
                    {
                        if (mLogicThread != null)
                        {
                            mLogicThread.Stop();
                        }
                    }
                    break;
            }

            mLogicThread = null;

            UnityEngine.Debug.LogWarning("Entrance.ShutdownLogicThread: finished");
        }

        #endregion

        #region NetThread

        /// <summary>
        /// 网络线程
        /// </summary>
        DemoGameThread mNetThread = null;

        /// <summary>
        /// 启动网络线程
        /// </summary>
        private void InitNetThread()
        {
            switch (LMDataOcean.mCurThreadMode)
            {
                case eDemoThreadMode.LMN:
                    {
                        if (MLogicCore.IsValid) MLogicCore.Instance.AttachNode(new NLogicCore());
                    }
                    break;
                case eDemoThreadMode.LM_N:
                case eDemoThreadMode.L_M_N:
                default:
                    {
                        NLogicCore nLogicCore = new NLogicCore();
                        mNetThread = new DemoGameThread();
                        mNetThread.Start(nLogicCore);
                    }
                    break;
            }
        }

        /// <summary>
        /// 停止网络线程
        /// </summary>
        private void ShutdownNetThread()
        {
            switch (LMDataOcean.mCurThreadMode)
            {
                case eDemoThreadMode.LMN:
                    {
                        if (MLogicCore.IsValid) MLogicCore.Instance.DetachNode(NLogicCore.Instance);
                    }
                    break;
                case eDemoThreadMode.LM_N:
                case eDemoThreadMode.L_M_N:
                default:
                    {
                        if (mNetThread != null)
                        {
                            mNetThread.Stop();
                        }
                    }
                    break;
            }
            mNetThread = null;

            UnityEngine.Debug.LogWarning("Entrance.ShutdownNetThread: finished");
        }

        #endregion

    }



}
