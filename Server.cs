﻿/*
     Copyright 2012 Kolja Dummann <k.dummann@gmail.com>

   Licensed under the Apache License, Version 2.0 (the "License");
   you may not use this file except in compliance with the License.
   You may obtain a copy of the License at

       http://www.apache.org/licenses/LICENSE-2.0

   Unless required by applicable law or agreed to in writing, software
   distributed under the License is distributed on an "AS IS" BASIS,
   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
   See the License for the specific language governing permissions and
   limitations under the License.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;

namespace SimpleHttpServer
{
    public class Server
    {
        private enum HttpVerb
        {
            Get,
            Post,
            Put,
            Delete
        }

        private HttpListener _listener;
        private string _serverAdress;

        private Dictionary<string,
            Dictionary<HttpVerb, Action<HttpListenerRequest, HttpListenerResponse>>> _handlerByPrefixAndVerb 
            = new Dictionary<string,Dictionary<HttpVerb,Action<HttpListenerRequest,HttpListenerResponse>>>();

        public Server(string root, int port)
        {
            _listener = new  HttpListener();
            _serverAdress = string.Format("http://{0}:{1}/", root, port);
        }

        private void AddHandler(string prefix, HttpVerb verb, Action<HttpListenerRequest, HttpListenerResponse> act)
        {

            string fulladress = prefix.EndsWith("/") 
                ? _serverAdress + prefix : _serverAdress + prefix + "/";

            _listener.Prefixes.Add(fulladress);

            if (!_handlerByPrefixAndVerb.ContainsKey(prefix))
                _handlerByPrefixAndVerb.Add(prefix, 
                    new Dictionary<HttpVerb, Action<HttpListenerRequest, HttpListenerResponse>>());

            _handlerByPrefixAndVerb[prefix].Add(verb, act);
        }

        public void Start()
        {
            _listener.Start();
            _listener.BeginGetContext(new AsyncCallback(IncommingRequest), _listener);
        }

        public void Get(string prefix, Action<HttpListenerRequest, HttpListenerResponse> act)
        {
              AddHandler(prefix, HttpVerb.Get, act);
        }



        public void Put(string prefix, Action<HttpListenerRequest, HttpListenerResponse> act)
        {
            AddHandler(prefix, HttpVerb.Put, act);
        }

        public void Post(string prefix, Action<HttpListenerRequest, HttpListenerResponse> act)
        {
            AddHandler(prefix, HttpVerb.Post, act);
        }

        public void Delete(string prefix, Action<HttpListenerRequest, HttpListenerResponse> act)
        {
            AddHandler(prefix, HttpVerb.Delete, act);
        }


        private void IncommingRequest(IAsyncResult result)
        {
            HttpListener listener = (HttpListener)result.AsyncState;
            HttpListenerContext context = listener.EndGetContext(result);

            _listener.BeginGetContext(new AsyncCallback(IncommingRequest), _listener);

            
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            
            string url = request.RawUrl.Substring(1, request.RawUrl.Length -1) ;

            if (_handlerByPrefixAndVerb.ContainsKey(url))
            {
                var handlers = _handlerByPrefixAndVerb[url];

                HttpVerb verb = HttpVerb.Get;

                switch (request.HttpMethod)
                {
                    case "GET":
                        verb = HttpVerb.Get;
                        break;
                    case "POST":
                        verb = HttpVerb.Post;
                        break;
                    case "PUT":
                        verb = HttpVerb.Put;
                        break;
                    case "DELETE":
                        verb = HttpVerb.Delete;
                        break;
                }

                if (handlers.ContainsKey(verb))
                {
                    try
                    {
                        handlers[verb](request, response);
                    }
                    catch (Exception ex)
                    {
                        response.StatusCode = 503;
                        response.StatusDescription = "Server Error";

                        if (request.IsLocal)
                        {                            
                                string message = "Message {0} /r/nSource {1}/r/n Stacktrace {2}";

                                var data = string.Format(message, 
                                    ex.Message,ex.Source, ex.StackTrace);
                                response.StatusDescription = data;
                        }                            
                    }

                }
                else
                {
                    response.StatusCode = 405;
                    response.StatusDescription = "No handler for this HTTP method";
                }
            }
            else
            {
                response.StatusCode = 404;
                response.StatusDescription = "Not Found";
            }
            response.Close();
        }

        public void Stop()
        {
            _listener.Stop();
            _listener.Close();
        }
    }
}