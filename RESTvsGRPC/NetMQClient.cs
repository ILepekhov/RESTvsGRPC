using Google.Protobuf;
using ModelLibrary.GRPC;
using NetMQ;
using NetMQ.Sockets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RESTvsGRPC
{
    public sealed class NetMQClient : IDisposable
    {
        #region Const

        private const string ServerAddress = "tcp://localhost:5555";

        #endregion

        #region Fields

        private RequestSocket _client;

        #endregion

        #region Constructor

        public NetMQClient()
        {
            _client = new RequestSocket();
            _client.Connect(ServerAddress);
        }

        #endregion

        #region Methods

        public async Task<string> GetSmallPayloadAsync()
        {
            return await Task.Run(() =>
            {
                _client.SendFrame(GetRequestTypeBytes(RequestType.GetSmallPayload));

                var response = _client.ReceiveMultipartMessage();

                if (response != null && response.FrameCount == 2)
                {
                    var requestType = ReadRequestType(response[0]);

                    if (requestType == RequestType.GetSmallPayload)
                    {
                        return response[1].ConvertToString();
                    }
                }

                return string.Empty;
            });
        }

        public async Task<List<MeteoriteLanding>> GetLargePayloadAsync()
        {
            return await Task.Run(() =>
            {
                var items = new List<MeteoriteLanding>();

                _client.SendFrame(GetRequestTypeBytes(RequestType.GetLargePayload));

                var response = _client.ReceiveMultipartMessage();

                if (response != null && response.FrameCount == 2)
                {
                    var requestType = ReadRequestType(response[0]);

                    if (requestType == RequestType.GetLargePayload)
                    {
                        var meteoriteLandingList = MeteoriteLandingList.Parser.ParseFrom(response[1].Buffer);

                        return meteoriteLandingList.MeteoriteLandings.ToList();
                    }
                }

                return items;
            });
        }

        public async Task<string> PostLargePayloadAsync(MeteoriteLandingList meteoriteLandings)
        {
            return await Task.Run(() =>
            {
                _client.SendMultipartMessage(GetLargePayloadMessage(meteoriteLandings));

                var response = _client.ReceiveMultipartMessage();

                if (response != null && response.FrameCount == 2)
                {
                    var requestType = ReadRequestType(response[0]);

                    if (requestType == RequestType.PostLargePayload)
                    {
                        var statusResponse = StatusResponse.Parser.ParseFrom(response[1].Buffer);

                        return statusResponse.Status;
                    }
                }

                return string.Empty;
            });
        }

        public async Task<List<MeteoriteLanding>> GetLargePayloadMultipartAsync()
        {
            return await Task.Run(() =>
            {
                var items = new List<MeteoriteLanding>();

                _client.SendFrame(GetRequestTypeBytes(RequestType.GetLargePayloadMultipart));

                var response = _client.ReceiveMultipartMessage();

                if (response != null && response.FrameCount > 1)
                {
                    var requestType = ReadRequestType(response[0]);

                    if (requestType == RequestType.GetLargePayloadMultipart)
                    {
                        for (int i = 1; i < response.FrameCount; i++)
                        {
                            items.Add(MeteoriteLanding.Parser.ParseFrom(response[i].Buffer));
                        }
                    }
                }

                return items;
            });
        }

        public async Task<string> PostLargePayloadMultipartAsync(List<MeteoriteLanding> grpcMeteoriteLandings)
        {
            return await Task.Run(() =>
            {
                _client.SendMultipartMessage(GetLargePayloadMessageMultipart(grpcMeteoriteLandings));

                var response = _client.ReceiveMultipartMessage();

                if (response != null && response.FrameCount == 2)
                {
                    var requestType = ReadRequestType(response[0]);

                    if (requestType == RequestType.PostLargePayloadMultipart)
                    {
                        var statusResponse = StatusResponse.Parser.ParseFrom(response[1].Buffer);

                        return statusResponse.Status;
                    }
                }

                return string.Empty;
            });
        }

        public void Dispose()
        {
            _client.Dispose();
            _client = null;
        }

        #endregion

        #region Helpers

        private byte[] GetRequestTypeBytes(RequestType requestType)
        {
            return BitConverter.GetBytes((int)requestType);
        }

        private RequestType ReadRequestType(NetMQFrame requestIdentificationFrame)
        {
            if (requestIdentificationFrame is null || requestIdentificationFrame.IsEmpty)
            {
                return RequestType.Unspecified;
            }

            var requestType = RequestType.Unspecified;

            try
            {
                var intRequestType = BitConverter.ToInt32(requestIdentificationFrame.Buffer, 0);

                requestType = (RequestType)intRequestType;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine("ReadRequestType(): неизвестная ошибка при разборе типа запроса. " + e.Message);
            }

            return requestType;
        }

        private NetMQMessage GetLargePayloadMessage(MeteoriteLandingList meteoriteLandings)
        {
            var message = new NetMQMessage();

            message.Append(BitConverter.GetBytes((int)RequestType.PostLargePayload));

            message.Append(meteoriteLandings.ToByteArray());

            return message;
        }

        private NetMQMessage GetLargePayloadMessageMultipart(List<MeteoriteLanding> grpcMeteoriteLandings)
        {
            var message = new NetMQMessage();

            message.Append(BitConverter.GetBytes((int)RequestType.PostLargePayloadMultipart));

            foreach (var item in grpcMeteoriteLandings)
            {
                message.Append(item.ToByteArray());
            }

            return message;
        }

        #endregion
    }
}
