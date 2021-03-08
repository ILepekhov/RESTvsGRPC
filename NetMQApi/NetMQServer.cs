using Google.Protobuf;
using ModelLibrary.Data;
using ModelLibrary.GRPC;
using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NetMQApi
{
    public sealed class NetMQServer
    {
        #region Const

        private const string DefaultPublicationAddress = "tcp://*:5555";

        #endregion

        #region Fields

        private CancellationTokenSource _cancellationTokenSource;

        #endregion

        #region Constructor

        public NetMQServer()
        {

        }

        #endregion

        #region Methods

        public void Start()
        {
            _cancellationTokenSource = new CancellationTokenSource();

            StartLoop(_cancellationTokenSource.Token);
        }

        public void Stop()
        {
            if (_cancellationTokenSource != null && !_cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource.Cancel();
            }

            _cancellationTokenSource = null;
        }

        #endregion

        #region Helpers

        private void StartLoop(CancellationToken cancellationToken)
        {
            Task.Run(() =>
            {
                var timeout = TimeSpan.FromMilliseconds(10);

                using (var server = new ResponseSocket())
                {
                    server.Bind(DefaultPublicationAddress);

                    while (!cancellationToken.IsCancellationRequested)
                    {
                        var bytes = new List<byte[]>();

                        if (!server.TryReceiveMultipartBytes(timeout, ref bytes)) continue;

                        var requestType = ParseRequestType(bytes[0]);

                        SendResponse(server, requestType);
                    }
                }
            },
            cancellationToken);
        }

        private RequestType ParseRequestType(byte[] requestBytes)
        {
            if (requestBytes is null || requestBytes.Length == 0)
                return RequestType.Unspecified;

            var requestType = RequestType.Unspecified;

            try
            {
                var intRequestType = BitConverter.ToInt32(requestBytes, 0);

                requestType = (RequestType)intRequestType;
            }
            catch (Exception e)
            {
                Console.Error.Write("ParseRequestType().Task: ошибка разбора типа запроса. " + e.Message);
            }

            return requestType;
        }

        private void SendResponse(ResponseSocket server, RequestType requestType)
        {
            /*
             * Идея структуры ответа такова:
             * - первым кадром возвращается RequestType, полученный в запросе;
             * - вторым и последующими кадрами - полезная нагрузка;
             */

            switch (requestType)
            {
                case RequestType.GetSmallPayload:
                    server.SendMultipartMessage(GetSmallPayloadMessage());
                    break;
                case RequestType.GetLargePayload:
                    server.SendMultipartMessage(GetLargePayloadMessage());
                    break;
                case RequestType.PostLargePayload:
                    server.SendMultipartMessage(PostLargePayloadMessage());
                    break;
                case RequestType.GetLargePayloadMultipart:
                    server.SendMultipartMessage(GetLargePayloadMessageMultipart());
                    break;
                case RequestType.PostLargePayloadMultipart:
                    server.SendMultipartMessage(PostLargePayloadMessageMultipart());
                    break;
                case RequestType.Unspecified:
                default:
                    server.SendMultipartMessage(GetUnspecifiedRequestMessage());
                    break;
            }
        }

        private NetMQMessage GetSmallPayloadMessage()
        {
            var message = new NetMQMessage();

            message.Append(BitConverter.GetBytes((int)RequestType.GetSmallPayload));
            message.Append("API Version 1.0");

            return message;
        }

        private NetMQMessage GetLargePayloadMessage()
        {
            var message = new NetMQMessage();

            message.Append(BitConverter.GetBytes((int)RequestType.GetLargePayload));

            message.Append(MeteoriteLandingData.GrpcMeteoriteLandingList.ToByteArray());

            return message;
        }

        private NetMQMessage PostLargePayloadMessage()
        {
            var message = new NetMQMessage();

            message.Append(BitConverter.GetBytes((int)RequestType.PostLargePayload));

            message.Append(new StatusResponse { Status = "SUCCESS" }.ToByteArray());

            return message;
        }

        private NetMQMessage GetUnspecifiedRequestMessage()
        {
            var message = new NetMQMessage();

            message.Append((int)RequestType.Unspecified);

            return message;
        }

        private NetMQMessage GetLargePayloadMessageMultipart()
        {
            var message = new NetMQMessage();

            message.Append(BitConverter.GetBytes((int)RequestType.GetLargePayloadMultipart));

            foreach (var item in MeteoriteLandingData.GrpcMeteoriteLandings)
            {
                message.Append(item.ToByteArray());
            }

            return message;
        }

        private NetMQMessage PostLargePayloadMessageMultipart()
        {
            var message = new NetMQMessage();

            message.Append(BitConverter.GetBytes((int)RequestType.PostLargePayloadMultipart));

            message.Append(new StatusResponse { Status = "SUCCESS" }.ToByteArray());

            return message;
        }

        #endregion
    }
}
