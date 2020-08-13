﻿using Microsoft.OData;
using Microsoft.OData.Client;
using OdataToEntity.Test.WcfService;
using System;
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;

namespace OdataToEntity.Test.WcfClient
{
    public sealed class WcfClientInterceptor : ClientInterceptor, IDisposable
    {
        private readonly ChannelFactory<IOdataWcf> _channelFactory;

        public WcfClientInterceptor(Binding binding, String remoteAddress)
        {
            _channelFactory = new ChannelFactory<IOdataWcf>(binding, new EndpointAddress(remoteAddress));
        }

        public void Dispose()
        {
            if (_channelFactory != null)
                _channelFactory.Close();
        }
        public async Task<OdataWcfQuery> Get(String query)
        {
            IOdataWcf client = null;
            try
            {
                client = _channelFactory.CreateChannel();
                return await client.Get(new OdataWcfQuery()
                {
                    Content = new MemoryStream(Encoding.UTF8.GetBytes(query)),
                    ContentType = OeRequestHeaders.JsonDefault.ToString()
                });
            }
            finally
            {
                if (client != null)
                {
                    var clientChannel = (IClientChannel)client;
                    clientChannel.Close();
                }
            }
        }
        protected internal async override Task<OdataWcfQuery> OnGetResponse(HttpWebRequestMessage requestMessage, Stream requestStream)
        {
            OdataWcfQuery response;
            IOdataWcf client = null;
            try
            {
                client = _channelFactory.CreateChannel();
                if (requestStream == null || requestStream.Length == 0)
                {
                    String accept = requestMessage.GetHeader("Accept");
                    String prefer = requestMessage.GetHeader("Prefer");
                    var query = new MemoryStream(Encoding.UTF8.GetBytes(requestMessage.Url.PathAndQuery));
                    response = await client.Get(new OdataWcfQuery() { Content = query, ContentType = accept, Prefer = prefer });
                }
                else
                {
                    String contentType = requestMessage.GetHeader(ODataConstants.ContentTypeHeader);
                    requestStream.Position = 0;
                    response = await client.Post(new OdataWcfQuery() { Content = requestStream, ContentType = contentType });
                }
            }
            finally
            {
                if (client != null)
                {
                    var clientChannel = (IClientChannel)client;
                    clientChannel.Close();
                }
            }
            return response;
        }
    }
}
