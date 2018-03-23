﻿using HslCommunication.Core.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace HslCommunication.Enthernet
{

    /// <summary>
    /// 同步消息处理服务器，主要用来实现接收客户端信息并进行消息反馈的操作
    /// </summary>
    public class NetSimplifyServer : NetworkServerBase
    {
        #region Constructor


        public NetSimplifyServer()
        {

        }

        #endregion
        
        #region 事件通知块

        /// <summary>
        /// 接收字符串信息的事件
        /// </summary>
        public event Action<AppSession, NetHandle, string> ReceiveStringEvent;
        /// <summary>
        /// 接收字节信息的事件
        /// </summary>
        public event Action<AppSession, NetHandle, byte[]> ReceivedBytesEvent;


        private void OnReceiveStringEvent( AppSession session, int customer, string str )
        {
            ReceiveStringEvent?.Invoke( session, customer, str );
        }

        private void OnReceivedBytesEvent( AppSession session, int customer, byte[] temp )
        {
            ReceivedBytesEvent?.Invoke( session, customer, temp );
        }

        #endregion

        #region 启动停止块

        /// <summary>
        /// 关闭网络的操作
        /// </summary>
        protected override void CloseAction()
        {
            ReceivedBytesEvent = null;
            ReceiveStringEvent = null;
            base.CloseAction( );
        }


        /// <summary>
        /// 向指定的通信对象发送字符串数据
        /// </summary>
        /// <param name="session">通信对象</param>
        /// <param name="customer">用户的指令头</param>
        /// <param name="str">实际发送的字符串数据</param>
        public void SendMessage( AppSession session, int customer, string str )
        {
            SendBytesAsync( session, HslProtocol.CommandBytes( customer, Token, str ) );
        }
        /// <summary>
        /// 向指定的通信对象发送字节数据
        /// </summary>
        /// <param name="session">连接对象</param>
        /// <param name="customer">用户的指令头</param>
        /// <param name="bytes">实际的数据</param>
        public void SendMessage( AppSession session, int customer, byte[] bytes )
        {
            SendBytesAsync( session, HslProtocol.CommandBytes( customer, Token, bytes ) );
        }

        /// <summary>
        /// 处理请求接收连接后的方法
        /// </summary>
        /// <param name="obj"></param>
        protected override void ThreadPoolLogin( object obj )
        {
            if (obj is Socket socket)
            {
                AppSession session = new AppSession( );
                session.WorkSocket = socket;
                try
                {
                    session.IpEndPoint = (System.Net.IPEndPoint)socket.RemoteEndPoint;
                    session.IpAddress = session.IpEndPoint.Address.ToString( );
                }
                catch(Exception ex)
                {
                    LogNet?.WriteException( ToString( ), "Ip信息获取失败", ex );
                }

                LogNet?.WriteDebug( ToString( ), $"客户端 [ {session.IpAddress} ] 上线" );
                ReBeginReceiveHead( session, false );
            }
        }

        /// <summary>
        /// 处理异常的方法
        /// </summary>
        /// <param name="session"></param>
        /// <param name="ex">异常信息</param>
        internal override void SocketReceiveException( AppSession session, Exception ex )
        {
            session.WorkSocket?.Close( );
            LogNet?.WriteDebug( ToString( ), $"客户端 [ {session.IpAddress} ] 异常下线" );
        }

        /// <summary>
        /// 正常下线
        /// </summary>
        /// <param name="session"></param>
        internal override void AppSessionRemoteClose( AppSession session )
        {
            session.WorkSocket?.Close( );
            LogNet?.WriteDebug( ToString( ), $"客户端 [ {session.IpAddress} ] 下线" );
        }

        /// <summary>
        /// 数据处理中心
        /// </summary>
        /// <param name="session">当前的会话</param>
        /// <param name="protocol">协议指令头</param>
        /// <param name="customer">客户端信号</param>
        /// <param name="content">触发的消息内容</param>
        internal override void DataProcessingCenter( AppSession session, int protocol, int customer, byte[] content )
        {
            //接收数据完成，进行事件通知，优先进行解密操作
            if (protocol == HslProtocol.ProtocolUserBytes)
            {
                //字节数据
                OnReceivedBytesEvent( session, customer, content );
            }
            else if (protocol == HslProtocol.ProtocolUserString)
            {
                //字符串数据
                OnReceiveStringEvent( session, customer, Encoding.Unicode.GetString( content ) );
            }
            else
            {
                //数据异常
                session?.WorkSocket?.Close( );
            }
        }

        #endregion

        #region Object Override

        /// <summary>
        /// 获取本对象的字符串表示形式
        /// </summary>
        /// <returns></returns>
        public override string ToString( )
        {
            return "NetSimplifyServer";
        }


        #endregion
    }
}